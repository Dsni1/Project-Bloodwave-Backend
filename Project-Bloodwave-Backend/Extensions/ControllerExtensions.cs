using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Project_Bloodwave_Backend.Extensions;

/// <summary>
/// Extension methods for controller operations
/// </summary>
public static class ControllerExtensions
{
    /// <summary>
    /// Extracts and validates the user ID from JWT claims
    /// </summary>
    /// <param name="controller">The controller instance</param>
    /// <param name="userId">Output parameter for the extracted user ID</param>
    /// <returns>An error response if validation fails; null if successful</returns>
    public static ActionResult? ValidateAndGetUserId(this ControllerBase controller, out int userId)
    {
        userId = 0;
        var userIdClaim = controller.User.FindFirst(ClaimTypes.NameIdentifier);

        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out userId))
        {
            return controller.Unauthorized(new { message = "Invalid or missing user ID in token" });
        }

        return null;
    }
}
