

namespace ReLoop_Technologies_Web_App.Data;

public sealed class UserDocument
{
    public string UserId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Role { get; set; } = "Member";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? ProfileImageUrl { get; set; }
    public string PreferredCategory { get; set; } = "Recyclables";
    public string FullName => string.Join(' ', new[] { FirstName, LastName }.Where(name => !string.IsNullOrWhiteSpace(name)));
}

public sealed class AgentDocument
{
    public string AgentId { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ContactNumber { get; set; } = string.Empty;
    public string AssignedZone { get; set; } = string.Empty;
    public bool IsAvailable { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class PickupDocument
{
    public string PickupId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? AgentId { get; set; }
    public DateTime ScheduledDate { get; set; }
    public string WasteType { get; set; } = string.Empty;
    public List<string> WasteTypes { get; set; } = [];
    public string Address { get; set; } = string.Empty;
    public string Status { get; set; } = "Scheduled";
    public string? Notes { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public double? WeightKg { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
}

public sealed class WasteLogDocument
{
    public string WasteLogId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? PickupId { get; set; }
    public string WasteCategoryId { get; set; } = string.Empty;
    public double WeightKg { get; set; }
    public DateTime LoggedAt { get; set; } = DateTime.UtcNow;
    public string Source { get; set; } = "scan";
    public string? ModelLabel { get; set; }
    public double? ConfidenceScore { get; set; }
    public string? ImageUrl { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string Disposition { get; set; } = "Recyclable";
    public int PointsAwarded { get; set; }
}

public sealed class WasteCategoryDocument
{
    public string WasteCategoryId { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ModelLabel { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class GuidanceContentDocument
{
    public string GuidanceId { get; set; } = string.Empty;
    public string WasteCategoryId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Preparation { get; set; }
    public List<string> AcceptedItems { get; set; } = [];
    public List<string> ExcludedItems { get; set; } = [];
    public string? ImagePath { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class RewardAccountDocument
{
    public string UserId { get; set; } = string.Empty;
    public int PointsBalance { get; set; }
    public int TotalEarned { get; set; }
    public int TotalRedeemed { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public sealed class RewardItemDocument
{
    public string RewardItemId { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int PointsCost { get; set; }
    public bool IsActive { get; set; } = true;
    public string? ImageUrl { get; set; }
    public int? StockQuantity { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class RedemptionLogDocument
{
    public string RedemptionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string RewardItemId { get; set; } = string.Empty;
    public int PointsUsed { get; set; }
    public string? VoucherCode { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime? RedeemedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class PointsTransactionDocument
{
    public string TransactionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty;
    public int Points { get; set; }
    public string? RelatedId { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class NotificationDocument
{
    public string NotificationId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "info";
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }
    public string? RelatedPickupId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class ContactMessageDocument
{
    public string ContactId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = "New";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
