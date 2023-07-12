using System.Net.Http.Json;
using Google.Protobuf;
using SMSSGsoProto.V1;
using Microsoft.AspNetCore.Components;
using SharedLibrary;
using SharedLibrary.GlobalEnums;
using SharedLibrary.Models;
using SMDataServiceProto.V1;
using static BlazorLibrary.Shared.Main;

namespace DeviceConsole.Client.Shared.SubParam
{
    partial class SubParamEdit
    {
        [CascadingParameter]
        public int SubsystemID { get; set; } = 1;

        private SubsystemParam? SubParam = new();

        private SubsystemParam? OldSubParam = new();

        private SndSetting p16XSetting = new();

        private SndSetting Oldp16XSetting = new();

        private bool IsSave = false;

        bool IsProcessing = false;

        private int StaffId = 0;

        private string TitleName = "";

        protected override async Task OnInitializedAsync()
        {

            TitleName = AsoDataRep["IDS_STRING_SUBSYSTEM_PARAMS"];

            StaffId = await _User.GetLocalStaff();

            if (SubsystemID == SubsystemType.SUBSYST_SZS)
                TitleName = GsoRep["IDS_STRING_PARAMS_SZS"];
            if (SubsystemID == SubsystemType.SUBSYST_ASO)
                TitleName = GsoRep["IDS_STRING_PARAMS_ASO"];

            await GetSubsystemParam();
            await GetSndSettingEx();

            OldSubParam = new SubsystemParam(SubParam);

            Oldp16XSetting = new SndSetting() { SndLevel = p16XSetting.SndLevel };
        }


        private async Task GetSndSettingEx()
        {
            var result = await Http.PostAsJsonAsync("api/v1/GetSndSettingEx", new OBJ_ID() { StaffID = StaffId, ObjID = (int)SoundSettingsType.RepSoundSettingType, SubsystemID = SubsystemID });
            if (result.IsSuccessStatusCode)
            {
                var json = await result.Content.ReadAsStringAsync();

                var m = JsonParser.Default.Parse<GetSndSettingExResponse>(json);

                p16XSetting = new(m.Info.Memory.ToArray());
                p16XSetting.Interfece = m.Interface;

            }
        }

        private async Task GetSubsystemParam()
        {
            var result = await Http.PostAsJsonAsync("api/v1/GetSubsystemParam", new OBJ_ID() { StaffID = StaffId, SubsystemID = SubsystemID });
            if (result.IsSuccessStatusCode)
            {
                SubParam = await result.Content.ReadFromJsonAsync<SubsystemParam>() ?? new();
                SubParam.TimeoutAbBu = SubParam.TimeoutAb;


                if (SubsystemID == SubsystemType.SUBSYST_SZS)
                {
                    SubParam.TimeoutMsg = SubParam.TimeoutMsg / 20;
                }
            }
            else
            {
                MessageView?.AddError("", GSOFormRep["IDS_EGETSUBSYSTPARAMS"]);
            }
        }

        private async Task CallBackSubParam()
        {
            IsProcessing = true;
            IsSave = false;
            if (SubParam != null && (!OldSubParam?.Equals(SubParam) ?? false))
            {
                if (SubParam.RedrawTimeout == 0)
                {
                    MessageView?.AddError(GsoRep["IDS_STRING_PARAMS_ASO"], AsoRep["ERR_REDRAW_TIME"]);
                }
                else
                {
                    if (SubsystemID == SubsystemType.SUBSYST_ASO)
                    {
                        // проверяем общие таймауты
                        if (SubParam.TimeoutAb < 60)
                        {
                            MessageView?.AddMessage(GsoRep["IDS_REG_PARAM_UPDATE"], GsoRep["IDS_STRING_TIMEOUT_BETWEEN_CALL"] + " " + AsoRep["ERR_TIMOUT_MSG"]);
                        }
                        if (SubParam.TimeoutAbBu < 60)
                        {
                            MessageView?.AddMessage(GsoRep["IDS_REG_PARAM_UPDATE"], GsoRep["IDS_STRING_TIMEOUT_BETWEEN_CALL_BUSY"] + " " + AsoRep["ERR_TIMOUT_MSG"]);
                        }
                    }

                    if (SubsystemID == SubsystemType.SUBSYST_SZS/*SUBSYST_SZS*/)
                    {
                        SubParam.TimeoutMsg = SubParam.TimeoutMsg * 20;
                    }

                    var result = await Http.PostAsJsonAsync("api/v1/UpdateSubsystemParam", SubParam);
                    if (!result.IsSuccessStatusCode)
                    {
                        MessageView?.AddError("", GSOFormRep["IDS_EFAILUPDATESUBSYSTPARAMS"]);
                    }
                    else
                    {
                        IsSave = true;
                        _ = Task.Delay(2000).ContinueWith(x =>
                        {
                            IsSave = false; StateHasChanged();
                        });
                    }

                    if (SubsystemID == SubsystemType.SUBSYST_ASO)
                    {
                        // проверяем введенные значения для Vip абонентов
                        if (SubParam.VipTioutNo < 60)
                        {
                            MessageView?.AddMessage(GsoRep["IDS_STRING_PARAMS_ASO"], AsoRep["TimoutNoAnswerVip"] + " " + AsoRep["ERR_TIMOUT_MSG"]);
                        }
                        if (SubParam.VipTioutBu < 60)
                        {
                            MessageView?.AddMessage(GsoRep["IDS_STRING_PARAMS_ASO"], AsoRep["TimoutBusyVip"] + " " + AsoRep["ERR_TIMOUT_MSG"]);
                        }

                        result = await Http.PostAsJsonAsync("api/v1/UpdateVipTimeout", new CVipTimeout() { StaffID = SubParam.StaffID, SubsystemID = SubParam.SubsystemID, VipTioutBu = SubParam.VipTioutBu, VipTioutNo = SubParam.VipTioutNo });
                        if (!result.IsSuccessStatusCode)
                        {
                            MessageView?.AddError(GsoRep["IDS_REG_PARAM_UPDATE"], AsoRep["ERR_TIMOUT_VIP"]);
                        }
                        else
                            OldSubParam = new SubsystemParam(SubParam);
                    }
                }
            }
            if (Oldp16XSetting.SndLevel != p16XSetting.SndLevel || (!Oldp16XSetting.Interfece?.Equals(p16XSetting.Interfece) ?? false))
            {
                SetSndSettingExRequest request = new() { MInfo = UnsafeByteOperations.UnsafeWrap(p16XSetting.ToBytes()), MInterface = p16XSetting.Interfece, OBJID = new OBJ_ID() { ObjID = (int)SoundSettingsType.RepSoundSettingType, StaffID = StaffId, SubsystemID = SubsystemID } };
                var result = await Http.PostAsJsonAsync("api/v1/SetSndSettingEx", JsonFormatter.Default.Format(request));
                if (!result.IsSuccessStatusCode)
                {
                    MessageView?.AddError(GsoRep["IDS_REG_PARAM_UPDATE"], AsoRep["ERR_PARAM_SND"]);
                }
                else
                {
                    Oldp16XSetting = new SndSetting() { SndLevel = p16XSetting.SndLevel };
                    IsSave = true;
                    _ = Task.Delay(2000).ContinueWith(x =>
                    {
                        IsSave = false; StateHasChanged();
                    });
                }
            }
            IsProcessing = false;
        }
    }
}
