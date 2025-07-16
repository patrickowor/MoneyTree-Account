using Moneytree.Account.Src.Internal.Schemas;

namespace Moneytree.Account.Src.Http.Middlewares;

public class GetUserFromClaimsMiddleware
{
    private readonly RequestDelegate _next;

    public GetUserFromClaimsMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            // Extract claims
            var userId = context.User.FindFirst("user_id")?.Value; // or "userId" or whatever key you're using
            var tokenType = context.User.FindFirst("token_type")?.Value;

            // Attach to HttpContext.Items
            if (userId != null && tokenType != null)
                context.Items["user"] = new TokenInfo(userId, tokenType);
        }

        await _next(context);
    }
}
