using Application.DTO;
using Domain.Models.ExceptionLog;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Persistence;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace HealthBanc.Middleware.GlobalErrorHandling.Extensions
{
    public static class ExceptionMiddlewareExtensions
    {
        public static void ConfigureExceptionHandler(this IApplicationBuilder app, ILogger logger)
        {
            app.UseExceptionHandler(appError =>
            {
                appError.Run(async context =>
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    context.Response.ContentType = "application/json";
                    var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
                    if (contextFeature != null)
                    {
                        try
                        {
                            var exceptionLog = new ExceptionLog
                            {
                                ErrorDate = DateTime.Now,
                                ErrorCode = contextFeature.Error.HResult.ToString() ?? " ",
                                ErrorMessage = contextFeature.Error.Message ?? " ",
                                StackTrace = contextFeature.Error.StackTrace ?? " ",
                                //exceptionLog.Link = contextFeature.Error.HelpLink.ToString() ?? " ";
                                Path = context.Request.Path.Value ?? " ",
                                Source = contextFeature.Error.Source ?? " "
                            };

                            var dbContext = app.ApplicationServices.GetService<ApplicationDbContext>();

                            dbContext.ExceptionLogs.Add(exceptionLog);
                            await dbContext.SaveChangesAsync();
                        }
                        catch(Exception ex)
                        {
                            logger.Error($"Something went wrong: {ex.Message}",ex);
                        }                             

                        logger.Error($"Something went wrong: {contextFeature.Error}");
                        await context.Response.WriteAsync(new ResponseMessage()
                        {
                            ResponseCode = context.Response.StatusCode,
                            Message = "This on us, an error occurred while trying to process your request."
                        }.ToString());
                    }
                });
            });
        }
    }
}
