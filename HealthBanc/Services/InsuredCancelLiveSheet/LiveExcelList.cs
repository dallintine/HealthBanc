using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using HealthBanc.DTO;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DataUse = Google.Apis.Sheets.v4.Data;

namespace HealthBanc.Services.InsuredCancelLiveSheet
{
    public class LiveExcelList
    {

        private readonly IWebHostEnvironment _environment;
        public LiveExcelList(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        private static readonly string[] Scopes = { SheetsService.Scope.Spreadsheets };
        private const string SpreadsheetId = "1QsktJte6pmRv0WXTQINZZsuB3B3aQ-o9lc9KDQ_wQm4";       
        private const string WriteRange = "A2:R2";
        private const string ReadRange = "Sheet1!A:B";

        private  SheetsService GetSheetsService()
        {
            string GoogleCredentialsFileName = Path.Combine(_environment.WebRootPath, "google-credentials.json");
            using (var stream = new FileStream(GoogleCredentialsFileName, FileMode.Open, FileAccess.Read))
            {
                var serviceInitializer = new BaseClientService.Initializer
                {
                    HttpClientInitializer = GoogleCredential.FromStream(stream).CreateScoped(Scopes)
                };
                return new SheetsService(serviceInitializer);
            }
        }


        public static List<object> ProcessObject(InactiveUsersDTO user)
        {
            return new List<object>{user.TransId,user.Surname,user.Othernames,user.MaidenName,user.DateOfBirth,
            user.PhoneNumber,user.ContactAddress,user.StateOfResidence,user.TownOfResidence,user.CareProviderName,user.CPAddress
            ,user.CPCity,user.AlternateHospital,user.Email,user.Gender,user.PlanCode,user.Premium,user.SubscriptionStatus};
        }
        
        public async Task WriteAsync(InactiveUsersDTO user)
        {
            var serviceValues = GetSheetsService().Spreadsheets.Values;
            var valueRange = new ValueRange { Values = new List<IList<object>> { ProcessObject(user) } };
           
            var update = serviceValues.Append(valueRange, SpreadsheetId, WriteRange);
            update.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;
            var response = await update.ExecuteAsync();
        }

        private async Task ReadAsync()
        {
            var serviceValues = GetSheetsService().Spreadsheets.Values;
            var response = await serviceValues.Get(SpreadsheetId, ReadRange).ExecuteAsync();
            var values = response.Values;
            if (values == null || !values.Any())
            {
                Console.WriteLine("No data found.");
                return;
            }
            var header = string.Join(" ", values.First().Select(r => r.ToString()));
            Console.WriteLine($"Header: {header}");

            foreach (var row in values.Skip(1))
            {
                var res = string.Join(" ", row.Select(r => r.ToString()));
                Console.WriteLine(res);
            }
        }
    }
}  