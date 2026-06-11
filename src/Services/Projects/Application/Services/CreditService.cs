using Microsoft.Extensions.Logging;
using PixPro.Services.Projects.Application.Common.Results;
using PixPro.Services.Projects.Application.DTOs.Responses;
using PixPro.Services.Projects.Domain.Entities;
using PixPro.Services.Projects.Domain.Enums;
using PixPro.Services.Projects.Domain.Repositories;

namespace PixPro.Services.Projects.Application.Services;

public sealed class CreditService : ICreditService
{
    private readonly IUserCreditRepository _creditRepository;
    private readonly ILogger<CreditService> _logger;

    private static readonly IReadOnlyDictionary<ModelTier, int> FreeCredits = new Dictionary<ModelTier, int>
    {
        { ModelTier.Kontext,        5 },
        { ModelTier.Gpt15Low,       3 },
        { ModelTier.NanobananaLow,  3 },
        { ModelTier.NanabanaMedium, 2 },
        { ModelTier.NanabanaMax,    1 },
        { ModelTier.Gpt15Medium,    1 },
    };

    public CreditService(IUserCreditRepository creditRepository, ILogger<CreditService> logger)
    {
        _creditRepository = creditRepository;
        _logger = logger;
    }

    public async Task<Result<bool>> TryDeductCreditAsync(
        Guid userId,
        ModelTier modelTier,
        CancellationToken cancellationToken = default)
    {
        var credit = await _creditRepository.GetByUserAndModelAsync(userId, modelTier, cancellationToken);

        if (credit is null)
        {
            await SeedFreeCreditsAsync(userId, cancellationToken);
            credit = await _creditRepository.GetByUserAndModelAsync(userId, modelTier, cancellationToken);
        }

        if (credit is null || !credit.TryDeduct())
        {
            _logger.LogWarning(
                "Insufficient credits for UserId: {UserId}, ModelTier: {ModelTier}",
                userId, modelTier);
            return Result<bool>.Failure("INSUFFICIENT_CREDITS");
        }

        await _creditRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Deducted 1 credit for UserId: {UserId}, ModelTier: {ModelTier}. Remaining: {Remaining}",
            userId, modelTier, credit.CreditsRemaining);

        return Result<bool>.Success(true);
    }

    public async Task RefundCreditAsync(
        Guid userId,
        ModelTier modelTier,
        CancellationToken cancellationToken = default)
    {
        var credit = await _creditRepository.GetByUserAndModelAsync(userId, modelTier, cancellationToken);

        if (credit is null)
        {
            _logger.LogWarning(
                "Attempted to refund credit but no record found for UserId: {UserId}, ModelTier: {ModelTier}",
                userId, modelTier);
            return;
        }

        credit.Refund();
        await _creditRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Refunded 1 credit for UserId: {UserId}, ModelTier: {ModelTier}. Remaining: {Remaining}",
            userId, modelTier, credit.CreditsRemaining);
    }

    public async Task<Result<CreditBalanceResponse>> GetBalanceAsync(
        Guid userId,
        bool isAdmin = false,
        CancellationToken cancellationToken = default)
    {
        if (isAdmin)
        {
            var adminResponse = new CreditBalanceResponse
            {
                UserId = userId.ToString(),
                SubscriptionTier = "Admin",
                Credits = Enum.GetValues<ModelTier>().Select(tier => new ModelCreditResponse
                {
                    ModelTier = tier.ToString(),
                    Remaining = int.MaxValue,
                    Total = int.MaxValue,
                    ResetAt = null
                }).ToList()
            };
            return Result<CreditBalanceResponse>.Success(adminResponse);
        }

        var credits = await _creditRepository.GetAllByUserAsync(userId, cancellationToken);

        if (credits.Count < FreeCredits.Count)
        {
            await SeedFreeCreditsAsync(userId, cancellationToken);
            credits = await _creditRepository.GetAllByUserAsync(userId, cancellationToken);
        }

        var subscriptionTier = credits.FirstOrDefault()?.SubscriptionTier ?? SubscriptionTier.Free;

        var response = new CreditBalanceResponse
        {
            UserId = userId.ToString(),
            SubscriptionTier = subscriptionTier.ToString(),
            Credits = credits.Select(c => new ModelCreditResponse
            {
                ModelTier = c.ModelTier.ToString(),
                Remaining = c.CreditsRemaining,
                Total = c.CreditsTotal,
                ResetAt = c.ResetAt
            }).ToList()
        };

        return Result<CreditBalanceResponse>.Success(response);
    }

    public async Task SeedFreeCreditsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        foreach (var (modelTier, freeAmount) in FreeCredits)
        {
            var existing = await _creditRepository.GetByUserAndModelAsync(userId, modelTier, cancellationToken);
            if (existing is not null)
                continue;

            var credit = new UserCredit(
                userId,
                modelTier,
                creditsRemaining: freeAmount,
                creditsTotal: freeAmount,
                subscriptionTier: SubscriptionTier.Free,
                resetAt: null);

            await _creditRepository.AddAsync(credit, cancellationToken);

            _logger.LogInformation(
                "Seeded {Credits} free credits for UserId: {UserId}, ModelTier: {ModelTier}",
                freeAmount, userId, modelTier);
        }

        await _creditRepository.SaveChangesAsync(cancellationToken);
    }
}
