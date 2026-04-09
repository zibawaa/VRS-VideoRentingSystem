using Microsoft.AspNetCore.Mvc;
using VideoRentingSystem.Api.Bootstrap;
using VideoRentingSystem.Api.Contracts.Auth;
using VideoRentingSystem.Api.Security;
using VideoRentingSystem.Core.Models;

namespace VideoRentingSystem.Api.Controllers;

/// <summary>
/// Handles user registration, login, and logout via lightweight bearer tokens.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly StoreRuntime _runtime;
    private readonly AuthSessionService _sessionService;

    public AuthController(StoreRuntime runtime, AuthSessionService sessionService)
    {
        _runtime = runtime;
        _sessionService = sessionService;
    }

    [HttpPost("register")]
    public ActionResult<AuthResponse> Register([FromBody] RegisterRequest request)
    {
        // reject empty credentials before touching the store
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Username and password are required.");
        }

        // validate the role string resolves to a known enum value
        if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out UserRole role))
        {
            return BadRequest("Role must be Customer or Publisher.");
        }

        // public registration only allows customer and publisher accounts
        if (role == UserRole.Admin)
        {
            return BadRequest("Admin registration is not exposed by this endpoint.");
        }

        // register and then immediately log in under a lock so no concurrent call
        // can race between the insert and the lookup
        User? user;
        lock (_runtime.SyncRoot)
        {
            bool registered = _runtime.UserStore.RegisterUser(request.Username, request.Password, role, request.StudioName);
            if (!registered)
            {
                return Conflict("Registration failed. Username may already exist.");
            }

            user = _runtime.UserStore.Login(request.Username, request.Password);
        }

        // guard against the edge case where registration succeeded but login missed the row
        if (user == null)
        {
            return Unauthorized("Registration succeeded but login failed unexpectedly.");
        }

        // mint a new session token and hand it back to the caller
        AuthSession session = _sessionService.CreateSession(user);
        return Ok(ToAuthResponse(session));
    }

    [HttpPost("login")]
    public ActionResult<AuthResponse> Login([FromBody] LoginRequest request)
    {
        // reject empty credentials early
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Username and password are required.");
        }

        // authenticate against the in-memory user index under lock
        User? user;
        lock (_runtime.SyncRoot)
        {
            user = _runtime.UserStore.Login(request.Username, request.Password);
        }

        if (user == null)
        {
            return Unauthorized("Invalid username or password.");
        }

        // issue a fresh bearer token for the authenticated user
        AuthSession session = _sessionService.CreateSession(user);
        return Ok(ToAuthResponse(session));
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        // require a valid session before attempting revocation
        if (!HttpContext.TryGetAuthSession(out AuthSession? session) || session == null)
        {
            return Unauthorized("Missing or invalid bearer token.");
        }

        // destroy the session so the token can never be reused
        _sessionService.Revoke(session.Token);
        return NoContent();
    }

    // maps an internal session object to the public API response shape
    private static AuthResponse ToAuthResponse(AuthSession session)
    {
        return new AuthResponse
        {
            Token = session.Token,
            UserId = session.UserId,
            Username = session.Username,
            Role = session.Role.ToString(),
            StudioName = session.StudioName,
            ExpiresAtUtc = session.ExpiresAtUtc
        };
    }
}
