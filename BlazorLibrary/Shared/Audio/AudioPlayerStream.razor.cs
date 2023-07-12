using System.Net.Http.Json;
using BlazorLibrary.Helpers;
using Google.Protobuf;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SharedLibrary.GlobalEnums;
using SharedLibrary.Models;
using SMDataServiceProto.V1;

namespace BlazorLibrary.Shared.Audio
{
    partial class AudioPlayerStream
    {
        [Parameter]
        public bool? IsAutoPlay { get; set; } = false;

        [Parameter]
        public string? TitleName { get; set; } = null;

        [Parameter]
        public string? BgColor { get; set; }

        private string? Blob = null;

        private bool IsSoundUrl { get; set; } = false;

        private bool IsLoadAudio = false;

        private ElementReference player;

        private SndSetting SettingSound = new();

        protected override async Task OnInitializedAsync()
        {
            await GetSndSettingExSound();
        }

        private async Task GetSndSettingExSound()
        {
            SettingSound = await _localStorage.GetSndSettingEx(SoundSettingsType.RepSoundSettingType) ?? new();
        }

        public async Task SetUrlSound(string url, bool? isDeleteOld = true)
        {
            IsLoadAudio = true;
            if (Blob != null && isDeleteOld == true)
            {
                try
                {
                    await JSRuntime.InvokeVoidAsync("RemoveBlob", Blob);
                }
                catch
                {
                    Console.WriteLine($"Error remove blob {Blob}");
                }
            }
            Blob = url;      
            await CheckUrl();            
        }

        private async Task CheckUrl()
        {
            IsSoundUrl = false;
            long length = 0;

            length = await Http.GetLengthFileAsync(Blob);

            if (length > 8000)
            {
                IsSoundUrl = true;
                await WaitSettingSound();                
            }
        }


        private async Task WaitSettingSound()
        {
            IsLoadAudio = false;
            StateHasChanged();
            await JSRuntime.InvokeVoidAsync("InitAudioPlayer", player, SettingSound.Interfece, SettingSound.SndLevel);
        }
    }
}
