using System.Diagnostics;
using System.Reflection;
using ServiceLibrary.Diagnostic;

namespace SynthesisService.Services;

public partial class GenerateSound
{
    public static ActivitySource Tracer { get; } = BootstrapLogger.Create(MethodBase.GetCurrentMethod());
}

public static class Tracing
{
    public static string[] AssemblySources { get; } = new string[] {
            GenerateSound.Tracer.Name
        };

    public static string[] GetSources()
    {
        var sources = new List<string>(AssemblySources);
        //sources.AddRange(ServiceLibrary.Diagnostics.Sources);
        return sources.ToArray();
    }

    public static string[] Sources { get; } = GetSources();
}

