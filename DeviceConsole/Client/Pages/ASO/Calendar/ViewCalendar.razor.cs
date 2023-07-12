using System.ComponentModel;
using System.Net.Http.Json;
using Google.Protobuf.WellKnownTypes;
using AsoDataProto.V1;
using Microsoft.AspNetCore.Components;
using SMDataServiceProto.V1;
using static BlazorLibrary.Shared.Main;
using SharedLibrary.Interfaces;
using SharedLibrary;
using BlazorLibrary.Shared.Table;
using BlazorLibrary.GlobalEnums;
using BlazorLibrary.Models;
using FiltersGSOProto.V1;

namespace DeviceConsole.Client.Pages.ASO.Calendar
{
    partial class ViewCalendar : IAsyncDisposable, IPubSubMethod
    {
        private int SubsystemID { get; set; } = SubsystemType.SUBSYST_ASO;

        private CalendarItem? SelectItem = null;

        private bool? IsViewEdit = false;

        private bool? IsDelete = false;

        TableVirtualize<CalendarItem>? table;

        protected override async Task OnInitializedAsync()
        {
            request.ObjID.StaffID = await _User.GetLocalStaff();
            request.ObjID.SubsystemID = SubsystemID;

            ThList = new Dictionary<int, string>
            {
                { 0, AsoRep["IDS_HOLIDAY_DATA"] },
                { 1, AsoRep["IDS_HOLIDAY_NAME"] }
            };

            HintItems.Add(new HintItem(nameof(FiltrModel.Name), AsoRep["IDS_HOLIDAY_NAME"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 20 }, LoadHelpName)));
            HintItems.Add(new HintItem(nameof(FiltrModel.DateRange), AsoRep["IDS_HOLIDAY_DATA"], TypeHint.DateOnly));

            await OnInitFiltr(RefreshTable, FiltrName.FiltrCalendar);

            _ = _HubContext.SubscribeAsync(this);
        }

        [Description(DaprMessage.PubSubName)]
        public async Task Fire_UpdateData(byte[] value)
        {
            try
            {
                if (table != null)
                {
                    var newItem = CalendarItem.Parser.ParseFrom(value);
                    if (newItem != null && newItem.Data != null)
                    {
                        if (SelectItem != null && SelectItem.Data == newItem.Data)
                        {
                            SelectItem = newItem;
                        }
                        await table.ForEachItems(x =>
                        {
                            if (x.Data == newItem.Data)
                            {
                                x.DataName = newItem.DataName;
                                return;
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        [Description(DaprMessage.PubSubName)]
        public async Task Fire_InsertDeleteData(byte[] value)
        {
            try
            {
                if (table != null)
                {
                    var newItem = CalendarItem.Parser.ParseFrom(value);

                    if (newItem != null && newItem.Data != null)
                    {
                        if (!table.AnyItemMatch(x => x.Data == newItem.Data))
                        {
                            await table.AddItem(newItem);
                        }
                        else
                        {
                            if (SelectItem != null && SelectItem.Data == newItem.Data)
                                SelectItem = null;
                            await table.RemoveAllItem(x => x.Data == newItem.Data);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        ItemsProvider<CalendarItem> GetProvider => new ItemsProvider<CalendarItem>(ThList, LoadChildList, request, new List<int>() { 50, 50 });

        private async ValueTask<IEnumerable<CalendarItem>> LoadChildList(GetItemRequest req)
        {
            List<CalendarItem> newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetItems_ICalendar", req, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                newData = await result.Content.ReadFromJsonAsync<List<CalendarItem>>() ?? new();
            }
            else
            {
                MessageView?.AddError(AsoRep["IDS_STRING_HOLIDAYS"], AsoRep["IDS_EFAILGETDATALISTINFO"]);
            }
            return newData;
        }

        private async ValueTask<IEnumerable<Hint>> LoadHelpName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetCalendarNameForCalendar", new StringValue() { Value = req.BstrFilter }, ComponentDetached);
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

        private async Task DeleteData(Timestamp data)
        {
            var result = await Http.PostAsJsonAsync("api/v1/DeleteData", new DeleteDataRequest() { MData = data }, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                SelectItem = null;
            }
            else
                MessageView?.AddError(AsoDataRep["IDS_REG_CALENDAR_DELETE"], AsoRep["IDS_EFAIL_DELCALENDAR"]);
            IsDelete = false;
        }


        private async Task SaveCalendar(CalendarItem item)
        {
            var result = await Http.PostAsJsonAsync("api/v1/SetCalendarInfo", item, ComponentDetached);
            if (!result.IsSuccessStatusCode)
            {
                MessageView?.AddError(AsoRep["IDS_REG_CALENDAR_INSERT"], AsoRep["IDS_E_SAVEHOLIDAY"]);
            }
        }


        private async Task CallBackEvent(CalendarItem? item)
        {
            IsViewEdit = false;
            if (item != null)
            {
                if (SelectItem != null)
                {
                    bool isDeleteOld = item.Data.Equals(SelectItem.Data);
                    if (!isDeleteOld)
                        await DeleteData(SelectItem.Data);
                }

                SelectItem = null;
                await SaveCalendar(item);
            }
        }

        public ValueTask DisposeAsync()
        {
            DisposeToken();
            return _HubContext.DisposeAsync();
        }
    }
}
