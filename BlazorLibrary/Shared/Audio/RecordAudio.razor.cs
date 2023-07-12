using System.Net.Http.Json;
using System.Reflection.Metadata;
using BlazorLibrary.Models;
using Google.Protobuf;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using SharedLibrary;
using SharedLibrary.GlobalEnums;
using SharedLibrary.Models;
using SMDataServiceProto.V1;
using static BlazorLibrary.Shared.Main;

namespace BlazorLibrary.Shared.Audio
{
    partial class RecordAudio : IAsyncDisposable
    {
        [Parameter]
        public EventCallback<string> SetSoundsUrlPlayer { get; set; }

        public string OldFileName = "";

        private bool records { get; set; } = false;

        private AudioRecordSetting setting = new();

        private SndSetting SettingRec = new();

        private TimeSpan TimeRecord;

        private long _uploaded = 0;
        private long _fileLength = 0;

        readonly int BufferSizeSignal = 24000;

        private string FileTmpName = "";

        private System.Threading.Channels.Channel<byte[]>? channel;

        InputFile? elem = null;

        protected override void OnInitialized()
        {
            _ = _HubContext.SubscribeAsync(this);
        }

        private async Task GetSndSettingExRec()
        {
            SettingRec = await _localStorage.GetSndSettingEx(SoundSettingsType.RecSoundSettingType) ?? new();
        }

        public async Task StartStreamWorklet()
        {
            channel?.Writer.TryComplete(new OperationCanceledException());
            TimeRecord = new();
            OldFileName = $"Created for AW OD {DateTime.Now}.wav";
            await GetSndSettingExRec();

            if (SettingRec.SndFormat == null)
            {
                MessageView?.AddError("", DeviceRep["ErrorRecSetting"]);
                return;
            }
            setting = new AudioRecordSetting();

            setting.ChannelCount = SettingRec.SndFormat.Channels;
            setting.SampleRate = SettingRec.SndFormat.SampleRate;
            setting.SampleSize = SettingRec.SndFormat.SampleSize;
            setting.Label = SettingRec.Interfece;
            setting.Volum = SettingRec.SndLevel;

            channel = System.Threading.Channels.Channel.CreateBounded<byte[]>(5);

            FileTmpName = Path.GetRandomFileName();
            await _HubContext.SendCoreAsync("RecordSoundToTmpFile", new object[] { channel.Reader, FileTmpName });

            if (channel != null)
            {
                await channel.Writer.WriteAsync(new WavHeaderModel(SettingRec.SndFormat.ToBytes()).ToBytesAllHeader());
            }
            records = true;
            var reference = DotNetObjectReference.Create(this);
            setting = await JSRuntime.InvokeAsync<AudioRecordSetting>(identifier: "RecordAudio.StartStreamWorklet", setting, reference) ?? new();

        }

        private async Task SendStopWorklet()
        {
            await JSRuntime.InvokeVoidAsync("RecordAudio.StopStream");
        }

        [JSInvokable]
        public async Task StopWorklet()
        {
            records = false;
            channel?.Writer.TryComplete();
            if (SetSoundsUrlPlayer.HasDelegate)
                await SetSoundsUrlPlayer.InvokeAsync(Path.Combine("tmp", FileTmpName));
        }

        private async Task LoadFiles(InputFileChangeEventArgs e)
        {
            channel?.Writer.TryComplete(new OperationCanceledException());
            var file = e.File;
            OldFileName = e.File.Name;
            _fileLength = 0;
            _uploaded = 0;

            if (file.ContentType.ToLower() != "audio/wav" && file.ContentType.ToLower() != "audio/mpeg")
                return;
            if (file.Size > int.MaxValue / 2)
            {
                MessageView?.AddError("", $"{GsoRep["IDS_E_MAX_SIZE"]} ({int.MaxValue / 2} байт)");
                return;
            }
            _fileLength = file.Size;

            if (file.ContentType.ToLower() == "audio/wav")
            {
                if (elem != null)
                {
                    var urlFile = await JSRuntime.InvokeAsync<string>("CreateObjUrl", elem.Element);
                    if (!string.IsNullOrEmpty(urlFile))
                    {
                        if (SetSoundsUrlPlayer.HasDelegate)
                            await SetSoundsUrlPlayer.InvokeAsync(urlFile);
                    }
                }
            }
            else
            {
                try
                {
                    using var readStreamAll = file.OpenReadStream(int.MaxValue / 2, ComponentDetached);
                    channel = System.Threading.Channels.Channel.CreateBounded<byte[]>(5);
                    var r = Task.Run(async () =>
                    {
                        if (channel != null)
                        {
                            byte[] buffer = new byte[BufferSizeSignal];
                            int readCount = 0;
                            while ((readCount = await readStreamAll.ReadAsync(buffer, ComponentDetached)) > 0)
                            {
                                if (readCount > 0)
                                {
                                    _uploaded += (readCount / 1024);
                                    await InvokeAsync(StateHasChanged);
                                    await channel.Writer.WriteAsync(buffer.Take(readCount).ToArray(), ComponentDetached);
                                }
                            }
                            channel.Writer.TryComplete();
                            _uploaded = 0;
                        }
                    });

                    FileTmpName = Path.GetRandomFileName();
                    var format = await _HubContext.InvokeCoreAsync<byte[]>("ConvertSoundToTmpFile", new object[] { channel.Reader, FileTmpName });

                    if (SetSoundsUrlPlayer.HasDelegate)
                        await SetSoundsUrlPlayer.InvokeAsync(Path.Combine("tmp", FileTmpName));
                    await readStreamAll.DisposeAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

        }

        [JSInvokable]
        public void ErrorRecord(string? errorMessage = null)
        {
            channel?.Writer.TryComplete(new OperationCanceledException());
            records = false;
            StateHasChanged();
            if (string.IsNullOrEmpty(errorMessage))
                MessageView?.AddError("", StartUIRep["IDS_STRING_NEED_MIC"]);
            else
                MessageView?.AddError("", errorMessage);
        }

        [JSInvokable]
        public void StreamToAudio(byte[]? btoa)
        {
            if (btoa != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        if (SettingRec.SndFormat != null)
                        {
                            if (channel != null)
                            {
                                foreach (var item in btoa.Chunk(BufferSizeSignal))
                                {
                                    await channel.Writer.WriteAsync(item, ComponentDetached);
                                }
                            }
                            TimeRecord = TimeRecord.Add(TimeSpan.FromSeconds(btoa.Length / SettingRec.SndFormat.ByteRate));
                            StateHasChanged();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"StreamToAudio {ex.Message}");
                    }
                });


            }
        }
        public ValueTask DisposeAsync()
        {
            channel?.Writer.TryComplete(new OperationCanceledException());
            JSRuntime.InvokeVoidAsync("RecordAudio.StopStream");
            DisposeToken();
            return _HubContext.DisposeAsync();
        }
    }
}
