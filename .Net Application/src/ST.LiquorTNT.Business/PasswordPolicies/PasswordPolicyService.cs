using FluentValidation;
using ST.LiquorTNT.Business.Common;
using ST.LiquorTNT.Business.Common.Abstractions;
using ST.LiquorTNT.Business.Common.Exceptions;
using ST.LiquorTNT.Business.Users;
using ST.LiquorTNT.Contracts.PasswordPolicies;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Business.PasswordPolicies;

/// <summary>Admin editing of PASSWORD_POLICY rows. The rules are data; changing them needs no deployment.</summary>
public sealed class PasswordPolicyService : IPasswordPolicyService
{
    private readonly IPasswordPolicyRepository _policies;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;
    private readonly IUserLogWriter _log;
    private readonly IValidator<UpdatePasswordPolicyRequest> _validator;

    public PasswordPolicyService(
        IPasswordPolicyRepository policies,
        IClock clock,
        ICurrentUser currentUser,
        IUserLogWriter log,
        IValidator<UpdatePasswordPolicyRequest> validator)
    {
        _policies = policies;
        _clock = clock;
        _currentUser = currentUser;
        _log = log;
        _validator = validator;
    }

    public async Task<IReadOnlyList<PasswordPolicyResponse>> GetAllAsync(CancellationToken ct)
    {
        var policies = await _policies.GetAllAsync(ct);
        return policies.Select(ToResponse).ToList();
    }

    public async Task<PasswordPolicyResponse> UpdateAsync(int id, UpdatePasswordPolicyRequest request, CancellationToken ct)
    {
        (await _validator.ValidateAsync(request, ct)).EnsureValid();

        var policy = await _policies.GetByIdAsync(id, ct) ?? throw new NotFoundException("Password policy");
        var before = ToResponse(policy);

        policy.UpdateRules(
            request.MinLength, request.MaxLength,
            request.RequireUppercase, request.RequireLowercase, request.RequireNumber, request.RequireSpecialCharacter,
            request.PasswordHistoryCount, request.PasswordExpiryEnabled, request.PasswordExpiryDays,
            request.AllowUsernameInPassword, request.AllowCommonPassword,
            _clock.IndiaNow, _currentUser.UserId);

        var after = ToResponse(policy);

        await _log.WriteAsync(UserLogEntry.Success(UserLogActions.PasswordPolicyChanged, UserLogModules.Security,
            "PASSWORD_POLICY", policy.Id.ToString(), $"Password policy '{policy.PolicyName}' changed.",
            oldValue: before, newValue: after), ct);
        await _policies.SaveChangesAsync(ct);

        return after;
    }

    private static PasswordPolicyResponse ToResponse(PASSWORD_POLICY p) => new()
    {
        Id = p.Id,
        PolicyName = p.PolicyName,
        MinLength = p.MinLength,
        MaxLength = p.MaxLength,
        RequireUppercase = p.RequireUppercase,
        RequireLowercase = p.RequireLowercase,
        RequireNumber = p.RequireNumber,
        RequireSpecialCharacter = p.RequireSpecialCharacter,
        PasswordHistoryCount = p.PasswordHistoryCount,
        PasswordExpiryEnabled = p.PasswordExpiryEnabled,
        PasswordExpiryDays = p.PasswordExpiryDays,
        AllowUsernameInPassword = p.AllowUsernameInPassword,
        AllowCommonPassword = p.AllowCommonPassword,
        Status = p.Status,
        UpdatedAt = p.UpdatedAt,
    };
}
