using Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Persistence;
using Application.Interfaces;
using Domain.Models.Axa_Hygeia_Insurance;
using Domain.Models.Axa.Hygeia_Insurance;

namespace Infrastructure.UploadService
{
    public class FileProcessor : IFileProcessor
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ExcelPackage _excelPackage;
        private readonly string[] Columns = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N" };
        List<FileModel> excelModels = new List<FileModel>();

        public FileProcessor(ApplicationDbContext dbContext, ExcelPackage excelPackage)
        {
            _dbContext = dbContext;
            _excelPackage = excelPackage;
        }

        /// <summary>
        /// Upload an Excel File 
        /// </summary>
        /// <param name="formFile"></param>S
        /// <returns></returns>
        public async Task<List<FileModel>> ProcessUserProfileFromExcelFile(IFormFile formFile)
        {
            var excelModels = new List<FileModel>();
            using (var fileStream = new MemoryStream())
            {
                await formFile.CopyToAsync(fileStream);
                fileStream.Position = 0;
                _excelPackage.Load(fileStream);

                var ws = _excelPackage.Workbook.Worksheets[0];

                var dataValidations = _excelPackage.Workbook.Worksheets[0].DataValidations;
                var isProtected = _excelPackage.Workbook.Worksheets[0].Protection.IsProtected;
                if(dataValidations.Count > 1 && isProtected)
                {
                    //int lastUsedColumn = ws.Dimension.End.Column;

                    //Let it take only 200 users per upload
                    int lastUsedColumn = 101;

                    for (int r = 2; r <= lastUsedColumn; r++)
                    {
                        if (String.IsNullOrWhiteSpace(ws.Cells[r, 6].Value?.ToString()))
                        {
                            continue;
                        }
                        var excelModel = new FileModel();
                        excelModel.FirstName = ws.Cells[r, 1].Value?.ToString();
                        excelModel.LastName = ws.Cells[r, 2].Value?.ToString();
                        excelModel.Address = ws.Cells[r, 3].Value?.ToString();
                        excelModel.Gender = ws.Cells[r, 4].Value?.ToString();
                        excelModel.DateOfBirth = ws.Cells[r, 5].GetValue<DateTime>().ToString();
                        excelModel.Email = ws.Cells[r, 6].Value?.ToString();
                        excelModel.PhoneNumber = ws.Cells[r, 7].Value?.ToString();
                        excelModel.StateOfResidence = ws.Cells[r, 8].Value?.ToString();
                        excelModel.TownOfResidence = ws.Cells[r, 9].Value?.ToString();
                        excelModel.CareProviderName = ws.Cells[r, 10].Value?.ToString();

                        excelModels.Add(excelModel);
                    }
                }
                else
                {
                    excelModels = null;
                }
            }
            return excelModels;
        }

        public async Task<List<AxaMansardHospitalList>> UploadAxaHospitalListFromExcel(IFormFile formFile)
        {
            var excelModels = new List<AxaMansardHospitalList>();
            using (var fileStream = new MemoryStream())
            {
                await formFile.CopyToAsync(fileStream);
                fileStream.Position = 0;
                _excelPackage.Load(fileStream);
                var ws = _excelPackage.Workbook.Worksheets[0];

                for (int r = 2; r < 5000; r++)
                {
                    if (String.IsNullOrWhiteSpace(ws.Cells[r, 3].Value?.ToString()))
                    {
                        break;
                    }
                    var hosiptal = new AxaMansardHospitalList();
                    hosiptal.State = ws.Cells[r, 1].Value?.ToString().Trim();
                    hosiptal.City = ws.Cells[r, 2].Value?.ToString().Trim();
                    hosiptal.HospitalName = ws.Cells[r, 3].Value?.ToString() + " , "+ hosiptal.City;
                    hosiptal.Address = ws.Cells[r, 4].Value?.ToString();
                    hosiptal.Specialisation = ws.Cells[r, 5].Value?.ToString();
                    excelModels.Add(hosiptal);
                }
            }
            return excelModels;
        }

        public async Task<List<HygeiaHospitalList>> UploadHygeiaHospitalListFromExcel(IFormFile formFile)
        {
            var excelModels = new List<HygeiaHospitalList>();
            using (var fileStream = new MemoryStream())
            {
                await formFile.CopyToAsync(fileStream);
                fileStream.Position = 0;
                _excelPackage.Load(fileStream);
                var ws = _excelPackage.Workbook.Worksheets[0];

                for (int r = 2; r < 5000; r++)
                {
                    if (String.IsNullOrWhiteSpace(ws.Cells[r, 3].Value?.ToString()))
                    {
                        break;
                    }
                    var hosiptal = new HygeiaHospitalList();
                    hosiptal.State = ws.Cells[r, 1].Value?.ToString().Trim();
                    hosiptal.City = ws.Cells[r, 2].Value?.ToString().Trim() + " , " + hosiptal.City;
                    hosiptal.HospitalName = ws.Cells[r, 3].Value?.ToString();
                    hosiptal.Address = ws.Cells[r, 4].Value?.ToString();
                    hosiptal.Specialisation = ws.Cells[r, 5].Value?.ToString();
                    excelModels.Add(hosiptal);
                }
            }
            return excelModels;
        }

    }
}
