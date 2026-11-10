using Google.Cloud.Firestore;

namespace ReLoop_Technologies_Web_App.Data;

[FirestoreData]
public sealed class UserDocument
{
    [FirestoreProperty("userId")] public string UserId { get; set; } = string.Empty;
    [FirestoreProperty("firstName")] public string FirstName { get; set; } = string.Empty;
    [FirestoreProperty("lastName")] public string LastName { get; set; } = string.Empty;
    [FirestoreProperty("email")] public string Email { get; set; } = string.Empty;
    [FirestoreProperty("phoneNumber")] public string PhoneNumber { get; set; } = string.Empty;
    [FirestoreProperty("address")] public string Address { get; set; } = string.Empty;
    [FirestoreProperty("role")] public string Role { get; set; } = "Member";
    [FirestoreProperty("isActive")] public bool IsActive { get; set; } = true;
    [FirestoreProperty("createdAt")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [FirestoreProperty("updatedAt")] public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    [FirestoreProperty("profileImageUrl")] public string? ProfileImageUrl { get; set; }
    [FirestoreProperty("preferredCategory")] public string PreferredCategory { get; set; } = "Recyclables";
    public string FullName => string.Join(' ', new[] { FirstName, LastName }.Where(name => !string.IsNullOrWhiteSpace(name)));
}

[FirestoreData]
public sealed class AgentDocument
{
    [FirestoreProperty("agentId")] public string AgentId { get; set; } = string.Empty;
    [FirestoreProperty("userId")] public string? UserId { get; set; }
    [FirestoreProperty("name")] public string Name { get; set; } = string.Empty;
    [FirestoreProperty("contactNumber")] public string ContactNumber { get; set; } = string.Empty;
    [FirestoreProperty("assignedZone")] public string AssignedZone { get; set; } = string.Empty;
    [FirestoreProperty("isAvailable")] public bool IsAvailable { get; set; } = true;
    [FirestoreProperty("isActive")] public bool IsActive { get; set; } = true;
    [FirestoreProperty("createdAt")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

[FirestoreData]
public sealed class PickupDocument
{
    [FirestoreProperty("pickupId")] public string PickupId { get; set; } = string.Empty;
    [FirestoreProperty("userId")] public string UserId { get; set; } = string.Empty;
    [FirestoreProperty("agentId")] public string? AgentId { get; set; }
    [FirestoreProperty("scheduledDate")] public DateTime ScheduledDate { get; set; }
    [FirestoreProperty("wasteType")] public string WasteType { get; set; } = string.Empty;
    [FirestoreProperty("wasteTypes")] public List<string> WasteTypes { get; set; } = [];
    [FirestoreProperty("address")] public string Address { get; set; } = string.Empty;
    [FirestoreProperty("status")] public string Status { get; set; } = "Scheduled";
    [FirestoreProperty("notes")] public string? Notes { get; set; }
    [FirestoreProperty("referenceNumber")] public string ReferenceNumber { get; set; } = string.Empty;
    [FirestoreProperty("weightKg")] public double? WeightKg { get; set; }
    [FirestoreProperty("createdAt")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [FirestoreProperty("updatedAt")] public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    [FirestoreProperty("completedAt")] public DateTime? CompletedAt { get; set; }
    [FirestoreProperty("cancelledAt")] public DateTime? CancelledAt { get; set; }
    [FirestoreProperty("cancellationReason")] public string? CancellationReason { get; set; }
}

[FirestoreData]
public sealed class WasteLogDocument
{
    [FirestoreProperty("wasteLogId")] public string WasteLogId { get; set; } = string.Empty;
    [FirestoreProperty("userId")] public string UserId { get; set; } = string.Empty;
    [FirestoreProperty("pickupId")] public string? PickupId { get; set; }
    [FirestoreProperty("wasteCategoryId")] public string WasteCategoryId { get; set; } = string.Empty;
    [FirestoreProperty("weightKg")] public double WeightKg { get; set; }
    [FirestoreProperty("loggedAt")] public DateTime LoggedAt { get; set; } = DateTime.UtcNow;
    [FirestoreProperty("source")] public string Source { get; set; } = "scan";
    [FirestoreProperty("modelLabel")] public string? ModelLabel { get; set; }
    [FirestoreProperty("confidenceScore")] public double? ConfidenceScore { get; set; }
    [FirestoreProperty("imageUrl")] public string? ImageUrl { get; set; }
    [FirestoreProperty("itemName")] public string ItemName { get; set; } = string.Empty;
    [FirestoreProperty("disposition")] public string Disposition { get; set; } = "Recyclable";
    [FirestoreProperty("pointsAwarded")] public int PointsAwarded { get; set; }
}

[FirestoreData]
public sealed class WasteCategoryDocument
{
    [FirestoreProperty("wasteCategoryId")] public string WasteCategoryId { get; set; } = string.Empty;
    [FirestoreProperty("categoryName")] public string CategoryName { get; set; } = string.Empty;
    [FirestoreProperty("description")] public string Description { get; set; } = string.Empty;
    [FirestoreProperty("modelLabel")] public string ModelLabel { get; set; } = string.Empty;
    [FirestoreProperty("isActive")] public bool IsActive { get; set; } = true;
    [FirestoreProperty("createdAt")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

[FirestoreData]
public sealed class GuidanceContentDocument
{
    [FirestoreProperty("guidanceId")] public string GuidanceId { get; set; } = string.Empty;
    [FirestoreProperty("wasteCategoryId")] public string WasteCategoryId { get; set; } = string.Empty;
    [FirestoreProperty("title")] public string Title { get; set; } = string.Empty;
    [FirestoreProperty("content")] public string Content { get; set; } = string.Empty;
    [FirestoreProperty("preparation")] public string? Preparation { get; set; }
    [FirestoreProperty("acceptedItems")] public List<string> AcceptedItems { get; set; } = [];
    [FirestoreProperty("excludedItems")] public List<string> ExcludedItems { get; set; } = [];
    [FirestoreProperty("imagePath")] public string? ImagePath { get; set; }
    [FirestoreProperty("isActive")] public bool IsActive { get; set; } = true;
    [FirestoreProperty("createdAt")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [FirestoreProperty("updatedAt")] public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

[FirestoreData]
public sealed class RewardAccountDocument
{
    [FirestoreProperty("userId")] public string UserId { get; set; } = string.Empty;
    [FirestoreProperty("pointsBalance")] public int PointsBalance { get; set; }
    [FirestoreProperty("totalEarned")] public int TotalEarned { get; set; }
    [FirestoreProperty("totalRedeemed")] public int TotalRedeemed { get; set; }
    [FirestoreProperty("lastUpdated")] public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

[FirestoreData]
public sealed class RewardItemDocument
{
    [FirestoreProperty("rewardItemId")] public string RewardItemId { get; set; } = string.Empty;
    [FirestoreProperty("itemName")] public string ItemName { get; set; } = string.Empty;
    [FirestoreProperty("description")] public string Description { get; set; } = string.Empty;
    [FirestoreProperty("pointsCost")] public int PointsCost { get; set; }
    [FirestoreProperty("isActive")] public bool IsActive { get; set; } = true;
    [FirestoreProperty("imageUrl")] public string? ImageUrl { get; set; }
    [FirestoreProperty("stockQuantity")] public int? StockQuantity { get; set; }
    [FirestoreProperty("createdAt")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [FirestoreProperty("updatedAt")] public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

[FirestoreData]
public sealed class RedemptionLogDocument
{
    [FirestoreProperty("redemptionId")] public string RedemptionId { get; set; } = string.Empty;
    [FirestoreProperty("userId")] public string UserId { get; set; } = string.Empty;
    [FirestoreProperty("rewardItemId")] public string RewardItemId { get; set; } = string.Empty;
    [FirestoreProperty("pointsUsed")] public int PointsUsed { get; set; }
    [FirestoreProperty("voucherCode")] public string? VoucherCode { get; set; }
    [FirestoreProperty("status")] public string Status { get; set; } = "Pending";
    [FirestoreProperty("redeemedAt")] public DateTime? RedeemedAt { get; set; }
    [FirestoreProperty("createdAt")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

[FirestoreData]
public sealed class PointsTransactionDocument
{
    [FirestoreProperty("transactionId")] public string TransactionId { get; set; } = string.Empty;
    [FirestoreProperty("userId")] public string UserId { get; set; } = string.Empty;
    [FirestoreProperty("transactionType")] public string TransactionType { get; set; } = string.Empty;
    [FirestoreProperty("points")] public int Points { get; set; }
    [FirestoreProperty("relatedId")] public string? RelatedId { get; set; }
    [FirestoreProperty("description")] public string Description { get; set; } = string.Empty;
    [FirestoreProperty("createdAt")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

[FirestoreData]
public sealed class NotificationDocument
{
    [FirestoreProperty("notificationId")] public string NotificationId { get; set; } = string.Empty;
    [FirestoreProperty("userId")] public string UserId { get; set; } = string.Empty;
    [FirestoreProperty("message")] public string Message { get; set; } = string.Empty;
    [FirestoreProperty("type")] public string Type { get; set; } = "info";
    [FirestoreProperty("sentAt")] public DateTime SentAt { get; set; } = DateTime.UtcNow;
    [FirestoreProperty("isRead")] public bool IsRead { get; set; }
    [FirestoreProperty("relatedPickupId")] public string? RelatedPickupId { get; set; }
    [FirestoreProperty("title")] public string Title { get; set; } = string.Empty;
    [FirestoreProperty("createdAt")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

[FirestoreData]
public sealed class ContactMessageDocument
{
    [FirestoreProperty("contactId")] public string ContactId { get; set; } = string.Empty;
    [FirestoreProperty("fullName")] public string FullName { get; set; } = string.Empty;
    [FirestoreProperty("email")] public string Email { get; set; } = string.Empty;
    [FirestoreProperty("subject")] public string Subject { get; set; } = string.Empty;
    [FirestoreProperty("message")] public string Message { get; set; } = string.Empty;
    [FirestoreProperty("status")] public string Status { get; set; } = "New";
    [FirestoreProperty("createdAt")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
