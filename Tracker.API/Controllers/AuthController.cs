using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tracker.API.Services;
using Tracker.Shared.Auth;

namespace Tracker.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _authService.RegisterAsync(
                    request.Email,
                    request.Password,
                    request.FirstName,
                    request.LastName,
                    request.Role);

                if (!result.Success)
                {
                    return BadRequest(new { Errors = result.Errors });
                }

                return Ok(new
                {
                    result.Token,
                    result.RefreshToken,
                    result.UserId,
                    result.Email,
                    result.FirstName,
                    result.LastName,
                    result.Role
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration");
                return StatusCode(500, new { Errors = new[] { "An error occurred while processing your request." } });
            }
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _authService.LoginAsync(request.Email, request.Password);

                if (!result.Success)
                {
                    return BadRequest(new { Errors = result.Errors });
                }

                return Ok(new
                {
                    result.Token,
                    result.RefreshToken,
                    result.UserId,
                    result.Email,
                    result.FirstName,
                    result.LastName,
                    result.Role
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user login");
                return StatusCode(500, new { Errors = new[] { "An error occurred while processing your request." } });
            }
        }

        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _authService.RefreshTokenAsync(request.Token, request.RefreshToken);

                if (!result.Success)
                {
                    return BadRequest(new { Errors = result.Errors });
                }

                return Ok(new
                {
                    result.Token,
                    result.RefreshToken,
                    result.UserId,
                    result.Email,
                    result.FirstName,
                    result.LastName,
                    result.Role
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return StatusCode(500, new { Errors = new[] { "An error occurred while refreshing token." } });
            }
        }

        [HttpPost("revoke-token")]
        [Authorize]
        public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _authService.RevokeTokenAsync(request.Token);
                
                if (!result)
                {
                    return BadRequest(new { Errors = new[] { "Invalid token." } });
                }

                return Ok(new { Message = "Token revoked successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking token");
                return StatusCode(500, new { Errors = new[] { "An error occurred while revoking token." } });
            }
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var token = await _authService.GeneratePasswordResetTokenAsync(request.Email);
                
                if (string.IsNullOrEmpty(token))
                {
                    // Don't reveal that the user doesn't exist
                    return Ok(new { Message = "If your email is registered, you will receive a password reset link." });
                }

                // In a real application, you would send an email with the reset link
                // For now, we'll just return the token (in production, never return the token in the response)
                return Ok(new { 
                    Message = "If your email is registered, you will receive a password reset link.",
                    Token = token // Remove this in production
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating password reset token");
                return StatusCode(500, new { Errors = new[] { "An error occurred while processing your request." } });
            }
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _authService.ResetPasswordAsync(request.Email, request.Token, request.NewPassword);
                
                if (!result)
                {
                    return BadRequest(new { Errors = new[] { "Invalid token or email." } });
                }

                return Ok(new { Message = "Password has been reset successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password");
                return StatusCode(500, new { Errors = new[] { "An error occurred while resetting your password." } });
            }
        }
    }

    // Request models
    public class RegisterRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Role { get; set; } = "Client";
    }

    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class RefreshTokenRequest
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
    }

    public class RevokeTokenRequest
    {
        public string Token { get; set; }
    }

    public class ForgotPasswordRequest
    {
        public string Email { get; set; }
    }

    public class ResetPasswordRequest
    {
        public string Email { get; set; }
        public string Token { get; set; }
        public string NewPassword { get; set; }
    }
}
