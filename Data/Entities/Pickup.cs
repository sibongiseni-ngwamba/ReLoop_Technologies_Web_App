namespace ReLoop_Technologies_Web_App.Data.Entities;

public sealed class Pickup
{
    public Guid Id { get; set; }
    public Guid UserAccountId { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public DateTimeOffset ScheduledFor { get; set; }
    public string Address { get; set; } = string.Empty;
    public string WasteType { get; set; } = string.Empty;
    public decimal? EstimatedWeightKg { get; set; }
    public string Status { get; set; } = "Scheduled";
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public UserAccount? UserAccount { get; set; }
}
