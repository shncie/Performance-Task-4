using System.Collections.Generic;

namespace CareFlow.Models;

public class Patient
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string DateOfBirth { get; set; } = string.Empty;
    public string ContactNumber { get; set; } = string.Empty;

    // Navigation property: One patient can have many bookings
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}