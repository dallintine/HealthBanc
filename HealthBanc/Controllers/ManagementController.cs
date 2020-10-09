using HealthBanc.Data;
using HealthBanc.Services.ADOTP;
using HealthBanc.Services.Insurance;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Controllers
{
    [Route("v1/api/[controller]")]
    [ApiController]
    public class ManagementController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly InsuranceService _insuranceService;
        private readonly ILogger<ManagementController> _logger;
        private readonly IBackendOTPService _iBAckend;

        public ManagementController(ApplicationDbContext dbContext, InsuranceService insuranceService,ILogger<ManagementController> logger, IBackendOTPService iBAckend)
        {
            _dbContext = dbContext;
            _insuranceService = insuranceService;
            _logger = logger;
            _iBAckend = iBAckend;
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> Error(string OPP)
        {
            var x =await _iBAckend.OtpValidationAsync(OPP, "Hassannh");
            _logger.LogError(x.ToString());
            return Ok(x);
        }
        [HttpGet("[action]")]
        public async Task<IActionResult> Delete()
        {
            var test = await _dbContext.TestUserProfiles.ToListAsync();
            _dbContext.TestUserProfiles.RemoveRange(test);
            await _dbContext.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            var result = await _insuranceService.CBNList(file);
            await _dbContext.TestUserProfiles.AddRangeAsync(result);
            await _dbContext.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetExcelFromDB()
        {
            try
            {
                //step1: create array to holder header labels
                string[] col_names = new string[]{
                "S/N",
                "TRANSID",
                "FIRSTNAME",
                "SURNAME",
                "OTHER NAMES",
                "MARITAL STATUS",
                "ADDRESS",
                "DATE OF BIRTH",
                "NEXT OF KIN",
                "PHONE NUMBER",
                "PLAN",
                "SEX"
            };

                var userList = await _dbContext.TestUserProfiles.ToListAsync();
                //step2: create result byte array
                byte[] result;

                //ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                //step3: create a new package using memory safe structure
                using (var package = new ExcelPackage())
                {
                    //step4: create a new worksheet
                    var worksheet = package.Workbook.Worksheets.Add("AXA MANSARD USERS");

                    //step5: fill in header row
                    //worksheet.Cells[row,col].  {Style, Value}
                    for (int i = 0; i < col_names.Length; i++)
                    {
                        worksheet.Cells[1, i + 1].Style.Font.Size = 14;  //font
                        worksheet.Cells[1, i + 1].Value = col_names[i];  //value
                        worksheet.Cells[1, i + 1].Style.Font.Bold = true; //bold  
                    }

                    int row = 2;
                    int count = 1;
                    //step6: loop through query result and fill in cells
                    foreach (var item in userList)
                    {
                        for (int col = 1; col <= 2; col++)
                        {
                            worksheet.Cells[row, col].Style.Font.Size = 12;
                        }
                        //set row,column data
                        worksheet.Cells[row, 1].Value = count;
                        worksheet.Cells[row, 2].Value = item.TransId;
                        worksheet.Cells[row, 3].Value = item.FirstName;
                        worksheet.Cells[row, 4].Value = item.SurnName;
                        worksheet.Cells[row, 5].Value = item.OtherName;
                        worksheet.Cells[row, 6].Value = item.MaritalStatus;
                        worksheet.Cells[row, 7].Value = item.Address;
                        worksheet.Cells[row, 8].Value = item.DateOfBirth;
                        worksheet.Cells[row, 9].Value = item.NextofKin;
                        worksheet.Cells[row, 10].Value = item.PhoneNumber;
                        worksheet.Cells[row, 11].Value = item.Plan;
                        worksheet.Cells[row, 12].Value = item.Sex;

                        if (row == 170)
                        {
                            row += 2;
                        }

                        row++;
                        count++;
                    }
                    //step7: auto fit columns
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    //step8: convert the package as byte array
                    result = package.GetAsByteArray();
                }//end using
                 //step9: return byte array as a file   
                return File(result, "application/vnd.ms-excel", "Users.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogCritical("An error occurred while trying to generate user excel file: " + ex);
                return BadRequest();
            }
        }
    }   
}
