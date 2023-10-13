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
            // examine headers
            var access_token = await content.GetTokenAsync("access_token");
            _logger.LogInformation($"Authorization: {access_token.ToString()}");
            foreach (var header in context.Request.Headers)
            {
                _logger.LogInformation($"Headers: {header.Key} = {header.Value}"); 
            }


            await _next(context);
        }
    }
}