using ReLoop_Technologies_Web_App.Data;
using ReLoop_Technologies_Web_App.Models;

namespace ReLoop_Technologies_Web_App.Services;

public sealed class ReLoopStore(IReLoopDocumentStore documents)
{
    public async Task<UserDocument> EnsureUserAsync(FirebaseIdentity identity)
    {
        var user = await documents.GetAsync<UserDocument>(FirestoreCollections.Users, identity.Uid);
        if (user is not null) return user;

        var (firstName, lastName) = SplitName(identity.DisplayName);
        user = new UserDocument
        {
            UserId = identity.Uid,
            FirstName = firstName,
            LastName = lastName,
            Email = identity.Email.Trim().ToLowerInvariant(),
            Role = "Member",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await documents.SaveAsync(FirestoreCollections.Users, user.UserId, user);
        await documents.SaveAsync(FirestoreCollections.Rewards, user.UserId, new RewardAccountDocument { UserId = user.UserId });
        return user;
    }

    public Task<UserDocument?> GetUserByEmailAsync(string? email) => string.IsNullOrWhiteSpace(email)
        ? Task.FromResult<UserDocument?>(null)
        : FindUserByEmailAsync(email.Trim());

    public async Task<bool> IsSessionValidAsync(string? uid, string? role)
    {
        if (string.IsNullOrWhiteSpace(uid)) return false;
        var user = await documents.GetAsync<UserDocument>(FirestoreCollections.Users, uid);
        return user is { IsActive: true } && string.Equals(user.Role, role, StringComparison.Ordinal);
    }

    public async Task<ProfileDto> GetProfileAsync(string? uid)
    {
        var user = await RequireUserAsync(uid);
        var rewards = await documents.GetAsync<RewardAccountDocument>(FirestoreCollections.Rewards, user.UserId);
        var scans = await documents.ListAsync<WasteLogDocument>(FirestoreCollections.WasteLogs);
        var pickups = await documents.ListAsync<PickupDocument>(FirestoreCollections.Pickups);
        return new ProfileDto(user.FullName, user.Email, user.Role, user.Address, user.PreferredCategory, rewards?.PointsBalance ?? 0,
            scans.Count(scan => scan.UserId == user.UserId), pickups.Count(pickup => pickup.UserId == user.UserId));
    }

    public async Task<ProfileDto> UpdateProfileAsync(string? uid, ProfileUpdateRequest request)
    {
        var user = await RequireUserAsync(uid);
        var email = request.Email.Trim().ToLowerInvariant();
        var duplicate = (await documents.ListAsync<UserDocument>(FirestoreCollections.Users)).Any(candidate => candidate.UserId != user.UserId && candidate.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        if (duplicate) throw new InvalidOperationException("An account already exists for this email address.");
        var (firstName, lastName) = SplitName(request.FullName.Trim());
        user.FirstName = firstName;
        user.LastName = lastName;
        user.Email = email;
        user.Address = request.Address.Trim();
        user.PreferredCategory = request.PreferredCategory.Trim();
        user.UpdatedAt = DateTime.UtcNow;
        await documents.SaveAsync(FirestoreCollections.Users, user.UserId, user);
        await AddNotificationAsync(user.UserId, "Profile updated", "Account and recycling preferences were refreshed", "info");
        return await GetProfileAsync(user.UserId);
    }

    public async Task<ContactMessageDocument> CreateContactMessageAsync(ContactRequest request)
    {
        var message = new ContactMessageDocument
        {
            ContactId = Guid.NewGuid().ToString(), FullName = request.FullName.Trim(), Email = request.Email.Trim().ToLowerInvariant(),
            Subject = request.Subject.Trim(), Message = request.Message.Trim(), Status = "New", CreatedAt = DateTime.UtcNow
        };
        await documents.SaveAsync(FirestoreCollections.ContactMessages, message.ContactId, message);
        return message;
    }

    public async Task<DashboardDto> GetDashboardAsync(string? uid)
    {
        var user = await RequireUserAsync(uid);
        var pickups = (await documents.ListAsync<PickupDocument>(FirestoreCollections.Pickups)).Where(item => item.UserId == user.UserId).OrderByDescending(item => item.ScheduledDate).ToList();
        var scans = (await documents.ListAsync<WasteLogDocument>(FirestoreCollections.WasteLogs)).Where(item => item.UserId == user.UserId).ToList();
        var notifications = (await documents.ListAsync<NotificationDocument>(FirestoreCollections.Notifications)).Where(item => item.UserId == user.UserId).OrderByDescending(item => item.CreatedAt).Take(6)
            .Select(item => new ActivityDto(item.Title, item.Message, FormatDate(item.CreatedAt), item.Type)).ToList();
        var rewards = await documents.GetAsync<RewardAccountDocument>(FirestoreCollections.Rewards, user.UserId);
        var completedWeight = pickups.Where(item => item.WeightKg.HasValue && item.Status == "Completed").Sum(item => item.WeightKg!.Value);

        return new DashboardDto(user.FullName,
        [
            new("Reward Points", $"{rewards?.PointsBalance ?? 0:N0} pts", $"{rewards?.TotalEarned ?? 0:N0} earned", "success"),
            new("Items Scanned", scans.Count.ToString("N0"), "Saved recycling logs", "info"),
            new("Weight Diverted", $"{completedWeight:N1} kg", "Completed collections", "warning"),
            new("Scheduled Pickups", pickups.Count(item => item.Status is "Scheduled" or "Assigned").ToString("N0"), "Current requests", "neutral")
        ], notifications, pickups.Take(3).Select(ToPickupDto));
    }

    public async Task<IReadOnlyList<PickupDto>> GetPickupsAsync(string? uid, string? status = null)
    {
        var user = await RequireUserAsync(uid);
        var pickups = (await documents.ListAsync<PickupDocument>(FirestoreCollections.Pickups)).Where(item => item.UserId == user.UserId);
        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("All", StringComparison.OrdinalIgnoreCase)) pickups = pickups.Where(item => item.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
        return pickups.OrderByDescending(item => item.ScheduledDate).Select(ToPickupDto).ToArray();
    }

    public async Task<PickupDto> CreatePickupAsync(string? uid, CreatePickupRequest request)
    {
        var user = await RequireUserAsync(uid);
        var id = Guid.NewGuid().ToString();
        var pickup = new PickupDocument
        {
            PickupId = id, UserId = user.UserId, ReferenceNumber = $"#LP-{Random.Shared.Next(1000, 9999)}",
            ScheduledDate = BuildPickupDate(request.PreferredDate, request.PreferredTimeWindow), WasteType = request.WasteCategory,
            WasteTypes = [request.WasteCategory], Address = request.HomeAddress.Trim(), Notes = request.Notes?.Trim(),
            Status = "Scheduled", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        var notification = NewNotification(user.UserId, "Pickup request submitted", $"{request.WasteCategory} collection created for {request.PreferredDate:dd MMM yyyy}", "info", id);
        await documents.RunTransactionAsync<object?>(transaction =>
        {
            transaction.Set(FirestoreCollections.Pickups, id, pickup);
            transaction.Set(FirestoreCollections.Notifications, notification.NotificationId, notification);
            return Task.FromResult<object?>(null);
        });
        return ToPickupDto(pickup);
    }

    public async Task<ScanResultDto> ClassifyScanAsync(string? uid, string fileName)
    {
        var user = await RequireUserAsync(uid);
        var lower = fileName.ToLowerInvariant();
        var categoryName = lower.Contains("glass") ? "Glass" : lower.Contains("paper") ? "Paper" : lower.Contains("metal") ? "Metal" : "Plastic";
        var category = (await documents.ListAsync<WasteCategoryDocument>(FirestoreCollections.WasteCategories)).FirstOrDefault(item => item.CategoryName.Equals(categoryName, StringComparison.OrdinalIgnoreCase));
        if (category is null || !category.IsActive) throw new InvalidOperationException($"The {categoryName} waste category is not configured in Firestore.");
        var item = categoryName switch { "Paper" => "Cardboard Sheet", "Glass" => "Glass Jar", "Metal" => "Aluminium Can", _ => "Plastic Bottle" };
        var points = categoryName switch { "Glass" => 14, "Paper" => 8, "Metal" => 12, _ => 10 };
        var scan = new WasteLogDocument
        {
            WasteLogId = Guid.NewGuid().ToString(), UserId = user.UserId, WasteCategoryId = category.WasteCategoryId,
            WeightKg = 0.2, LoggedAt = DateTime.UtcNow, Source = "scan", ModelLabel = category.ModelLabel,
            ConfidenceScore = 0.94, ItemName = item, Disposition = "Recyclable", PointsAwarded = points
        };
        var tx = new PointsTransactionDocument
        {
            TransactionId = Guid.NewGuid().ToString(), UserId = user.UserId, TransactionType = "earn", Points = points,
            RelatedId = scan.WasteLogId, Description = $"{item} scan verified", CreatedAt = DateTime.UtcNow
        };
        var notification = NewNotification(user.UserId, $"{item} scan completed", $"{category.CategoryName} verified with high confidence", "success");
        await documents.RunTransactionAsync<object?>(async transaction =>
        {
            var balance = await transaction.GetAsync<RewardAccountDocument>(FirestoreCollections.Rewards, user.UserId) ?? new RewardAccountDocument { UserId = user.UserId };
            balance.PointsBalance += points;
            balance.TotalEarned += points;
            balance.LastUpdated = DateTime.UtcNow;
            transaction.Set(FirestoreCollections.Rewards, user.UserId, balance);
            transaction.Set(FirestoreCollections.WasteLogs, scan.WasteLogId, scan);
            transaction.Set(FirestoreCollections.PointsTransactions, tx.TransactionId, tx);
            transaction.Set(FirestoreCollections.Notifications, notification.NotificationId, notification);
            return null;
        });
        return new ScanResultDto(item, scan.Disposition, category.CategoryName, "0.2 kg", points, 94, DateTimeOffset.UtcNow);
    }

    public async Task<AdminStatsDto> GetAdminStatsAsync()
    {
        var users = await documents.ListAsync<UserDocument>(FirestoreCollections.Users);
        var scans = await documents.ListAsync<WasteLogDocument>(FirestoreCollections.WasteLogs);
        var pickups = await documents.ListAsync<PickupDocument>(FirestoreCollections.Pickups);
        var transactions = await documents.ListAsync<PointsTransactionDocument>(FirestoreCollections.PointsTransactions);
        var activeUsers = users.Count(user => user.IsActive);
        var weight = pickups.Where(item => item.WeightKg.HasValue && item.Status == "Completed").Sum(item => item.WeightKg!.Value);
        var types = scans.GroupBy(scan => scan.ModelLabel ?? scan.WasteCategoryId).Select(group => new { Type = group.Key, Count = group.Count() }).ToList();
        var total = Math.Max(1, types.Sum(item => item.Count));
        return new AdminStatsDto(
            [new("Total Active Users", activeUsers.ToString("N0"), "Current active accounts", "success"), new("Items Scanned", scans.Count.ToString("N0"), "Persisted waste logs", "info"), new("Total Weight Diverted", $"{weight:N1} kg", "Completed collection weight", "warning"), new("Reward Points Logged", transactions.Sum(item => item.Points).ToString("N0"), "Points transactions", "danger")],
            types.Select((item, index) => new WasteSliceDto(item.Type, (int)Math.Round(item.Count * 100d / total), new[] { "success", "info", "warning", "danger" }[index % 4])),
            pickups.OrderByDescending(item => item.CreatedAt).Take(8).Select(ToPickupDto));
    }

    public async Task<IReadOnlyList<WasteGuidanceDto>> GetWasteGuidanceAsync(string? categoryName = null)
    {
        var categories = await documents.ListAsync<WasteCategoryDocument>(FirestoreCollections.WasteCategories);
        var categoryById = categories.ToDictionary(item => item.WasteCategoryId, StringComparer.OrdinalIgnoreCase);
        var guidance = await documents.ListAsync<GuidanceContentDocument>(FirestoreCollections.GuidanceContent);
        var activeGuidance = guidance.Where(item => item.IsActive && categoryById.ContainsKey(item.WasteCategoryId));
        if (!string.IsNullOrWhiteSpace(categoryName))
            activeGuidance = activeGuidance.Where(item => categoryById[item.WasteCategoryId].CategoryName.Equals(categoryName, StringComparison.OrdinalIgnoreCase));
        return activeGuidance.Select(item => new WasteGuidanceDto(
            categoryById[item.WasteCategoryId].CategoryName, item.Preparation ?? item.Content,
            string.Join(", ", item.AcceptedItems), string.Join(", ", item.ExcludedItems))).ToArray();
    }

    public async Task<UserDocument?> GetUserAsync(string uid) => await documents.GetAsync<UserDocument>(FirestoreCollections.Users, uid);
    public Task SaveUserAsync(UserDocument user) => documents.SaveAsync(FirestoreCollections.Users, user.UserId, user);
    public Task<IReadOnlyList<UserDocument>> GetUsersAsync() => documents.ListAsync<UserDocument>(FirestoreCollections.Users);
    public Task<IReadOnlyList<PickupDocument>> GetAllPickupsAsync() => documents.ListAsync<PickupDocument>(FirestoreCollections.Pickups);
    public Task<IReadOnlyList<WasteLogDocument>> GetAllScansAsync() => documents.ListAsync<WasteLogDocument>(FirestoreCollections.WasteLogs);
    public Task<IReadOnlyList<NotificationDocument>> GetAllNotificationsAsync() => documents.ListAsync<NotificationDocument>(FirestoreCollections.Notifications);
    public Task<IReadOnlyList<AgentDocument>> GetAgentsAsync() => documents.ListAsync<AgentDocument>(FirestoreCollections.Agents);
    public Task<IReadOnlyList<RewardItemDocument>> GetRewardItemsAsync() => documents.ListAsync<RewardItemDocument>(FirestoreCollections.RewardItems);
    public Task<IReadOnlyList<RedemptionLogDocument>> GetRedemptionsAsync() => documents.ListAsync<RedemptionLogDocument>(FirestoreCollections.RedemptionLogs);
    public Task<IReadOnlyList<WasteCategoryDocument>> GetWasteCategoriesAsync() => documents.ListAsync<WasteCategoryDocument>(FirestoreCollections.WasteCategories);
    public Task<IReadOnlyList<GuidanceContentDocument>> GetGuidanceAsync() => documents.ListAsync<GuidanceContentDocument>(FirestoreCollections.GuidanceContent);
    public Task SavePickupAsync(PickupDocument pickup) => documents.SaveAsync(FirestoreCollections.Pickups, pickup.PickupId, pickup);
    public Task SaveRewardItemAsync(RewardItemDocument reward) => documents.SaveAsync(FirestoreCollections.RewardItems, reward.RewardItemId, reward);
    public Task SaveRedemptionAsync(RedemptionLogDocument redemption) => documents.SaveAsync(FirestoreCollections.RedemptionLogs, redemption.RedemptionId, redemption);
    public Task SaveNotificationAsync(NotificationDocument notification) => documents.SaveAsync(FirestoreCollections.Notifications, notification.NotificationId, notification);
    public Task SavePointsTransactionAsync(PointsTransactionDocument transaction) => documents.SaveAsync(FirestoreCollections.PointsTransactions, transaction.TransactionId, transaction);

    private async Task<UserDocument?> FindUserByEmailAsync(string email) =>
        (await documents.ListAsync<UserDocument>(FirestoreCollections.Users)).FirstOrDefault(user => user.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

    private async Task<UserDocument> RequireUserAsync(string? uid) =>
        (!string.IsNullOrWhiteSpace(uid) ? await documents.GetAsync<UserDocument>(FirestoreCollections.Users, uid) : null)
        ?? throw new InvalidOperationException("The signed-in account could not be found.");

    private async Task AddNotificationAsync(string uid, string title, string message, string type, string? pickupId = null)
    {
        var notification = NewNotification(uid, title, message, type, pickupId);
        await documents.SaveAsync(FirestoreCollections.Notifications, notification.NotificationId, notification);
    }

    private static NotificationDocument NewNotification(string uid, string title, string message, string type, string? pickupId = null) => new()
    {
        NotificationId = Guid.NewGuid().ToString(), UserId = uid, Title = title, Message = message, Type = type,
        RelatedPickupId = pickupId, SentAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow
    };

    private static (string FirstName, string LastName) SplitName(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length == 0 ? ("ReLoop", "Member") : (parts[0], string.Join(' ', parts.Skip(1)));
    }

    private static PickupDto ToPickupDto(PickupDocument pickup) => new(pickup.ReferenceNumber, pickup.ScheduledDate.ToString("dd MMM yyyy, hh:mm tt"), pickup.Address, pickup.WasteType, pickup.WeightKg.HasValue ? $"{pickup.WeightKg:0.0} kg" : "Pending", pickup.Status);
    private static string FormatDate(DateTime date) => date.Date == DateTime.UtcNow.Date ? "Today" : date.ToString("dd MMM yyyy");

    private static DateTime BuildPickupDate(DateTime date, string timeWindow)
    {
        var timeText = timeWindow.Split('-')[0].Trim();
        return DateTime.TryParse($"{date:yyyy-MM-dd} {timeText}", out var scheduled) ? DateTime.SpecifyKind(scheduled, DateTimeKind.Utc) : DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
    }
}


public static class FirestoreCollections
{
    public const string Users = "users";
    public const string Agents = "agents";
    public const string Pickups = "pickups";
    public const string WasteLogs = "wasteLogs";
    public const string WasteCategories = "wasteCategories";
    public const string GuidanceContent = "guidanceContent";
    public const string Rewards = "rewards";
    public const string RewardItems = "rewardItems";
    public const string RedemptionLogs = "redemptionLogs";
    public const string PointsTransactions = "pointsTransactions";
    public const string Notifications = "notifications";
    public const string ContactMessages = "contactMessages";
}
