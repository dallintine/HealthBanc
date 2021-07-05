using Application.Interfaces;
using Infrastructure.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Infrastructure.ImageService
{
    public class ImageService : IImageService
    {
        private readonly IWebHostEnvironment _environment;

        public ImageStorage ImageAzureConnectionString { get; }

        public ImageService(IOptions<ImageStorage> imageAccessor, IWebHostEnvironment environment)
        {            
            ImageAzureConnectionString = imageAccessor.Value;
            _environment = environment;
        }

        public async Task<string> UploadPics(string containerName, IFormFile file)
        {
            var storageAccount = CloudStorageAccount.Parse(ImageAzureConnectionString.AzureConnectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(containerName);
            if (await container.ExistsAsync() == false)
            {
                await container.CreateIfNotExistsAsync();
            }
            BlobContainerPermissions permissions = await container.GetPermissionsAsync();
            permissions.PublicAccess = BlobContainerPublicAccessType.Container;
            await container.SetPermissionsAsync(permissions);

            var content = ContentDispositionHeaderValue.Parse(file.ContentDisposition);
            var fileName = content.FileName.Trim('"');
            var blockBlob = container.GetBlockBlobReference(fileName);
            await blockBlob.UploadFromStreamAsync(file.OpenReadStream());
            return blockBlob.Uri.ToString();
        }

        public async void DeleteImage(string containerName, string picturePath)
        {
            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(ImageAzureConnectionString.AzureConnectionString);
            CloudBlobClient _blobClient = cloudStorageAccount.CreateCloudBlobClient();
            CloudBlobContainer _cloudBlobContainer = _blobClient.GetContainerReference(containerName);
            var fileName = picturePath.Split('/');
            var pathToUse = fileName[fileName.Length - 1];
            CloudBlockBlob _blockBlob = _cloudBlobContainer.GetBlockBlobReference(pathToUse);
            //delete blob from container    
            var result = await _blockBlob.DeleteIfExistsAsync();
        }

        public string ConvertImageToBase64(IFormFile file)
        {
            string wwwPath = _environment.WebRootPath;

            string path = Path.Combine(wwwPath, "InsuranceUploads");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string fileName = file.FileName;
            var newPath = Path.Combine(path, fileName);
            using (FileStream stream = new FileStream(newPath, FileMode.Create))
            {
                file.CopyTo(stream);
                stream.Flush();
            }
            Byte[] bytes = File.ReadAllBytes(Path.Combine(path, fileName));
            String r = Convert.ToBase64String(bytes);
            return r;
            //if (file.Length > 0)
            //{
            //    using (var ms = new MemoryStream())
            //    {
            //        file.CopyTo(ms);
            //        var fileBytes = ms.ToArray();
            //        string s = Convert.ToBase64String(fileBytes);
            //        return s;
            //    }
            //}
            //return "false";
        }
    }
}
