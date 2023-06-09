using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Logging;


namespace messaging
{
    public class ExtractCustomHeaderMiddleware
    {
        private readonly RequestDelegate _next;
        protected readonly ILogger<ExtractCustomHeaderMiddleware> _logger;

        public ExtractCustomHeaderMiddleware(RequestDelegate next, ILogger<ExtractCustomHeaderMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        } 

        public async Task InvokeAsync(HttpContext context)
        {
            const string HeaderKeyName = "MiddlewareHeaderKey";
            context.Request.Headers.TryGetValue(HeaderKeyName, out StringValues headerValue);
            if (context.Items.ContainsKey(HeaderKeyName))
            {
                context.Items[HeaderKeyName] = headerValue;
            }
            else
            {
                context.Items.Add(HeaderKeyName, $"{headerValue}-received");
            }
            foreach (string name in context.Items.Keys)
            {
                _logger.LogDebug($"The header value: {name}");            
            }


            await _next(context);
        }
    }
}