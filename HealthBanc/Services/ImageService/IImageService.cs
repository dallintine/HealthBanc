using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Services.ImageService
{
    public interface IImageService
    {
        Task<string> UploadPics(string containerName, IFormFile file);

        void DeleteImage(string containerName, string picturePath);
    }
}
