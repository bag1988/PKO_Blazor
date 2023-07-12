using System.Net.Http.Json;
using System.Reflection.Metadata;
using System.Threading.Channels;
using BlazorLibrary.Helpers;
using BlazorLibrary.Shared.Audio;
using Google.Protobuf;
using Microsoft.AspNetCore.Components;
using SharedLibrary;
using SharedLibrary.GlobalEnums;
using SMDataServiceProto.V1;
using SyntezServiceProto.V1;
using static BlazorLibrary.Shared.Main;

namespace BlazorLibrary.Shared.Messages
{

    partial class CreateMessage : IAsyncDisposable
    {
        [CascadingParameter]
        public int SubsystemID { get; set; } = SubsystemType.SUBSYST_ASO;

        [Parameter]
        public int? MsgId { get; set; }

        [Parameter]
        public string? TitleText { get; set; }

        [Parameter]
        public bool? Edit { get; set; } = false;

        [Parameter]
        public EventCallback<OBJ_ID?> CallBack { get; set; }

        private MsgParam? Model = null;
        private MsgParam? OldModel = null;

        private SynthesisData? SyntezData = null;

        private int StaffId = 0;

        private long _uploaded = 0;

        private long _fileLength = 0;

        bool IsProcessing = false;

        private AudioPlayerStream? player = default!;

        private bool IsChangeSound = true;

        int SaveFileOrBase = 0;
        int OldSaveFileOrBase = 0;

        string ActiveUrlFile = string.Empty;

        int? TimeOut;

        private System.Threading.Channels.Channel<byte[]>? channel;

        protected override async Task OnInitializedAsync()
        {
            if (MsgId > 0)
                IsChangeSound = false;
            StaffId = await _User.GetLocalStaff();
            await GetList();
            await GetSettingSynthesis();
            _ = _HubContext.SubscribeAsync(this);
        }
        private async Task WriteMessages()
        {
            if (Model != null && Edit == true)
            {
                IsProcessing = true;
                _fileLength = 0;
                _uploaded = 0;

                if (OldSaveFileOrBase != SaveFileOrBase)
                {
                    IsChangeSound = true;
                }

                if (string.IsNullOrEmpty(Model.MsgName) || (Model.MsgType != (int)MessageType.MessageSound && string.IsNullOrEmpty(Model.MsgText)))
                {
                    MessageView?.AddError("", AsoRep["IDS_MISMATCHERROR"]);
                }
                else
                {
                    if ((Model.MsgType == (int)MessageType.MessageSound || Model.MsgType == (int)MessageType.MessageSoundAndText) && IsChangeSound)
                    {
                        if (string.IsNullOrEmpty(ActiveUrlFile))
                        {
                            MessageView?.AddError("", AsoRep["IDS_MISMATCHERROR"]);
                            IsProcessing = false;
                            return;
                        }

                        long fileLength = 0;

                        fileLength = await Http.GetLengthFileAsync(ActiveUrlFile);

                        if (fileLength < 8000)
                        {
                            MessageView?.AddError("", AsoRep["IDS_MISMATCHERROR"]);
                            IsProcessing = false;
                            return;
                        }
                    }

                    if (SubsystemID == SubsystemType.SUBSYST_SZS)
                    {
                        if (TimeOut == 0)
                        {
                            MessageView?.AddError("", GsoRep["ERROR_TIMEOUT"]);
                            IsProcessing = false;
                            return;
                        }
                        else if (TimeOut > 0)
                            Model.DopParam = TimeOut.Value * 20;
                        else
                            Model.DopParam = 0;
                    }


                    if (!Model.Equals(OldModel) || IsChangeSound)
                    {
                        OBJ_ID oBJ_ID = new OBJ_ID() { ObjID = (MsgId ?? 0), StaffID = StaffId, SubsystemID = SubsystemID };
                        OBJ_ID newMsgId = new();

                        string? json = JsonFormatter.Default.Format(new MsgInfo() { Msg = oBJ_ID, Param = Model });
                        await Http.PostAsJsonAsync("api/v1/WriteMessages", json, ComponentDetached).ContinueWith(async x =>
                        {
                            if (x.Result.IsSuccessStatusCode)
                            {
                                newMsgId = await x.Result.Content.ReadFromJsonAsync<OBJ_ID>() ?? new();
                            }
                            else
                            {
                                MessageView?.AddError("", GsoRep["IDS_ERRSAVEDIRECTIVE"]);
                            }
                            StateHasChanged();
                        });

                        if (newMsgId.ObjID != 0)
                        {
                            if (Model.MsgType != (int)MessageType.MessageText && IsChangeSound)
                            {
                                if (IsChangeSound)
                                {
                                    await SaveSoundStreamSignalR(newMsgId);
                                }
                            }
                            await CallBackEvent(newMsgId);
                        }
                        MsgId = null;
                    }
                    else
                    {
                        await CallBackEvent();
                        MsgId = null;
                    }
                }
            }
            IsProcessing = false;
        }

        private async Task GetList()
        {
            Model = null;

            if (MsgId != null)
            {
                OBJ_ID request = new OBJ_ID() { ObjID = MsgId.Value, StaffID = StaffId, SubsystemID = SubsystemID };
                await Http.PostAsJsonAsync("api/v1/GetMessageShortInfo", request, ComponentDetached).ContinueWith(async x =>
                {
                    if (x.Result.IsSuccessStatusCode)
                    {
                        try
                        {
                            Model = MsgParam.Parser.ParseJson(await x.Result.Content.ReadAsStringAsync());
                            SaveFileOrBase = Model?.Sound?.Length < 520 ? 1 : 0;
                            OldSaveFileOrBase = SaveFileOrBase;

                            if (SubsystemID == SubsystemType.SUBSYST_SZS && Model?.DopParam > 0)
                            {
                                TimeOut = Model.DopParam / 20;
                            }

                        }
                        catch
                        {
                            await CallBackEvent();
                            MessageView?.AddError("", GsoRep["IDS_ECREATEMESSWIN"]);
                        }

                        StateHasChanged();

                    }
                    else
                    {
                        await CallBackEvent();
                        MessageView?.AddError("", GsoRep["IDS_ECREATEMESSWIN"]);
                    }
                });


                if (Model?.MsgType == (int)MessageType.MessageSound || Model?.MsgType == (int)MessageType.MessageSoundAndText)
                {
                    try
                    {
                        WavHeaderModel m = new(Model?.Format.Memory.ToArray() ?? new byte[0]);
                        if (player != null)
                        {
                            ActiveUrlFile = $"api/v1/GetSoundServer?MsgId={MsgId}&Staff={StaffId}&System={SubsystemID}&version={DateTime.Now.Second}";
                            await player.SetUrlSound(ActiveUrlFile);
                        }

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
            else
            {
                Model = new() { MsgType = (int)MessageType.MessageSoundAndText };
            }
            if (Model != null)
                OldModel = new(Model);
        }

        private async Task SetSoundsUrlPlayer(string url)
        {
            IsChangeSound = true;
            if (Model != null)
                Model.Format = ByteString.Empty;
            if (player != null)
            {
                await player.SetUrlSound(url);
            }

            ActiveUrlFile = url;
        }

        private async Task Cancel()
        {
            await CallBackEvent();
        }

        private async Task SaveSoundStreamSignalR(OBJ_ID msgId)
        {
            channel?.Writer.TryComplete(new OperationCanceledException());
            if (Model == null)
                return;

            using var s = await Http.GetStreamAsync(ActiveUrlFile);
            if (s == null)
                return;

            int CountBuffer = 24000;

            _fileLength = s.Length;
            channel = Channel.CreateBounded<byte[]>(5);
            try
            {
                var r = Task.Run(async () =>
                {
                    if (channel != null)
                    {
                        byte[] buffer = new byte[CountBuffer];
                        int readCount = 0;
                        while ((readCount = await s.ReadAsync(buffer, ComponentDetached)) > 0)
                        {
                            if (readCount > 0)
                            {
                                _uploaded += (readCount / 1024);
                                await InvokeAsync(StateHasChanged);
                                await channel.Writer.WriteAsync(buffer.Take(readCount).ToArray(), ComponentDetached);
                            }
                        }
                        channel.Writer.TryComplete();
                    }
                });

                var result = await _HubContext.InvokeCoreAsync<bool>("UploadMessages", new object[] { channel.Reader, msgId, SaveFileOrBase });

                if (!result)
                {
                    MessageView?.AddMessage(Model?.MsgName ?? "", GsoRep["ERROR_SAVE_SOUND"]);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error {ex.Message}");
            }
            finally
            {
                await s.DisposeAsync();
                IsChangeSound = false;
            }
        }

        private async Task GetSettingSynthesis()
        {
            var result = await Http.PostAsync("api/v1/GetParamSyntez", null);
            if (result.IsSuccessStatusCode)
            {
                SyntezData = await result.Content.ReadFromJsonAsync<SynthesisData>() ?? new();
            }
        }

        private async Task CallBackEvent(OBJ_ID? NewMsgId = null)
        {
            channel?.Writer.TryComplete(new OperationCanceledException());
            if (CallBack.HasDelegate)
                await CallBack.InvokeAsync(NewMsgId);
        }

        public ValueTask DisposeAsync()
        {
            channel?.Writer.TryComplete(new OperationCanceledException());
            DisposeToken();
            return _HubContext.DisposeAsync();
        }
    }
}
