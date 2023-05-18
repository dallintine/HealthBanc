using Application.CommonDTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IImageService
    {
        Task<BaseResponse> UploadPics(UploadImageDTO image);
        BaseResponse VerifyImage(UploadImageDTO image);
    }
}
