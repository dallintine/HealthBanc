using Application.DTO;
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

        public async Task<List<string>> ListFiles(string containerName,string prefix)
        {
            List<string> blobs = new List<string>();
            var storageAccount = CloudStorageAccount.Parse(ImageAzureConnectionString.AzureConnectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(containerName);
            BlobResultSegment resultSegment = await container.ListBlobsSegmentedAsync(prefix,null);

            foreach (IListBlobItem item in resultSegment.Results)
            {
                if (item.GetType() == typeof(CloudBlockBlob))
                {
                    CloudBlockBlob blob = (CloudBlockBlob)item;
                    blobs.Add(blob.Name);
                }
                else if (item.GetType() == typeof(CloudPageBlob))
                {
                    CloudPageBlob blob = (CloudPageBlob)item;
                    blobs.Add(blob.Name);
                }
                else if (item.GetType() == typeof(CloudBlobDirectory))
                {
                    CloudBlobDirectory dir = (CloudBlobDirectory)item;
                    blobs.Add(dir.Uri.ToString());
                }
                else if (item.GetType() == typeof(CloudAppendBlob))
                {
                    CloudAppendBlob dir = (CloudAppendBlob)item;
                    blobs.Add(dir.Name);
                }
            }
            return blobs;
        }

        public async void DeleteImage(string containerName, string picturePath)
        {
            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(ImageAzureConnectionString.AzureConnectionString);
            CloudBlobClient _blobClient = cloudStorageAccount.CreateCloudBlobClient();
            CloudBlobContainer _cloudBlobContainer = _blobClient.GetContainerReference(containerName);
            //var fileName = picturePath.Split('/');
            //var pathToUse = fileName[fileName.Length - 1];
            CloudBlockBlob _blockBlob = _cloudBlobContainer.GetBlockBlobReference(picturePath);
            //delete blob from container    
            
            var result = await _blockBlob.DeleteIfExistsAsync();
        }

        public ResponseMessage ConvertImageToBase64(IFormFile file)
        {
            var validImageExtension = new [] { ".JPG",".JPEG", ".JPE", ".BMP", ".GIF", ".PNG" };

            var fileExtension = System.IO.Path.GetExtension(file.FileName.ToUpper());
            if (!validImageExtension.Contains(fileExtension))
            {
                return new ResponseMessage { Message = "Image type is not supported - Only upload PNG/JPEG/JPG/JPE" };
            }

            var fileSize = file.Length;
            if((fileSize/1048576) > 2.1)
            {
                return new ResponseMessage { Message = "Image Size is too large - Size should be less than 2MB" };
            }
            if (file.Length > 0)
            {
                using (var ms = new MemoryStream())
                {
                    file.CopyTo(ms);
                    var fileBytes = ms.ToArray();
                    string base64Image = Convert.ToBase64String(fileBytes);
                    return new ResponseMessage {Status = true, Data= base64Image };
                }
            }
            return new ResponseMessage { Status = false, Message= "Image cannot be processed,please try again later" };
        }
    }
}
