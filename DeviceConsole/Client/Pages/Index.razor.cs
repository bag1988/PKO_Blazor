using SMSSGsoProto.V1;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using SharedLibrary;
using System.Net.Http.Json;
using DeviceConsole.Client.Shared;
using SMDataServiceProto.V1;

namespace DeviceConsole.Client.Pages
{
    partial class Index
    {
        [Parameter]
        public int SystemId { get; set; }

        string SelectItem = string.Empty;

        ElementReference? divElem;

        Dictionary<int, Dictionary<string, string>> urlForSubsystemList
        {
            get
            {
                return MainLayout.urlForSubsystemList;
            }
        }

        protected override async Task OnParametersSetAsync()
        {            
            await Task.Yield();
            divElem?.FocusAsync();
        }


        string GetDesc(string key)
        {
            if (key.Contains("DevicesSZS"))
                return string.Empty;

            if (key == "ViewSituation" && SystemId == SubsystemType.SUBSYST_GSO_STAFF)
            {
                return Rep[$"DESC_{key}_STAFF"];
            }
            return Rep[$"DESC_{key}"];
        }

        bool IsSelect(string url)
        {
            return SelectItem == url;
        }


        public void KeySet(KeyboardEventArgs e)
        {
            if (e.Type == "keydown")
            {
                int index = 1;

                if (e.Code == "ArrowUp" || e.Code == "ArrowLeft")
                {
                    index = -1;
                }
                else if (e.Code == "ArrowDown" || e.Code == "ArrowRight")
                {
                    index = 1;
                }
                else
                    return;

                if (SystemId == 0)
                {
                    if (index == 1)
                    {
                        var newElem = urlForSubsystemList.SkipWhile(x => $"SystemId/{x.Key}" != SelectItem).FirstOrDefault(x => $"SystemId/{x.Key}" != SelectItem);

                        if (newElem.Key == 0)
                        {
                            SelectItem = $"SystemId/{urlForSubsystemList.First().Key}";
                        }
                        else
                            SelectItem = $"SystemId/{newElem.Key}";
                    }
                    else
                    {
                        var newElem = urlForSubsystemList.TakeWhile(x => $"SystemId/{x.Key}" != SelectItem).LastOrDefault(x => $"SystemId/{x.Key}" != SelectItem);

                        if (newElem.Key == 0)
                        {
                            SelectItem = $"SystemId/{urlForSubsystemList.Last().Key}";
                        }
                        else
                            SelectItem = $"SystemId/{newElem.Key}";
                    }
                }
                else if (urlForSubsystemList.ContainsKey(SystemId))
                {

                    var elems = urlForSubsystemList[SystemId];

                    if (index == 1)
                    {
                        var newElem = elems.SkipWhile(x => x.Key != SelectItem).FirstOrDefault(x => x.Key != SelectItem);

                        if (string.IsNullOrEmpty(newElem.Key))
                        {
                            SelectItem = elems.First().Key;
                        }
                        else
                            SelectItem = newElem.Key;
                    }
                    else
                    {
                        var newElem = elems.TakeWhile(x => x.Key != SelectItem).LastOrDefault(x => x.Key != SelectItem);

                        if (string.IsNullOrEmpty(newElem.Key))
                        {
                            SelectItem = elems.Last().Key;
                        }
                        else
                            SelectItem = newElem.Key;
                    }
                }
            }
        }


        int GetCountColumn
        {
            get
            {
                int count = 3;
                if (SystemId > 0 && urlForSubsystemList.ContainsKey(SystemId))
                {
                    var sizeArray = urlForSubsystemList[SystemId].Count;                    
                    count = (int)Math.Ceiling(Math.Sqrt((double)sizeArray));
                    if (count < 3)
                        count = 3;
                }
                return count;
            }
        }

        string GetTitle
        {
            get
            {
                string title = AsoRep["IDS_STRING_Parameters"];
                switch (SystemId)
                {
                    case SubsystemType.SUBSYST_Setting:
                        title = DeviceRep["IDS_STRING_SYSTEM_SETTINGS"]; break;
                    case SubsystemType.SUBSYST_ASO:
                        title = SMGateRep["IDS_STRING_ASO"]; break;
                    case SubsystemType.SUBSYST_P16x:
                        title = @GsoRep["P16X"]; break;
                    case SubsystemType.SUBSYST_SRS:
                        title = DeviceRep["REGISTR_SRS"]; break;
                    case SubsystemType.SUBSYST_SZS:
                        title = SMGateRep["IDS_STRING_SZS"]; break;
                    case SubsystemType.SUBSYST_GSO_STAFF:
                        title = StartUIRep["IDS_STAFFTITLE"]; break;
                    case SubsystemType.SUBSYST_Security:
                        title = DeviceRep["IDS_STRING_SYSTEM_SECURITY"]; break;
                    case SubsystemType.SUBSYST_TASKS:
                        title = DeviceRep["ADDITIONAL"]; break;
                }
                return title;
            }
        }
    }
}
