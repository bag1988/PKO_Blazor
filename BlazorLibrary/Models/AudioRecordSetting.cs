namespace BlazorLibrary.Models
{
    public class AudioRecordSetting
    {
        public UInt16 ChannelCount { get; set; } = 1;
        public UInt32 SampleRate { get; set; } = 16000;
        public UInt16 SampleSize { get; set; } = 16;
        public string? Label { get; set; }
        public UInt16 Volum { get; set; } = 100;
    }
}
