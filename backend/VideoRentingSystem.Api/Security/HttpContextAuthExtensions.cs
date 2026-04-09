namespace VideoRentingSystem.Api.Security;

/// <summary>
/// Extension method for HttpContext that retrieves the AuthSession
/// attached by <see cref="AuthMiddleware"/> during the pipeline.
/// </summary>
public static class HttpContextAuthExtensions
{
    // safely attempts to pull the validated session from HttpContext.Items
    public static bool TryGetAuthSession(this HttpContext context, out AuthSession? session)
    {
        if (context.Items.TryGetValue(AuthMiddleware.SessionItemKey, out object? value) && value is AuthSession s)
        {
            session = s;
            return true;
        }

        session = null;
        return false;
    }
}
