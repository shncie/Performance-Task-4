using System;
using System.Collections.Generic;

namespace CareFlow.Models;

public class Booking
{
    public int Id { get; set; }

    // Foreign key to Patient
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    // Date of booking (BR-03)
    public DateOnly BookingDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property: 1 to 3 service items (BR-02)
    public ICollection<BookingItem> BookingItems { get; set; } = new List<BookingItem>();
}