using CareFlow.Models;
using System;
using System.Collections.Generic;

namespace CareFlow.DTOs;

// Represents real-time service capacity for a chosen date
public record ServiceAvailabilityDto(
    int ServiceId,
    string ServiceName,
    string Description,
    int DailyCapacity,
    int BookedCount,
    int RemainingCapacity
)
{
    public bool IsAvailable => RemainingCapacity > 0;
}

// Booking request payload from the UI
public record BookingRequestDto(
    int PatientId,
    DateOnly BookingDate,
    IReadOnlyList<int> ServiceIds,
    bool SimulateFailure = false // Helper flag for AC-05 verification testing
);

// Standard result returned from the service to the ViewModel
public record BookingResult(
    bool Success,
    string Message,
    Booking? Booking = null
);