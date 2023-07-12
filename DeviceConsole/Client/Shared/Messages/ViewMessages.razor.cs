using System.ComponentModel;
using System.Net.Http.Json;
using SMSSGsoProto.V1;
using Microsoft.AspNetCore.Components;
using SharedLibrary;
using SMDataServiceProto.V1;
using static BlazorLibrary.Shared.Main;
using SharedLibrary.Interfaces;
using BlazorLibrary.Models;
using BlazorLibrary.Shared.Table;
using Google.Protobuf.WellKnownTypes;
using FiltersGSOProto.V1;
using BlazorLibrary.GlobalEnums;

namespace DeviceConsole.Client.Shared.Messages
{
    partial class ViewMessages : IAsyncDisposable, IPubSubMethod
    {
        [CascadingParameter]
        public int SubsystemID { get; set; } = SubsystemType.SUBSYST_ASO;

        private List<MessageItem>? SelectedList = null;

        private bool? IsViewEdit = false;

        private bool? IsDelete = false;

        private string TitleName = "";

        TableVirtualize<MessageItem>? table;

        protected override async Task OnInitializedAsync()
        {
            TitleName = AsoDataRep["IDS_STRING_MESSAGE"];

            request.ObjID.StaffID = await _User.GetLocalStaff();
            request.ObjID.SubsystemID = SubsystemID;

            switch (request.ObjID.SubsystemID)
            {
                case SubsystemType.SUBSYST_ASO: TitleName = GsoRep["IDS_STRING_MESSAGES_ASO"]; break;
                case SubsystemType.SUBSYST_GSO_STAFF: TitleName = GsoRep["IDS_STRING_MESSAGES_CU"]; break;
                case SubsystemType.SUBSYST_SZS: TitleName = GsoRep["IDS_STRING_MESSAGES_SZS"]; break;
                case SubsystemType.SUBSYST_P16x: TitleName = GsoRep["IDS_STRING_MESSAGES_P16x"]; break;
            }
            ThList = new Dictionary<int, string>
            {
                { 0, GsoRep["IDS_STRING_NAME"] },
                { 1, AsoRep["IDS_STRING_COMMENT"] }
            };

            HintItems.Add(new HintItem(nameof(FiltrModel.Name), GsoRep["IDS_STRING_NAME"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 20 }, LoadHelpName)));
            HintItems.Add(new HintItem(nameof(FiltrModel.Comment), AsoRep["IDS_STRING_COMMENT"], TypeHint.Input));

            await OnInitFiltr(RefreshTable, FiltrName.FiltrMessage);


            _ = _HubContext.SubscribeAsync(this);
        }

        [Description(DaprMessage.PubSubName)]
        public async Task Fire_InsertDeleteMessage(ulong Value)
        {
            SelectedList = null;
            await CallRefreshData();
            StateHasChanged();
        }
        [Description(DaprMessage.PubSubName)]
        public async Task Fire_UpdateMessage(ulong Value)
        {
            SelectedList = null;
            await CallRefreshData();
            StateHasChanged();
        }

        ItemsProvider<MessageItem> GetProvider => new ItemsProvider<MessageItem>(ThList, LoadChildList, request, new List<int>() { 50, 50 });

        private async ValueTask<IEnumerable<MessageItem>> LoadChildList(GetItemRequest req)
        {
            List<MessageItem> newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetItems_IMessage", req, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                newData = await result.Content.ReadFromJsonAsync<List<MessageItem>>() ?? new();
            }
            return newData;
        }

        private async ValueTask<IEnumerable<Hint>> LoadHelpName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetMessageNameForMessages", new OBJIDAndStr() { OBJID = request.ObjID, Str = req.BstrFilter }, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                var response = await result.Content.ReadFromJsonAsync<List<IntAndString>>();

                if (response?.Count > 0)
                {
                    newData.AddRange(response.Select(x => new Hint(x.Str)));
                }
            }
            return newData ?? new();
        }

        private void CallBackEvent()
        {
            IsViewEdit = false;
            SelectedList = null;
        }

        private void AddItem(List<MessageItem>? item)
        {
            if (item?.Where(x => x.MsgID != 0).Count() == 0)
                SelectedList = null;
            else
                SelectedList = item?.Where(x => x.MsgID != 0).ToList();
        }

        private void ViewDelete()
        {
            if (SelectedList?.Any() ?? false)
                IsDelete = true;
        }

        private async Task RefreshTable()
        {
            SelectedList = null;
            if (table != null)
                await table.ResetData();
        }

        private async Task DeleteMsg()
        {
            if (SelectedList?.Any() ?? false)
            {
                List<string>? r = null;

                OBJ_ID obj = new OBJ_ID() { StaffID = request.ObjID.StaffID, SubsystemID = request.ObjID.SubsystemID };

                foreach (var item in SelectedList)
                {
                    obj.ObjID = item.MsgID;
                    var result = await Http.PostAsJsonAsync("api/v1/GetLinkObjects_IMessage", obj);
                    if (result.IsSuccessStatusCode)
                    {
                        r = await result.Content.ReadFromJsonAsync<List<string>>();
                    }

                    if (r != null && r.Count > 0)
                    {
                        MessageView?.AddError(AsoRep["IDS_STRING_DELETE_DENIDE"] + ", " + AsoRep["ERR_DELETE_DENIDE"].ToString().Replace("{name}", item.MsgName), r);
                    }
                    else
                    {
                        result = await Http.PostAsJsonAsync("api/v1/DeleteMsg", obj);
                        if (!result.IsSuccessStatusCode)
                        {
                            MessageView?.AddError(GsoRep["IDS_REG_MESS_DELETE"], item.MsgName + " " + AsoRep["IDS_EFAIL_DELETEMESSAGE"]);
                        }
                        else
                        {
                            MessageView?.AddMessage(GsoRep["IDS_REG_MESS_DELETE"], item.MsgName + "-" + AsoRep["IDS_OK_DELETE"]);
                        }
                    }
                }
                SelectedList = null;
                IsDelete = false;
            }
        }

        private async Task ChangeView()
        {
            SelectedList = null;
            request.ObjID.SubsystemID = request.ObjID.SubsystemID == 0 ? SubsystemID : 0;
            await CallRefreshData();
        }

        public ValueTask DisposeAsync()
        {
            DisposeToken();
            return _HubContext.DisposeAsync();
        }
    }
}
