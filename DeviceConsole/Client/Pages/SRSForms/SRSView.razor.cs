using System.Net.Http.Json;
using BlazorLibrary;
using Google.Protobuf.WellKnownTypes;
using SMDataServiceProto.V1;
using StaffDataProto.V1;
using Microsoft.AspNetCore.Components;
using SharedLibrary;
using SharedLibrary.Models;
using SharedLibrary.Utilities;
using static BlazorLibrary.Shared.Main;

namespace DeviceConsole.Client.Pages.SRSForms
{
    partial class SRSView
    {
        private Dictionary<int, string>? ThList;

        List<SRSLine>? Model { get; set; }

        SRSLine? SelectItem { get; set; }

        SRSLine NewItem { get; set; } = new();

        readonly Dictionary<CcommSubSystem, List<Objects>> SubSystemList = new();

        bool IsDelete = false;

        bool IsAddObject = false;

        bool IsProcessing = false;

        private int StaffId = 0;

        string IpAddress = string.Empty;

        uint TypeConnect
        {
            get
            {
                return NewItem.Version;
            }
            set
            {
                NewItem.Port = 0;
                NewItem.Line = 0;
                NewItem.Version = value;
            }
        }

        protected override async Task OnInitializedAsync()
        {
            ThList = new Dictionary<int, string>
            {
                { -1, SRSRep["IDS_STRING_N_PORT"] },//№ порта
                { -2, SRSRep["IDS_STRING_N_CHANNEL"] },//№ канала
                { -3, SRSRep["IDS_STRING_SUBSYSTEM"] },//Подсистема
                { -4, SRSRep["IDS_STRING_SITUATION"] },//Сценарий
            };

            StaffId = await _User.GetLocalStaff();
            await GetSubSystemList();
            await GetList();
        }


        private async Task GetList()
        {
            var result = await Http.PostAsync("api/v1/LoadSRSConfig", null, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                Model = await result.Content.ReadFromJsonAsync<List<SRSLine>>() ?? new();
            }
            else
            {
                MessageView?.AddError("", SRSRep["ERROR_GET_DATA"]);
            }

            if (Model == null)
                Model = new();
        }

        private async Task SaveSRSConfig()
        {
            var result = await Http.PostAsJsonAsync("api/v1/SaveSRSConfig", Model, ComponentDetached);
            if (!result.IsSuccessStatusCode)
            {
                MessageView?.AddError("", SRSRep["IDS_STRING_ERR_SAVE"]);
            }
        }

        private async Task GetSubSystemList()
        {
            var result = await Http.PostAsync("api/v1/GetSubSystemList", null);
            if (result.IsSuccessStatusCode)
            {
                var response = await result.Content.ReadFromJsonAsync<List<CcommSubSystem>>();

                if (response != null)
                {
                    foreach (var item in response)
                    {
                        switch (item.SubSystID)
                        {
                            case SubsystemType.SUBSYST_ASO:
                            case SubsystemType.SUBSYST_SZS:
                            case SubsystemType.SUBSYST_GSO_STAFF:
                            {
                                var sitList = await GetSituationList(new OBJ_ID() { StaffID = StaffId, SubsystemID = item.SubSystID });
                                if (sitList?.Count > 0)
                                {
                                    SubSystemList.Add(item, sitList);
                                }
                            }
                            break;
                        }
                    }
                }
            }
        }

        private async Task<List<Objects>?> GetSituationList(OBJ_ID request)
        {
            var result = await Http.PostAsJsonAsync("api/v1/GetObjects_ISituation", request, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                return await result.Content.ReadFromJsonAsync<List<Objects>>();
            }
            else
            {
                string error = SRSRep["IDS_STRING_ERROR"];

                switch (request.SubsystemID)
                {
                    case SubsystemType.SUBSYST_ASO: error = SMP16xFormRep["ERROR_GET_SIT_ASO"]; break;
                    case SubsystemType.SUBSYST_SZS: error = SMP16xFormRep["ERROR_GET_SIT_SZS"]; break;
                    case SubsystemType.SUBSYST_GSO_STAFF: error = SMP16xFormRep["ERROR_GET_SIT_STAFF"]; break;
                }

                MessageView?.AddError("", error);
            }

            return null;
        }


        async Task DeleteSrs()
        {
            if (SelectItem == null || Model == null)
            {
                IsDelete = false;
                return;
            }
            var countDel = Model.RemoveAll(x => x.Equals(SelectItem));

            if (countDel > 0)
            {
                await SaveSRSConfig();
            }
            IsDelete = false;
        }

        async Task SaveSrs()
        {
            bool IsValid = true;
            IsProcessing = true;
            NewItem.Port = IpAddressUtilities.StringToUint(IpAddress);
            if (NewItem.Version == 0)
            {
                MessageView?.AddError(SRSRep["IDS_STRING_INVALID_SET_PARAMS"], $"{SRSRep["CONNECT_TYPE"]} - {Rep["NoData"]}");
                IsValid = false;
            }

            if ((NewItem.Version == 3 && NewItem.Port == 0) || (NewItem.Version == 2 && (NewItem.Port < 1 || NewItem.Port > 127)))
            {
                string error = $"{SRSRep["IDS_STRING_PORT"]} [1..127]";
                if (NewItem.Version == 3)
                    error = $"{SRSRep["IDS_STRING_ADDR"]} [192.168.1.1]";
                MessageView?.AddError(SRSRep["IDS_STRING_INVALID_SET_PARAMS"], error);
                IsValid = false;
            }

            if (NewItem.Line < 1 || NewItem.Line > 64)
            {
                MessageView?.AddError(SRSRep["IDS_STRING_INVALID_SET_PARAMS"], $"{SRSRep["IDS_STRING_LINE"]} [1..64]");
                IsValid = false;
            }

            if (NewItem.SubSystID == 0)
            {
                MessageView?.AddError(SRSRep["IDS_STRING_INVALID_SET_PARAMS"], $"{SRSRep["IDS_STRING_SUBSYSTEM"]} - {Rep["NoData"]}");
                IsValid = false;
            }
            if (NewItem.SitID == 0)
            {
                MessageView?.AddError(SRSRep["IDS_STRING_INVALID_SET_PARAMS"], $"{SRSRep["IDS_STRING_SITUATION"]} - {Rep["NoData"]}");
                IsValid = false;
            }

            if (IsValid)
            {
                if (Model == null)
                    Model = new();

                if (NewItem.Id == 0)
                {
                    NewItem.Id = Model.Count + 1;
                    Model.Add(NewItem);
                    await SaveSRSConfig();
                }
                else
                {
                    var elem = Model.First(x => x.Id == NewItem.Id);
                    elem.SubSystID = NewItem.SubSystID;
                    elem.SitID = NewItem.SitID;
                    await SaveSRSConfig();
                }
                NewItem = new();
                IsAddObject = false;
            }

            IsProcessing = false;
        }


        void NewOrEdit(bool? isEdit = true)
        {
            if (isEdit == true && SelectItem == null)
                return;
            NewItem = new()
            {
                Version = 1,
                SubSystID = (uint)(SubSystemList.FirstOrDefault().Key?.SubSystID ?? 0),
                StaffID = (uint)StaffId
            };
            if (isEdit == false)
            {
                SelectItem = null;
            }
            else if (SelectItem != null)
            {
                NewItem = new()
                {
                    Id = SelectItem.Id,
                    Version = SelectItem.Version,
                    SubSystID = SelectItem.SubSystID,
                    StaffID = SelectItem.StaffID,
                    Line = SelectItem.Line,
                    Port = SelectItem.Port,
                    SitID = SelectItem.SitID
                };
            }
            IpAddress = IpAddressUtilities.UintToIpString(NewItem.Port);
            if (NewItem.SitID == 0)
                ChangeSubSystem(NewItem.SubSystID);
            IsAddObject = true;
        }

        void ChangeEventArgsSubsystem(ChangeEventArgs e)
        {
            int.TryParse(e?.Value?.ToString(), out int subSystemId);

            if (subSystemId > 0)
            {
                NewItem.SubSystID = (uint)subSystemId;
                ChangeSubSystem(NewItem.SubSystID);
            }
        }


        void ChangeSubSystem(uint subSystemId)
        {
            if (SubSystemList.FirstOrDefault(x => x.Key?.SubSystID == subSystemId).Value?.Count > 0)
            {
                NewItem.SitID = (uint)(SubSystemList.FirstOrDefault(x => x.Key?.SubSystID == subSystemId).Value.First().OBJID?.ObjID ?? 0);
            }
        }

    }
}
