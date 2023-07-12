using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using SharedLibrary;
using SharedLibrary.Interfaces;
using SMControlSysProto.V1;
using SMDataServiceProto.V1;

namespace BlazorLibrary.Shared.Situation.NextPage
{
    partial class StaffPage : IAsyncDisposable, IPubSubMethod
    {
        [Parameter]
        public EventCallback<List<SMControlSysProto.V1.SituationItem>?> NextAction { get; set; }

        [Parameter]
        public List<SMControlSysProto.V1.SituationItem>? StaffList { get; set; }

        [Parameter]
        public bool? IsReadOnly { get; set; } = false;

        //private List<GrpcSMControlSys.SituationItem>? OldStaffList = null;

        private SMControlSysProto.V1.SituationItem? SelectItem = null;

        private bool ViewInfoMessage = false;

        private List<Objects>? MsgList = null;

        private Dictionary<int, string> ThList = new();

        private bool IsNewMessage = false;
        private int StaffId = 0;

        bool IsAllMsg = false;

        protected override async Task OnInitializedAsync()
        {
            ThList = new()
            {
                { -1, GsoRep["IDS_STRING_NAME1"] },
                { -2, StaffRep["GlobalNumSit"] },
                { -3, GsoRep["IDS_STRING_MESSAGE_TYPE_SOUND"] },
                { -4, StaffRep["TypeInfoCollection"] }
            };

            StaffId = await _User.GetLocalStaff();

            await GetList();
            _ = _HubContext.SubscribeAsync(this);
        }

        [Description(DaprMessage.PubSubName)]
        public async Task Fire_InsertDeleteMessage(ulong Value)
        {
            await GetList();

            if (SelectItem != null && Value > 0)
            {
                if (StaffList != null && (MsgList?.Any(x => x.OBJID?.ObjID == (int)Value) ?? false))
                {
                    var elem = MsgList.First(x => x.OBJID?.ObjID == (int)Value);

                    StaffList.Single(x => x.SitID.Equals(SelectItem.SitID)).CustMsg = elem.OBJID;
                    StaffList.Single(x => x.SitID.Equals(SelectItem.SitID)).MsgName = elem.Name;

                    SelectItem.CustMsg = elem.OBJID;
                    SelectItem.MsgName = elem.Name;
                }
            }
            else if (Value == 0 && SelectItem?.CustMsg?.ObjID == (int)Value)
            {
                SelectItem.CustMsg = new();
            }
            StateHasChanged();
        }
        [Description(DaprMessage.PubSubName)]
        public async Task Fire_UpdateMessage(ulong Value)
        {
            await GetList();
            StateHasChanged();
        }

        private void SaveCallBack(OBJ_ID? NewMsgId = null)
        {
            IsNewMessage = false;
            ViewInfoMessage = false;
        }

        private async Task GetList()
        {
            var result = await Http.PostAsJsonAsync("api/v1/GetObjects_IMessage", new OBJ_ID() { StaffID = StaffId });
            if (result.IsSuccessStatusCode)
            {
                MsgList = await result.Content.ReadFromJsonAsync<List<Objects>>() ?? new();
                MsgList?.RemoveAll(x => x.OBJID == null);
            }
            if (MsgList == null)
                MsgList = new List<Objects>();
        }

        void SetSelectList(List<SituationItem>? items)
        {
            if (items?.LastOrDefault() != null && items.Last().CustMsg?.ObjID > 0 && (!SelectItem?.Equals(items.Last()) ?? true))
            {
                IsAllMsg = MsgList?.FirstOrDefault(x => x.OBJID.ObjID == items.Last()?.CustMsg?.ObjID)?.OBJID?.SubsystemID != SubsystemType.SUBSYST_GSO_STAFF;                
            }
            else if ((!SelectItem?.Equals(items?.LastOrDefault()) ?? true))
                IsAllMsg = false;
            SelectItem = items?.LastOrDefault();
        }

        private void SetAllView(ChangeEventArgs e)
        {
            if (e != null && e.Value != null)
                IsAllMsg = (bool)e.Value;

            if (!IsAllMsg)
            {
                if (SelectItem?.CustMsg != null)
                    SelectItem.CustMsg.ObjID = 0;
            }

        }

        private async Task Next()
        {
            //меняем значение StaffID
            if (StaffList != null /*&& OldStaffList != null*/)
            {
                foreach (var item in StaffList)
                {
                    if (item.CustMsg != null)
                    {
                        if (item.CustMsg.ObjID == 0)
                        {
                            item.CustMsg.StaffID = 0;
                            item.CustMsg.ObjID = 0;
                        }
                        else
                        {
                            item.CustMsg.StaffID = MsgList?.FirstOrDefault(m => m.OBJID?.ObjID == item.CustMsg?.ObjID)?.OBJID?.StaffID ?? 0;
                        }
                    }
                }
            }

            await SaveSit(StaffList);
        }

        private async Task Cancel()
        {
            await SaveSit();
        }

        private async Task SaveSit(List<SMControlSysProto.V1.SituationItem>? NewStaffList = null)
        {
            if (NextAction.HasDelegate)
                await NextAction.InvokeAsync(NewStaffList);
        }
        public ValueTask DisposeAsync()
        {
            return _HubContext.DisposeAsync();
        }
    }
}
