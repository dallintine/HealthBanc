using HealthBanc.Domain.Models;
using HealthBanc.Request.AxaMansard;
using HealthBanc.Response;
using HealthBanc.ViewModels.AxaMansard;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace HealthBanc.Services.Insurance
{
    public class InsuranceService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ExcelPackage _excelPackage;

        public InsuranceService(IWebHostEnvironment environment,ExcelPackage excelPackage)
        {
            _environment = environment;
            _excelPackage = excelPackage;
        }

        public async Task<string> GetBase64(IFormFile file,string surnName)
        {
            try
            {
                if (file.Length > 0)
                {
                    var fileName = file.FileName;
                    var myUniqueFileName = Convert.ToString(Guid.NewGuid()).Substring(0, 5);
                    //var fileExtension = Path.GetExtension(fileName);
                    ////var name = string.Concat( surnName, myUniqueFileName);
                    //var newFileName = string.Concat(myUniqueFileName, fileExtension);

                    var fileDirectoryPath = Path.Combine(_environment.WebRootPath, "insurance");
                    var filePath = Path.Combine(_environment.WebRootPath, "insurance",myUniqueFileName);
                    if (!System.IO.Directory.Exists(fileDirectoryPath))
                    {
                        Directory.CreateDirectory(fileDirectoryPath);
                    }
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                    var base64String = Convert.ToBase64String(File.ReadAllBytes(filePath));
                    File.Delete(filePath);
                    return base64String;
                }
                return "false";
            }
            catch (Exception ex)
            {
                return "false";
            }
        }

        public async Task<List<TestUserProfile>> CBNList(IFormFile formFile)
        {
            var excelModels = new List<TestUserProfile>();
            using (var fileStream = new MemoryStream())
            {
                await formFile.CopyToAsync(fileStream);
                fileStream.Position = 0;
                _excelPackage.Load(fileStream);
                var ws = _excelPackage.Workbook.Worksheets[0];
                for (int r = 3; r < 331; r++)
                {
                    if(ws.Cells[r, 2].Value?.ToString() == "" || ws.Cells[r, 2].Value?.ToString() == null)
                    {
                        break;
                    }
                    var profile = new TestUserProfile();
                    profile.FirstName = ws.Cells[r, 2].Value?.ToString();
                    profile.SurnName = ws.Cells[r, 3].Value?.ToString();
                    profile.OtherName = ws.Cells[r, 4].Value?.ToString();
                    profile.MaritalStatus = ws.Cells[r, 5].Value?.ToString();
                    profile.Address = ws.Cells[r, 6].Value?.ToString();
                    profile.DateOfBirth = ws.Cells[r, 7].Value?.ToString();
                    profile.NextofKin = ws.Cells[r, 8].Value?.ToString();
                    profile.PhoneNumber = ws.Cells[r, 9].Value?.ToString();
                    profile.Plan = ws.Cells[r, 10].Value?.ToString();
                    profile.Sex = ws.Cells[r, 11].Value?.ToString();

                    profile.TransId = GetUniqueCode(10);

                    excelModels.Add(profile);
                }
            }
            return excelModels;
        }

        public string GetUniqueCode(int nCount)
        {
            string uniqueNumber = string.Empty;
            while (uniqueNumber.Length < nCount)
            {
                string varWW0 = uniqueNumber;
                Guid guid = Guid.NewGuid();
                uniqueNumber = varWW0 + RefineString(guid.ToString().Replace("-", ""));
            }

            if (uniqueNumber.Length > nCount)
            {
                uniqueNumber = uniqueNumber.Substring(0, nCount);
            }
            return uniqueNumber;
        }

        private static string RefineString(string input)
        {
            string str_ = string.Empty;
            char[] arr_ = input.ToCharArray();
            for (int i = 0; i < arr_.Length; i++)
            {
                char chr_ = arr_[i];
                if (char.IsDigit(chr_))
                {
                    str_ += chr_;
                }
            }

            return str_;
        }

    }
}
