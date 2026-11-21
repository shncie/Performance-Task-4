using CareFlow.Data;
using CareFlow.DTOs;
using CareFlow.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CareFlow.Services;

public class BookingService : IBookingService
{
    private readonly IDbContextFactory<CareFlowDbContext> _contextFactory;

    public BookingService(IDbContextFactory<CareFlowDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<Patient>> GetPatientsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Patients
            .AsNoTracking()
            .OrderBy(p => p.FullName)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<ServiceAvailabilityDto>> GetAvailabilityAsync(DateOnly date)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var services = await context.DiagnosticServices
            .AsNoTracking()
            .OrderBy(s => s.Id)
            .ToListAsync();

        // Calculate booked count for each service on the chosen date
        var bookedCounts = await context.BookingItems
            .AsNoTracking()
            .Where(bi => bi.Booking.BookingDate == date)
            .GroupBy(bi => bi.DiagnosticServiceId)
            .Select(g => new { ServiceId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ServiceId, x => x.Count);

        var list = new List<ServiceAvailabilityDto>();
        foreach (var service in services)
        {
            bookedCounts.TryGetValue(service.Id, out int booked);
            int remaining = Math.Max(0, service.DailyCapacity - booked);
            list.Add(new ServiceAvailabilityDto(
                service.Id,
                service.Name,
                service.Description,
                service.DailyCapacity,
                booked,
                remaining
            ));
        }

        return list;
    }

    public async Task<IReadOnlyList<Booking>> GetRecentBookingsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Bookings
            .AsNoTracking()
            .Include(b => b.Patient)
            .Include(b => b.BookingItems)
                .ThenInclude(bi => bi.DiagnosticService)
            .OrderByDescending(b => b.BookingDate)
            .ThenByDescending(b => b.Id)
            .ToListAsync();
    }

    public async Task<BookingResult> CreateBookingAsync(BookingRequestDto request)
    {
        // BR-03: Booking date cannot be earlier than current local date
        var today = DateOnly.FromDateTime(DateTime.Today);
        if (request.BookingDate < today)
        {
            return new BookingResult(false, $"BR-03 Violation: Booking date ({request.BookingDate:yyyy-MM-dd}) cannot be earlier than today ({today:yyyy-MM-dd}).");
        }

        // BR-02: Must contain one to three services
        if (request.ServiceIds == null || request.ServiceIds.Count < 1 || request.ServiceIds.Count > 3)
        {
            return new BookingResult(false, "BR-02 Violation: Each booking must contain 1 to 3 distinct diagnostic services.");
        }

        // BR-06: The same service cannot appear more than once
        if (request.ServiceIds.Distinct().Count() != request.ServiceIds.Count)
        {
            return new BookingResult(false, "BR-06 Violation: Duplicate services cannot appear in a single booking request.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        // BR-01: Each booking must reference an existing patient
        var patientExists = await context.Patients.AnyAsync(p => p.Id == request.PatientId);
        if (!patientExists)
        {
            return new BookingResult(false, $"BR-01 Violation: Patient with ID {request.PatientId} does not exist.");
        }

        // BR-07: All required booking changes must be committed together, or none (Atomicity)
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // BR-08: Recheck live availability inside the transaction boundary
            var requestedServices = await context.DiagnosticServices
                .Where(s => request.ServiceIds.Contains(s.Id))
                .ToListAsync();

            if (requestedServices.Count != request.ServiceIds.Count)
            {
                await transaction.RollbackAsync();
                return new BookingResult(false, "One or more selected services do not exist.");
            }

            var bookedCounts = await context.BookingItems
                .Where(bi => bi.Booking.BookingDate == request.BookingDate && request.ServiceIds.Contains(bi.DiagnosticServiceId))
                .GroupBy(bi => bi.DiagnosticServiceId)
                .Select(g => new { ServiceId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ServiceId, x => x.Count);

            // BR-04 & BR-05: Verify capacity for each selected service
            foreach (var service in requestedServices)
            {
                bookedCounts.TryGetValue(service.Id, out int currentBooked);
                if (currentBooked + 1 > service.DailyCapacity)
                {
                    // If any service is full, abort and roll back the whole transaction!
                    await transaction.RollbackAsync();
                    return new BookingResult(false,
                        $"BR-05 / AC-04 Violation: Service '{service.Name}' has reached maximum daily capacity ({service.DailyCapacity}/{service.DailyCapacity}) for {request.BookingDate:yyyy-MM-dd}. The entire booking is rejected.");
                }
            }

            // Create Booking header
            var booking = new Booking
            {
                PatientId = request.PatientId,
                BookingDate = request.BookingDate,
                CreatedAt = DateTime.UtcNow
            };

            context.Bookings.Add(booking);
            await context.SaveChangesAsync();

            // Create individual BookingItems
            foreach (var serviceId in request.ServiceIds)
            {
                context.BookingItems.Add(new BookingItem
                {
                    BookingId = booking.Id,
                    DiagnosticServiceId = serviceId
                });
            }

            // AC-05 Test Hook: Simulate sudden failure before commit to verify rollback
            if (request.SimulateFailure)
            {
                throw new InvalidOperationException("Controlled database failure simulated for AC-05 verification. Transaction rolling back.");
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Load navigation data for UI display
            await context.Entry(booking).Reference(b => b.Patient).LoadAsync();
            await context.Entry(booking).Collection(b => b.BookingItems).Query()
                .Include(bi => bi.DiagnosticService)
                .LoadAsync();

            return new BookingResult(true, "Booking confirmed successfully.", booking);
        }
        catch (Exception ex)
        {
            // Atomicity Guarantee: Rollback transaction on any error
            await transaction.RollbackAsync();
            return new BookingResult(false, $"Booking failed (transaction rolled back): {ex.Message}");
        }
    }
}