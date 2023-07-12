using System.Net.Http.Json;
using System.Text.Json;
using BBImport.Client.Shared;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using AsoDataProto.V1;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using SharedLibrary;
using SharedLibrary.Models;
using SMDataServiceProto.V1;
using static BlazorLibrary.Shared.Main;

namespace BBImport.Client.Pages
{
    partial class Index
    {
        [CascadingParameter]
        public MainLayout? Layout { get; set; }

        ReadFileRequestInfo request
        {
            get
            {
                return Layout?.request ?? new();
            }
        }

        Dictionary<int, string> ThListFile
        {
            get
            {
                return Layout?.ThListFile ?? new();
            }
            set
            {
                if (Layout != null)
                {
                    Layout.ThListFile = value;
                }
            }
        }

        List<List<string>>? FileAbonInfo = new();
        List<ASOAbonent>? ImportAbonList = null;
        List<AbonMsgParam> abList = new();

        bool IsAddData = false;
        bool IsLoadData = false;

        bool IsAddDataInfo = false;
        bool IsLoadDataInfo = false;

        bool IsProcessingDelete = false;
        bool IsProcessingSave = false;
        bool IsCreateSit = false;


        bool IsWarning = false;

        int StafId { get; set; } = 0;


        protected override async Task OnInitializedAsync()
        {
            FileAbonInfo = new();
            ImportAbonList = new();
            StafId = await _User.GetLocalStaff();
            if (Layout != null)
            {
                Layout.StartUpdateFile = async () => await ChangeRequest();
                Layout.StartUpdateInfo = async () => await ChangeRequestInfo();
            }
        }

        async Task ChangeRequest()
        {
            if (!string.IsNullOrEmpty(request.FileRequest.FileName))
            {
                FileAbonInfo = null;
                ImportAbonList = null;
                await GetAbonInfo();
                await GetImportAbon();
            }
            else
            {
                FileAbonInfo = new();
                ImportAbonList = new();
            }
        }
        async Task ChangeRequestInfo()
        {
            if (!string.IsNullOrEmpty(request.FileRequest.FileName))
            {
                ImportAbonList = null;
                await GetImportAbon();
            }
            else
                ImportAbonList = new();
        }

        async Task GetAbonInfo()
        {
            IsLoadData = true;
            IsAddData = false;
            List<List<string>> newData = new();

            await Http.PostAsJsonAsync("api/v1/ReadTmpFileImport", request.FileRequest, ComponentDetached).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    newData = await x.Result.Content.ReadFromJsonAsync<List<List<string>>>() ?? new();
                }
            });

            if (FileAbonInfo == null)
                FileAbonInfo = new();

            if (newData.Count > 0)
            {
                FileAbonInfo.AddRange(newData);
            }

            if (FileAbonInfo.Count > 0)
            {
                if (request.FileRequest.FirstStringAsName)
                {
                    int pos = 0;

                    ThListFile = FileAbonInfo.First().ToDictionary(x => pos++, x => string.IsNullOrEmpty(x) ? $"F{pos + 1}" : x);
                    FileAbonInfo.RemoveAt(0);
                }
                else
                {
                    ThListFile = Enumerable.Range(0, FileAbonInfo.Max(x => x.Count)).ToDictionary(x => x, x => $"F{x + 1}");
                }
            }

            if (newData.Count() == request.FileRequest.DataSize)
            {
                IsAddData = true;
            }

            IsLoadData = false;
            StateHasChanged();
        }

        async Task GetImportAbon()
        {
            IsLoadDataInfo = true;
            IsAddDataInfo = false;
            List<ASOAbonent> newData = new();

            await Http.PostAsJsonAsync("api/v1/ReadTmpFileInfoAbon", request, ComponentDetached).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    newData = await x.Result.Content.ReadFromJsonAsync<List<ASOAbonent>>() ?? new();
                }
            });

            if (ImportAbonList == null)
                ImportAbonList = new();

            if (newData.Count > 0)
            {
                ImportAbonList.AddRange(newData);
            }

            if (newData.Count() == request.FileRequest.DataSize)
            {
                IsAddDataInfo = true;
            }

            IsLoadDataInfo = false;
            StateHasChanged();
        }

        void ViewWarning()
        {
            if (ImportAbonList == null || ImportAbonList.Count == 0)
            {
                MessageView?.AddError("", GsoRep["NO_DATA_SAVE"]);
                return;
            }

            if (request.SelectMessage == 0)
            {
                MessageView?.AddError("", GsoRep["ERROR_NO_MSG"]);
                return;
            }
            if (request.SelectLocation == 0)
            {
                MessageView?.AddError("", GsoRep["ERROR_NO_LOC"]);
                return;
            }

            if (!request.PhoneLine && !request.SendSms)
            {
                MessageView?.AddError("", GsoRep["SELECT_TYPE_CONNECT"]);
                return;
            }
            IsWarning = true;
        }

        async Task DeleteOldAbon()
        {
            IsProcessingDelete = true;

            try
            {
                Int32Value result = new();
                MessageView?.AddMessage("", GsoRep["DELETE_SERVICE_INFO"]);
                await Http.PostAsync("api/v1/ClearMsgParam", null).ContinueWith(async x =>
                {
                    if (x.Result.IsSuccessStatusCode)
                    {
                        result = await x.Result.Content.ReadFromJsonAsync<Int32Value>() ?? new();
                    }
                });

                if (result.Value == -1)
                {
                    MessageView?.AddError("", GsoRep["DELETE_SERVICE_INFO"] + " " + GsoRep["ERROR_DELETE"]);
                }
                else
                {
                    MessageView?.AddMessage("", $"{GsoRep["DELETE_SERVICE_INFO"]} {GsoRep["DELETE_COUNT"]} {result.Value}");
                }

                await Task.Delay(100);
                MessageView?.AddMessage("", GsoRep["DELETE_INFO_ABON"]);

                await Http.PostAsync("api/v1/DeleteAbonentList", null).ContinueWith(async x =>
                {
                    if (x.Result.IsSuccessStatusCode)
                    {
                        result = await x.Result.Content.ReadFromJsonAsync<Int32Value>() ?? new();
                    }
                });

                if (result.Value == -1)
                {
                    MessageView?.AddError("", GsoRep["DELETE_INFO_ABON"] + " " + GsoRep["ERROR_DELETE"]);
                }
                else
                {
                    MessageView?.AddMessage("", $"{GsoRep["DELETE_INFO_ABON"]} {GsoRep["DELETE_COUNT"]} {result.Value}");
                }

                await Task.Delay(100);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                MessageView?.AddError("", GsoRep["ERROR_DELETE"]);
            }
            IsProcessingDelete = false;

            await SaveImportAbon();

        }

        async Task SaveImportAbon()
        {
            IsProcessingSave = true;
            try
            {
                abList = new();
                StateHasChanged();
                using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "api/v1/ImportAbonentListWithParams");
                requestMessage.SetBrowserResponseStreamingEnabled(true);
                requestMessage.Content = JsonContent.Create(request);
                var response = await Http.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, ComponentDetached);

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync(ComponentDetached);
                    var lines = JsonSerializer.DeserializeAsyncEnumerable<string>(responseStream, cancellationToken: ComponentDetached);
                    await foreach (var line in lines)
                    {
                        if (line != null)
                        {
                            var r = JsonParser.Default.Parse<AbonMsgParamList>(line);
                            abList.AddRange(r.Array);
                            StateHasChanged();
                            await Task.Delay(5);
                        }
                    }
                }
                else
                {
                    MessageView?.AddError("", GsoRep["ERROR_IMPORT"]);
                }

                IsCreateSit = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                MessageView?.AddError("", GsoRep["ERROR_IMPORT"]);
            }

            IsWarning = false;
            IsProcessingSave = false;
        }

        async Task CreateSit()
        {
            IsProcessingSave = true;
            try
            {
                if (abList.Count > 0)
                {
                    UpdateSituation Sit = new();

                    Sit.Info = new SituationInfo()
                    {
                        Sit = new() { StaffID = StafId, SubsystemID = SubsystemType.SUBSYST_ASO },
                        SitName = $"{GsoRep["AUTO_IMPORT"]} - {DateTime.Now.ToString("u")}",
                        SitTypeID = 1,
                        SitPriority = 1,
                        SysSet = 1,
                        Status = 1,
                        Param = new SubsystemParam() { CountRepeat = 1 }
                    };

                    Sit.Items = abList.GroupBy(x => x.AbonID).Select(x => new CGetSitItemInfo()
                    {
                        AsoAbonID = x.First().AbonID,
                        AsoAbonStaffID = x.First().StaffID,
                        MsgID = request.SelectMessage,
                        MsgStaffID = StafId,
                        Param1 = request.PhoneLine ? 1 : 0
                    }).ToList();


                    OBJ_ID sitId = new();
                    await Http.PostAsJsonAsync("api/v1/AddSituation", Sit).ContinueWith(async x =>
                    {
                        if (x.Result.IsSuccessStatusCode)
                        {
                            sitId = await x.Result.Content.ReadFromJsonAsync<OBJ_ID>() ?? new();
                        }
                        else
                        {
                            MessageView?.AddError("", GsoRep["IDS_ERRORCAPTION"]);
                        }
                    });

                    if (sitId.ObjID > 0)
                    {
                        AbonMsgParamList abonMsgParamList = new();

                        abonMsgParamList.Array.Add(abList.Select(x => new AbonMsgParam(x) { SitID = sitId.ObjID }));

                        await Http.PostAsJsonAsync("api/v1/AddMsgParamList", JsonFormatter.Default.Format(abonMsgParamList)).ContinueWith(x =>
                        {
                            if (x.Result.IsSuccessStatusCode)
                            {
                                MessageView?.AddMessage("", $"{GsoRep["IMPORT_OK"]} {Sit.Info.SitName}");
                            }
                            else
                            {
                                MessageView?.AddError("", GsoRep["ERROR_SAVE_MSGPARAMS"]);
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                MessageView?.AddError("", GsoRep["ERROR_IMPORT"]);
            }

            IsCreateSit = false;
            IsProcessingSave = false;
        }

        async Task AddData()
        {
            if (IsAddData && !IsLoadData)
            {
                request.FileRequest.NumberPage += 1;
                await GetAbonInfo();
            }
        }

        async Task AddDataInfo()
        {
            if (IsAddDataInfo && !IsLoadDataInfo)
            {
                request.NumberPage += 1;
                await GetImportAbon();
            }
        }

    }
}
