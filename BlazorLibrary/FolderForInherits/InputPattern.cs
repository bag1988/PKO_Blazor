using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;

namespace BlazorLibrary.FolderForInherits
{
    public class InputPattern : ComponentBase
    {
        [Parameter(CaptureUnmatchedValues = true)]
        public Dictionary<string, object>? InputAttributes { get; set; }

        [Parameter]
        public string? Pattern { get; set; }

        [Parameter]
        public string? ErrorMessage { get; set; }

        [Parameter]
        public string? Value { get; set; }

        [Parameter]
        public EventCallback<string?> ValueChanged { get; set; }

        public bool IsValid = true;

        public string? bindValue
        {
            get
            {
                return Value;
            }
            set
            {
                if (Value?.Equals(value) ?? false) return;

                if (!string.IsNullOrEmpty(Pattern))
                {
                    if (Regex.IsMatch(value ?? string.Empty, Pattern))
                    {
                        IsValid = true;
                    }
                    else
                    {
                        IsValid = false;                        
                    }
                }
                else
                {
                    IsValid = false;
                }
                SetValue(value);
            }
        }

        void SetValue(string? value)
        {
            Value = value;
            if (ValueChanged.HasDelegate)
                ValueChanged.InvokeAsync(Value);
        }
    }
}
