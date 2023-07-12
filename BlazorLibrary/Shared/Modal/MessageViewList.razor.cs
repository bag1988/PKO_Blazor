using Microsoft.AspNetCore.Components;

namespace BlazorLibrary.Shared.Modal
{
    partial class MessageViewList
    {
        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        public EventCallback? AddCallback { get; set; }

        private Dictionary<string, List<string>> MessageList = new();

        private Dictionary<string, List<string>> ErrorList = new();

        ElementReference? div;

        private async Task CloseDialog()
        {
            MessageList = new();
            ErrorList = new();
            if (AddCallback?.HasDelegate ?? false)
            {
                await AddCallback.Value.InvokeAsync();
                AddCallback = null;
            }
        }

        public void AddError(string key, List<string> errorList)
        {
            if (ErrorList.ContainsKey(key))
                ErrorList[key].AddRange(errorList);
            else
                ErrorList.Add(key, errorList);
            StateHasChanged();
            _ = FocusDiv();
        }

        public void AddError(string key, string errorStr)
        {
            if (ErrorList.ContainsKey(key))
                ErrorList[key].Add(errorStr);
            else
                ErrorList.Add(key, new List<string>() { errorStr });
            StateHasChanged();
            _ = FocusDiv();
        }

        public void AddMessage(string key, List<string> messageList)
        {
            if (MessageList.ContainsKey(key))
                MessageList[key].AddRange(messageList);
            else
                MessageList.Add(key, messageList);
            StateHasChanged();
            _ = FocusDiv();
        }

        public void AddMessage(string key, string messageStr)
        {
            if (MessageList.ContainsKey(key))
                MessageList[key].Add(messageStr);
            else
                MessageList.Add(key, new List<string>() { messageStr });
            StateHasChanged();
            _ = FocusDiv();
        }

        async Task FocusDiv()
        {
            await Task.Yield();

            if (div != null)
            {
                await div.Value.FocusAsync();
            }
        }
        public void Clear()
        {
            MessageList = new();
            ErrorList = new();
            StateHasChanged();
        }
    }
}
