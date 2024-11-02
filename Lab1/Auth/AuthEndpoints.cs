using System.Security.Claims;
using Lab1.Auth.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Lab1.Auth;

public static class AuthEndpoints
{
    public static void AddAuthApi(this WebApplication app)
    {
        app.MapPost("api/register", async (UserManager<ForumRestUser> userManager, RegisterUserDto registerUserDto) =>
        {
            var user = await userManager.FindByNameAsync(registerUserDto.UserName);
            if (user != null)
                return Results.UnprocessableEntity("username already exists");
            var newUser = new ForumRestUser
            {
                Email = registerUserDto.Email,
                UserName = registerUserDto.UserName
            };
            var createUserResult = await userManager.CreateAsync(newUser, registerUserDto.Password);
            if (!createUserResult.Succeeded)
            {
                //return Results.UnprocessableEntity(registerUserDto.Password);
                var errors = string.Join(", ", createUserResult.Errors.Select(e => e.Description));
                return Results.UnprocessableEntity($"Failed to create user: {errors}");
            }
            await userManager.AddToRoleAsync(newUser, ForumRoles.ForumUser);

            return Results.Created("api/login", new UserDto(newUser.Id, newUser.UserName, newUser.Email));
        });
        
        app.MapPost("api/login", async (UserManager<ForumRestUser> userManager, LoginDto loginDto, JwtTokenService jwtTokenService) =>
        {
            var user = await userManager.FindByNameAsync(loginDto.UserName);
            if (user == null)
                return Results.UnprocessableEntity("username or password is incorrect");

            var isPasswordValid = await userManager.CheckPasswordAsync(user, loginDto.Password);
            if (!isPasswordValid)
                return Results.UnprocessableEntity("username or password is incorrect");
            user.ForceRelogin = false;
            await userManager.UpdateAsync(user);
            
            var roles = await userManager.GetRolesAsync(user);
            var accessToken = jwtTokenService.CreateAccessToken(user.UserName, user.Id, roles);
            var refreshToken = jwtTokenService.CreateRefreshToken(user.Id);
            
            return Results.Ok(new SuccessfulLoginDto(accessToken, refreshToken));
        });

        app.MapPost("api/accessToken", async (UserManager<ForumRestUser> userManager, RefreshAccessTokenDto refreshAccessTokenDto, JwtTokenService jwtTokenService) =>
            {
                if (!jwtTokenService.TryParseRefreshToken(refreshAccessTokenDto.RefreshToken, out var claims))
                {
                    return Results.UnprocessableEntity();
                }

                var userId = claims.FindFirstValue(JwtRegisteredClaimNames.Sub);
                var user = await userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Results.UnprocessableEntity("invalid token");
                }

                if (user.ForceRelogin)
                {
                    return Results.UnprocessableEntity();
                }
                var roles = await userManager.GetRolesAsync(user);
                var accessToken = jwtTokenService.CreateAccessToken(user.UserName, user.Id, roles);
                var refreshToken = jwtTokenService.CreateRefreshToken(user.Id);
                return Results.Ok(new SuccessfulLoginDto(accessToken, refreshToken));
            });
    }
    

}

public record RegisterUserDto(string UserName, string Email, string Password);
public record LoginDto(string UserName, string Password);
public record UserDto(string UserId, string UserName, string Email);
public record SuccessfulLoginDto(string AccessToken, string RefreshToken);
public record RefreshAccessTokenDto(string RefreshToken);