namespace VideoRentingSystem.Api.Security;

/// <summary>
/// Intercepts every HTTP request, extracts the Bearer token from the Authorization
/// header, and attaches the validated AuthSession to HttpContext.Items so downstream
/// controllers can read identity without re-querying the session store.
/// </summary>
public sealed class AuthMiddleware
{
    // key used to stash the session in HttpContext.Items for later retrieval
    public const string SessionItemKey = "VrsAuthSession";

    private readonly RequestDelegate _next;

    public AuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, AuthSessionService sessionService)
    {
        // check whether the request carries an Authorization header with a Bearer scheme
        string? authHeader = context.Request.Headers.Authorization;
        if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            // strip the scheme prefix and look up the raw token in the session store
            string token = authHeader["Bearer ".Length..].Trim();
            if (sessionService.TryGetSession(token, out AuthSession? session) && session != null)
            {
                // attach to Items so controllers can trust the identity via extension method
                context.Items[SessionItemKey] = session;
            }
        }

        await _next(context);
    }
}
