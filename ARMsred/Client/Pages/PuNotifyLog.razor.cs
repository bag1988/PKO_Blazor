
using System.Net.Http.Json;
using BlazorLibrary;
using SMSSGsoProto.V1;
using SharedLibrary;
using SMDataServiceProto.V1;

namespace ARMsred.Client.Pages
{
    partial class PuNotifyLog
    {
        private List<CSessions>? Model;

        private CSessions? selectItem;
        //private int? StaffID;
        private Dictionary<int, string>? ThList;

        readonly GetItemRequest request = new() { ObjID = new OBJ_ID() { SubsystemID = SubsystemType.SUBSYST_GSO_STAFF }, LSortOrder = 1, BFlagDirection = 1 };

        private void SetSort(int? id)
        {
            if (id == request.LSortOrder)
                request.BFlagDirection = request.BFlagDirection == 1 ? 0 : 1;
            else
            {
                request.LSortOrder = id ?? 0;
                request.BFlagDirection = 1;
            }

            SortList.Sort(ref Model, request.LSortOrder, request.BFlagDirection);
            StateHasChanged();
            //await GetList();
        }

        protected override async Task OnParametersSetAsync()
        {
            ThList = new Dictionary<int, string>
            {
                { 0, ARMRep["tSessBeg"] },
                { 1, ARMRep["tSessEnd"] },
                { 2, ARMRep["tSitName"] }
            };
            await GetList();
        }

        private async Task GetList()
        {
            Model = null;
            await Http.PostAsJsonAsync("api/v1/", request).ContinueWith(async x =>
            {
                Model = await x.Result.Content.ReadFromJsonAsync<List<CSessions>>();
                if (selectItem == null && Model != null)
                {
                    selectItem = Model.First();
                }

                StateHasChanged();

            });
        }

        private void GetItemInfo(List<CSessions> items)
        {
            selectItem = items?.FirstOrDefault();
        }

    }
}
