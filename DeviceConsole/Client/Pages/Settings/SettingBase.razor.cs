using System.Net.Http.Json;
using BlazorLibrary;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using SharedLibrary;
using SharedLibrary.Models;
using SMDataServiceProto.V1;
using static BlazorLibrary.Shared.Main;

namespace DeviceConsole.Client.Pages.Settings
{
    partial class SettingBase : IAsyncDisposable
    {

        private bool IsCreate = false;

        private bool IsRestore = false;

        private bool IsDelete = false;

        private string? BackupPath = "";

        private bool IsLoadFile = false;

        private List<BackupInfo>? Model = null;

        private BackupInfo? SelectItem = null;

        private IBrowserFile? file = null;

        private long _uploaded = 0;

        private Dictionary<int, string>? ThList;

        readonly GetItemRequest request = new() { ObjID = new OBJ_ID(), LSortOrder = 0, BFlagDirection = 0 };

        private ProductVersion PVersion = null!;

        private

        System.Threading.Channels.Channel<byte[]>? channel;

        protected override async Task OnInitializedAsync()
        {
            ThList = new Dictionary<int, string>
            {
                { 0, GsoRep["IDS_STRING_NAME"] },
                { 1, DeviceRep["IDS_BACKUP_CREATE"] },
                { 2, DeviceRep["IDS_BACKUP_SIZE"] }
            };
            await GetList();
            await PVersionFull();

            _ = _HubContext.SubscribeAsync(this);
        }


        private async Task PVersionFull()
        {
            await Http.PostAsync("api/v1/allow/PVersionFull", null).ContinueWith(async (x) =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    PVersion = await x.Result.Content.ReadFromJsonAsync<ProductVersion>();
                }
            });
        }

        private void SetSort(int? id)
        {
            if (id == request.LSortOrder)
                request.BFlagDirection = request.BFlagDirection == 1 ? 0 : 1;
            else
            {
                request.LSortOrder = id ?? 0;
                request.BFlagDirection = 1;
            }
            SortList.Sort(ref Model, request.LSortOrder, request.BFlagDirection);
            StateHasChanged();
        }

        private void ViewCreateBackUp()
        {
            IsCreate = true;
            BackupPath = $"{PVersion.CompanyName}-{PVersion.ProductName}.{PVersion.ProductVersionNumberMajor}.{PVersion.ProductVersionNumberMinor}.{PVersion.ProductVersionNumberPatch}-{PVersion.BuildNumber}.pds.{DateTime.Now.ToString("yyyyMMddHHmmss")}";
        }

        private async Task DownLoadFile()
        {
            if (SelectItem == null)
                return;
            await JSRuntime.InvokeVoidAsync("triggerFileDownload", SelectItem.Name, $"api/v1/GetFileServer?FileName={SelectItem.Name}");
        }

        /// <summary>
        /// Сохраняем при помощи signalR
        /// </summary>
        /// <returns></returns>
        private async Task UploadSignalR()
        {
            channel?.Writer.TryComplete(new OperationCanceledException());
            if (file == null)
                return;

            int CountBuffer = 24000;

            _uploaded = 0;
            channel = System.Threading.Channels.Channel.CreateBounded<byte[]>(5);

            using var s = file.OpenReadStream(int.MaxValue / 2);
            try
            {
                var r = Task.Run(async () =>
                {
                    if (channel != null)
                    {
                        byte[] buffer = new byte[CountBuffer];
                        int readCount = 0;
                        while ((readCount = await s.ReadAsync(buffer, default)) > 0)
                        {
                            if (readCount > 0)
                            {
                                _uploaded += (readCount / 1024);
                                await InvokeAsync(StateHasChanged);
                                await channel.Writer.WriteAsync(buffer.Take(readCount).ToArray());
                            }
                        }
                        channel.Writer.TryComplete();
                    }
                });


                var result = await _HubContext.InvokeCoreAsync<int>("UploadDataBase", new object[] { channel.Reader, file.Name }, ComponentDetached);
                if (result == 0)
                {
                    MessageView?.AddMessage("", GsoRep["IDS_IS_SAVE_BACKUP"]);
                }
                else if (result == 2)
                {
                    MessageView?.AddError("", GsoRep["IDS_E_BACKUP_EXISTS"]);
                }
                else
                {
                    MessageView?.AddError("", DeviceRep["ERROR_UPLOAD_BASE"]);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error {ex.Message}");
            }
            finally
            {
                _uploaded = 0;
                await s.DisposeAsync();
                IsLoadFile = false;
                await GetList();
            }
        }

        private void LoadFiles(InputFileChangeEventArgs e)
        {
            var Extension = Path.GetExtension(e.File.Name);

            if (e.File.Size > int.MaxValue / 2)
            {
                MessageView?.AddError("", $"{GsoRep["IDS_E_MAX_SIZE"]} ({int.MaxValue / 2} байт)");
                return;
            }


            if (Extension.ToLower() == ".bak")
            {
                file = e.File;
            }
            else
            {
                MessageView?.AddError("", GsoRep["IDS_E_SELECTFILE"]);
                return;
            }
        }

        private async Task GetList()
        {
            await Http.PostAsync("api/v1/GetFileBackupGSO", null, ComponentDetached).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    Model = await x.Result.Content.ReadFromJsonAsync<List<BackupInfo>>() ?? new();
                }
                else
                {
                    Model = new();
                    MessageView?.AddError("", AsoRep["IDS_EGETDATA"]);
                }
            });
            SortList.Sort(ref Model, request.LSortOrder, request.BFlagDirection);
        }

        private async Task DeleteBackup()
        {
            if (SelectItem == null)
                return;
            await Http.PostAsJsonAsync("api/v1/DeleteDBPostgreeSql", new StringValue() { Value = SelectItem.Name }).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    var b = await x.Result.Content.ReadFromJsonAsync<BoolValue>() ?? new();

                    if (b.Value != true)
                    {
                        MessageView?.AddError("", GSOFormRep["IDS_E_DELETE"]);
                    }
                }
                else
                {
                    MessageView?.AddError("", GSOFormRep["IDS_E_DELETE"]);
                }
            });
            SelectItem = null;
            await GetList();
            IsDelete = false;
        }

        private async Task RestoreBackup()
        {
            if (string.IsNullOrEmpty(SelectItem?.Name))
            {
                MessageView?.AddError(DeviceRep["IDS_REG_RESORE"], DeviceRep["ErrorNull"] + " " + DeviceRep["NameFileBackup"]);
                return;
            }

            await Http.PostAsJsonAsync("api/v1/RestoreGSO", new StringValue() { Value = SelectItem.Name }, ComponentDetached).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    var response = await x.Result.Content.ReadFromJsonAsync<IntID>() ?? new();

                    if (response == null || response.ID != 0)
                    {
                        MessageView?.AddError("", DeviceRep["IDS_REG_ERR_RELOAD"]);
                    }
                    else
                    {
                        if (MessageView != null)
                            MessageView.AddCallback = new EventCallback(this, async () =>
                            {
                                await AuthenticationService.Logout();
                            });
                        MessageView?.AddMessage("", DeviceRep["IsOkRestoreBase"]);
                    }
                }
                else
                    MessageView?.AddError("", DeviceRep["IDS_REG_ERR_RELOAD"]);
            });
            IsRestore = false;

        }

        private async Task StartBackup()
        {
            if (string.IsNullOrEmpty(BackupPath))
            {
                MessageView?.AddError("", DeviceRep["ErrorNull"] + " " + DeviceRep["NameFileBackup"]);
                return;
            }

            await Http.PostAsJsonAsync("api/v1/BackupGSO", new StringValue() { Value = BackupPath + ".bak" }, ComponentDetached).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    var response = await x.Result.Content.ReadFromJsonAsync<IntID>() ?? new();

                    if (response == null || response.ID != 0)
                    {
                        MessageView?.AddError("", AsoRep["IDS_E_REPORTER_SAVE"]);
                    }
                }
                else
                    MessageView?.AddError("", AsoRep["IDS_E_REPORTER_SAVE"]);
            });
            await GetList();
            IsCreate = false;
        }

        public ValueTask DisposeAsync()
        {
            channel?.Writer.TryComplete(new OperationCanceledException());
            DisposeToken();
            return _HubContext.DisposeAsync();
        }
    }
}
