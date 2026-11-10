using ReLoop_Technologies_Web_App.Data;
using ReLoop_Technologies_Web_App.Models;

namespace ReLoop_Technologies_Web_App.Services;

public sealed class AdminManagementService(ReLoopStore store, IReLoopDocumentStore documents)
{
    private const int MaxPageSize = 50;

    public async Task<AdminOverviewDto> GetOverviewAsync()
    {
        var users = await store.GetUsersAsync();
        var scans = await store.GetAllScansAsync();
        var pickups = await store.GetAllPickupsAsync();
        var active = users.Count(user => user.IsActive);
        var weight = pickups.Where(item => item.WeightKg.HasValue && item.Status == "Completed").Sum(item => item.WeightKg!.Value);
        var open = pickups.Count(item => item.Status is "Scheduled" or "Assigned");
        var byMaterial = scans.GroupBy(item => item.ModelLabel ?? item.WasteCategoryId).Select(group => new { Type = group.Key, Count = group.Count() }).ToList();
        var total = Math.Max(1, byMaterial.Sum(item => item.Count));
        return new AdminOverviewDto(
            [new("Active members", active.ToString(), "Current active accounts", "success"), new("Items scanned", scans.Count.ToString(), "Saved waste logs", "info"), new("Weight diverted", $"{weight:N1} kg", "Completed collection weight", "warning"), new("Open pickups", open.ToString(), "Scheduled or assigned", "neutral")],
            byMaterial.Select((item, index) => new WasteSliceDto(item.Type, (int)Math.Round(item.Count * 100d / total), new[] { "success", "info", "warning", "danger" }[index % 4])));
    }

    public async Task<PagedResult<AdminUserDto>> GetUsersAsync(string? search, int page, int pageSize)
    {
        var users = await store.GetUsersAsync();
        var pickups = await store.GetAllPickupsAsync();
        var query = users.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(user => user.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) || user.Email.Contains(search, StringComparison.OrdinalIgnoreCase));
        var items = query.OrderByDescending(user => user.CreatedAt).Select(user => new AdminUserDto(user.UserId, user.FullName, user.Email, user.Role, user.IsActive,
            0, pickups.Count(pickup => pickup.UserId == user.UserId), user.CreatedAt.ToString("dd MMM yyyy"), user.Address, user.PreferredCategory)).ToList();
        var rewards = await documents.ListAsync<RewardAccountDocument>(FirestoreCollections.Rewards);
        items = items.Select(item => item with { RewardPoints = rewards.FirstOrDefault(reward => reward.UserId == item.Id)?.PointsBalance ?? 0 }).ToList();
        return Page(items, page, pageSize);
    }

    public async Task<AdminUserDto?> UpdateUserAsync(string uid, AdminUserUpdateRequest request)
    {
        var user = await store.GetUserAsync(uid);
        if (user is null) return null;
        var (firstName, lastName) = SplitName(request.FullName);
        user.FirstName = firstName; user.LastName = lastName; user.Role = request.Role; user.IsActive = request.IsActive;
        user.Address = request.Address; user.UpdatedAt = DateTime.UtcNow;
        await store.SaveUserAsync(user);
        var rewards = await documents.GetAsync<RewardAccountDocument>(FirestoreCollections.Rewards, uid);
        var pickups = await store.GetAllPickupsAsync();
        return new AdminUserDto(user.UserId, user.FullName, user.Email, user.Role, user.IsActive, rewards?.PointsBalance ?? 0,
            pickups.Count(pickup => pickup.UserId == uid), user.CreatedAt.ToString("dd MMM yyyy"), user.Address, user.PreferredCategory);
    }

    public async Task<PagedResult<AdminPickupDto>> GetPickupsAsync(string? status, string? search, int page, int pageSize)
    {
        var pickups = await store.GetAllPickupsAsync();
        var users = (await store.GetUsersAsync()).ToDictionary(item => item.UserId, StringComparer.Ordinal);
        var agents = (await store.GetAgentsAsync()).ToDictionary(item => item.AgentId, StringComparer.Ordinal);
        var query = pickups.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(status) && status != "All") query = query.Where(item => item.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(item => item.ReferenceNumber.Contains(search, StringComparison.OrdinalIgnoreCase) || (users.GetValueOrDefault(item.UserId)?.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
        var items = query.OrderByDescending(item => item.ScheduledDate).Select(item => MapPickup(item, users, agents)).ToList();
        return Page(items, page, pageSize);
    }

    public async Task<AdminPickupDto?> UpdatePickupAsync(string id, AdminPickupUpdateRequest request)
    {
        var pickup = await documents.GetAsync<PickupDocument>(FirestoreCollections.Pickups, id);
        if (pickup is null) return null;
        var agentId = string.IsNullOrWhiteSpace(request.AssignedAgent) ? null : request.AssignedAgent.Trim();
        if (agentId is not null)
        {
            var agent = (await store.GetAgentsAsync()).FirstOrDefault(item => item.IsActive && (item.AgentId == agentId || item.Name.Equals(agentId, StringComparison.OrdinalIgnoreCase)));
            if (agent is null) throw new InvalidOperationException("Choose an active recycling agent from the agents collection.");
            agentId = agent.AgentId;
        }
        pickup.Status = request.Status; pickup.AgentId = agentId; pickup.UpdatedAt = DateTime.UtcNow;
        if (request.Status == "Completed") pickup.CompletedAt = DateTime.UtcNow;
        if (request.Status == "Cancelled") pickup.CancelledAt = DateTime.UtcNow;
        await store.SavePickupAsync(pickup);
        var users = (await store.GetUsersAsync()).ToDictionary(item => item.UserId, StringComparer.Ordinal);
        var agents = (await store.GetAgentsAsync()).ToDictionary(item => item.AgentId, StringComparer.Ordinal);
        return MapPickup(pickup, users, agents);
    }

    public async Task<PagedResult<AdminActivityDto>> GetActivityAsync(int page, int pageSize)
    {
        var logs = await store.GetAllNotificationsAsync();
        var users = (await store.GetUsersAsync()).ToDictionary(item => item.UserId, StringComparer.Ordinal);
        return Page(logs.OrderByDescending(log => log.CreatedAt).Select(log => new AdminActivityDto(users.GetValueOrDefault(log.UserId)?.FullName ?? "Unknown member", log.Title, log.Message, log.CreatedAt.ToString("dd MMM yyyy, HH:mm"), log.Type)).ToList(), page, pageSize);
    }

    public async Task<List<AdminRewardDto>> GetRewardsAsync() => (await store.GetRewardItemsAsync()).OrderBy(item => item.PointsCost)
        .Select(item => new AdminRewardDto(item.RewardItemId, item.ItemName, item.Description, item.PointsCost, item.IsActive)).ToList();

    public async Task<AdminRewardDto> CreateRewardAsync(RewardCatalogueRequest request)
    {
        var item = new RewardItemDocument { RewardItemId = Guid.NewGuid().ToString(), ItemName = request.Name.Trim(), Description = request.Description.Trim(), PointsCost = request.PointsCost, IsActive = request.IsActive, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        await store.SaveRewardItemAsync(item);
        return new AdminRewardDto(item.RewardItemId, item.ItemName, item.Description, item.PointsCost, item.IsActive);
    }

    public async Task<AdminRewardDto?> UpdateRewardAsync(string id, RewardCatalogueRequest request)
    {
        var item = await documents.GetAsync<RewardItemDocument>(FirestoreCollections.RewardItems, id);
        if (item is null) return null;
        item.ItemName = request.Name.Trim(); item.Description = request.Description.Trim(); item.PointsCost = request.PointsCost; item.IsActive = request.IsActive; item.UpdatedAt = DateTime.UtcNow;
        await store.SaveRewardItemAsync(item);
        return new AdminRewardDto(item.RewardItemId, item.ItemName, item.Description, item.PointsCost, item.IsActive);
    }

    public async Task<PagedResult<AdminRedemptionDto>> GetRedemptionsAsync(int page, int pageSize)
    {
        var redemptions = await store.GetRedemptionsAsync();
        var users = (await store.GetUsersAsync()).ToDictionary(item => item.UserId, StringComparer.Ordinal);
        var rewards = (await store.GetRewardItemsAsync()).ToDictionary(item => item.RewardItemId, StringComparer.Ordinal);
        var items = redemptions.OrderByDescending(item => item.CreatedAt).Select(item => new AdminRedemptionDto(item.RedemptionId, users.GetValueOrDefault(item.UserId)?.FullName ?? "Unknown member",
            rewards.GetValueOrDefault(item.RewardItemId)?.ItemName ?? "Reward", item.PointsUsed, item.Status, item.CreatedAt.ToString("dd MMM yyyy"))).ToList();
        return Page(items, page, pageSize);
    }

    public async Task<AdminRedemptionDto?> UpdateRedemptionAsync(string id, RedemptionUpdateRequest request)
    {
        var item = await documents.GetAsync<RedemptionLogDocument>(FirestoreCollections.RedemptionLogs, id);
        if (item is null) return null;
        item.Status = request.Status;
        if (request.Status == "Fulfilled") item.RedeemedAt = DateTime.UtcNow;
        await store.SaveRedemptionAsync(item);
        var user = await store.GetUserAsync(item.UserId);
        var reward = await documents.GetAsync<RewardItemDocument>(FirestoreCollections.RewardItems, item.RewardItemId);
        return new AdminRedemptionDto(item.RedemptionId, user?.FullName ?? "Unknown member", reward?.ItemName ?? "Reward", item.PointsUsed, item.Status, item.CreatedAt.ToString("dd MMM yyyy"));
    }

    public async Task<string> BuildReportAsync()
    {
        var overview = await GetOverviewAsync();
        var users = await store.GetUsersAsync();
        var pickups = await store.GetAllPickupsAsync();
        var activity = await store.GetAllNotificationsAsync();
        var redemptions = await store.GetRedemptionsAsync();
        var rewards = (await store.GetRewardItemsAsync()).ToDictionary(item => item.RewardItemId, StringComparer.Ordinal);
        var lines = new List<string> { "ReLoop system report", $"Generated (UTC),{DateTimeOffset.UtcNow:u}", "", "Summary", "Metric,Value" };
        lines.AddRange(overview.Metrics.Select(metric => $"{Csv(metric.Label)},{Csv(metric.Value)}"));
        lines.AddRange(["", "Members", "Name,Email,Role,Active"]);
        lines.AddRange(users.Select(user => $"{Csv(user.FullName)},{Csv(user.Email)},{Csv(user.Role)},{user.IsActive}"));
        lines.AddRange(["", "Pickups", "Reference,Member,Agent,Date,Material,Status,Address"]);
        var names = users.ToDictionary(user => user.UserId, user => user.FullName, StringComparer.Ordinal);
        lines.AddRange(pickups.Select(item => $"{Csv(item.ReferenceNumber)},{Csv(names.GetValueOrDefault(item.UserId))},{Csv(item.AgentId)},{Csv(item.ScheduledDate.ToString("O"))},{Csv(item.WasteType)},{Csv(item.Status)},{Csv(item.Address)}"));
        lines.AddRange(["", "Activity", "Member,Title,Message,Date"]);
        lines.AddRange(activity.Select(item => $"{Csv(names.GetValueOrDefault(item.UserId))},{Csv(item.Title)},{Csv(item.Message)},{Csv(item.CreatedAt.ToString("O"))}"));
        lines.AddRange(["", "Reward redemptions", "Member,Reward,Points,Status,Date"]);
        lines.AddRange(redemptions.Select(item => $"{Csv(names.GetValueOrDefault(item.UserId))},{Csv(rewards.GetValueOrDefault(item.RewardItemId)?.ItemName)},{item.PointsUsed},{Csv(item.Status)},{Csv(item.CreatedAt.ToString("O"))}"));
        return string.Join(Environment.NewLine, lines);
    }

    private static AdminPickupDto MapPickup(PickupDocument item, Dictionary<string, UserDocument> users, Dictionary<string, AgentDocument> agents) =>
        new(item.PickupId, item.ReferenceNumber, users.GetValueOrDefault(item.UserId)?.FullName ?? "Unknown member", item.ScheduledDate.ToString("dd MMM yyyy, HH:mm"), item.WasteType, item.Status,
            item.AgentId is not null && agents.TryGetValue(item.AgentId, out var agent) ? agent.Name : null, item.WeightKg is null ? "Pending" : $"{item.WeightKg:N1} kg");

    private static (string FirstName, string LastName) SplitName(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length == 0 ? ("ReLoop", "Member") : (parts[0], string.Join(' ', parts.Skip(1)));
    }

    private static string Csv(string? value) => $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";
    private static PagedResult<T> Page<T>(IReadOnlyList<T> items, int page, int pageSize)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        return new(items.Skip((page - 1) * pageSize).Take(pageSize).ToArray(), page, pageSize, items.Count);
    }
}
