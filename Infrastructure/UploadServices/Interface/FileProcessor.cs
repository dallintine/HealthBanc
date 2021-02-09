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

namespace Infrastructure.UploadService.Implementation
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
        /// <param name="formFile"></param>
        /// <returns></returns>
        public async Task<List<FileModel>> ProcessExcelFile(IFormFile formFile)
        {
            var excelModels = new List<FileModel>();
            using (var fileStream = new MemoryStream())
            {
                await formFile.CopyToAsync(fileStream);
                fileStream.Position = 0;
                _excelPackage.Load(fileStream);

                var ws = _excelPackage.Workbook.Worksheets[0];

                int lastUsedColumn = ws.Dimension.End.Column;

                for (int r = 3; r <= lastUsedColumn; r++)
                {
                    if (String.IsNullOrWhiteSpace(ws.Cells[r, 5].Value?.ToString()))
                    {
                        continue;
                    }
                    //Gender: Male/Female, Dateofbirth, Premium fee, Phonenumber, Firstname,Lastname, Address.
                    var excelModel = new FileModel();
                    excelModel.FirstName = ws.Cells[r, 1].Value?.ToString();
                    excelModel.LastName = ws.Cells[r, 2].Value?.ToString();
                    excelModel.Address = ws.Cells[r, 3].Value?.ToString();
                    excelModel.Gender = ws.Cells[r, 4].Value?.ToString();
                    excelModel.DateOfBirth = ws.Cells[r, 5].Value?.ToString();
                    excelModel.PremiumFee = ws.Cells[r, 6].Value?.ToString();
                    excelModel.PhoneNumber = ws.Cells[r, 7].Value?.ToString();
                    excelModels.Add(excelModel);

                }
            }

            return excelModels;
        }

        public async Task<bool> ProcessFile(string adminEmail, FileInfo fileInfo)
        {
            using (ExcelPackage package = new ExcelPackage(fileInfo))
            {
                ExcelWorksheet workSheet = package.Workbook.Worksheets[0];//change 3 to 0 to read the first worksheet..
                //class representing all the exact data in the table;
                int totalRows = workSheet.Dimension.Rows;
                //number of all records in the table
                //loops through each row in the excel sheet
                for (int i = 3; i <= totalRows; i++)
                {
                    //loops through all the columns in the excel file {B, C, D}
                    try
                    {
                        var model = new FileModel();
                        model.FirstName = workSheet?.Cells[$"{Columns[0]}{i}"]?.Value?.ToString();
                        model.LastName = workSheet?.Cells[$"{Columns[1]}{i}"]?.Value?.ToString();
                        model.Address = workSheet?.Cells[$"{Columns[2]}{i}"]?.Value?.ToString();
                        model.Gender = workSheet?.Cells[$"{Columns[3]}{i}"]?.Value?.ToString();
                        model.DateOfBirth = workSheet?.Cells[$"{Columns[4]}{i}"]?.Value?.ToString();
                        model.PremiumFee = workSheet?.Cells[$"{Columns[5]}{i}"]?.Value?.ToString();
                        model.PhoneNumber = workSheet?.Cells[$"{Columns[6]}{i}"]?.Value?.ToString();
                        excelModels.Add(model);
                    }
                    catch (Exception ex)
                    {
                        var error = ex.Message ?? ex.InnerException.Message;
                    }
                }
            }
            try
            {
                var result = await PopulateDatabase(excelModels);
                if (result)
                {
                    System.IO.DirectoryInfo di = new DirectoryInfo(adminEmail);
                    foreach (FileInfo filesDelete in di.GetFiles())
                    {
                        filesDelete.Delete();
                    }// End Deleting files form directories
                }
                return true;
            }
            catch (Exception ex)
            {
                System.IO.DirectoryInfo di = new DirectoryInfo(adminEmail);
                foreach (FileInfo filesDelete in di.GetFiles())
                {
                    filesDelete.Delete();
                }// End Deleting files form directories
                return false;
            }
        }

        private async Task<bool> PopulateDatabase(List<FileModel> excelModels)
        {
            List<InsuranceUserProfile> insuranceUserProfiles = new List<InsuranceUserProfile>();
            InsuranceUserProfile insuranceUserProfile;
            foreach (var excelModel in excelModels)
            {
                insuranceUserProfile = await _dbContext.InsuranceUserProfiles.Where(c => c.Othernames == excelModel.FirstName && c.Surname == excelModel.LastName ).FirstOrDefaultAsync();    //&& c.Premium == excelModel.PremiumFee
                var excelCategoryExist = insuranceUserProfiles.Where(c => c.Surname == excelModel.LastName).FirstOrDefault();
                if (insuranceUserProfile == null && excelCategoryExist == null)
                {
                    var checkRecord = insuranceUserProfiles.Where(c => c.Othernames == "Not Specified").FirstOrDefault();
                    var toaddvendorCategory = new InsuranceUserProfile()
                    {
                        Id = new int(),
                        Surname = excelModel.FirstName ?? "Not Specified",
                        Othernames = excelModel.LastName ?? "Not specified",
                        ContactAddress = excelModel.Address ?? "Not specified",
                        DateOfBirth = excelModel.DateOfBirth != null ? DateTime.Parse(excelModel.DateOfBirth) : default,
                        Premium = excelModel.PremiumFee != null ? decimal.Parse(excelModel.PremiumFee) : 0,
                        PhoneNumber = excelModel.PhoneNumber ?? "Not specified",
                        CompanyProfileId = excelModel.CompanyProfileId

                    };
                    if (checkRecord != null && toaddvendorCategory.Surname == "Not Specified")
                    {
                        insuranceUserProfile = checkRecord;
                    }
                    else
                    {
                        insuranceUserProfile = toaddvendorCategory;
                        insuranceUserProfiles.Add(toaddvendorCategory);
                    }
                }
                else
                {
                    insuranceUserProfile = excelCategoryExist;
                }
            }
            try
            {
                if (insuranceUserProfiles.Count == 1)
                {
                    await _dbContext.InsuranceUserProfiles.AddAsync(insuranceUserProfiles[0]);
                    await _dbContext.SaveChangesAsync();
                    return true;
                }

                await _dbContext.AddRangeAsync(insuranceUserProfiles);
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                var error = ex.Message ?? ex.InnerException.Message;
                return false;

            }
        }
    }
}
