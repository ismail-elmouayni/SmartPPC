using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartPPC.Core.Domain;
using System.ComponentModel.DataAnnotations;
using Api;

namespace SmartPPC.Api.Endpoints;

public static class AuthenticationEndpoints
{
    public static void MapAuthenticationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var authGroup = endpoints.MapGroup("/api/auth")
            .WithTags("Authentication");

        authGroup.MapPost("/login", Login)
            .WithName("Login");

        authGroup.MapPost("/register", Register)
            .WithName("Register");

        authGroup.MapPost("/logout", Logout)
            .WithName("Logout");
    }

    private static async Task<IResult> Login(
        [FromBody] LoginRequest request,
        SignInManager<User> signInManager,
        ILogger<Program> logger)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(new { error = "Email and password are required." });
        }

        try
        {
            var result = await signInManager.PasswordSignInAsync(
                request.Email,
                request.Password,
                isPersistent: request.RememberMe,
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                logger.LogInformation("User {Email} logged in successfully", request.Email);
                return Results.Ok(new { success = true });
            }

            if (result.IsLockedOut)
            {
                logger.LogWarning("User {Email} account locked out", request.Email);
                return Results.Unauthorized();
            }

            if (result.RequiresTwoFactor)
            {
                logger.LogInformation("User {Email} requires two-factor authentication", request.Email);
                return Results.BadRequest(new { error = "Two-factor authentication required." });
            }

            logger.LogWarning("Invalid login attempt for user {Email}", request.Email);
            return Results.Unauthorized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during login for user {Email}", request.Email);
            return Results.Problem("An error occurred during login. Please try again.");
        }
    }

    private static async Task<IResult> Register(
        [FromBody] RegisterRequest request,
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        ILogger<Program> logger)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(new { error = "Email and password are required." });
        }

        if (request.Password != request.ConfirmPassword)
        {
            return Results.BadRequest(new { error = "Passwords do not match." });
        }

        try
        {
            var user = new User
            {
                UserName = request.Email,
                Email = request.Email,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(user, request.Password);

            if (result.Succeeded)
            {
                logger.LogInformation("User {Email} created successfully", request.Email);

                // Sign in the user automatically
                await signInManager.SignInAsync(user, isPersistent: false);

                return Results.Ok(new { success = true });
            }

            var errors = result.Errors.Select(e => e.Description).ToList();
            logger.LogWarning("User registration failed for {Email}: {Errors}", request.Email, string.Join(", ", errors));
            return Results.BadRequest(new { error = "Registration failed", errors });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during registration for user {Email}", request.Email);
            return Results.Problem("An error occurred during registration. Please try again.");
        }
    }

    private static async Task<IResult> Logout(
        SignInManager<User> signInManager,
        ILogger<Program> logger)
    {
        try
        {
            await signInManager.SignOutAsync();
            logger.LogInformation("User logged out successfully");
            return Results.Ok(new { success = true });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during logout");
            return Results.Problem("An error occurred during logout. Please try again.");
        }
    }
}

// Request models
public record LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;

    public bool RememberMe { get; init; } = false;
}

public record RegisterRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; init; } = string.Empty;

    [Required]
    [Compare(nameof(Password))]
    public string ConfirmPassword { get; init; } = string.Empty;
}
