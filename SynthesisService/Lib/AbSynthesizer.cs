using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using SharedLibrary;
using SharedLibrary.Extensions;

namespace SynthesisService.Lib;

public abstract class AbSynthesizer
{
    private readonly ILogger<AbSynthesizer> _logger;

    public AbSynthesizer(ILogger<AbSynthesizer> logger)
    {
        _logger = logger;
    }

    //private static readonly TestMessage[] _testMessages =
    //{
    //    new(
    //        Text: "Hello! Now " + DateTime.Now + "! By!",
    //        Language: "english"
    //        ),
    //    new(
    //        Text: "Добрый день! Сейчас " + DateTime.Now + "! Досвидание!",
    //        Language: "russian"
    //        ),
    //};

    //public async Task RunTests()
    //{
    //    int sampleRate = 8000;

    //    foreach (var message in _testMessages)
    //    {
    //        await Convert(message.Text, message.Language, sampleRate);
    //    }
    //}

    //public abstract SynthesisService.Model.VoiceInfo[] GetTestVoices();

    //public async Task RunTests(string message, string language, int sampleRate)
    //{
    //    var voices = GetTestVoices()
    //        .Where(v => v.Language == language);
    //    foreach (var voice in voices)
    //    {
    //        await Convert(message, voice.Name, sampleRate);
    //    }
    //}



    public bool WriteToFile(string message, string voice, int sampleRate, string PathWav)
    {
        _logger.LogInformation("Synthesizing...");
        _logger.LogInformation("voice: {voice}", voice);
        _logger.LogInformation("sampleRate: {sampleRate}", sampleRate);

        var inputFile = voice + ".txt";
        var outputFile = voice + ".wav";

        var dir = Path.GetDirectoryName(PathWav);

        if (!string.IsNullOrEmpty(dir))
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            outputFile = PathWav;
            inputFile = Path.ChangeExtension(PathWav, ".txt");
        }

        var startInfo = CreateStartInfo(inputFile, outputFile, voice, sampleRate);

        return WriteFile(startInfo, inputFile, message, outputFile);
    }

    public IAsyncEnumerable<byte[]> ConvertToBytesAsync(string message, string voice, int sampleRate, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Converting...");
        _logger.LogInformation("message: {message}", message);
        _logger.LogInformation("voice: {voice}", voice);
        _logger.LogInformation("sampleRate: {sampleRate}", sampleRate);

        var inputFile = "-";
        var outputFile = "-";

        var startInfo = CreateStartInfo(inputFile, outputFile, voice, sampleRate);

        return GetBytesAsync(startInfo, message, cancellationToken);
    }

    protected abstract ProcessStartInfo CreateStartInfo(string inputFile, string outputFile, string voice, int sampleRate);
    //{
    //    return new ProcessStartInfo
    //    {
    //        FileName = "RHVoice-test",
    //        ArgumentList = {
    //            "--input", inputFile,
    //            "--output", outputFile,
    //            "--sample-rate", sampleRate.ToString(),
    //            "--profile", voice
    //        }
    //    };
    //}


    public bool WriteFile(ProcessStartInfo startInfo, string inputFile, string message, string outputFile)
    {
        try
        {
            var exitCode = CreateFile(startInfo, inputFile, message, outputFile);
            _logger.LogInformation("exitCode: {exitCode}", exitCode);
            if (exitCode == 0)
            {
                return File.Exists(outputFile);
            }
        }
        catch (Exception exception)
        {
            _logger.LogError("FAILED {message}", exception.Message);
        }

        return false;
    }

    public async IAsyncEnumerable<byte[]> GetBytesAsync(ProcessStartInfo startInfo, string message, [EnumeratorCancellation] CancellationToken token)
    {
        _logger.LogInformation("Synthesizing...");
        _logger.LogInformation("message: {message}", message);

        //chunk size for 1 second for 2 chanels, 16 bit, 24 kHz sample rates sound + header
        const int ChunkSize = 4096 + 2 * 2 * 24000;

        var startTime = DateTime.Now;
        using var process = Process.Start(startInfo);
        try
        {
            if (process is not null)
            {
                await process.StandardInput.WriteAsync(message);
                process.StandardInput.Close();
                using var result = process.StandardOutput.BaseStream;
                byte[] buffer = new byte[ChunkSize];
                int readCount = 0;
                while ((readCount = await result.ReadAsync(buffer, token)) > 0)
                {
                    yield return buffer.Take(readCount).ToArray();
                }
                process.StandardOutput.Close();
                await process.StandardOutput.DisposeAsync();
                var exitCode = process.ExitCode;
                _logger.LogInformation("exitCode: {exitCode}", exitCode);
            }
            else
            {
                _logger.LogError("FAILED {message}", "process is null");
            }
            var finishTime = DateTime.Now;
            var processingTime = finishTime - startTime;
            _logger.LogInformation($"processingTime (c): {processingTime.TotalSeconds}");
        }
        finally
        {
            if (process is not null)
                await process.DisposeAsync();
        }
        yield break;
    }

    private int CreateFile(ProcessStartInfo startInfo, string inputFile, string message, string outputFile)
    {
        _logger.LogInformation("Synthesizing...");
        _logger.LogInformation("inputFile: {inputFile}", inputFile);
        _logger.LogInformation("message: {message}", message);
        _logger.LogInformation("outputFile: {outputFile}", outputFile);
        int exitCode = 0;
        try
        {
            var utf8WithoutBom = new UTF8Encoding(false);
            File.WriteAllText(inputFile, message, utf8WithoutBom);

            var startTime = DateTime.Now;
            using var process = Process.Start(startInfo);

            if (process is not null)
            {
                process.WaitForExit();
                var finishTime = DateTime.Now;
                var processingTime = finishTime - startTime;
                _logger.LogInformation("processingTime: {processingTime}", processingTime);

                exitCode = process.ExitCode;
                _logger.LogInformation("exitCode: {exitCode}", exitCode);
            }
            else
            {
                _logger.LogError("FAILED {message}", "process is null");
            }
        }
        catch (Exception exception)
        {
            _logger.LogError("FAILED {message}", exception.Message);
        }

        return exitCode;
    }

    public IAsyncEnumerable<byte[]> ConvertSoundDataToGsmStreamAsync(IAsyncEnumerable<byte[]> inputData, CancellationToken token)
    {
        _logger.LogInformation($"ConvertSoundDataToGsmStreamAsync...");

        var args = $"-f s16le -ar 8000 -i - -c:a libgsm -f gsm -";

        var ffmpeg = new FFmpegOnPipes();

        return ffmpeg.ConvertStream(inputData, args, token);
    }

    public IAsyncEnumerable<byte[]> ConvertGsmToSoundDataStreamAsync(IAsyncEnumerable<byte[]> inputData, CancellationToken token)
    {
        _logger.LogInformation($"ConvertGsmToSoundDataStreamAsync...");

        var args = $"-f gsm -i - -c:a pcm_s16le -f s16le -";

        var ffmpeg = new FFmpegOnPipes();

        return ffmpeg.ConvertStream(inputData, args, token);
    }

    public IAsyncEnumerable<byte[]> ConvertFileToFileStreamAsync(IAsyncEnumerable<byte[]> inputData, string format, CancellationToken token)
    {
        _logger.LogInformation($"ConvertFileToFileStreamAsync to {format}...");

        var args = $"-i - -f {format} -";

        var ffmpeg = new FFmpegOnPipes();

        return ffmpeg.ConvertStream(inputData, args, token);
    }

    public Task<byte[]> ConvertWavToGsmAsync(byte[] inputData, int channels, uint sampleRate, int sampleSize, CancellationToken token)
    {
        _logger.LogInformation($"ConvertWavToGsmAsync from channels {channels}, sampleRate {sampleRate}, sampleSize {sampleSize}...");

        string inputFormat = sampleSize == 8 ? "s8" : "s16le";

        var args = $"-f {inputFormat} -ar {sampleRate} -ac {channels} -i - -c:a libgsm -f gsm -ar 8000 -ac 1 -";

        var ffmpeg = new FFmpegOnPipes();

        return ffmpeg.ConvertPartSound(inputData, args, token);
    }

    public Task<byte[]> ConvertGsmToWavAsync(ReadOnlyMemory<byte> inputData, CancellationToken token)
    {
        _logger.LogInformation($"ConvertGsmToWavAsync...");


        var args = $"-f gsm -i - -c:a pcm_s16le -f s16le -";

        var ffmpeg = new FFmpegOnPipes();

        return ffmpeg.ConvertPartSound(inputData, args, token);
    }

    //public async Task<byte[]?> ConvertToWavAsync(IAsyncStreamReader<BytesValue> inputData)
    //{
    //    _logger.LogInformation("ConvertingToWavAsync...");
    //    //_logger.LogInformation("Length: {Length}", inputData.Length);

    //    var ffmpeg = new FFmpegOnPipes();

    //    return await ffmpeg.ConvertToWav(inputData);
    //}
}
