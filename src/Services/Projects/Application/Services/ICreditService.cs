using PixPro.Services.Projects.Application.Common.Results;
using PixPro.Services.Projects.Application.DTOs.Responses;
using PixPro.Services.Projects.Domain.Enums;

namespace PixPro.Services.Projects.Application.Services;

public interface ICreditService
{
    Task<Result<bool>> TryDeductCreditAsync(Guid userId, ModelTier modelTier, CancellationToken cancellationToken = default);
    Task RefundCreditAsync(Guid userId, ModelTier modelTier, CancellationToken cancellationToken = default);
    Task<Result<CreditBalanceResponse>> GetBalanceAsync(Guid userId, bool isAdmin = false, CancellationToken cancellationToken = default);
    Task SeedFreeCreditsAsync(Guid userId, CancellationToken cancellationToken = default);
}
