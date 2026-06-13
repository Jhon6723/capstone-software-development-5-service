using PixPro.Services.Projects.Domain.Enums;

namespace PixPro.Services.Projects.Domain.Entities;

public sealed class UserCredit
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public ModelTier ModelTier { get; private set; }
    public int CreditsRemaining { get; private set; }
    public int CreditsTotal { get; private set; }
    public SubscriptionTier SubscriptionTier { get; private set; }
    public DateTimeOffset? ResetAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public int FluxDailyCount { get; private set; }
    public DateTimeOffset? FluxDailyResetAt { get; private set; }

    public UserCredit(
        Guid userId,
        ModelTier modelTier,
        int creditsRemaining,
        int creditsTotal,
        SubscriptionTier subscriptionTier = SubscriptionTier.Free,
        DateTimeOffset? resetAt = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be an empty GUID.", nameof(userId));
        if (creditsRemaining < 0)
            throw new ArgumentException("CreditsRemaining cannot be negative.", nameof(creditsRemaining));
        if (creditsTotal < 0)
            throw new ArgumentException("CreditsTotal cannot be negative.", nameof(creditsTotal));

        Id = Guid.NewGuid();
        UserId = userId;
        ModelTier = modelTier;
        CreditsRemaining = creditsRemaining;
        CreditsTotal = creditsTotal;
        SubscriptionTier = subscriptionTier;
        ResetAt = resetAt;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private UserCredit() { }

    public bool HasCredits() => CreditsRemaining > 0;

    public bool TryDeduct()
    {
        if (CreditsRemaining <= 0)
            return false;

        CreditsRemaining--;
        UpdatedAt = DateTimeOffset.UtcNow;
        return true;
    }

    public void Refund(int amount = 1)
    {
        if (amount <= 0)
            throw new ArgumentException("Refund amount must be positive.", nameof(amount));

        CreditsRemaining += amount;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
    public bool TryIncrementFluxDaily(int dailyLimit)
    {
        var now = DateTimeOffset.UtcNow;

        if (FluxDailyResetAt is null || now >= FluxDailyResetAt)
        {
            FluxDailyCount = 0;
            FluxDailyResetAt = now.AddHours(24);
        }

        if (FluxDailyCount >= dailyLimit)
            return false;

        FluxDailyCount++;
        UpdatedAt = now;
        return true;
    }

    public void UpdateSubscription(
        SubscriptionTier subscriptionTier,
        int creditsRemaining,
        int creditsTotal,
        DateTimeOffset? resetAt)
    {
        SubscriptionTier = subscriptionTier;
        CreditsRemaining = creditsRemaining;
        CreditsTotal = creditsTotal;
        ResetAt = resetAt;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
