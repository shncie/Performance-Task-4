using CareFlow.DTOs;
using CareFlow.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CareFlow.Services;

public interface IBookingService
{
    Task<IReadOnlyList<Patient>> GetPatientsAsync();
    Task<IReadOnlyList<ServiceAvailabilityDto>> GetAvailabilityAsync(DateOnly date);
    Task<BookingResult> CreateBookingAsync(BookingRequestDto request);
    Task<IReadOnlyList<Booking>> GetRecentBookingsAsync();
}