using Domain.Entities.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Persistence.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Middlewares
{
    public class RequestResponseLoggerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IApplicationBuilder _app;
        private readonly ILogger _logger;

        public RequestResponseLoggerMiddleware(RequestDelegate next, IApplicationBuilder app, ILogger logger)
        {
            _next = next;
            _app = app;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                var log = new LogResponse();
                HttpRequest request = httpContext?.Request;

            log.RequestId = httpContext.TraceIdentifier;
            var ip = request.HttpContext.Connection.RemoteIpAddress;
            log.ServiceName = "HealthBanc";
            log.UserId = request.HttpContext.User.Claims.FirstOrDefault(x => x.Type == "UserId")?.Value;
            log.Port = request.Host.Port.ToString();
            log.ActionName = httpContext.Request.HttpContext.GetEndpoint()?.DisplayName;
            /*request*/
            log.RequestMethod = request.Method;
            log.Route = request.Path;
            log.QueryString = JsonConvert.SerializeObject(FormatQueries(request.QueryString.ToString()));
            log.RequestHeader = JsonConvert.SerializeObject(FormatHeaders(request.Headers));
            log.RequestDetails = await ReadBodyFromRequest(request);
            log.HostName = request.Host.ToString();
            log.ContentType = request.ContentType;

                // Temporarily replace the HttpResponseStream, which is a write-only stream, with a MemoryStream to capture it's value in-flight.
                HttpResponse response = httpContext.Response;
                var originalResponseBody = response?.Body;
                using var newResponseBody = new MemoryStream();
                response.Body = newResponseBody;

                await _next(httpContext);

                newResponseBody.Seek(0, SeekOrigin.Begin);
                var responseBodyText = await new StreamReader(response.Body).ReadToEndAsync();

                newResponseBody.Seek(0, SeekOrigin.Begin);
                await newResponseBody.CopyToAsync(originalResponseBody);

                /*response*/
                log.StatusCode = response?.StatusCode.ToString();
                log.RequestHeader = JsonConvert.SerializeObject(FormatHeaders(response.Headers));
                log.ResponseDetails = responseBodyText;
                log.DateLogged = DateTime.Now.AddDays(-1);


                var jsonString = JsonConvert.SerializeObject(log);
                var w = log;

                using var scope = _app.ApplicationServices.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                dbContext.LogResponses.Add(log);
                await dbContext.SaveChangesAsync();
            }
            catch(Exception ex)
            {
                _logger.LogError($"Error occured while trying to log Audit [Exception : {ex.ToString()}]");
                await _next(httpContext);
            }           
        }


        private static Dictionary<string, string> FormatHeaders(IHeaderDictionary headers)
        {
            Dictionary<string, string> pairs = new();
            foreach (var header in headers)
            {
                pairs.Add(header.Key, header.Value);
            }
            return pairs;
        }

        private static List<KeyValuePair<string, string>> FormatQueries(string queryString)
        {
            List<KeyValuePair<string, string>> pairs = new();
            string key, value;
            foreach (var query in queryString.TrimStart('?').Split("&"))
            {
                var items = query.Split("=");
                key = items.Count() >= 1 ? items[0] : string.Empty;
                value = items.Count() >= 2 ? items[1] : string.Empty;
                if (!String.IsNullOrEmpty(key))
                {
                    pairs.Add(new KeyValuePair<string, string>(key, value));
                }
            }
            return pairs;
        }

        private static async Task<string> ReadBodyFromRequest(HttpRequest request)
        {
            // Ensure the request's body can be read multiple times (for the next middlewares in the pipeline).
            request.EnableBuffering();
            using var streamReader = new StreamReader(request.Body, leaveOpen: true);
            var requestBody = await streamReader.ReadToEndAsync();
            // Reset the request's body stream position for next middleware in the pipeline.
            request.Body.Position = 0;
            return requestBody;
        }
    }
}
