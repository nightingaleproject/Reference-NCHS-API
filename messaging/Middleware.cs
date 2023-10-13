using System;
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
            var userid = context.Request.Headers["useraccountid"];
            _logger.LogInformation($"UserId: {userid}");

            var userinfo = context.Request.Headers["userinfo"];
            if (!String.IsNullOrEmpty(userinfo))
            {
                byte[] data = Convert.FromBase64String(userinfo);
                string decodedString = System.Text.Encoding.UTF8.GetString(data);
                _logger.LogInformation($"UserInfo: {decodedString}");
            }
            else
            {
                _logger.LogInformation($"UserInfo: not found");
            }

            foreach (var header in context.Request.Headers)
            {
                _logger.LogInformation($"Headers: {header.Key} = {header.Value}"); 
            }


            await _next(context);
        }
    }
}