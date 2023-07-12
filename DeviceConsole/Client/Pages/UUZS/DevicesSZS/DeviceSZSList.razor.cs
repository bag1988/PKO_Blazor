using System.ComponentModel;
using System.Net.Http.Json;
using BlazorLibrary.Shared.Table;
using UUZSDataProto.V1;
using Microsoft.AspNetCore.Components;
using SharedLibrary;
using SMDataServiceProto.V1;
using static BlazorLibrary.Shared.Main;
using SharedLibrary.Interfaces;
using BlazorLibrary.GlobalEnums;
using BlazorLibrary.Models;
using FiltersGSOProto.V1;
using Label.V1;

namespace DeviceConsole.Client.Pages.UUZS.DevicesSZS
{
    partial class DeviceSZSList : IAsyncDisposable, IPubSubMethod
    {
        [Parameter]
        public int DevType { get; set; } = 0;

        public int SubsystemID { get; set; } = SubsystemType.SUBSYST_SZS;

        private Tuple<bool, CGetDeviceInfo>? SelectItem = null;

        private bool? IsViewEdit = false;

        private bool? IsDelete = false;

        private bool IsViewObjList = false;

        private bool OnDelete = false;

        List<string>? ListObj = new();

        TableVirtualize<Tuple<bool, CGetDeviceInfo>>? table;

        protected override async Task OnInitializedAsync()
        {
            request.ObjID.StaffID = await _User.GetLocalStaff();
            request.ObjID.SubsystemID = SubsystemID;

            ThList = new Dictionary<int, string>
            {
                { 0, UUZSRep["IDS_STRING_NAME"] },
                { 1, UUZSRep["IDS_STRING_SERIAL_NUMBER"] },
                { 2, UUZSRep["IDS_STRING_ALIGMENT"] },
                { 3, UUZSRep["IDS_STRING_PRIORITY"] },
                { 4, UUZSRep["IDS_STRING_GLOB_NUMBER"] },
                { 5, UUZSRep["IDS_STRING_NUMBER_LINE"] },
                { 6, UUZSRep["IDS_STRING_LOCATION"] },
                { 7, UUZSRep["IDS_STRING_COMMENT"] }
            };


            HintItems.Add(new HintItem(nameof(FiltrModel.Name), UUZSRep["IDS_STRING_NAME"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 20 }, LoadHelpDevName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.Serial), UUZSRep["IDS_STRING_SERIAL_NUMBER"], TypeHint.Number));

            HintItems.Add(new HintItem(nameof(FiltrModel.Address), UUZSRep["IDS_STRING_ALIGMENT"], TypeHint.Input));

            HintItems.Add(new HintItem(nameof(FiltrModel.Prior), UUZSRep["IDS_STRING_PRIORITY"], TypeHint.Number));

            HintItems.Add(new HintItem(nameof(FiltrModel.GlobalNumber), UUZSRep["IDS_STRING_GLOB_NUMBER"], TypeHint.Number));

            HintItems.Add(new HintItem(nameof(FiltrModel.ConnParam), UUZSRep["IDS_STRING_NUMBER_LINE"], TypeHint.Input));

            HintItems.Add(new HintItem(nameof(FiltrModel.Location), UUZSRep["IDS_STRING_LOCATION"], TypeHint.Input));

            HintItems.Add(new HintItem(nameof(FiltrModel.Comment), UUZSRep["IDS_STRING_COMMENT"], TypeHint.Input));

            await OnInitFiltr(RefreshTable, FiltrName.FiltrSzsEndDevice);


            _ = _HubContext.SubscribeAsync(this);
        }

        protected override async Task OnParametersSetAsync()
        {
            request.NObjType = DevType;
            request.ObjID.ObjID = DevType;
            await CallRefreshData();
            table?.SetFocus();
        }

        [Description(DaprMessage.PubSubName)]
        public async Task Fire_UpdateTermDevice(long Value)
        {
            SelectItem = null;
            await CallRefreshData();
            StateHasChanged();
        }
        [Description(DaprMessage.PubSubName)]
        public async Task Fire_InsertDeleteTermDevice(long Value)
        {
            SelectItem = null;
            await CallRefreshData();
            StateHasChanged();
        }

        ItemsProvider<Tuple<bool, CGetDeviceInfo>> GetProvider => new ItemsProvider<Tuple<bool, CGetDeviceInfo>>(ThList, LoadChildList, request, new List<int>() { 20, 15, 15, 7, 7, 9, 15, 12 });

        private async ValueTask<IEnumerable<Tuple<bool, CGetDeviceInfo>>> LoadChildList(GetItemRequest req)
        {
            List<Tuple<bool, CGetDeviceInfo>> response = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetItems_ITerminalDevice", req, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                var newData = await result.Content.ReadFromJsonAsync<List<CGetDeviceInfo>>() ?? new();

                foreach (var item in newData.GroupBy(x => x.CDeviceInfoListOut.DevID))
                {
                    foreach (var t in item)
                    {
                        response.Add(new Tuple<bool, CGetDeviceInfo>((item.Count() == 0 || t.Equals(item.First()) ? true : false), t));
                    }
                }
            }
            else
            {
                MessageView?.AddError("", UUZSRep["IDS_STRING_CANNOT_INFO_COUNT_DEVICES"]);
            }
            return response;
        }


        private async ValueTask<IEnumerable<Hint>> LoadHelpDevName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetDevNameForEndDevice", new OBJIDAndStr() { OBJID = request.ObjID, Str = req.BstrFilter });
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

        private async Task DeleteDevice()
        {
            if (SelectItem == null)
                return;


            OBJ_ID r = new(request.ObjID) { ObjID = SelectItem.Item2.CDeviceInfoListOut?.DevID ?? 0 };

            if (OnDelete == false)
            {
                var result = await Http.PostAsJsonAsync("api/v1/GetLinkObjects_ITerminalDevice", r);
                if (result.IsSuccessStatusCode)
                {
                    ListObj = await result.Content.ReadFromJsonAsync<List<string>>();
                }

                OnDelete = true;

                if (ListObj != null && ListObj.Count > 0)
                {
                    IsViewObjList = true;
                    return;
                }
            }

            if (OnDelete == true)
            {
                var result = await Http.PostAsJsonAsync("api/v1/DeleteDevice", r);
                if (!result.IsSuccessStatusCode)
                {
                    MessageView?.AddError("", UUZSRep["IDS_STRING_E_DELETE_DEVICE"]);
                }
            }
            SelectItem = null;
            CanselDelete();

        }

        private void CanselDelete()
        {
            OnDelete = false;
            ListObj = null;
            IsViewObjList = false;
            IsDelete = false;
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
