namespace CareFlow.Models;

public class BookingItem
{
    public int Id { get; set; }

    // Foreign key to Booking
    public int BookingId { get; set; }
    public Booking Booking { get; set; } = null!;

    // Foreign key to DiagnosticService
    public int DiagnosticServiceId { get; set; }
    public DiagnosticService DiagnosticService { get; set; } = null!;
}