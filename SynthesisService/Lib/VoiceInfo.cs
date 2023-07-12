

namespace SynthesisService.Lib;

//    USAGE: 
//   RHVoice-test[-q<quality>][-v<percent>][-t<percent>][-R<Hz>][-r
//                 <percent>][-p<spec>][-s][-o<path>][-i<path>][--]
//                 [--version][-h]
//Where: 
//   -q<quality>,  --quality<quality>
//     quality
//   -v<percent>,  --volume<percent>
//     speech volume
//   -t<percent>,  --pitch<percent>
//     speech pitch
//   -R<Hz>,  --sample-rate<Hz>
//     sample rate
//   -r<percent>,  --rate<percent>
//     speech rate
//   -p<spec>,  --profile<spec>
//     voice profile
//   -s,  --ssml
//     Process as ssml
//   -o<path>,  --output<path>
//     output file
//   -i<path>,  --input<path>
//     input file
//   --,  --ignore_rest
//     Ignores the rest of the labeled arguments following this flag.
//   --version
//     Displays version information and exits.
//   -h,  --help
//     Displays usage information and exits.
//   Simple test of the synthesizer

//https://wiki.archlinux.org/title/RHVoice

public class LanguageInfo
{
    public readonly static SynthesisService.Model.LanguageInfo[] Languages =
    {
        new (
            Name: "brazilian-portuguese",
            Description: "brazilian-portuguese language",
            Note : "",
            Version: "1.8.0-1",
            Date: "2022-04-10"
            ),
        new (
            Name: "english",
            Description: "english language",
            Note : "",
            Version: "1.8.0-1",
            Date: "2022-04-10"
            ),
        new (
            Name: "esperanto",
            Description: "esperanto language",
            Note : "",
            Version: "1.8.0-1",
            Date: "2022-04-10"
            ),
        new (
            Name: "georgian",
            Description: "georgian language",
            Note : "",
            Version: "1.8.0-1",
            Date: "2022-04-10"
            ),
        new (
            Name: "kyrgyz",
            Description: "kyrgyz language",
            Note : "",
            Version: "1.8.0-1",
            Date: "2022-04-10"
            ),
        new (
            Name: "macedonian",
            Description: "macedonian language",
            Note : "",
            Version: "1.8.0-1",
            Date: "2022-04-10"
            ),
        new (
            Name: "polish",
            Description: "polish language",
            Note : "",
            Version: "1.8.0-1",
            Date: "2022-04-10"
            ),
        new (
            Name: "russian",
            Description: "russian language",
            Note: "",
            Version: "1.8.0-1",
            Date: "2022-04-10"
            ),
        new (
            Name: "tatar",
            Description: "tatar language",
            Note: "",
            Version: "1.8.0-1",
            Date: "2022-04-10"
            ),
        new (
            Name: "ukrainian",
            Description: "ukrainian language",
            Note: "",
            Version: "1.8.0-1",
            Date: "2022-04-10"
            ),
    };
}

public class VoiceInfo
{
    public readonly static SynthesisService.Model.VoiceInfo[] Voices =
    {
        new (
            Name: "alan",
            Language: "english",
            Description: "voice for english language",
            Note: "Scottish English",
            Version: "1.8.0-1",
            Date: "2022-04-10"
            ),
        new (
            Name: "aleksandr",
            Language: "russian",
            Description: "voice for russian language",
            Note: "Aleksandr Karlov (TV and radio host, audiobook reader)",
            Version: "1.8.0-1",
            Date: "2022-04-10"
            ),
        //The current version of the HQ voice has a higher quality than the previous version, their sound is different, 
        //so the new version is temporarily separated into a separate voice to collect feedback. 
        //This version may contain issues that are not present in the original voice. 
        //Since the speech base is open, we will be happy for your participation in improving the voice.
        new (
            Name: "aleksandr-hq",
            Language: "russian",
            Description: "voice for russian language",
            Note: "Aleksandr Karlov (TV and radio host, audiobook reader) (HQ voice)",
            Version: "1.8.0-1",
            Date: "2022-04-10"
            ),
        new (
            Name: "anatol",
            Language: "ukrainian",
            Description: "voice for ukrainian language",
            Note: "",
            Version: "1.8.0-1",
            Date: "2022-04-10"
            ),
        new (
            Name: "anna",
            Language: "russian",
            Description: "voice for russian language",
            Note: "",
            Version: "1.8.0-1",
            Date: "2022-04-10"
            ),
        new (
            Name: "arina",
            Language: "russian",
            Description: "voice for russian language",
            Note: "Arina Syukkya (event organizer, designer)",
            Version: "1.8.0-1",
            Date: "2022-04-10"
            ),
        new (
            Name: "artemiy",
            Language: "russian",
            Description: "voice for russian language",
            Note: "Artemiy Lebedev (designer, blogger, traveler)",
            Version: "1.8.0-1",
            Date: "2022-04-10"
            ),
        new (
            Name: "azamat",
            Language: "kyrgyz",
            Description: "voice for kyrgyz language",
            Note: "",
            Version: "1.8.0-1",
            Date: "2022-04-10"
            ),
        new (
            Name: "bdl",
            Language: "english",
            Description: "voice for english language",
            Note: "",
            Version: "1.8.0-1",
            Date: "2022-04-10"
            ),
        new (
            Name: "clb",
            Language: "english",
            Description: "voice for english language",
            Note: "",
            Version: "1.8.0-1",
            Date: "2022-04-10"
            ),
        new (
            Name: "elena",
            Language: "russian",
            Description: "voice for russian language",
            Note: "",
            Version: "1.8.0-1",
            Date: "2022-04-10"
            ),
        new (
            Name: "evgeniy-eng",
            Language: "english",
            Description: "voice for english language",
            Note: "Evgeniy Chebatkov (StandUp comedian, voice actor)",
            Version: "1.8.0-1",
            Date: "2022-04-10"
            ),
        new (
            Name: "evgeniy-rus",
            Language: "russian",
            Description: "voice for russian language",
            Note: "Evgeniy Chebatkov (StandUp comedian, voice actor)",
            Version: "1.8.0-1",
            Date: "2022-04-10"
            ),
        new (
            Name: "hana",
            Language: "albanian",
            Description: "voice for albanian language",
            Note: "",
            Version: "1.8.0-1",
            Date: "2022-04-10"
            ),
        new (
            Name: "irina",
            Language: "russian",
            Description: "voice for russian language",
            Note: "",
            Version: "1.8.0-1",
            Date: "2022-04-10"
            ),
        new (
            Name: "kiko",
            Language: "macedonian",
            Description: "voice for macedonian language",
            Note: "",
            Version: "1.8.0-1",
            Date: "2022-04-10"
            ),
        new (
            Name: "leticia-f123",
            Language: "brazilian-portuguese",
            Description: "voice for brazilian-portuguese language",
            Note: "",
            Version: "1.8.0-1",
            Date: "2022-04-10"
            ),
        new (
            Name: "lyubov",
            Language: "english",
            Description: "voice for english language",
            Note: "Lyubov Sablina (teacher at the language center \"Lingua Belle\")",
            Version: "1.8.0-1",
            Date: "2022-04-10"
            ),
        new (
            Name: "magda",
            Language: "polish",
            Description: "voice for polish language",
            Note: "",
            Version: "1.8.0-1",
            Date: "2022-04-10"
            ),
        new (
            Name: "marianna",
            Language: "ukrainian",
            Description: "voice for english language",
            Note: "",
            Version: "1.8.0-1",
            Date: "2022-04-10"
            ),
        new (
            Name: "mikhail",
            Language: "russian",
            Description: "voice for russian language",
            Note: "Mikhail Sokolov (news anchor on Autoradio)",
            Version: "1.8.0-1",
            Date: "2022-04-10"
            ),
        new (
            Name: "natalia",
            Language: "ukrainian",
            Description: "voice for english language",
            Note: "",
            Version: "1.8.0-1",
            Date: "2022-04-10"
            ),
        new (
            Name: "natan",
            Language: "polish",
            Description: "voice for english language",
            Note: "",
            Version: "1.8.0-1",
            Date: "2022-04-10"
            ),
        new (
            Name: "natia",
            Language: "georgian",
            Description: "voice for english language",
            Note: "",
            Version: "1.8.0-1",
            Date: "2022-04-10"
            ),
        new (
            Name: "nazgul",
            Language: "kyrgyz",
            Description: "voice for english language",
            Note: "",
            Version: "1.8.0-1",
            Date: "2022-04-10"
            ),
        new (
            Name: "pavel",
            Language: "russian",
            Description: "voice for russian language",
            Note: "Pavel Klyachenko (psychologist, tiflopsychologist)",
            Version: "1.8.0-1",
            Date: "2022-04-10"
            ),
        new (
            Name: "slt",
            Language: "english",
            Description: "voice for english language",
            Note: "",
            Version: "1.8.0-1",
            Date: "2022-04-10"
            ),
        new (
            Name: "spomenka",
            Language: "esperanto",
            Description: "voice for english language",
            Note: "",
            Version: "1.8.0-1",
            Date: "2022-04-10"
            ),
        new (
            Name: "suze",
            Language: "macedonian",
            Description: "voice for english language",
            Note: "",
            Version: "1.8.0-1",
            Date: "2022-04-10"
            ),
        new (
            Name: "talgat",
            Language: "tatar",
            Description: "voice for english language",
            Note: "",
            Version: "1.8.0-1",
            Date: "2022-04-10"
            ),
        new (
            Name: "tatiana",
            Language: "russian",
            Description: "voice for russian language",
            Note: "Tatiana Kruk (host of broadcasts on «Tiflo Info»)",
            Version: "1.8.0-1",
            Date: "2022-04-10"
            ),
        new (
            Name: "victoria",
            Language: "russian",
            Description: "voice for russian language",
            Note: "Natalya Arsenyeva (radio host and author of the travel blog \"I was there\")",
            Version: "1.8.0-1",
            Date: "2022-04-10"
            ),
        new (
            Name: "vitaliy",
            Language: "russian",
            Description: "voice for russian language",
            Note: "Vitaliy Chuvaev (brand voice of Russia Today TV channel)",
            Version: "1.8.0-1",
            Date: "2022-04-10"
            ),
        new (
            Name: "volodymyr",
            Language: "ukrainian",
            Description: "voice for english language",
            Note: "",
            Version: "1.8.0-1",
            Date: "2022-04-10"
            ),
        new (
            Name: "yuriy",
            Language: "russian",
            Description: "voice for russian language",
            Note: "Yuriy Zaborovsky (Soviet and Russian actor, audiobook reader)",
            Version: "1.8.0-1",
            Date: "2022-04-10"
            ),
//##new
//##RUN    pacman -S --noconfirm umka
//##RUN    pacman -S --noconfirm vitaliy-ng
    };
}
