using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.Interfaces
{
    public interface ITemplateService
    {
        byte[] GeneratePDF_ParseXhtml(string html);
        Task<string> RenderAsync<TViewModel>(string filename, TViewModel viewModel);
    }
}
