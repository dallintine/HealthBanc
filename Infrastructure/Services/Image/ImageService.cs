using Application.Common.DTO;
using Application.Common.ConfigSettings;
using Application.Common.Interfaces;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Image.DTO;

namespace Infrastructure.Image
{
    public class ImageService : IImageService
    {
        private readonly ILogger<ImageService> _logger;
        private readonly AzureBlobStorageSettings _blobSettings;

        public ImageService(ILogger<ImageService> logger, IOptions<AzureBlobStorageSettings> blobSettings)
        {
            _logger = logger;
            _blobSettings = blobSettings.Value;
        }

        public BaseResponse VerifyImage(UploadImageDTO image)
        {
            var validExtension = new string[] { "PNG", "JPEG", "JPG" , "SVG" };

            var extension = image.FileName.Split('.');
            if (extension.Length < 2)
            {
                _logger.LogInformation($"Verify Image Size Request Terminated [Reason: Image extension not valid | Extension : {extension}]\n");
                return BaseResponse.Failure("30","Format Error -Upload an image with a valid file extension \n");
            }
            // File extension validation
            var extensionIndex = extension.Length - 1;
            var imageExtension = extension[extensionIndex].ToUpper();
            if (!(validExtension.Any(x => x.ToUpper() == imageExtension)))
            {
                _logger.LogInformation($"Verify Image Size Request Terminated [Reason: Image extension not valid | ImageExtension : {imageExtension}]\n");
                return BaseResponse.Failure("30", $"Format Error - File name extension not valid. Valid extensions are (PNG,JPEG,JPG)" );
            }
            //Size validation
            var fileSize = (image.Base64Image.Length * (Convert.ToDouble(3) / 4)) / 1048576;
            if (fileSize > 1.2)
            {
                _logger.LogInformation($"Verify Image Size Request Terminated [Reason: Image Size too large ]\n");
                return BaseResponse.Failure("30", "Image Size is too large - Size should be less than 1MB" );
            }

            if (!IsBase64String(image.Base64Image))
            {
                _logger.LogInformation($"Verify Image Size Request Terminated [Reason: Image not a valid base 64 string ]\n");
                return BaseResponse.Failure("30",  "Image is not valid" );
            }

            return BaseResponse.Success();
        }

        public async Task<BaseResponse<ImagesURLDTO>> UploadPics(UploadImageDTO image)
        {
            var container = new BlobContainerClient(_blobSettings.AzureConnectionString, "logfolder");
            if (await container.ExistsAsync() == false)
            {
                await container.CreateIfNotExistsAsync();
            }

            await container.SetAccessPolicyAsync(Azure.Storage.Blobs.Models.PublicAccessType.BlobContainer);

            var fileName = $"{Guid.NewGuid().ToString()}_{image.FileName}";
            var blob2 = container.GetBlobClient(fileName);
            var bytes = Convert.FromBase64String(image.Base64Image);
            using (var stream = new MemoryStream(bytes))
            {
                await blob2.UploadAsync(stream);
            }
            var url = blob2.Uri.AbsoluteUri;
            var imageDTO = new ImagesURLDTO { URL = url, FileName = fileName };

            return BaseResponse<ImagesURLDTO>.Success(imageDTO);
        }

        private static bool IsBase64String(string base64)
        {
            Span<byte> buffer = new Span<byte>(new byte[base64.Length]);
            return Convert.TryFromBase64String(base64, buffer, out int bytesParsed);
        }

    }
}
