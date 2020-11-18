using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using HealthBanc.DTO;
using HealthBanc.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SheetData = Google.Apis.Sheets.v4.Data;

namespace HealthBanc.Services.InsuredCancelLiveSheet
{
    public class LiveExcelList
    {
        private GoogleSheetAPI Options { get; }
        private HealthBanc.Helpers.Environment envOptions { get; }
        private string activatedUserId {get;set;}
        private string deactivatedUserId { get; set; }

        public LiveExcelList(IWebHostEnvironment environment, IOptions<GoogleSheetAPI> optionAccessor, IOptions<HealthBanc.Helpers.Environment> envAccessor)
        {
            Options = optionAccessor.Value;
            envOptions = envAccessor.Value;
            activatedUserId = envAccessor.Value.Production ? optionAccessor.Value.ActivatedUsersId : optionAccessor.Value.StagingActivatedUsersId;
            deactivatedUserId = envAccessor.Value.Production ? optionAccessor.Value.DeactivatedUsersExcelId : optionAccessor.Value.StagingDeactivatedUsersExcelId;
            _environment = environment;
        }

        private static readonly string[] Scopes = { SheetsService.Scope.Spreadsheets };
        private readonly IWebHostEnvironment _environment;

        public SheetsService GetSheetsService()
        {
            if(envOptions.Production == true)
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
            else
            {
                string GoogleCredentialsFileName = Path.Combine(_environment.WebRootPath, "exceldevelopment.json");
                using (var stream = new FileStream(GoogleCredentialsFileName, FileMode.Open, FileAccess.Read))
                {
                    var serviceInitializer = new BaseClientService.Initializer
                    {
                        HttpClientInitializer = GoogleCredential.FromStream(stream).CreateScoped(Scopes)
                    };
                    return new SheetsService(serviceInitializer);
                }
            }   
        }

        public static List<object> ProcessDeactivatedUsers(InactiveUsersDTO user)
        {
            return new List<object>{user.TransId,user.Surname,user.Othernames,user.MaidenName,user.DateOfBirth,
            user.PhoneNumber,user.ContactAddress,user.StateOfResidence,user.TownOfResidence,user.CareProviderName,user.CPAddress
            ,user.CPCity,user.AlternateHospital,user.Email,user.Gender,user.PlanCode,user.Premium,user.SubscriptionStatus,user.EndDate};
        }

        public static List<object> ProcessActivatedUsers(ActivatedUsersSheetDTO user)
        {
            return new List<object>{user.TransId,user.Surname,user.Othernames,user.MaidenName,user.DateOfBirth,
            user.PhoneNumber,user.ContactAddress,user.StateOfResidence,user.TownOfResidence,user.CareProviderName,user.CPAddress
            ,user.CPCity,user.AlternateHospital,user.Email,user.Gender,user.PlanCode,user.Premium,user.SubscriptionStatus,user.FreeTrail,user.StartDate};
        }

        public async Task ActivatedUsers(ActivatedUsersSheetDTO user)
        {
            const string WriteRange = "A2:T2";
            var serviceValues = GetSheetsService().Spreadsheets.Values;
            var valueRange = new ValueRange { Values = new List<IList<object>> { ProcessActivatedUsers(user) } };
            var update = serviceValues.Append(valueRange, activatedUserId, WriteRange);
            update.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;
            update.InsertDataOption = SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum.INSERTROWS;
            var response = update.ExecuteAsync();
            await Task.CompletedTask;
        }

        public async Task DeactivatedUsers(InactiveUsersDTO user)
        {
            const string WriteRange = "A2:S2";
            var serviceValues = GetSheetsService().Spreadsheets.Values;
            var valueRange = new ValueRange { Values = new List<IList<object>> { ProcessDeactivatedUsers(user) } };

            var update = serviceValues.Append(valueRange,deactivatedUserId, WriteRange);
            update.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;
            try
            {
                var response = await update.ExecuteAsync();
            }
            catch(Exception ex)
            {
                var x = ex;
            }
            
            await Task.CompletedTask;
        }

        public async Task RemoveFromActiveListToInactiveList(InactiveUsersDTO user)
        {
            var range = await ReadActivatedUsers(user.TransId);
            if(range != "false")
            {
                List<SheetData.Request> requests = new List<SheetData.Request>();  // TODO: Update placeholder value.
                SheetData.Request request = new SheetData.Request();

                var delete = new DeleteDimensionRequest();
                delete.Range = new DimensionRange();
                delete.Range.Dimension = "ROWS";
                delete.Range.SheetId = 0;
                delete.Range.StartIndex = int.Parse(range) - 1;
                delete.Range.EndIndex = int.Parse(range);

                request.DeleteDimension = delete;

                requests.Add(request);

                SheetData.BatchUpdateSpreadsheetRequest requestBody = new SheetData.BatchUpdateSpreadsheetRequest();
                requestBody.Requests = requests;

                SpreadsheetsResource.BatchUpdateRequest updateRequest = GetSheetsService().Spreadsheets.BatchUpdate(requestBody, activatedUserId);

                SheetData.BatchUpdateSpreadsheetResponse response = await updateRequest.ExecuteAsync();

                await DeactivatedUsers(user);
            }
        }

        public async Task RemoveFromInactiveListToActiveList(ActivatedUsersSheetDTO user)
        {
            var range = await ReadDeactivatedUsers(user.TransId);
            if (range != "false")
            {
                List<SheetData.Request> requests = new List<SheetData.Request>();  // TODO: Update placeholder value.
                SheetData.Request request = new SheetData.Request();
                
                var delete = new DeleteDimensionRequest();
                delete.Range = new DimensionRange();
                delete.Range.Dimension = "ROWS";
                delete.Range.SheetId = 0;
                delete.Range.StartIndex =int.Parse(range)-1;
                delete.Range.EndIndex = int.Parse(range);

                request.DeleteDimension = delete;

                requests.Add(request);

                SheetData.BatchUpdateSpreadsheetRequest requestBody = new SheetData.BatchUpdateSpreadsheetRequest();
                requestBody.Requests = requests;

                SpreadsheetsResource.BatchUpdateRequest updateRequest =  GetSheetsService().Spreadsheets.BatchUpdate(requestBody, deactivatedUserId);


                SheetData.BatchUpdateSpreadsheetResponse response =await updateRequest.ExecuteAsync();

                await ActivatedUsers(user);
            }
        }

        public async Task<string> ReadActivatedUsers(string transId)
        {
            const string ReadRange = "A2:A";
            var serviceValues = GetSheetsService().Spreadsheets.Values;
            var response = await serviceValues.Get(activatedUserId, ReadRange).ExecuteAsync();
            var values = response.Values;
            if (values != null)
            {
                for (int i = 0; i < values.Count; i++)
                {
                    if (values[i][0].Equals(transId))
                    {
                        var range = i+2;
                        return range.ToString();
                    }
                }
                return "false";
            }
            return "false";
        }

        public async Task<string> ReadDeactivatedUsers(string transId)
        {
            const string ReadRange = "A2:A";
            var serviceValues = GetSheetsService().Spreadsheets.Values;
            var response = await serviceValues.Get(deactivatedUserId, ReadRange).ExecuteAsync();
            var values = response.Values;
            if (values != null)
            {
                for (int i = 0; i < values.Count; i++)
                {
                    if (values[i][0].Equals(transId))
                    {
                        var range = i+2;
                        return range.ToString();
                    }
                }
                return "false";
            }
            return "false";
        }
    }
}  