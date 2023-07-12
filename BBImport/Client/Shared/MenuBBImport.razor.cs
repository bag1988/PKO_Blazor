using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using SharedLibrary.Models;
using SMDataServiceProto.V1;
using static BlazorLibrary.Shared.Main;

namespace BBImport.Client.Shared
{
    partial class MenuBBImport : IAsyncDisposable
    {
        [Parameter]
        public EventCallback UpdateFile { get; set; }

        [Parameter]
        public EventCallback UpdateInfo { get; set; }
        public ReadFileRequestInfo request { get; set; } = new();

        Dictionary<int, string>? thListFile { get; set; }

        public Dictionary<int, string> ThListFile
        {
            get
            {
                return thListFile ?? new() { { 0, "F1" } };
            }
            set
            {
                thListFile = value;
                if (value.Count > 0)
                {
                    if (request.ColumnInfo.SurnameColumn == -1)
                        request.ColumnInfo.SurnameColumn = value.ElementAtOrDefault(0).Key;
                    if (request.ColumnInfo.PhoneColumn == -1)
                        request.ColumnInfo.PhoneColumn = value.ElementAtOrDefault(1).Key;
                }
                var r = request.ColumnInfo;
                request.ColumnInfo = new();
                StateHasChanged();
                request.ColumnInfo = r;
            }
        }


        bool IsProcessing = false;

        Dictionary<string, string> WorkSheets = new();

        int StafId { get; set; } = 0;

        long FileSize = 0;

        List<Objects> MessageList = new();
        List<Objects> LocationList { get; set; } = new();



        System.Threading.Channels.Channel<byte[]>? channel;

        long _uploaded = 0;

        readonly string[] FileTypeSupport = new string[] {
            "text/plain"
            , "text/xml"
            , "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            /*, "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
              , "application/vnd.ms-excel"*/ };

        protected override async Task OnInitializedAsync()
        {
            try
            {
                System.Text.Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                request.FileRequest.CodePage = System.Text.Encoding.GetEncoding(866).CodePage;
            }
            catch
            {
                request.FileRequest.CodePage = System.Text.Encoding.GetEncodings().First().CodePage;
            }

            StafId = await _User.GetLocalStaff();
            await GetLocationInfo();
            await GetMessageList();

            _ = _HubContext.SubscribeAsync(this);
        }

        async Task LoadFiles(InputFileChangeEventArgs e)
        {
            channel?.Writer.TryComplete(new OperationCanceledException());
            Http.CancelPendingRequests();
            IsProcessing = false;
            FileSize = 0;
            _uploaded = 0;
            request.FileRequest.NumberPage = 0;
            request.NumberPage = 0;
            ThListFile = new();
            WorkSheets = new();

            if (e.File.Size > int.MaxValue / 2)
            {
                MessageView?.AddError("", $"{GsoRep["IDS_E_MAX_SIZE"]} ({int.MaxValue / 2} байт)");
                return;
            }
            request.FileRequest.ContentType = e.File.ContentType.ToLower();
            if (!FileTypeSupport.Contains(request.FileRequest.ContentType))
            {
                MessageView?.AddError("", GsoRep["FORMAT_NO_SUPPORT"]);
                request.FileRequest.ContentType = string.Empty;
                return;
            }
            FileSize = e.File.Size;
            request.FileRequest.FileName = System.IO.Path.GetRandomFileName();
            int CountBuffer = 24000;
            channel = System.Threading.Channels.Channel.CreateBounded<byte[]>(5);
            var s = e.File.OpenReadStream(int.MaxValue / 2);
            try
            {
                var r = Task.Run(async () =>
                {
                    IsProcessing = true;
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

                var b = await _HubContext.InvokeCoreAsync<bool>("UploadTmpFile", new object[] { channel.Reader, request.FileRequest.FileName }, default);

                if (b)
                {
                    if (request.FileRequest.ContentType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
                        await ReadWorkSheets();
                    if (UpdateFile.HasDelegate)
                    {
                        await UpdateFile.InvokeAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error {ex.Message}");
            }

            _uploaded = 0;
            IsProcessing = false;
            await s.DisposeAsync();
        }

        async Task ChangeRequest(ChangeEventArgs e, int field)
        {
            if (field == 1)
            {
                int.TryParse(e.Value?.ToString(), out int CodePage);
                request.FileRequest.CodePage = CodePage;
            }
            else if (field == 2)
            {
                request.FileRequest.Separotor = e.Value?.ToString() ?? "";
            }
            else if (field == 3)
            {
                request.FileRequest.SelectSheet = e.Value?.ToString() ?? "";
            }
            else if (field == 4)
            {
                request.FileRequest.FirstStringAsName = Convert.ToBoolean(e.Value?.ToString());
            }
            else if (field == 5)
            {
                int.TryParse(e.Value?.ToString(), out int firstLine);
                request.FileRequest.IgnoreStrFirstCount = firstLine;
            }

            request.FileRequest.NumberPage = 0;
            request.NumberPage = 0;
            IsProcessing = true;
            IsProcessing = false;

            if (UpdateFile.HasDelegate)
            {
                await UpdateFile.InvokeAsync();
            }

        }

        async Task ChangeRequestInfo(ChangeEventArgs e, int field)
        {
            if (field == 1)
            {
                request.ContractNumber = Convert.ToBoolean(e.Value?.ToString());
            }
            else if (field == 2)
            {
                request.CurrencyCode = Convert.ToBoolean(e.Value?.ToString());
            }
            else if (field == 3)
            {
                request.WaitingTone = Convert.ToBoolean(e.Value?.ToString());
            }
            else if (field == 4)
            {
                request.RoundUp = Convert.ToBoolean(e.Value?.ToString());
            }
            else if (field == 5)
            {
                request.AccountDebt = Convert.ToBoolean(e.Value?.ToString());
            }
            else
            {
                int.TryParse(e.Value?.ToString(), out int CodePage);

                if (field == 6)
                {
                    request.ColumnInfo.AddressColumn = CodePage;
                }
                else if (field == 7)
                {
                    request.ColumnInfo.ArrearsColumn = CodePage;
                }
                else if (field == 8)
                {
                    request.ColumnInfo.CodeColumn = CodePage;
                }
                else if (field == 9)
                {
                    request.ColumnInfo.ContractColumn = CodePage;
                }
                else if (field == 10)
                {
                    request.ColumnInfo.PhoneColumn = CodePage;
                }
                else if (field == 11)
                {
                    request.ColumnInfo.SurnameColumn = CodePage;
                }
            }

            request.NumberPage = 0;
            if (UpdateInfo.HasDelegate)
            {
                await UpdateInfo.InvokeAsync();
            }
        }

        async Task ReadWorkSheets()
        {
            await Http.PostAsJsonAsync("api/v1/ReadWorkSheets", request.FileRequest.FileName).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    WorkSheets = await x.Result.Content.ReadFromJsonAsync<Dictionary<string, string>>() ?? new();
                    request.FileRequest.SelectSheet = WorkSheets.FirstOrDefault().Key;
                }
            });
        }

        async Task GetLocationInfo()
        {
            await Http.PostAsJsonAsync("api/v1/GetObjects_ILocation", new OBJ_ID() { StaffID = StafId }).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    LocationList = await x.Result.Content.ReadFromJsonAsync<List<Objects>>() ?? new();
                    request.SelectLocation = LocationList.FirstOrDefault()?.OBJID?.ObjID ?? 0;
                }
            });
        }

        async Task GetMessageList()
        {
            await Http.PostAsJsonAsync("api/v1/GetObjects_IMessage", new OBJ_ID() { StaffID = StafId }).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    MessageList = await x.Result.Content.ReadFromJsonAsync<List<Objects>>() ?? new();
                    MessageList?.RemoveAll(x => x.OBJID == null);
                    request.SelectMessage = MessageList?.FirstOrDefault()?.OBJID?.ObjID ?? 0;

                }
            });
        }

        public ValueTask DisposeAsync()
        {
            Http.CancelPendingRequests();
            channel?.Writer.TryComplete(new OperationCanceledException());
            return _HubContext.DisposeAsync();
        }
    }
}
