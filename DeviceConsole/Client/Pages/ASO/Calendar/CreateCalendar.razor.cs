using Google.Protobuf.WellKnownTypes;
using AsoDataProto.V1;
using Microsoft.AspNetCore.Components;
using static BlazorLibrary.Shared.Main;

namespace DeviceConsole.Client.Pages.ASO.Calendar
{
    partial class CreateCalendar
    {

        [Parameter]
        public CalendarItem? Model { get; set; }

        [Parameter]
        public EventCallback<CalendarItem?> CallBack { get; set; }

        private DateTime? date { get; set; } = DateTime.Now.Date;

        private string? nameHoliday { get; set; }

        private string TitleError = "";
        protected override void OnInitialized()
        {
            if (Model != null)
            {
                date = Model.Data?.ToDateTime().ToLocalTime();
                nameHoliday = Model.DataName;
            }
            TitleError = AsoRep["IDS_REG_CALENDAR_INSERT"];
        }

        private async Task SaveCalendar()
        {
            if (date == null)
            {
                MessageView?.AddError(TitleError, DeviceRep["ErrorNull"] + " " + AsoRep["IDS_HOLIDAY_DATA"]);
                return;
            }

            if (string.IsNullOrEmpty(nameHoliday))
            {
                MessageView?.AddError(TitleError, DeviceRep["ErrorNull"] + " " + AsoRep["IDS_HOLIDAY_NAME"]);
                return;
            }

            if (Model == null)
                Model = new();

            Model.DataName = nameHoliday;
            Model.Data = date.Value.Date.ToUniversalTime().ToTimestamp();

            await CallEvent(Model);
        }


        private async Task Close()
        {
            await CallEvent(null);
        }

        private async Task CallEvent(CalendarItem? b)
        {
            if (CallBack.HasDelegate)
                await CallBack.InvokeAsync(b);
        }

    }
}
