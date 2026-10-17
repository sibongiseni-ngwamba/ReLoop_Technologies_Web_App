using Microsoft.EntityFrameworkCore;
using ReLoop_Technologies_Web_App.Data;
using ReLoop_Technologies_Web_App.Data.Entities;
using ReLoop_Technologies_Web_App.Models;

namespace ReLoop_Technologies_Web_App.Services;

public sealed class AdminManagementService(ReLoopDbContext db)
{
    private const int MaxPageSize = 50;

    public async Task<AdminOverviewDto> GetOverviewAsync()
    {
        var users = await db.Users.CountAsync(user => user.IsActive);
        var scans = await db.ScanRecords.CountAsync();
        var weight = await db.Pickups.Where(pickup => pickup.EstimatedWeightKg != null).SumAsync(pickup => pickup.EstimatedWeightKg ?? 0);
        var open = await db.Pickups.CountAsync(pickup => pickup.Status == "Scheduled" || pickup.Status == "Assigned");
        var byMaterial = await db.Pickups.GroupBy(pickup => pickup.WasteType).Select(group => new { Type = group.Key, Count = group.Count() }).ToListAsync();
        var total = Math.Max(1, byMaterial.Sum(item => item.Count));
        return new AdminOverviewDto(
        [new("Active members", users.ToString(), "Current active accounts", "success"), new("Items scanned", scans.ToString(), "Verified and pending scans", "info"), new("Weight diverted", $"{weight:N1} kg", "Completed collection weight", "warning"), new("Open pickups", open.ToString(), "Scheduled or assigned", "neutral")],
        byMaterial.Select((item, index) => new WasteSliceDto(item.Type, (int)Math.Round(item.Count * 100d / total), new[] { "success", "info", "warning", "danger" }[index % 4])));
    }

    public async Task<PagedResult<AdminUserDto>> GetUsersAsync(string? search, int page, int pageSize)
    {
        var query = db.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(user => user.FullName.Contains(search) || user.Email.Contains(search));
        return await PageAsync(query.OrderByDescending(user => user.CreatedAt).Select(user => new AdminUserDto(user.Id, user.FullName, user.Email, user.Role, user.IsActive, user.RewardPoints, user.Pickups.Count, user.CreatedAt.ToString("dd MMM yyyy"), user.Address, user.PreferredCategory)), page, pageSize);
    }

    public async Task<AdminUserDto?> UpdateUserAsync(Guid id, AdminUserUpdateRequest request)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return null;
        user.FullName = request.FullName; user.Role = request.Role; user.IsActive = request.IsActive; user.Address = request.Address; user.PreferredCategory = request.PreferredCategory;
        await db.SaveChangesAsync();
        return new AdminUserDto(user.Id, user.FullName, user.Email, user.Role, user.IsActive, user.RewardPoints, await db.Pickups.CountAsync(pickup => pickup.UserAccountId == user.Id), user.CreatedAt.ToString("dd MMM yyyy"), user.Address, user.PreferredCategory);
    }

    public async Task<PagedResult<AdminPickupDto>> GetPickupsAsync(string? status, string? search, int page, int pageSize)
    {
        var query = db.Pickups.Include(pickup => pickup.UserAccount).AsQueryable();
        if (!string.IsNullOrWhiteSpace(status) && status != "All") query = query.Where(pickup => pickup.Status == status);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(pickup => pickup.ReferenceNumber.Contains(search) || pickup.UserAccount!.FullName.Contains(search));
        return await PageAsync(query.OrderByDescending(pickup => pickup.ScheduledFor).Select(pickup => new AdminPickupDto(pickup.Id, pickup.ReferenceNumber, pickup.UserAccount!.FullName, pickup.ScheduledFor.ToString("dd MMM yyyy, HH:mm"), pickup.WasteType, pickup.Status, pickup.AssignedAgent, pickup.EstimatedWeightKg == null ? "Pending" : $"{pickup.EstimatedWeightKg:N1} kg")), page, pageSize);
    }

    public async Task<AdminPickupDto?> UpdatePickupAsync(Guid id, AdminPickupUpdateRequest request)
    {
        var pickup = await db.Pickups.Include(item => item.UserAccount).FirstOrDefaultAsync(item => item.Id == id);
        if (pickup is null) return null;
        pickup.Status = request.Status; pickup.AssignedAgent = string.IsNullOrWhiteSpace(request.AssignedAgent) ? null : request.AssignedAgent;
        await db.SaveChangesAsync();
        return new AdminPickupDto(pickup.Id, pickup.ReferenceNumber, pickup.UserAccount!.FullName, pickup.ScheduledFor.ToString("dd MMM yyyy, HH:mm"), pickup.WasteType, pickup.Status, pickup.AssignedAgent, pickup.EstimatedWeightKg == null ? "Pending" : $"{pickup.EstimatedWeightKg:N1} kg");
    }

    public async Task<PagedResult<AdminActivityDto>> GetActivityAsync(int page, int pageSize) => await PageAsync(db.ActivityLogs.Include(log => log.UserAccount).OrderByDescending(log => log.CreatedAt).Select(log => new AdminActivityDto(log.UserAccount!.FullName, log.Title, log.Detail, log.CreatedAt.ToString("dd MMM yyyy, HH:mm"), log.Tone)), page, pageSize);
    public async Task<List<AdminRewardDto>> GetRewardsAsync() => await db.RewardCatalogue.OrderBy(item => item.PointsCost).Select(item => new AdminRewardDto(item.Id, item.Name, item.Description, item.PointsCost, item.IsActive)).ToListAsync();
    public async Task<AdminRewardDto> CreateRewardAsync(RewardCatalogueRequest request) { var item = new RewardCatalogueItem { Id = Guid.NewGuid(), Name = request.Name, Description = request.Description, PointsCost = request.PointsCost, IsActive = request.IsActive }; db.RewardCatalogue.Add(item); await db.SaveChangesAsync(); return new(item.Id, item.Name, item.Description, item.PointsCost, item.IsActive); }
    public async Task<AdminRewardDto?> UpdateRewardAsync(Guid id, RewardCatalogueRequest request) { var item = await db.RewardCatalogue.FindAsync(id); if (item is null) return null; item.Name = request.Name; item.Description = request.Description; item.PointsCost = request.PointsCost; item.IsActive = request.IsActive; await db.SaveChangesAsync(); return new(item.Id, item.Name, item.Description, item.PointsCost, item.IsActive); }
    public async Task<PagedResult<AdminRedemptionDto>> GetRedemptionsAsync(int page, int pageSize) => await PageAsync(db.RewardRedemptions.Include(item => item.UserAccount).Include(item => item.RewardCatalogueItem).OrderByDescending(item => item.CreatedAt).Select(item => new AdminRedemptionDto(item.Id, item.UserAccount!.FullName, item.RewardCatalogueItem!.Name, item.PointsSpent, item.Status, item.CreatedAt.ToString("dd MMM yyyy"))), page, pageSize);
    public async Task<AdminRedemptionDto?> UpdateRedemptionAsync(Guid id, RedemptionUpdateRequest request) { var item = await db.RewardRedemptions.Include(x => x.UserAccount).Include(x => x.RewardCatalogueItem).FirstOrDefaultAsync(x => x.Id == id); if (item is null) return null; item.Status = request.Status; await db.SaveChangesAsync(); return new(item.Id, item.UserAccount!.FullName, item.RewardCatalogueItem!.Name, item.PointsSpent, item.Status, item.CreatedAt.ToString("dd MMM yyyy")); }
    public async Task<string> BuildReportAsync()
    {
        var overview = await GetOverviewAsync();
        var users = await db.Users.OrderBy(user => user.FullName).Select(user => new { user.FullName, user.Email, user.Role, user.IsActive, user.RewardPoints }).ToListAsync();
        var pickups = await db.Pickups.Include(pickup => pickup.UserAccount).OrderByDescending(pickup => pickup.ScheduledFor).Select(pickup => new { pickup.ReferenceNumber, Member = pickup.UserAccount!.FullName, pickup.ScheduledFor, pickup.WasteType, pickup.Status, pickup.AssignedAgent, pickup.EstimatedWeightKg }).ToListAsync();
        var activity = await db.ActivityLogs.Include(log => log.UserAccount).OrderByDescending(log => log.CreatedAt).Select(log => new { Member = log.UserAccount!.FullName, log.Title, log.Detail, log.CreatedAt }).ToListAsync();
        var redemptions = await db.RewardRedemptions.Include(item => item.UserAccount).Include(item => item.RewardCatalogueItem).OrderByDescending(item => item.CreatedAt).Select(item => new { Member = item.UserAccount!.FullName, Reward = item.RewardCatalogueItem!.Name, item.PointsSpent, item.Status, item.CreatedAt }).ToListAsync();
        var lines = new List<string> { "ReLoop system report", $"Generated (UTC),{DateTimeOffset.UtcNow:u}", "", "Summary", "Metric,Value" };
        lines.AddRange(overview.Metrics.Select(metric => $"{Csv(metric.Label)},{Csv(metric.Value)}"));
        lines.AddRange(["", "Members", "Name,Email,Role,Active,Reward points"]);
        lines.AddRange(users.Select(user => $"{Csv(user.FullName)},{Csv(user.Email)},{Csv(user.Role)},{user.IsActive},{user.RewardPoints}"));
        lines.AddRange(["", "Pickups", "Reference,Member,Scheduled for,Material,Status,Assigned agent,Weight kg"]);
        lines.AddRange(pickups.Select(pickup => $"{Csv(pickup.ReferenceNumber)},{Csv(pickup.Member)},{Csv(pickup.ScheduledFor.ToString("O"))},{Csv(pickup.WasteType)},{Csv(pickup.Status)},{Csv(pickup.AssignedAgent)},{Csv(pickup.EstimatedWeightKg?.ToString("0.00"))}"));
        lines.AddRange(["", "Recycling activity", "Member,Activity,Details,Date"]);
        lines.AddRange(activity.Select(item => $"{Csv(item.Member)},{Csv(item.Title)},{Csv(item.Detail)},{Csv(item.CreatedAt.ToString("O"))}"));
        lines.AddRange(["", "Reward redemptions", "Member,Reward,Points spent,Status,Date"]);
        lines.AddRange(redemptions.Select(item => $"{Csv(item.Member)},{Csv(item.Reward)},{item.PointsSpent},{Csv(item.Status)},{Csv(item.CreatedAt.ToString("O"))}"));
        return string.Join(Environment.NewLine, lines);
    }

    private static string Csv(string? value) => $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";
    private static async Task<PagedResult<T>> PageAsync<T>(IQueryable<T> query, int page, int pageSize) { page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, MaxPageSize); var total = await query.CountAsync(); var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(); return new(items, page, pageSize, total); }
}
