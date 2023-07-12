using System.Runtime.CompilerServices;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using SharedLibrary;
using SyntezServiceProto.V1;
using SynthesisService.Lib;
using Serilog;

namespace SynthesisService.Services;

public partial class GenerateSoundV1 : SyntezServiceProto.V1.SyntezService.SyntezServiceBase
{
    private readonly ILogger<GenerateSoundV1> _logger;
    private readonly Synthesizer _synthesizer;
    public GenerateSoundV1(ILogger<GenerateSoundV1> logger, Synthesizer synthesizer)
    {
        _logger = logger;
        _synthesizer = synthesizer;
    }


    public override async Task TextSynthesisStream(SynthesisDataStream request, IServerStreamWriter<BytesValue> responseStream, ServerCallContext context)
    {
        _logger.LogTrace("TextSynthesisStream");
        try
        {
            string message = request.Param.Text;
            string voice = request.Param.VoiceIsMen ? "aleksandr" : "anna";
            int sampleRate = request.Param.Rate;

            WavHeaderModel? m = null;
            uint startIndex = request.StartIndex;
            uint endIndex = request.EndIndex;

            await foreach (var item in _synthesizer.ConvertToBytesAsync(message, voice, sampleRate, context.CancellationToken))
            {
                if (startIndex > 0)
                {
                    if (item.Length > startIndex)
                    {
                        await responseStream.WriteAsync(new BytesValue
                        {
                            Value = UnsafeByteOperations.UnsafeWrap(item.Skip((int)startIndex).ToArray())
                        });
                        startIndex = 0;
                    }
                    else
                    {
                        startIndex = (uint)(startIndex - item.Length);

                    }
                    continue;
                }

                if (endIndex > 0)
                {
                    if (endIndex - item.Length < 0)
                    {
                        await responseStream.WriteAsync(new BytesValue
                        {
                            Value = UnsafeByteOperations.UnsafeWrap(item.Take(item.Length - (int)endIndex).ToArray())
                        });
                        break;
                    }
                    else
                        endIndex = (uint)(endIndex - item.Length);
                }

                if (m == null && request.StartIndex == 0)
                {
                    m = new WavHeaderModel(item.Take(1000).ToArray());
                    if (!m.WAVE.SequenceEqual("WAVE".ToCharArray()))
                        break;
                    m.ChunkHeaderSize = uint.MaxValue;
                    await context.WriteResponseHeadersAsync(new Metadata() { new(MetaDataName.FormatSound, m.ToBase64AllHeader()) });
                }

                await responseStream.WriteAsync(new BytesValue
                {
                    Value = UnsafeByteOperations.UnsafeWrap(item)
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("TextSynthesisStream: {Message}", ex.Message);
            throw new RpcException(new Grpc.Core.Status(StatusCode.Internal, ex.ToString()));
        }

    }


    public override async Task TextSynthesis(SynthesisData request, IServerStreamWriter<BytesValue> responseStream, ServerCallContext context)
    {
        _logger.LogInformation("TextSynthesis {text}", request.Text);

        try
        {
            string message = request.Text;
            string voice = request.VoiceIsMen ? "aleksandr" : "anna";
            int sampleRate = request.Rate;

            WavHeaderModel? m = null;

            await foreach (var item in _synthesizer.ConvertToBytesAsync(message, voice, sampleRate, context.CancellationToken))
            {
                if (m == null)
                {
                    m = new WavHeaderModel(item.Take(100).ToArray());
                    if (!m.WAVE.SequenceEqual("WAVE".ToCharArray()))
                        break;

                    await context.WriteResponseHeadersAsync(new Metadata() { new(MetaDataName.FormatSound, m.ToBase64AllHeader()) });
                }

                await responseStream.WriteAsync(new BytesValue
                {
                    Value = UnsafeByteOperations.UnsafeWrap(item)
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }
    }

    public override Task<BoolValue> SynthesisWriteToFile(ParamAndPath request, ServerCallContext context)
    {
        _logger.LogInformation("SynthesisWriteToFile {path}", request.Path);
        BoolValue response = new() { Value = false };
        try
        {
            if (request.Param != null)
            {
                string message = request.Param.Text;
                string voice = request.Param.VoiceIsMen ? "aleksandr" : "anna";
                int sampleRate = request.Param.Rate;

                response.Value = _synthesizer.WriteToFile(message, voice, sampleRate, request.Path);

            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }
        return Task.FromResult(response);
    }

    private async IAsyncEnumerable<byte[]> GetAsyncEnumerable(IAsyncStreamReader<BytesValue> request, [EnumeratorCancellation] CancellationToken token)
    {
        while (await request.MoveNext(token))
        {
            var value = request.Current;
            yield return value.Value.ToArray();
        }
        yield break;
    }

    public override async Task ConvertFileToFileStream(IAsyncStreamReader<BytesValue> request, IServerStreamWriter<BytesValue> responseStream, ServerCallContext context)
    {
        _logger.LogInformation("ConvertFileToFileStream");

        try
        {
            var format = context.RequestHeaders.FirstOrDefault(x => x.Key == MetaDataName.Format)?.Value ?? MetaDataName.Wav;

            if (format == MetaDataName.Wav)
            {
                WavHeaderModel? m = null;
                await foreach (var item in _synthesizer.ConvertFileToFileStreamAsync(GetAsyncEnumerable(request, context.CancellationToken), MetaDataName.Wav, context.CancellationToken))
                {
                    if (m == null)
                    {
                        m = new WavHeaderModel(item.Take(100).ToArray());
                        if (!m.WAVE.SequenceEqual("WAVE".ToCharArray()))
                            break;

                        await context.WriteResponseHeadersAsync(new Metadata() { new(MetaDataName.FormatSound, m.ToBase64AllHeader()) });
                    }

                    await responseStream.WriteAsync(new BytesValue
                    {
                        Value = UnsafeByteOperations.UnsafeWrap(item)
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }
    }

    public override async Task ConvertSoundDataToGsmStream(IAsyncStreamReader<BytesValue> request, IServerStreamWriter<BytesValue> responseStream, ServerCallContext context)
    {
        _logger.LogInformation("ConvertSoundDataToGsmStream");

        try
        {
            await foreach (var item in _synthesizer.ConvertSoundDataToGsmStreamAsync(GetAsyncEnumerable(request, context.CancellationToken), context.CancellationToken))
            {
                await responseStream.WriteAsync(new BytesValue
                {
                    Value = UnsafeByteOperations.UnsafeWrap(item)
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }
    }

    public override async Task ConvertGsmToSoundDataStream(IAsyncStreamReader<BytesValue> request, IServerStreamWriter<BytesValue> responseStream, ServerCallContext context)
    {
        _logger.LogInformation("ConvertGsmToSoundDataStream");

        try
        {
            await foreach (var item in _synthesizer.ConvertGsmToSoundDataStreamAsync(GetAsyncEnumerable(request, context.CancellationToken), context.CancellationToken))
            {
                await responseStream.WriteAsync(new BytesValue
                {
                    Value = UnsafeByteOperations.UnsafeWrap(item)
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }
    }

    public override async Task<BytesValue> ConvertGsmToWav(BytesValue request, ServerCallContext context)
    {
        _logger.LogInformation("ConvertGsmToWav");
        try
        {
            BytesValue result = new();

            var response = await _synthesizer.ConvertGsmToWavAsync(request.Value.Memory, context.CancellationToken);

            result.Value = UnsafeByteOperations.UnsafeWrap(response);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            throw new RpcException(new Status(StatusCode.InvalidArgument, ""));
        }
    }
}
