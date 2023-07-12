using System.ComponentModel;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using SharedLibrary.Interfaces;
using SMDataServiceProto.V1;
using static BlazorLibrary.Shared.Main;
using SharedLibrary;
using BlazorLibrary.Shared.Table;
using BlazorLibrary.GlobalEnums;
using BlazorLibrary.Models;
using FiltersGSOProto.V1;
using Label.V1;

namespace DeviceConsole.Client.Pages.Staff.RegistrationPU
{
    partial class RegistrationList : IAsyncDisposable, IPubSubMethod
    {
        int SubsystemID => SubsystemType.SUBSYST_GSO_STAFF;

        private CGetRegList? SelectItem = null;


        private bool? IsCreate = false;

        private bool? IsViewShedule = false;

        private bool? IsDelete = false;

        TableVirtualize<CGetRegList>? table;

        protected override async Task OnInitializedAsync()
        {
            request.ObjID.StaffID = await _User.GetLocalStaff();
            request.ObjID.SubsystemID = SubsystemID;

            ThList = new Dictionary<int, string>
            {
                { 0, GSOFormRep["IDS_CU_NAME"] },
                { 1, GSOFormRep["IDS_CU_TYPE"] },
                { 2, GSOFormRep["IDS_CU_UNC"] },
                { 3, GSOFormRep["IDS_CU_STAFFID"] }
            };

            HintItems.Add(new HintItem(nameof(FiltrModel.Name), GSOFormRep["IDS_CU_NAME"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 20 }, LoadHelpName)));
            HintItems.Add(new HintItem(nameof(FiltrModel.Type), GSOFormRep["IDS_CU_TYPE"], TypeHint.OnlySelect, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 20 }, LoadHelpTypeName)));
            HintItems.Add(new HintItem(nameof(FiltrModel.UNC), GSOFormRep["IDS_CU_UNC"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 20 }, LoadHelpUNC)));
            HintItems.Add(new HintItem(nameof(FiltrModel.StaffID), GSOFormRep["IDS_CU_STAFFID"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 20 }, LoadHelpStaffId)));

            await OnInitFiltr(RefreshTable, FiltrName.FiltrStaff);
            _ = _HubContext.SubscribeAsync(this);

        }
        [Description(DaprMessage.PubSubName)]
        public async Task Fire_UpdateRegistration(ulong Value)
        {
            await CallRefreshData();
        }
        [Description(DaprMessage.PubSubName)]
        public async Task Fire_InsertDeleteRegistration(ulong Value)
        {
            await CallRefreshData();
        }

        ItemsProvider<CGetRegList> GetProvider => new ItemsProvider<CGetRegList>(ThList, LoadChildList, request, new List<int>() { 40, 20, 20, 20 });

        private async ValueTask<IEnumerable<CGetRegList>> LoadChildList(GetItemRequest req)
        {
            List<CGetRegList> newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetItems_IRegistration", req, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                newData = await result.Content.ReadFromJsonAsync<List<CGetRegList>>() ?? new();
            }
            else
            {
                MessageView?.AddError("", GSOFormRep["IDS_EFAILGETREGLISTINFO"]);
            }
            return newData;
        }

        private async ValueTask<IEnumerable<Hint>> LoadHelpName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetStaffNameForStaffList", new IntAndString() { Number = request.ObjID.StaffID, Str = req.BstrFilter }, ComponentDetached);
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

        private async ValueTask<IEnumerable<Hint>> LoadHelpStaffId(GetItemRequest req)
        {
            List<Hint>? newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetStaffIdForStaffList", new IntAndString() { Number = request.ObjID.StaffID, Str = req.BstrFilter }, ComponentDetached);
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

        private async ValueTask<IEnumerable<Hint>> LoadHelpUNC(GetItemRequest req)
        {
            List<Hint>? newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetUNCNameForStaffList", new IntAndString() { Number = request.ObjID.StaffID, Str = req.BstrFilter }, ComponentDetached);
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

        private async ValueTask<IEnumerable<Hint>> LoadHelpTypeName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetTypeNameForStaffList", new IntAndString() { Number = request.ObjID.StaffID, Str = req.BstrFilter }, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                var response = await result.Content.ReadFromJsonAsync<List<CGetAllStateBySessId>>();

                if (response?.Count > 0)
                {
                    newData.AddRange(response.Select(x => new Hint(x.Resultname, x.Status.ToString())));
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

        private async Task CloseModal(bool? IsUpdate = false)
        {
            IsCreate = false;
            IsDelete = false;
            if (IsUpdate == true)
                await CallRefreshData();
        }
               
        public ValueTask DisposeAsync()
        {
            DisposeToken();
            return _HubContext.DisposeAsync();
        }
    }
}
