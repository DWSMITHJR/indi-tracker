using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Tracker.Infrastructure.Data;
using Tracker.Infrastructure.Models;
using Tracker.Shared.Auth;

namespace Tracker.API.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly JwtSettings _jwtSettings;
        private readonly ApplicationDbContext _context;

        public AuthService(
            UserManager<User> userManager,
            IOptions<JwtSettings> jwtSettings,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _jwtSettings = jwtSettings.Value;
            _context = context;
        }

        public async Task<AuthResult> RegisterAsync(string email, string password, string firstName, string lastName, string role = "Client")
        {
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                return new AuthResult
                {
                    Success = false,
                    Errors = new[] { "User with this email already exists." }
                };
            }

            var newUser = new User
            {
                Email = email,
                UserName = email,
                FirstName = firstName,
                LastName = lastName,
                IsActive = true
            };

            var createdUser = await _userManager.CreateAsync(newUser, password);
            if (!createdUser.Succeeded)
            {
                return new AuthResult
                {
                    Success = false,
                    Errors = createdUser.Errors.Select(x => x.Description)
                };
            }

            // Add user to role
            var roleResult = await _userManager.AddToRoleAsync(newUser, role);
            if (!roleResult.Succeeded)
            {
                // If adding to role fails, delete the user to maintain data consistency
                await _userManager.DeleteAsync(newUser);
                return new AuthResult
                {
                    Success = false,
                    Errors = roleResult.Errors.Select(x => x.Description)
                };
            }

            // Generate JWT token
            var token = await GenerateJwtToken(newUser);
            return new AuthResult
            {
                Success = true,
                Token = token,
                RefreshToken = GenerateRefreshToken(),
                UserId = newUser.Id.ToString(),
                Email = newUser.Email,
                FirstName = newUser.FirstName,
                LastName = newUser.LastName,
                Role = role
            };
        }

        public async Task<AuthResult> LoginAsync(string email, string password)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return new AuthResult
                {
                    Success = false,
                    Errors = new[] { "User does not exist." }
                };
            }

            if (!user.IsActive)
            {
                return new AuthResult
                {
                    Success = false,
                    Errors = new[] { "This account has been deactivated." }
                };
            }

            var userHasValidPassword = await _userManager.CheckPasswordAsync(user, password);
            if (!userHasValidPassword)
            {
                // Increment failed login count and lock account if necessary
                user.AccessFailedCount++;
                if (user.AccessFailedCount >= 5)
                {
                    user.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(15); // Lock for 15 minutes
                    await _userManager.UpdateAsync(user);
                    
                    return new AuthResult
                    {
                        Success = false,
                        Errors = new[] { "Account locked due to multiple failed login attempts. Please try again in 15 minutes." }
                    };
                }
                
                await _userManager.UpdateAsync(user);
                
                return new AuthResult
                {
                    Success = false,
                    Errors = new[] { "Invalid credentials." }
                };
            }

            // Reset failed login count on successful login
            user.AccessFailedCount = 0;
            user.LastLogin = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            // Get user roles
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "User";

            // Generate JWT token
            var token = await GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();
            
            // Store refresh token in the database
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationInDays);
            await _userManager.UpdateAsync(user);

            return new AuthResult
            {
                Success = true,
                Token = token,
                RefreshToken = refreshToken,
                UserId = user.Id.ToString(),
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = role
            };
        }

        public async Task<AuthResult> RefreshTokenAsync(string token, string refreshToken)
        {
            var principal = GetPrincipalFromExpiredToken(token);
            if (principal?.Identity?.Name == null)
            {
                return new AuthResult { Success = false, Errors = new[] { "Invalid token." } };
            }

            var user = await _userManager.FindByEmailAsync(principal.Identity.Name);
            if (user == null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return new AuthResult { Success = false, Errors = new[] { "Invalid refresh token." } };
            }

            var newToken = await GenerateJwtToken(user);
            var newRefreshToken = GenerateRefreshToken();
            
            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationInDays);
            await _userManager.UpdateAsync(user);

            return new AuthResult
            {
                Success = true,
                Token = newToken,
                RefreshToken = newRefreshToken,
                UserId = user.Id.ToString(),
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "User"
            };
        }

        public async Task<bool> RevokeTokenAsync(string token)
        {
            var principal = GetPrincipalFromExpiredToken(token);
            if (principal?.Identity?.Name == null) return false;

            var user = await _userManager.FindByEmailAsync(principal.Identity.Name);
            if (user == null) return false;

            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            await _userManager.UpdateAsync(user);
            return true;
        }

        public async Task Logout()
        {
            // This method is intentionally left empty as the actual logout logic
            // is handled by the client-side authentication state provider.
            // The client will remove the token from local storage.
            await Task.CompletedTask;
        }

        public async Task<string> GeneratePasswordResetTokenAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return null;

            return await _userManager.GeneratePasswordResetTokenAsync(user);
        }

        public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return false;

            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            return result.Succeeded;
        }

        private async Task<string> GenerateJwtToken(User user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "User";

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.GivenName, user.FirstName),
                new Claim(ClaimTypes.Surname, user.LastName),
                new Claim(ClaimTypes.Role, role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes);

            var token = new JwtSecurityToken(
                _jwtSettings.Issuer,
                _jwtSettings.Audience,
                claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret)),
                ValidateLifetime = false // We want to get the expired token
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
            if (securityToken is not JwtSecurityToken jwtSecurityToken || 
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Invalid token");
            }

            return principal;
        }
    }
}
