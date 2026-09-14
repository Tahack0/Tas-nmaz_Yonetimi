using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using TasinmazYönetimi.Application.Auth;

namespace TasinmazYönetimi.Api.Security;

public class JwtTokenService
{
    private readonly SymmetricSecurityKey _key;

    public JwtTokenService(SymmetricSecurityKey key)
    {
        _key = key;
    }

    public (string Token, DateTime ExpiresAtUtc) CreateToken(
        AuthenticatedUser user)
    {
        DateTime expiresAtUtc = DateTime.UtcNow.AddHours(1);

        var token = new JwtSecurityToken(
            issuer: "TasinmazYonetimi.Api",
            audience: "TasinmazYonetimi.Client",
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            ],
            expires: expiresAtUtc,
            signingCredentials: new SigningCredentials(
                _key, SecurityAlgorithms.HmacSha256));

        return (
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAtUtc
        );
    }
}