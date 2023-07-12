using System.Net.Http.Json;
using BlazorLibrary.Models;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using SMDataServiceProto.V1;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SharedLibrary;
using SharedLibrary.GlobalEnums;
using SharedLibrary.Models;
using static BlazorLibrary.Shared.Main;

namespace DeviceConsole.Client.Shared.SubParam
{
    partial class SubParamP16
    {
        [CascadingParameter]
        public int SubsystemID { get; set; } = 4;

        private SndSettingP16? p16XSetting = new();

        private SndSettingP16 Oldp16XSetting = new();

        private bool IsUpdate = false;

        private int StaffId = 0;

        private string TitleName = "";

        private bool m_BNotifyStaff = false;

        private bool IsSave = false;
        private bool IsProcessing = false;
        private List<DeviceSoundInfo>? Model;

        ElementReference? elem;
        private TimeOnly IDC_NOTIFYTIME
        {
            get
            {
                if (p16XSetting != null && p16XSetting.NotifyTime > 0)
                {
                    var h = (int)p16XSetting.NotifyTime / 3600;
                    var m = (int)(p16XSetting.NotifyTime - h * 3600) / 60;
                    var s = (int)(p16XSetting.NotifyTime - h * 3600 - m * 60);

                    return new(h, m, s);
                }
                else
                    return new();
            }
            set
            {
                if (p16XSetting != null)
                    p16XSetting.NotifyTime = (uint)(value.Hour * 3600 + value.Minute * 60 + value.Second);
            }
        }

        protected override async Task OnInitializedAsync()
        {
            TitleName = SMP16xFormRep["IDS_STRING_PARAMS_SETTINGS"];

            StaffId = await _User.GetLocalStaff();

            await GetListDevice();
            await GetParams();
            await GetSndSettingEx();
            elem?.FocusAsync();
        }

        private async Task SaveSetting()
        {
            IsProcessing = true;
            await SetParams();
            if (p16XSetting != null)
            {
                if (Model != null && Model.Any(x => x.label == p16XSetting.Interfece))
                    p16XSetting.SndSource = (UInt16)Model.FindIndex(x => x.label == p16XSetting.Interfece);
                else
                    p16XSetting.SndSource = 0;
                SetSndSettingExRequest request = new() { MInfo = UnsafeByteOperations.UnsafeWrap(p16XSetting.ToBytes()), MInterface = p16XSetting.Interfece, OBJID = new OBJ_ID() { StaffID = StaffId, ObjID = (int)SoundSettingsType.P16SoundSettingType, SubsystemID = SubsystemID } };

                await Http.PostAsJsonAsync("api/v1/SetSndSettingEx", JsonFormatter.Default.Format(request)).ContinueWith(x =>
                {
                    if (!x.Result.IsSuccessStatusCode)
                    {
                        MessageView?.AddError("", StartUIRep["IDS_ERRORCAPTION"]);
                    }
                    else
                    {
                        IsSave = true;
                        StateHasChanged();
                    }
                });
                _ = Task.Delay(2000).ContinueWith(x =>
                {
                    IsSave = false; StateHasChanged();
                });
            }
            IsProcessing = false;

        }

        private void ChangeFormat(WavOnlyHeaderModel? newFormat)
        {
            if (newFormat != null && p16XSetting != null)
                p16XSetting.SndFormat = newFormat;
            StateHasChanged();
        }

        private async Task GetListDevice()
        {
            Model = await JSRuntime.InvokeAsync<List<DeviceSoundInfo>>("GetAudioTrack");

            if (Model != null)
            {
                Model.RemoveAll(x => x.deviceId == "communications" || x.deviceId == "default");

                Model.ForEach(x => x.label = string.IsNullOrEmpty(x.label) ? x.kind : x.label);
                Model.ForEach(x => x.deviceId = string.IsNullOrEmpty(x.deviceId) ? x.groupId : x.deviceId);
            }
        }
        private async Task GetSndSettingEx()
        {
            await Http.PostAsJsonAsync("api/v1/GetSndSettingEx", new OBJ_ID() { StaffID = StaffId, ObjID = (int)SoundSettingsType.P16SoundSettingType, SubsystemID = SubsystemID }).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    var json = await x.Result.Content.ReadAsStringAsync();

                    var m = JsonParser.Default.Parse<GetSndSettingExResponse>(json);
                    p16XSetting = new(m.Info.Memory.ToArray());
                    p16XSetting.Interfece = m.Interface;

                    Oldp16XSetting = new(m.Info.Memory.ToArray());
                    Oldp16XSetting.Interfece = m.Interface;
                }
                else
                    MessageView?.AddError("", SMP16xFormRep["IDS_STRING_ERR_INFO_PARAMS"]);
            });
        }

        private async Task GetParams()
        {
            await Http.PostAsJsonAsync("api/v1/GetParams", new StringValue() { Value = nameof(ParamsSystem.TotalStopByNewNotify) }).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    var g = await x.Result.Content.ReadFromJsonAsync<StringValue>() ?? new();
                    m_BNotifyStaff = g.Value == "1" ? true : false;
                }
                else
                    MessageView?.AddError("", SMP16xFormRep["IDS_STRING_ERR_INFO_PARAMS"]);
            });
        }

        private async Task SetParams()
        {
            await Http.PostAsJsonAsync("api/v1/SetParams", new SetParamRequest() { ParamName = nameof(ParamsSystem.TotalStopByNewNotify), ParamValue = m_BNotifyStaff == true ? "1" : "0" }).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    var r = await x.Result.Content.ReadFromJsonAsync<SMDataServiceProto.V1.String>();
                    if (string.IsNullOrEmpty(r?.Value))
                    {
                        MessageView?.AddError("", AsoRep["IDS_E_REPORTER_SAVE"]);
                    }
                }
                else
                    MessageView?.AddError("", AsoRep["IDS_E_REPORTER_SAVE"]);
            });
        }
    }
}
