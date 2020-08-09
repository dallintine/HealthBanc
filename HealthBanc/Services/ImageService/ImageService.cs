using HealthBanc.Infrastructure.Mail;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace HealthBanc.Services.ImageService
{
    public class ImageService : IImageService
    {
        private readonly IConfiguration _config;
        public AuthMessageSenderOption Options { get; }
        private string AzureConnectionString { get; set; }


        public ImageService(IConfiguration config, IOptions<AuthMessageSenderOption> optionAccessor)
        {
            Options = optionAccessor.Value;
            _config = config;
            AzureConnectionString = _config["AzureConnectionString"];
        }

        public async Task<string> UploadPics(string containerName, IFormFile file)
        {
            var storageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=pharmhallstracct;AccountKey=6L23/VXGOIk8QDo87OzGTs0wXbp7Vra2DPPWZ34AUGheDYtpNyCffJDW1oZZNvibJzfaYYLE+3ESaqwRryaM+g==;EndpointSuffix=core.windows.net");
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
            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=pharmhallstracct;AccountKey=6L23/VXGOIk8QDo87OzGTs0wXbp7Vra2DPPWZ34AUGheDYtpNyCffJDW1oZZNvibJzfaYYLE+3ESaqwRryaM+g==;EndpointSuffix=core.windows.net");
            CloudBlobClient _blobClient = cloudStorageAccount.CreateCloudBlobClient();
            CloudBlobContainer _cloudBlobContainer = _blobClient.GetContainerReference(containerName);
            var fileName = picturePath.Split('/');
            var pathToUse = fileName[fileName.Length - 1];
            CloudBlockBlob _blockBlob = _cloudBlobContainer.GetBlockBlobReference(pathToUse);
            //delete blob from container    
            var result = await _blockBlob.DeleteIfExistsAsync();
        }
    }
}
