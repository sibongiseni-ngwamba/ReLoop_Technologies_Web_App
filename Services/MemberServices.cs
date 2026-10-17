using Microsoft.EntityFrameworkCore;
using ReLoop_Technologies_Web_App.Data;
using ReLoop_Technologies_Web_App.Data.Entities;
using ReLoop_Technologies_Web_App.Models;

namespace ReLoop_Technologies_Web_App.Services;

public sealed class MemberServices(ReLoopDbContext db)
{
    public async Task<RewardsDto> GetRewardsAsync(string email)
    {
        var user = await db.Users.SingleAsync(item => item.Email == email);
        var rewards = await db.RewardCatalogue.Where(item => item.IsActive).OrderBy(item => item.PointsCost)
            .Select(item => new RewardOptionDto(item.Id, item.Name, item.Description, item.PointsCost)).ToListAsync();
        var redemptions = await db.RewardRedemptions.Where(item => item.UserAccountId == user.Id)
            .OrderByDescending(item => item.CreatedAt).Select(item => new MemberRedemptionDto(item.Id, item.RewardCatalogueItem!.Name, item.PointsSpent, item.Status, item.CreatedAt)).ToListAsync();
        return new RewardsDto(user.RewardPoints, rewards, redemptions);
    }

    public async Task<(MemberRedemptionDto? Redemption, string? Error)> RedeemAsync(string email, RedeemRewardRequest request)
    {
        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync() : null;
        var user = await db.Users.SingleOrDefaultAsync(item => item.Email == email);
        var reward = await db.RewardCatalogue.SingleOrDefaultAsync(item => item.Id == request.RewardId && item.IsActive);
        if (user is null) return (null, "The signed-in account was not found.");
        if (reward is null) return (null, "This reward is unavailable.");
        if (user.RewardPoints < reward.PointsCost) return (null, "You do not have enough points for this reward.");

        if (db.Database.IsRelational())
        {
            var changed = await db.Users.Where(item => item.Id == user.Id && item.RewardPoints >= reward.PointsCost)
                .ExecuteUpdateAsync(update => update.SetProperty(item => item.RewardPoints, item => item.RewardPoints - reward.PointsCost));
            if (changed == 0) return (null, "You do not have enough points for this reward.");
        }
        else
        {
            if (user.RewardPoints < reward.PointsCost) return (null, "You do not have enough points for this reward.");
            user.RewardPoints -= reward.PointsCost;
        }
        var redemption = new RewardRedemption
        {
            Id = Guid.NewGuid(), UserAccountId = user.Id, RewardCatalogueItemId = reward.Id,
            PointsSpent = reward.PointsCost, Status = "Pending", CreatedAt = DateTimeOffset.UtcNow
        };
        db.RewardRedemptions.Add(redemption);
        db.ActivityLogs.Add(new ActivityLog
        {
            Id = Guid.NewGuid(), UserAccountId = user.Id, Title = "Reward redemption requested",
            Detail = $"{reward.Name} redemption is awaiting review", Tone = "info", CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
        if (transaction is not null) await transaction.CommitAsync();
        return (new MemberRedemptionDto(redemption.Id, reward.Name, redemption.PointsSpent, redemption.Status, redemption.CreatedAt), null);
    }

    public async Task<IReadOnlyList<NotificationDto>> GetNotificationsAsync(string email)
    {
        var userId = await db.Users.Where(item => item.Email == email).Select(item => item.Id).SingleAsync();
        return await db.ActivityLogs.Where(item => item.UserAccountId == userId).OrderByDescending(item => item.CreatedAt)
            .Take(30).Select(item => new NotificationDto(item.Id, item.Title, item.Detail, item.Tone, item.CreatedAt)).ToListAsync();
    }
}

public sealed record NotificationDto(Guid Id, string Title, string Message, string Tone, DateTimeOffset CreatedAt);
