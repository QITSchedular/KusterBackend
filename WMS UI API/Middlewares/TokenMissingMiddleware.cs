namespace WMS_UI_API.Middlewares
{
    public class TokenMissingMiddleware : IMiddleware
    {
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (!context.Request.Headers.ContainsKey("Authorization"))
            {
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("Token is missing. Please provide a valid token.");
                return;
            }

            await next(context);
        }
    }
}
