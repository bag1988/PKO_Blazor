using System.ComponentModel;
using System.Net.Http.Json;
using Google.Protobuf.WellKnownTypes;
using SMDataServiceProto.V1;
using StaffDataProto.V1;
using Microsoft.AspNetCore.Components;
using SharedLibrary;
using SharedLibrary.GlobalEnums;
using static BlazorLibrary.Shared.Main;
using SharedLibrary.Interfaces;

namespace DeviceConsole.Client.Pages.Staff.SheduleStaff
{
    partial class SheduleList : IAsyncDisposable, IPubSubMethod
    {
        [Parameter]
        public int? IdStaff { get; set; }

        [Parameter]
        public string? StaffName { get; set; }

        [Parameter]
        public EventCallback ActionBack { get; set; }

        readonly GetItemRequest request = new() { ObjID = new OBJ_ID(), LSortOrder = 0, BFlagDirection = 0 };
        private Dictionary<int, string>? ThList;

        private List<SheduleCmd>? Model = null;
        private SheduleCmd? SelectItem = null;
        private List<IntAndString> LineTypes = new();
        //private StaffGetAccessor? InfoStaff = null;

        private bool IsCreate = false;
        private bool IsDelete = false;

        protected override async Task OnInitializedAsync()
        {
            request.ObjID.SubsystemID = SubsystemType.SUBSYST_GSO_STAFF;
            request.ObjID.StaffID = IdStaff ?? 0;

            ThList = new Dictionary<int, string>
            {
                { 0, GSOFormRep["IDS_PRIORITY"] },
                { 1, GSOFormRep["IDS_DATE_TYPE"] },
                { 2, GSOFormRep["IDS_BEGIN"] },
                { 3, GSOFormRep["IDS_END"] },
                { 4, GSOFormRep["IDS_CONN_TYPE"] },
                { 5, GSOFormRep["IDS_LOCATION"] },
                { 6, GSOFormRep["IDS_CONN_PARAM"] }
            };

            await GetLineTypeList();
            await GetList();
            _ = _HubContext.SubscribeAsync(this);
        }


        [Description(DaprMessage.PubSubName)]
        public async Task Fire_UpdateShedule(ulong Value)
        {
            await GetList();
            StateHasChanged();
        }
        [Description(DaprMessage.PubSubName)]
        public async Task Fire_InsertDeleteShedule(ulong Value)
        {
            if (SelectItem?.OBJKey?.ObjID?.ObjID == (int)Value)
                SelectItem = null;
            await GetList();
            StateHasChanged();
        }

        private async Task GetList()
        {
            await Http.PostAsJsonAsync("api/v1/GetItems_IStaffAccess", request).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    Model = await x.Result.Content.ReadFromJsonAsync<List<SheduleCmd>>() ?? new();
                }
                else
                {
                    Model = new();
                    MessageView?.AddError("", GSOFormRep["IDS_EGETDATALISTINFO"]);
                }
            });
        }

        private async Task DeleteSheduleInfoStaff()
        {
            if (SelectItem != null)
            {
                await Http.PostAsJsonAsync("api/v1/DeleteSheduleInfoStaff", new OBJ_ID(SelectItem.OBJKey?.ObjID)).ContinueWith(async x =>
                {
                    if (x.Result.IsSuccessStatusCode)
                    {
                        var b = await x.Result.Content.ReadFromJsonAsync<BoolValue>();

                        if (b == null || b.Value != true)
                            MessageView?.AddError("", GSOFormRep["IDS_EDELSHEDULEINFO"]);
                        else
                        {
                            IsDelete = false;
                        }
                    }
                    else
                    {
                        MessageView?.AddError("", GSOFormRep["IDS_EFAILDELSHEDULEINFO"]);
                    }
                });
            }
        }

        private async Task GetLineTypeList()
        {
            await Http.PostAsJsonAsync("api/v1/GetLineTypeList", new IntID() { ID = request.ObjID.SubsystemID }).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    LineTypes = await x.Result.Content.ReadFromJsonAsync<List<IntAndString>>() ?? new();

                    LineTypes.RemoveAll(x => x.Number != (int)BaseLineType.LINE_TYPE_DIAL_UP && x.Number != (int)BaseLineType.LINE_TYPE_DEDICATED);

                    LineTypes.Insert(0, new IntAndString() { Number = (int)BaseLineType.LINE_TYPE_UNDEF, Str = "ЛВС" });

                }
                else
                {
                    LineTypes = new();
                    MessageView?.AddError("", GSOFormRep["IDS_EFAILGETLINETYPE"]);
                }

            });
        }


        private async Task SetSort(int? id)
        {
            if (id == request!.LSortOrder)
                request.BFlagDirection = request.BFlagDirection == 1 ? 0 : 1;
            else
            {
                request.LSortOrder = id ?? 0;
                request.BFlagDirection = 1;
            }
            await GetList();
            StateHasChanged();
        }

        private void CloseModal()
        {
            IsCreate = false;
        }

        private async Task ClallBack()
        {
            if (ActionBack.HasDelegate)
                await ActionBack.InvokeAsync();
        }

        public ValueTask DisposeAsync()
        {
            return _HubContext.DisposeAsync();
        }
    }
}
