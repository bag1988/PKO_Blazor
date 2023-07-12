using System.Net.Http.Json;
using SMDataServiceProto.V1;
using SMSSGsoProto.V1;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SharedLibrary.Models;

namespace ViewState.Client.Shared
{
    public partial class MainLayout
    {
        public static ParamViewState param = new();

        protected override async Task OnInitializedAsync()
        {
            await GetParamViewState();
            //add detect close window
            var reference = DotNetObjectReference.Create(this);

            //await JSRuntime.InvokeVoidAsync("CloseWindows", reference);

            //await Http.PostAsJsonAsync("api/v1/allow/WriteLog", new WriteLog2Request() { Source = (int)GSOModules.ViewStates_Module, EventCode = 132, SubsystemID = 0, UserID = await _User.GetUserId() });
        }

        private async Task SetInterval(ChangeEventArgs e)
        {
            int.TryParse(e?.Value?.ToString(), out int interval);
            param.IntervalUpdateState = interval;
            await SetParams(nameof(param.IntervalUpdateState), interval.ToString());
        }

        private async Task ChangeBool(bool value, string paramName)
        {
            if (paramName == nameof(param.IsCheckCu))
                param.IsCheckCu = value;
            else if (paramName == nameof(param.IsPOSIgnore))
                param.IsPOSIgnore = value;
            else if (paramName == nameof(param.IsHistoryCommand))
                param.IsHistoryCommand = value;

            await SetParams(paramName, value ? "1" : "0");
        }

        private async Task SetParams(string paramName, string paramValue)
        {
            await Http.PostAsJsonAsync("api/v1/SetParams", new SetParamRequest() { ParamName = paramName, ParamValue = paramValue });
        }


        private async Task GetParamViewState()
        {
            await Http.PostAsync("api/v1/GetParamViewState", null).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    param = await x.Result.Content.ReadFromJsonAsync<ParamViewState>() ?? new();
                }
            });
        }

        //[JSInvokable]
        //public async Task CloseWindows()
        //{
        //    await Http.PostAsJsonAsync("api/v1/allow/WriteLog", new WriteLog2Request() { Source = (int)GSOModules.ViewStates_Module, EventCode = 133, SubsystemID = 0, UserID = await _User.GetUserId() });
        //}
    }
}
