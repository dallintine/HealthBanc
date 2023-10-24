using Application.Common.DTO;
using Application.Common.Interfaces;
using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Infrastructure.Services.FileProcessor
{
    public class FileService : IFileService
    {
        private readonly ExcelPackage _excelPackage;
        private readonly ILogger<FileService> _logger;

        public FileService(ExcelPackage excelPackage,ILogger<FileService> logger)
        {
            _excelPackage = excelPackage;
            _logger = logger;
        }

        public async Task ProcessExports<T>(IQueryable<T> query)
        {
            var count = query.Count();
            var transactDivisionLength = Math.Ceiling(Convert.ToDecimal(count) / 100);
            for (int i = 0; i < transactDivisionLength; i++)
            {
                var skip = 500 * i;
                var transaction = query.Skip(skip).Take(500).ToList();
                var data = await ZipFiles<T>(transaction);
                var base64String = Convert.ToBase64String(data);
                //var attachements = new List<Attachment>
                //{
                //    new Attachment { Attachmentfile = base64String, FileName = $"{orgName}_Statements{i}.zip" }
                //};
                //var email = new EmailViewModel
                //{
                //    IsBodyHtml = false,
                //    Subject = $"Statement of Account as at {DateTime.Now.Date.ToShortDateString()}",
                //    ToEmail = orgEmail,
                //    Message = template,
                //    Attachments = attachements
                //};
                //var emailResponse = await _emailService.SendEmailWithAttachement(email);                
            }
        }

        public async Task<byte[]> ZipFiles<T>(List<T> transactionViewModels)
        {
            try
            {
                var attachement = new BytesAttachement();
                var base64String = GenerateGenericExcelFile<T>(transactionViewModels, "teset");
                attachement.Attachmentfile = base64String;
                attachement.FileName = "sjddbh,xlsx";
                using var ms = new MemoryStream();
                using (var archive =
                    new ZipArchive(ms, ZipArchiveMode.Create, true))
                {
                    var entry = archive.CreateEntry(attachement.FileName,
                       System.IO.Compression.CompressionLevel.Fastest);
                    using var zipStream = entry.Open();
                    zipStream.Write(attachement.Attachmentfile, 0, attachement.Attachmentfile.Length);
                }
                return ms.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error occured while trying to zip file [Exception : {ex.ToString()}]");
                throw;
            }
        }

        public byte[] GenerateGenericExcelFile<T>(List<T> sample, string sheetName)
        {
            using var workbook = new XLWorkbook();
            try
            {
                _excelPackage.Workbook.Worksheets.Delete(0);
            }
            catch (Exception ex)
            {

            }

            var worksheet = _excelPackage.Workbook.Worksheets.Add($"{sheetName}");

            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(typeof(T));

            var headerCount = 1;
            foreach (PropertyDescriptor prop in properties)
            {
                string name = Regex.Replace(prop.Name, "([A-Z])", " $1").Trim(); //space seperated name by caps for header
                worksheet.Cells[3, headerCount].Value = name;
                headerCount++;
            }

            var count = 5;
            var start = sample.Count;
            foreach (T item in sample)
            {
                var columnCount = 1;
                foreach (PropertyDescriptor prop in properties)
                {
                    var data = prop.GetValue(item) ?? null;
                    worksheet.Cells[count, columnCount].Value = data;
                    columnCount++;
                }
                count++;
            }

            worksheet.Cells.AutoFitColumns();

            using var stream = new MemoryStream();
            _excelPackage.SaveAs(stream);
            return stream.ToArray();
        }
    }
}
