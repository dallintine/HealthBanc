using Application.Common.Interfaces;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.tool.xml;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Infrastructure.TemplateService
{
    public class RazorViewsTemplateService : ITemplateService
    {
        private IRazorViewEngine _viewEngine;
        private readonly IServiceProvider _serviceProvider;
        private readonly ITempDataProvider _tempDataProvider;
        private readonly ILogger<RazorViewsTemplateService> _logger;

        public RazorViewsTemplateService(IRazorViewEngine viewEngine, IServiceProvider serviceProvider, ITempDataProvider tempDataProvider, ILogger<RazorViewsTemplateService> logger)
        {
            _viewEngine = viewEngine;
            _serviceProvider = serviceProvider;
            _tempDataProvider = tempDataProvider;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> RenderAsync<TViewModel>(string filename, TViewModel viewModel)
        {
            var httpContext = new DefaultHttpContext
            {
                RequestServices = _serviceProvider
            };

            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            await using var outputWriter = new StringWriter();
            var viewResult = _viewEngine.FindView(actionContext, filename, false);
            var viewDictionary = new ViewDataDictionary<TViewModel>(new EmptyModelMetadataProvider(), new ModelStateDictionary())
            {
                Model = viewModel
            };

            var tempDataDictionary = new TempDataDictionary(httpContext, _tempDataProvider);
            if (!viewResult.Success)
            {
                throw new KeyNotFoundException(
                    $"Could not render the HTML, because {filename} template does not exist");
            }

            try
            {
                var viewContext = new ViewContext(actionContext, viewResult.View, viewDictionary,
                    tempDataDictionary, outputWriter, new HtmlHelperOptions());

                await viewResult.View.RenderAsync(viewContext);
                return outputWriter.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not render the HTML because of an error");
                return string.Empty;
            }
        }

         public byte[] GeneratePDF_ParseXhtml(string html)
         {
            _logger.LogInformation($"Generating PDF File");
            try
            {
                StringReader reader = new(html);
                Document pdfDoc = new(PageSize.A4);
                using (MemoryStream memoryStream = new())
                {
                    var writer = PdfWriter.GetInstance(pdfDoc, memoryStream);
                    pdfDoc.Open();
                    XMLWorkerHelper.GetInstance().ParseXHtml(writer, pdfDoc, reader);
                    pdfDoc.Close();
                    byte[] bytes = memoryStream.ToArray();
                    memoryStream.Close();
                    return bytes;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occured while trying to generate PDF - [Exception | {ex.ToString()}]");
                return new byte[0];
            }
         }
    }
}
