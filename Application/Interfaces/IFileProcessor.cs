using Microsoft.AspNetCore.Http;
using Domain.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Domain.Models.AxaMansard_Insurance;
using Domain.Models.Axa.Hygeia_Insurance;

namespace Application.Interfaces
{
    public interface IFileProcessor
    {
        Task<List<FileModel>> ProcessUserProfileFromExcelFile(IFormFile formFile);

        Task<List<AxaMansardHospitalList>> UploadAxaHospitalListFromExcel(IFormFile formFile);
        Task<List<HygeiaHospitalList>> UploadHygeiaHospitalListFromExcel(IFormFile formFile);
    }
}
