using System.Collections.Generic;

namespace CareFlow.Models;

public class DiagnosticService
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DailyCapacity { get; set; }

    // Navigation property: One service can be in many booking items
    public ICollection<BookingItem> BookingItems { get; set; } = new List<BookingItem>();
}