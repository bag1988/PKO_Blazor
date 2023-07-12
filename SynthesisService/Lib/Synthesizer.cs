using System.Diagnostics;
using System.Text;

namespace SynthesisService.Lib;

public class Synthesizer : AbSynthesizer
{
    public Synthesizer(ILogger<AbSynthesizer> logger) : base(logger)
    {
    }

    //public override SynthesisService.Model.VoiceInfo[] GetTestVoices()
    //{
    //    return VoiceInfo.Voices;
    //}

    protected override ProcessStartInfo CreateStartInfo(string inputFile
                                                      , string outputFile
                                                      , string voice
                                                      , int sampleRate)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "RHVoice-test"
          ,
            ArgumentList = {
                "--input"
              , inputFile
              , "--output"
              , outputFile
              , "--sample-rate"
              , sampleRate.ToString()
              , "--profile"
              , voice
            }
          ,
            UseShellExecute = false
        };

        if (inputFile == "-")
        {
            processStartInfo.RedirectStandardInput = true;
            processStartInfo.StandardInputEncoding = Encoding.UTF8;
        }

        if (outputFile == "-")
        {
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.StandardOutputEncoding = Encoding.UTF8;
        }

        return processStartInfo;
    }
}

