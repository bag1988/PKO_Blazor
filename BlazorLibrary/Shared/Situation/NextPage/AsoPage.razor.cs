using System.ComponentModel;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using SharedLibrary;
using SharedLibrary.Interfaces;
using SMDataServiceProto.V1;

namespace BlazorLibrary.Shared.Situation.NextPage
{
    partial class AsoPage : IAsyncDisposable, IPubSubMethod
    {
        [Parameter]
        public EventCallback<OBJ_ID> NextAction { get; set; }

        [Parameter]
        public int? MsgID { get; set; }

        private bool ViewInfoMessage = false;

        private List<Objects>? Model = null;

        private Objects? SelectItem = null;

        private bool IsNewMessage = false;
        private int StaffId = 0;

        bool IsAllMsg = false;

        protected override async Task OnInitializedAsync()
        {
            StaffId = await _User.GetLocalStaff();
            await GetList();

            _ = _HubContext.SubscribeAsync(this);
        }
        [Description(DaprMessage.PubSubName)]
        public async Task Fire_InsertDeleteMessage(ulong Value)
        {
            await GetList();

            if (Value > 0)
            {
                SelectItem = Model?.Where(x => x.OBJID != null).FirstOrDefault(x => x.OBJID.ObjID == (int)Value);
            }
            else
            {
                SelectItem = null;
            }

            StateHasChanged();
        }
        [Description(DaprMessage.PubSubName)]
        public async Task Fire_UpdateMessage(ulong Value)
        {
            await GetList();
            if (Value > 0)
            {
                SelectItem = Model?.Where(x => x.OBJID != null).FirstOrDefault(x => x.OBJID.ObjID == (int)Value);
            }
            else
            {
                SelectItem = null;
            }
            StateHasChanged();
        }

        private void SaveCallBack(OBJ_ID? newMsg)
        {
            IsNewMessage = false;
            ViewInfoMessage = false;
        }

        private async Task GetList()
        {
            var result = await Http.PostAsJsonAsync("api/v1/GetObjects_IMessage", new OBJ_ID() { StaffID = StaffId });

            if (result.IsSuccessStatusCode)
            {
                Model = await result.Content.ReadFromJsonAsync<List<Objects>>() ?? new();
                Model.RemoveAll(x => x.OBJID == null);

                if (MsgID != null)
                {
                    SelectItem = Model.FirstOrDefault(m => m.OBJID.ObjID == MsgID);

                    if(SelectItem != null)
                    {
                        IsAllMsg = SelectItem.OBJID?.SubsystemID != SubsystemType.SUBSYST_ASO;
                    }
                }
            }
            if (Model == null)
                Model = new List<Objects>();
        }

        private void SetAllView(ChangeEventArgs e)
        {
            if (e != null && e.Value != null)
                IsAllMsg = (bool)e.Value;

            if (!IsAllMsg)
                SelectItem = null;
        }

        private async Task Next()
        {
            if (SelectItem == null)
                return;

            if (NextAction.HasDelegate)
                await NextAction.InvokeAsync(SelectItem.OBJID);
        }

        private async Task Cancel()
        {
            if (NextAction.HasDelegate)
                await NextAction.InvokeAsync();
        }
        public ValueTask DisposeAsync()
        {
            return _HubContext.DisposeAsync();
        }

    }
}
