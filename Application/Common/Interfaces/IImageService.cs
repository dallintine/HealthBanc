using Application.Common.DTO;
using Application.CommonDTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.Interfaces
{
    public interface IImageService
    {
        Task<BaseResponse<ImagesURLDTO>> UploadPics(UploadImageDTO image);
        BaseResponse VerifyImage(UploadImageDTO image);
    }
}
