using System.ComponentModel;
using System.Net.Http.Json;
using UUZSDataProto.V1;
using Microsoft.AspNetCore.Components;
using SMDataServiceProto.V1;
using static BlazorLibrary.Shared.Main;
using SharedLibrary.Interfaces;
using SharedLibrary;
using BlazorLibrary.Shared.Table;
using BlazorLibrary.Models;
using FiltersGSOProto.V1;
using BlazorLibrary.GlobalEnums;
using Label.V1;

namespace DeviceConsole.Client.Pages.UUZS.Groups
{
    partial class GroupList : IAsyncDisposable, IPubSubMethod
    {
        [CascadingParameter]
        public int SubsystemID { get; set; } = SubsystemType.SUBSYST_SZS;

        private CGroupInfoListOut? SelectItem = null;


        private bool? IsViewEdit = false;

        private bool? IsDelete = false;

        TableVirtualize<CGroupInfoListOut>? table;

        protected override async Task OnInitializedAsync()
        {
            request.ObjID.StaffID = await _User.GetLocalStaff();
            request.ObjID.SubsystemID = SubsystemID;

            ThList = new Dictionary<int, string>
            {
                { 0, UUZSRep["IDS_STRING_NAME_GROUP"] },
                { 1, UUZSRep["IDS_STRING_GROUP_NUMBER"] },
                { 2, UUZSRep["IDS_STRING_PRIORITY"] }
            };

            HintItems.Add(new HintItem(nameof(FiltrModel.Name), UUZSRep["IDS_STRING_NAME_GROUP"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 20 }, LoadHelpName)));
            HintItems.Add(new HintItem(nameof(FiltrModel.Number), UUZSRep["IDS_STRING_GROUP_NUMBER"], TypeHint.Number));
            HintItems.Add(new HintItem(nameof(FiltrModel.Prior), UUZSRep["IDS_STRING_PRIORITY"], TypeHint.Number));

            await OnInitFiltr(RefreshTable, FiltrName.FiltrGroup);

            _ = _HubContext.SubscribeAsync(this);
        }
        [Description(DaprMessage.PubSubName)]
        public async Task Fire_UpdateTermDevicesGroup(long Value)
        {
            SelectItem = null;
            await CallRefreshData();
            StateHasChanged();
        }
        [Description(DaprMessage.PubSubName)]
        public async Task Fire_InsertDeleteTermDevicesGroup(long Value)
        {
            SelectItem = null;
            await CallRefreshData();
            StateHasChanged();
        }


        ItemsProvider<CGroupInfoListOut> GetProvider => new ItemsProvider<CGroupInfoListOut>(ThList, LoadChildList, request, new List<int>() { 60, 20, 20 });

        private async ValueTask<IEnumerable<CGroupInfoListOut>> LoadChildList(GetItemRequest req)
        {
            List<CGroupInfoListOut> newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetItems_IGroup", req, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                newData = await result.Content.ReadFromJsonAsync<List<CGroupInfoListOut>>() ?? new();
            }
            else
            {
                MessageView?.AddError("", UUZSRep["IDS_STRING_ERR_INFO_COUNT_GROUPS"]);
            }
            return newData;
        }

        private async ValueTask<IEnumerable<Hint>> LoadHelpName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetGroupNameForGroups", new IntAndString() { Number = request.ObjID.StaffID, Str = req.BstrFilter }, ComponentDetached);
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

        private async Task RefreshTable()
        {
            SelectItem = null;
            if (table != null)
                await table.ResetData();
        }

        private async Task DeleteGroup()
        {
            if (SelectItem != null)
            {
                List<string>? r = null;

                OBJ_ID GroupObj = new() { ObjID = SelectItem.GroupID, StaffID = request.ObjID.StaffID, SubsystemID = request.ObjID.SubsystemID };

                var result = await Http.PostAsJsonAsync("api/v1/GetLinkObjects_IGroup", GroupObj);
                if (result.IsSuccessStatusCode)
                {
                    r = await result.Content.ReadFromJsonAsync<List<string>>();
                }

                if (r != null && r.Count > 0)
                {
                    MessageView?.AddError(AsoRep["IDS_STRING_DELETE_DENIDE"] + ", " + AsoRep["ERR_DELETE_DENIDE"].ToString().Replace("{name}", SelectItem.GroupName), r);
                }
                else
                {
                    result = await Http.PostAsJsonAsync("api/v1/DeleteGroup", GroupObj);
                    if (result.IsSuccessStatusCode)
                    {
                        SelectItem = null;
                    }
                    else
                        MessageView?.AddError("", GsoRep["IDS_E_DELETE"]);
                }
                IsDelete = false;
            }
        }


        private void CallBackEvent()
        {
            IsViewEdit = false;
            SelectItem = null;
        }

       

        public ValueTask DisposeAsync()
        {
            DisposeToken();
            return _HubContext.DisposeAsync();
        }
    }
}
