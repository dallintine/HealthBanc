using Application.CommonDTO;
using Domain.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Persistence.Data;
using System.Net;

namespace Infrastructure.Middlewares
{
    public static class ExceptionMiddlewareExtensions
    {
        public static void ConfigureExceptionHandler(this IApplicationBuilder app, ILogger logger)
        {
            app.UseExceptionHandler(appError =>
            {
                appError.Run(async context =>
                {
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    context.Response.ContentType = "application/json";
                    var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
                    if (contextFeature != null)
                    {
                        try
                        {
                            var errorLog = new ErrorLog
                            {
                                ErrorCode = contextFeature.Error.HResult.ToString(),
                                ErrorMessage = contextFeature.Error.Message,
                                StackTrace = contextFeature.Error.StackTrace,
                                Path = context.Request.Path.Value,
                                Source = contextFeature.Error.Source
                            };

                            var dbContext = app.ApplicationServices.GetService<ApplicationDbContext>();

                            dbContext.ErrorLogs.Add(errorLog);
                            await dbContext.SaveChangesAsync();
                        }
                        catch (Exception ex)
                        {
                            logger.LogError($"Something went wrong: {ex.Message}", ex);
                        }

                        logger.LogError($"Something went wrong: {contextFeature.Error}");
                        await context.Response.WriteAsync(BaseResponse.Failure("99", "This on us, an error occurred while trying to process your request.Please try again later").ToString());
                    }
                });
            });
        }
    }
}
