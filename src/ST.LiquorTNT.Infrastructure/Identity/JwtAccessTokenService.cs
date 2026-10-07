using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ST.LiquorTNT.Business.Auth;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Infrastructure.Identity;

/// <summary>
/// HS256 JWT carrying identity claims only (user id, name, home company). Rights are deliberately NOT in the
/// token: they are read from the database per request, so a change by an administrator applies on the next call
/// without a new login. The token's hash is what USER_SESSION stores.
/// </summary>
public sealed class JwtAccessTokenService : IAccessTokenService
{
    private const int MinKeyLength = 32;

    private readonly JwtOptions _options;
    private readonly SigningCredentials _credentials;

    public JwtAccessTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;

        if (string.IsNullOrWhiteSpace(_options.SigningKey) || _options.SigningKey.Length < MinKeyLength)
        {
            throw new InvalidOperationException($"Jwt:SigningKey must be at least {MinKeyLength} characters.");
        }

        _credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
            SecurityAlgorithms.HmacSha256);
    }

    public string Create(USERS user, DateTime expiresAtUtc)
    {
        var claims = new List<Claim>
        {
            new("sub", user.Id.ToString(CultureInfo.InvariantCulture)),
            new("unique_name", user.UserName),
            new("jti", Guid.NewGuid().ToString("N")),
        };

        if (user.CompanyId.HasValue)
        {
            claims.Add(new Claim("company_id", user.CompanyId.Value.ToString(CultureInfo.InvariantCulture)));
        }

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAtUtc,
            signingCredentials: _credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
