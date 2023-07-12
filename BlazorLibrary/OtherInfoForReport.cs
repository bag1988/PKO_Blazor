using Microsoft.Extensions.Localization;
using ReplaceLibrary;

namespace BlazorLibrary
{
    public class OtherInfoForReport
    {
        private readonly IStringLocalizer<ASOReplace> AsoRep;
        private readonly IStringLocalizer<StartUIReplace> StartUIRep;
        public OtherInfoForReport(IStringLocalizer<ASOReplace> _AsoRep, IStringLocalizer<StartUIReplace> _StartUIRep)
        {
            AsoRep = _AsoRep;
            StartUIRep = _StartUIRep;
        }


        public string FiltrOther(string? filtr = null)
        {
            if (!string.IsNullOrEmpty(filtr))
                return StartUIRep["APPLIED_FILTR"] + ": " + filtr;
            return string.Empty;
        }


        public List<List<string>> AsoOther(DateTime? startDate, DateTime? endDate, int? countyes = 0, int? countno = 0)
        {
            var allCount = (countyes + countno) ?? 0;

            List<List<string>> OtherInfo = new()
            {
              new List<string>()  { StartUIRep["IDS_SESSION_B"], startDate?.ToString() ?? string.Empty},
                new List<string>()  {StartUIRep["IDS_SESSION_E"], endDate?.ToString() ?? string.Empty},
                new List<string>()  {StartUIRep["IDS_COUNTYES"] ,$"{countyes}/{allCount} ({Math.Ceiling((double)(countyes ?? 0) / allCount)*100}%)" },
                new List<string>()  {StartUIRep["IDS_COUNTNO"] , $"{countno}/{allCount} ({Math.Ceiling((double)(countno ?? 0) / allCount)*100}%)" }
            };
            return OtherInfo;
        }


        public List<string> ChannelsOther(string? nameBlock = "", string? comment = "", int? countChannel = 0, string? state = "", string? info = "")
        {
            List<string> OtherInfo = new List<string>
            {
                AsoRep["NameBlok"] + ": " + nameBlock,
                AsoRep["IDS_STRING_COMMENT"] + ": " + comment,
                AsoRep["CountChannel"] + ": " + countChannel,
                StartUIRep["IDS_CHANNEL_STATE"] + ": " + state,
                StartUIRep["IDS_C_INFO"] + ": " + info
            };

            return OtherInfo;
        }

        public List<string> SzsOther(DateTime? StartDate, DateTime? EndDate, int? COUNTYES = 0, int? COUNTNO = 0)
        {
            List<string> OtherInfo = new List<string>
            {
                StartUIRep["IDS_SESSION_B"] + ": " + StartDate.ToString(),
                StartUIRep["IDS_SESSION_E"] + ": " + EndDate.ToString(),
                StartUIRep["IDS_SUCC"] + ": " + COUNTYES,
                StartUIRep["IDS_FAIL"] + ": " + COUNTNO
            };
            return OtherInfo;
        }

        public List<string> SmpOther(DateTime? StartDate, DateTime? EndDate, string? commandName)
        {
            List<string> OtherInfo = new List<string>
            {
                StartUIRep["IDS_SESSION_B"] + ": " + StartDate.ToString(),
                StartUIRep["IDS_SESSION_E"] + ": " + EndDate.ToString(),
                StartUIRep["IDS_COMMANDCOLUMN"] + ": " + commandName
            };
            return OtherInfo;
        }

        public List<string> CUDetaliOther(string? SitName, DateTime? StartDate, DateTime? EndDate)
        {
            List<string> OtherInfo = new List<string>
            {
                StartUIRep["IDS_COMMANDCOLUMN"] + ": " + SitName,
                StartUIRep["IDS_SESSION_B"] + ": " + StartDate.ToString(),
                StartUIRep["IDS_SESSION_E"] + ": " + EndDate.ToString()
            };
            return OtherInfo;
        }

        public List<string> CUResultView(string? SitName, DateTime? StartDate, DateTime? EndDate, int Succes, int Fail)
        {
            List<string> OtherInfo = new List<string>
            {
                StartUIRep["IDS_COMMANDCOLUMN"] + ": " + SitName,
                StartUIRep["IDS_SESSION_B"] + ": " + StartDate.ToString(),
                StartUIRep["IDS_SESSION_E"] + ": " + EndDate.ToString(),
                StartUIRep["IDS_SUCC"] + ": " + Succes.ToString(),
                StartUIRep["IDS_FAIL"] + ": " + Fail.ToString()
            };
            return OtherInfo;
        }

        public List<string> AbonOther(int? CountAbon = 0)
        {
            List<string> OtherInfo = new List<string>
            {
                AsoRep["CountAbon"] + ": " + CountAbon
            };
            return OtherInfo;
        }

    }
}
