using ReLoop_Technologies_Web_App.Data;
using ReLoop_Technologies_Web_App.Models;

namespace ReLoop_Technologies_Web_App.Services;

public sealed class MemberServices(IReLoopDocumentStore documents, ReLoopStore store)
{
    public async Task<RewardsDto> GetRewardsAsync(string uid)
    {
        var balance = await documents.GetAsync<RewardAccountDocument>(FirestoreCollections.Rewards, uid) ?? new RewardAccountDocument { UserId = uid };
        var rewardItems = await store.GetRewardItemsAsync();
        var rewardNames = rewardItems.ToDictionary(item => item.RewardItemId, item => item.ItemName, StringComparer.Ordinal);
        var rewards = rewardItems.Where(item => item.IsActive).OrderBy(item => item.PointsCost)
            .Select(item => new RewardOptionDto(item.RewardItemId, item.ItemName, item.Description, item.PointsCost)).ToList();
        var redemptions = (await store.GetRedemptionsAsync()).Where(item => item.UserId == uid).OrderByDescending(item => item.CreatedAt)
            .Select(item => new MemberRedemptionDto(item.RedemptionId, rewardNames.GetValueOrDefault(item.RewardItemId, "Reward"), item.PointsUsed, item.Status, new DateTimeOffset(item.CreatedAt, TimeSpan.Zero))).ToList();
        return new RewardsDto(balance.PointsBalance, rewards, redemptions);
    }

    public async Task<(MemberRedemptionDto? Redemption, string? Error)> RedeemAsync(string uid, RedeemRewardRequest request)
    {
        var redemptionId = Guid.NewGuid().ToString();
        var result = await documents.RunTransactionAsync(async transaction =>
        {
            var reward = await transaction.GetAsync<RewardItemDocument>(FirestoreCollections.RewardItems, request.RewardId);
            var balance = await transaction.GetAsync<RewardAccountDocument>(FirestoreCollections.Rewards, uid);
            if (reward is null || !reward.IsActive) return new RedemptionAttempt(null, "This reward is unavailable.", null);
            balance ??= new RewardAccountDocument { UserId = uid };
            if (balance.PointsBalance < reward.PointsCost) return new RedemptionAttempt(null, "You do not have enough points for this reward.", null);

            var now = DateTime.UtcNow;
            balance.PointsBalance -= reward.PointsCost;
            balance.TotalRedeemed += reward.PointsCost;
            balance.LastUpdated = now;
            var redemption = new RedemptionLogDocument
            {
                RedemptionId = redemptionId, UserId = uid, RewardItemId = reward.RewardItemId,
                PointsUsed = reward.PointsCost, Status = "Pending", CreatedAt = now
            };
            var points = new PointsTransactionDocument
            {
                TransactionId = Guid.NewGuid().ToString(), UserId = uid, TransactionType = "redeem",
                Points = -reward.PointsCost, RelatedId = redemptionId, Description = $"Redeemed {reward.ItemName}", CreatedAt = now
            };
            transaction.Set(FirestoreCollections.Rewards, uid, balance);
            transaction.Set(FirestoreCollections.RedemptionLogs, redemption.RedemptionId, redemption);
            transaction.Set(FirestoreCollections.PointsTransactions, points.TransactionId, points);
            return new RedemptionAttempt(redemption, null, reward.ItemName);
        });

        return result.Redemption is null
            ? (null, result.Error)
            : (new MemberRedemptionDto(result.Redemption.RedemptionId, result.RewardName!, result.Redemption.PointsUsed, result.Redemption.Status, new DateTimeOffset(result.Redemption.CreatedAt, TimeSpan.Zero)), null);
    }

    public async Task<IReadOnlyList<NotificationDto>> GetNotificationsAsync(string uid)
    {
        var items = (await store.GetAllNotificationsAsync()).Where(item => item.UserId == uid).OrderByDescending(item => item.SentAt).Take(30);
        return items.Select(item => new NotificationDto(item.NotificationId, item.Title, item.Message, item.Type, new DateTimeOffset(item.SentAt, TimeSpan.Zero))).ToArray();
    }

    private sealed record RedemptionAttempt(RedemptionLogDocument? Redemption, string? Error, string? RewardName);
}

public sealed record NotificationDto(string Id, string Title, string Message, string Tone, DateTimeOffset CreatedAt);
