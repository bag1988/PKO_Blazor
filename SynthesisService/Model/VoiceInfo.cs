namespace SynthesisService.Model;

public record LanguageInfo(string Name, string Description, string Note, string Version, string Date);

public record VoiceInfo(string Name, string Language, string Description, string Note, string Version, string Date);

public record TestMessage(string Text, string Language);
