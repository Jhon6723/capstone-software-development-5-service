namespace API.Models;

/// <summary>
/// AI model tier for credit billing
/// </summary>
public enum ModelTier
{
    /// <summary>gpt-image-1-mini-low — everyday image editing, cheapest tier</summary>
    GptMiniLow = 0,

    /// <summary>kontext — high-quality instruction editing, medium tier</summary>
    Kontext = 1,

    /// <summary>gpt-image-1-mini-high — complex reasoning edits, premium tier</summary>
    GptMiniHigh = 2
}

/// <summary>
/// Subscription tier that determines credit allocation
/// </summary>
public enum SubscriptionTier
{
    /// <summary>Free tier — limited credits allocated once at registration</summary>
    Free = 0,

    /// <summary>Basic subscription</summary>
    Basic = 1,

    /// <summary>Pro subscription</summary>
    Pro = 2,

    /// <summary>Unlimited subscription — no credit restrictions</summary>
    Unlimited = 3
}

/// <summary>
/// Credit balance for a single AI model tier
/// </summary>
public class ModelCreditResponse
{
    /// <summary>
    /// AI model tier name
    /// </summary>
    /// <example>GptMiniLow</example>
    public string ModelTier { get; set; } = string.Empty;

    /// <summary>
    /// Credits remaining for this model tier
    /// </summary>
    /// <example>4</example>
    public int CreditsRemaining { get; set; }

    /// <summary>
    /// Total credits allocated for this model tier in the current period
    /// </summary>
    /// <example>5</example>
    public int CreditsTotal { get; set; }

    /// <summary>
    /// Current subscription tier
    /// </summary>
    /// <example>Free</example>
    public string SubscriptionTier { get; set; } = string.Empty;

    /// <summary>
    /// When credits will reset (null for free tier)
    /// </summary>
    public DateTimeOffset? ResetAt { get; set; }
}

/// <summary>
/// Full credit balance for the authenticated user across all model tiers
/// </summary>
public class CreditBalanceResponse
{
    /// <summary>
    /// User ID
    /// </summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid UserId { get; set; }

    /// <summary>
    /// Credit balances per model tier.
    /// Models not listed have not been used yet and will be seeded on first use.
    /// </summary>
    public List<ModelCreditResponse> Credits { get; set; } = [];
}
