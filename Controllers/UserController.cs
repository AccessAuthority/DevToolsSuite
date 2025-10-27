using DevToolsSuite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DevToolsSuite.Controllers
{
    [ApiController]
    [Route("api/user")]
    public class UserController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UserController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            // Check if user is authenticated
            if (!User.Identity?.IsAuthenticated ?? false)
            {
                return Ok(new
                {
                    isAuthenticated = false,
                    message = "User not logged in"
                });
            }

            // Get user details
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "User not found"
                });
            }

            // Get assigned roles
            var roles = await _userManager.GetRolesAsync(user);

            // Return structured profile data
            return Ok(new
            {
                success = true,
                isAuthenticated = true,
                id = user.Id,
                name = user.DisplayName ?? user.UserName,
                email = user.Email,
                roles,
                isAdmin = roles.Contains("Admin"),
                createdAt = user.CreatedAt,
                lastLogin = user.LastLogin,
                avatar = $"/images/avatars/{user.Id}.png"
            });
        }
        [HttpPost("profile/update")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest model)
        {
            if (!User.Identity?.IsAuthenticated ?? false)
                return Unauthorized(new { success = false, message = "User not logged in" });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return NotFound(new { success = false, message = "User not found" });

            // Update profile fields
            user.DisplayName = model.DisplayName ?? user.DisplayName;
            user.JobTitle = model.JobTitle ?? user.JobTitle;
            user.Company = model.Company ?? user.Company;
            user.Bio = model.Bio ?? user.Bio;
            user.Website = model.Website ?? user.Website;
            user.Location = model.Location ?? user.Location;

            await _userManager.UpdateAsync(user);

            return Ok(new
            {
                success = true,
                message = "Profile updated successfully",
                updatedAt = DateTime.UtcNow
            });
        }

        public class UpdateProfileRequest
        {
            public string? DisplayName { get; set; }
            public string? JobTitle { get; set; }
            public string? Company { get; set; }
            public string? Bio { get; set; }
            public string? Website { get; set; }
            public string? Location { get; set; }
        }

    }
}
