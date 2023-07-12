using System.Text.RegularExpressions;
using SharedLibrary;

namespace BlazorLibrary.Helpers
{
    public static class ReadFormatBlob
    {
        public static async Task<byte[]> GetFormatBlobAsync(this HttpClient http, string urlFile)
        {
            try
            {
                WavHeaderModel formatSound = new();
                using var s = await http.GetStreamAsync(urlFile);
                if (s != null)
                {
                    if (s != null)
                    {
                        byte[] buffer = new byte[1000];
                        int readCount = 0;
                        readCount = await s.ReadAsync(buffer);
                        if (readCount > 0)
                        {
                            formatSound = new(buffer);

                            Console.WriteLine("///////////////////////");
                            Console.WriteLine(string.Join("", formatSound.WAVE));
                            Console.WriteLine("ChunkHeaderSize " + formatSound.ChunkHeaderSize);
                            Console.WriteLine("HeaderSize " + formatSound.HeaderSize);
                            Console.WriteLine("AllHeaderSize " + formatSound.GetAllHeaderLength());
                            Console.WriteLine("SubChunk " + formatSound.SubChunk);
                            Console.WriteLine("SampleRate " + formatSound.SampleRate);
                            Console.WriteLine("SampleSize " + formatSound.SampleSize);
                            Console.WriteLine("ByteRate " + formatSound.ByteRate);
                            Console.WriteLine("Channels " + formatSound.Channels);
                            Console.WriteLine(string.Join("", formatSound.DATA));
                            Console.WriteLine("///////////////////////");
                        }
                        return formatSound.ToBytesAllHeader();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка чтения формата {Message}", ex.Message);
            }
            return Array.Empty<byte>();
        }

        public static async Task<long> GetLengthFileAsync(this HttpClient http, string? urlFile)
        {
            try
            {
                if (string.IsNullOrEmpty(urlFile))
                    return 0;
                using var s = await http.GetAsync(urlFile, HttpCompletionOption.ResponseHeadersRead);
                if (s.Content.Headers.ContentLength > 0)
                    return s.Content.Headers.ContentLength.Value;
                else
                {
                    if (s.Content.Headers.Contains("Content-Range"))
                    {
                        var range = s.Content.Headers.FirstOrDefault(x => x.Key == "Content-Range").Value.FirstOrDefault();
                        Regex regex = new Regex(@"^bytes\s([0-9]*)-([0-9]*)");

                        if (!string.IsNullOrEmpty(range) && regex.IsMatch(range))
                        {
                            var match = regex.Match(range);

                            if (match.Groups.Count > 1)
                            {
                                long.TryParse(match.Groups[2].Value, out long result);

                                Console.WriteLine($"Get Range {result}");
                                return result;
                            }
                        }
                    }

                    return 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка чтения файла {Message}", ex.Message);
                return 0;
            }
        }
    }
}
