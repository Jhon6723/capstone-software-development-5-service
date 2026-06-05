namespace PixPro.Services.Projects.Application.DTOs.Responses;

public class CreditBalanceResponse
{
    public string UserId { get; set; } = string.Empty;
    public string SubscriptionTier { get; set; } = string.Empty;
    public List<ModelCreditResponse> Credits { get; set; } = new();
}

public class ModelCreditResponse
{
    public string ModelTier { get; set; } = string.Empty;
    public int Remaining { get; set; }
    public int Total { get; set; }
    public DateTimeOffset? ResetAt { get; set; }
}
