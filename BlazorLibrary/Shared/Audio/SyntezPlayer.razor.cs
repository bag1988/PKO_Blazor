using System.Web;
using Microsoft.AspNetCore.Components;
using static BlazorLibrary.Shared.Main;

namespace BlazorLibrary.Shared.Audio
{
    partial class SyntezPlayer
    {
        [Parameter]
        public string? Text { get; set; }

        [Parameter]
        public bool? VoiceIsMen { get; set; }

        [Parameter]
        public int? Rate { get; set; }

        private bool ViewPlayer = false;

        private AudioPlayerStream? player = default!;

        protected override void OnParametersSet()
        {
            ViewPlayer = false;
        }

        private async Task TextSynthesisStream()
        {
            ViewPlayer = false;
            if (string.IsNullOrEmpty(Text))
            {
                MessageView?.AddError("", DeviceRep["ErrorNull"] + ": " + GsoRep["MessageText"]);
                return;
            }
            ViewPlayer = true;
            await Task.Yield();
            try
            {                
                if (player != null)
                {
                    await player.SetUrlSound($"api/v1/TextSynthesisStream?Rate={Rate ?? 8000}&VoiceIsMen={VoiceIsMen ?? true}&Text={HttpUtility.UrlEncode(Text)}");

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }



}
