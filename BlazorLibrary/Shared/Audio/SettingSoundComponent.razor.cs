using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlazorLibrary.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SharedLibrary.GlobalEnums;
using SharedLibrary.Models;
using SharedLibrary;
using static BlazorLibrary.Shared.Main;

namespace BlazorLibrary.Shared.Audio
{
    partial class SettingSoundComponent
    {
        [Parameter]
        public EventCallback Callback { get; set; }

        private List<DeviceSoundInfo>? Model;

        private bool IsSave = false;

        private SndSetting SettingRec = new();
        private SndSetting SettingSound = new();

        ElementReference? elem;

        protected override async Task OnInitializedAsync()
        {
            await GetListDevice();
            await GetSndSettingExRec();
            await GetSndSettingExSound();
            elem?.FocusAsync();
        }

        private async Task GetListDevice()
        {
            try
            {
                Model = await JSRuntime.InvokeAsync<List<DeviceSoundInfo>?>("GetAudioTrack");

                if (Model != null)
                {
                    Model.ForEach(x => x.label = string.IsNullOrEmpty(x.label) ? x.kind : x.label);
                    Model.ForEach(x => x.deviceId = string.IsNullOrEmpty(x.deviceId) ? x.groupId : x.deviceId);
                }
                else
                    Model = new();
            }
            catch
            {
                MessageView?.AddError(GsoRep["IDS_STRING_SB_PARAMS"], StartUIRep["IDS_ERRORCAPTION"]);
            }
        }

        private async Task GetSndSettingExRec()
        {
            SettingRec = await _localStorage.GetSndSettingEx(SoundSettingsType.RecSoundSettingType) ?? new();
        }

        private async Task GetSndSettingExSound()
        {
            SettingSound = await _localStorage.GetSndSettingEx(SoundSettingsType.RepSoundSettingType) ?? new();
        }


        private void ChangeFormat(WavOnlyHeaderModel? newFormat)
        {
            if (newFormat != null)
                SettingRec.SndFormat = newFormat;
            StateHasChanged();
        }

        private async Task SaveSetting()
        {
            if (Model != null)
            {
                SettingRec.SndSource = (UInt16)Model.FindIndex(x => x.deviceId == SettingRec.Interfece);
                SettingSound.SndSource = (UInt16)Model.FindIndex(x => x.deviceId == SettingSound.Interfece);
            }
            try
            {
                await _localStorage.SaveSndSettingEx(SoundSettingsType.RecSoundSettingType, SettingRec);
                await _localStorage.SaveSndSettingEx(SoundSettingsType.RepSoundSettingType, SettingSound);
                IsSave = true;
                StateHasChanged();
            }
            catch
            {
                MessageView?.AddError("", StartUIRep["IDS_ERRORCAPTION"]);
            }
            _ = Task.Delay(2000).ContinueWith(x =>
            {
                IsSave = false; StateHasChanged();
            });
        }
    }
}
