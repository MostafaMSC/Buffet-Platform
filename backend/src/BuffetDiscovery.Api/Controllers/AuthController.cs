using BuffetDiscovery.Api.Data;
using BuffetDiscovery.Api.Dtos;
using BuffetDiscovery.Api.Entities;
using BuffetDiscovery.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BuffetDiscovery.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AppDbContext db, JwtTokenService jwt) : ControllerBase
{
    [HttpPost("signup")]
    public async Task<ActionResult<AuthResponseDto>> Signup(SignupDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.PhoneNumber) || string.IsNullOrWhiteSpace(dto.Password))
        {
            return BadRequest(new { message = "Phone number and password are required." });
        }

        if (dto.Password.Length < 6)
        {
            return BadRequest(new { message = "Password must be at least 6 characters." });
        }

        var exists = await db.Users.AnyAsync(u => u.PhoneNumber == dto.PhoneNumber);
        if (exists)
        {
            return Conflict(new { message = "This phone number is already registered." });
        }

        var areaExists = await db.Areas.AnyAsync(a => a.Id == dto.AreaId);
        if (!areaExists)
        {
            return BadRequest(new { message = "Invalid area." });
        }

        var restaurant = new Restaurant
        {
            Name = dto.RestaurantName,
            NameAr = dto.RestaurantNameAr,
            AreaId = dto.AreaId,
            PhoneNumber = dto.PhoneNumber,
            Status = RestaurantStatus.Pending
        };
        db.Restaurants.Add(restaurant);
        await db.SaveChangesAsync();

        var user = new User
        {
            PhoneNumber = dto.PhoneNumber,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = UserRole.RestaurantOwner,
            RestaurantId = restaurant.Id
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var token = jwt.CreateToken(user);
        return Ok(new AuthResponseDto(token, user.Role.ToString(), restaurant.Id, restaurant.Status));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
    {
        var user = await db.Users
            .Include(u => u.Restaurant)
            .FirstOrDefaultAsync(u => u.PhoneNumber == dto.PhoneNumber);

        if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid phone number or password." });
        }

        var token = jwt.CreateToken(user);
        return Ok(new AuthResponseDto(token, user.Role.ToString(), user.RestaurantId, user.Restaurant?.Status));
    }
}
