using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Logging;


namespace messaging
{
    public class ExtractHeaderMiddleware
    {
        private readonly RequestDelegate _next;
        protected readonly ILogger<ExtractHeaderMiddleware> _logger;

        public ExtractHeaderMiddleware(RequestDelegate next, ILogger<ExtractHeaderMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        } 

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // examine headers
                var userid = context.Request.Headers["useraccountid"];
                if (!string.IsNullOrEmpty(userid))
                {
                    _logger.LogInformation($"UserId: {userid}");
                }
                else
                {
                    _logger.LogInformation($"UserId: not found");
                    throw new Exception("Unauthorized");
                }


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
                    throw new Exception("Unauthorized");
                }

                foreach (var header in context.Request.Headers)
                {
                    _logger.LogInformation($"Headers: {header.Key} = {header.Value}"); 
                }

                await _next(context);
            }
            catch (Exception e)
            {
                await HandleExceptionAsync(context, e.Message);
            }
        }

        private async Task HandleExceptionAsync(HttpContext httpContext, String msg)
        {
            httpContext.Response.ContentType = "application/json";
            httpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            await httpContext.Response.WriteAsync("Request Validation Has Failed. Request Access Denied");
        }
    }
}