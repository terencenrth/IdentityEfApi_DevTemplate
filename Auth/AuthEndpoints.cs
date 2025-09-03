using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace IdentityEfApi.Auth
{
    public static class AuthEndpoints
    {
        public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapPost("/auth/register", async (
                [FromBody] RegisterRequest req,
                UserManager<ApplicationUser> userManager) =>
            {
                var user = new ApplicationUser { UserName = req.Email, Email = req.Email };
                var result = await userManager.CreateAsync(user, req.Password);
                if (!result.Succeeded)
                    return Results.BadRequest(result.Errors);

                return Results.Ok();
            });

            app.MapPost("/auth/login", async (
                [FromBody] LoginRequest req,
                UserManager<ApplicationUser> userManager,
                SignInManager<ApplicationUser> signInManager,
                IConfiguration config) =>
            {
                var user = await userManager.Users.FirstOrDefaultAsync(u => u.Email == req.Email);
                if (user == null) return Results.Unauthorized();

                var pwd = await signInManager.CheckPasswordSignInAsync(user, req.Password, false);
                if (!pwd.Succeeded) return Results.Unauthorized();

                var jwtOptions = config.GetSection("Jwt").Get<JwtOptions>()!;
                var claims = new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                    new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
                    new Claim(ClaimTypes.NameIdentifier, user.Id)
                };

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                var token = new JwtSecurityToken(
                    issuer: jwtOptions.Issuer,
                    audience: jwtOptions.Audience,
                    claims: claims,
                    expires: DateTime.UtcNow.AddHours(2),
                    signingCredentials: creds
                );

                var jwt = new JwtSecurityTokenHandler().WriteToken(token);
                return Results.Ok(new { token = jwt });
            });

            return app;
        }

        public record RegisterRequest(string Email, string Password);
        public record LoginRequest(string Email, string Password);
    }
}
