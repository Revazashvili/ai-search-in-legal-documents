using LegalDocumentAISearch.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace LegalDocumentAISearch.Api.Endpoints.Admin;

public static class UserAdminEndpoints
{
    public static IEndpointRouteBuilder MapUserAdminEndpoints(this IEndpointRouteBuilder group)
    {
        group.MapGet("/users", ListUsers)
            .RequireAuthorization()
            .WithName("ListUsers");

        group.MapPost("/users", CreateUser)
            .RequireAuthorization()
            .WithName("CreateUser");

        group.MapDelete("/users/{id}", DeleteUser)
            .RequireAuthorization()
            .WithName("DeleteUser");

        return group;
    }

    private record UserResponse(string Id, string Email, DateTimeOffset? LockoutEnd);
    private record CreateUserRequest(string Email, string Password);

    private static async Task<IResult> ListUsers(UserManager<AdminUser> userManager)
    {
        var users = userManager.Users
            .Select(u => new UserResponse(u.Id, u.Email!, u.LockoutEnd))
            .ToList();

        return Results.Ok(users);
    }

    private static async Task<IResult> CreateUser(
        CreateUserRequest request,
        UserManager<AdminUser> userManager)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return Results.BadRequest("Email and password are required.");

        var user = new AdminUser { UserName = request.Email, Email = request.Email };
        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToArray();
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [""] = errors
            });
        }

        return Results.Created($"/api/admin/users/{user.Id}", new UserResponse(user.Id, user.Email, null));
    }

    private static async Task<IResult> DeleteUser(
        string id,
        HttpContext context,
        UserManager<AdminUser> userManager)
    {
        var currentUserId = userManager.GetUserId(context.User);
        if (string.Equals(id, currentUserId, StringComparison.OrdinalIgnoreCase))
            return Results.BadRequest("Cannot delete your own account.");

        var user = await userManager.FindByIdAsync(id);
        if (user is null)
            return Results.NotFound();

        var result = await userManager.DeleteAsync(user);
        return result.Succeeded ? Results.NoContent() : Results.Problem("Failed to delete user.");
    }
}
