using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BuffetDiscovery.Api.Entities;
using Microsoft.IdentityModel.Tokens;

namespace BuffetDiscovery.Api.Services;

public class JwtTokenService(IConfiguration config)
{
    public string CreateToken(User user)
    {
        var jwtSection = config.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.MobilePhone, user.PhoneNumber),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        if (user.RestaurantId.HasValue)
        {
            claims.Add(new Claim("restaurantId", user.RestaurantId.Value.ToString()));
        }

        var token = new JwtSecurityToken(
            issuer: jwtSection["Issuer"],
            audience: jwtSection["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(30),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
