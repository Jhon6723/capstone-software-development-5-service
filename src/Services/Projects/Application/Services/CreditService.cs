using Microsoft.Extensions.Logging;
using PixPro.Services.Projects.Application.Common.Exceptions;
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

    private const int FluxDailyDefaultLimit = 200;
    private const int MaxConcurrencyRetries = 3;

    public CreditService(IUserCreditRepository creditRepository, ILogger<CreditService> logger)
    {
        _creditRepository = creditRepository;
        _logger = logger;
    }

    /// <summary>
    /// Tracks daily usage of <c>flux-schnell</c> (free model) for a user and enforces
    /// the <see cref="FluxDailyDefaultLimit"/> cap. Unlike paid models, flux-schnell
    /// does not consume credits — this is a fixed rolling 24-hour window to prevent
    /// cost abuse on the underlying Pixazo API.
    /// </summary>
    /// <remarks>
    /// A <c>UserCredit</c> row with <c>ModelTier.Pixazo</c> is created on first use
    /// and reused as the counter anchor. The counter resets automatically after 24 hours.
    /// Admins bypass this check entirely (handled in <c>ImageService</c>).
    /// Race conditions are prevented via optimistic concurrency (the entity uses an
    /// <c>xmin</c> token); on a conflict the operation is retried with fresh data.
    /// </remarks>
    /// <returns>
    /// <c>Success(true)</c> if the request is within the daily limit;
    /// <c>Failure("FLUX_DAILY_LIMIT_EXCEEDED")</c> if the cap has been reached.
    /// </returns>
    public async Task<Result<bool>> TryIncrementFluxDailyAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; attempt <= MaxConcurrencyRetries; attempt++)
        {
            var credit = await _creditRepository.GetByUserAndModelAsync(
                userId, ModelTier.Pixazo, cancellationToken);

            if (credit is null)
            {
                credit = new UserCredit(
                    userId,
                    ModelTier.Pixazo,
                    creditsRemaining: 0,
                    creditsTotal: 0,
                    subscriptionTier: SubscriptionTier.Free);

                await _creditRepository.AddAsync(credit, cancellationToken);
            }

            // Business rule lives in the domain entity: reset window, check cap, increment.
            if (!credit.TryIncrementFluxDaily(FluxDailyDefaultLimit))
            {
                _logger.LogWarning(
                    "flux-schnell daily limit reached for UserId: {UserId}. Limit: {Limit}",
                    userId, FluxDailyDefaultLimit);
                return Result<bool>.Failure("FLUX_DAILY_LIMIT_EXCEEDED");
            }

            try
            {
                await _creditRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "flux-schnell usage incremented for UserId: {UserId}. Count: {Count}",
                    userId, credit.FluxDailyCount);

                return Result<bool>.Success(true);
            }
            catch (ConcurrencyConflictException)
            {
                // A concurrent request modified or inserted the row first. The
                // repository has already refreshed the tracked state, so retry with
                // fresh data and re-evaluate the limit.
                _logger.LogWarning(
                    "Concurrency conflict on flux-schnell increment for UserId: {UserId}. Attempt {Attempt}/{Max}",
                    userId, attempt, MaxConcurrencyRetries);
            }
        }

        _logger.LogError(
            "flux-schnell increment failed after {Max} concurrency retries for UserId: {UserId}",
            MaxConcurrencyRetries, userId);
        return Result<bool>.Failure("FLUX_DAILY_CONCURRENCY_RETRY_EXCEEDED");
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
