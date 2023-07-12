using Microsoft.AspNetCore.Components;
using SMDataServiceProto.V1;

namespace BlazorLibrary.Shared.SubParamSystem
{
    partial class EditSubsystemParam
    {
        [Parameter]
        public SubsystemParam? SubParam { get; set; } = new();

        [Parameter]
        public EventCallback<SubsystemParam> CallBackParam { get; set; }

        private async Task SaveParam()
        {
            await CallEvent(SubParam);
        }

        private async Task Cancel()
        {
            await CallEvent(null);
        }

        private async Task CallEvent(SubsystemParam? Param)
        {
            StateHasChanged();
            if (CallBackParam.HasDelegate)
                await CallBackParam.InvokeAsync(Param);
        }
    }
}
