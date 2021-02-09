using Microsoft.AspNetCore.Http;
using Domain.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.UploadService
{
    public interface IFileProcessor
    {
        Task<List<FileModel>> ProcessExcelFile(IFormFile formFile);
    }
}
