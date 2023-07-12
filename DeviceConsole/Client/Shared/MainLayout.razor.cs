using System.Linq;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using BlazorLibrary.Shared;
using GateServiceProto.V1;
using SMSSGsoProto.V1;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using SharedLibrary;
using SharedLibrary.Models;

namespace DeviceConsole.Client.Shared
{
    partial class MainLayout : IDisposable
    {
        private int ActiveSubMenu = 0;
        private int IsViewSubMenu = 0;
        bool IsViewChild = false;

        private ConfigStart configStart = new();

        private Main? elem = new();

        readonly Dictionary<int, int> subsystemList = new Dictionary<int, int>()
        {
            { SubsystemType.SUBSYST_Setting, SubsystemType.SUBSYST_Setting },
            { SubsystemType.SUBSYST_ASO, SubsystemType.SUBSYST_ASO },
            { SubsystemType.SUBSYST_P16x, SubsystemType.SUBSYST_P16x },
            { SubsystemType.SUBSYST_SRS, SubsystemType.SUBSYST_SZS },
            { SubsystemType.SUBSYST_SZS, SubsystemType.SUBSYST_SZS },
            { SubsystemType.SUBSYST_GSO_STAFF, SubsystemType.SUBSYST_GSO_STAFF },
            { SubsystemType.SUBSYST_Security, SubsystemType.SUBSYST_Security },
            { SubsystemType.SUBSYST_TASKS, SubsystemType.SUBSYST_ASO }
        };

        public readonly static Dictionary<int, Dictionary<string, string>> urlForSubsystemList = new();

        protected override async Task OnInitializedAsync()
        {
            await GetConfStart();
            urlForSubsystemList.Clear();
            urlForSubsystemList.Add(SubsystemType.SUBSYST_Setting, new Dictionary<string, string>{
                            { "SettingsSound",SMDataRep["IDS_STRING_SOUND"] },
                            { "Settings",AsoRep["IDS_STRING_PARAMS"] },
                            { "SettingBase",DeviceRep["BackupGSO"] }
                        });
            if (configStart.ASO)
            {
                urlForSubsystemList.Add(SubsystemType.SUBSYST_ASO, new Dictionary<string, string>{
                            { "ViewLocation" ,AsoDataRep["IDS_STRING_LOCATION"]},
                            { "ViewLine" ,AsoDataRep["IDS_STRING_LINE"]},
                            { "ViewDevice" ,AsoRep["IDS_ASODEVICE"]},
                            { "SubParamEdit" ,AsoDataRep["IDS_STRING_SUBSYSTEM_PARAMS"]},
                            { "ViewMessages",AsoDataRep["IDS_STRING_MESSAGE"] },
                            { "ViewDepartment",AsoRep["IDS_STRING_DEPARTMENTS"] },
                            { "ViewAbonent",AsoRep["IDS_STRING_ABONENTS"] },
                            { "ViewList",GsoRep["IDS_STRING_LIST_AB"] },
                            { "ViewSituation",AsoDataRep["IDS_STRING_SITUATION"] },
                            { "ViewCalendar",AsoDataRep["IDS_STRING_BIRTHDAY"] },
                            { "ViewReports",AsoDataRep["IDS_STRING_REPORT_SETTINGS"] },
                            { "ViewPatterns",AsoRep["PATTERNS_PREPARE"] },
                            { "Linaso",AsoRep["TEST_CHANNEL"] }
                        });
            }
            if (configStart.P16x)
            {
                urlForSubsystemList.Add(SubsystemType.SUBSYST_P16x, new Dictionary<string, string>{
                            { "TestMode",UUZSDataRep["IDS_STRING_TESTING_UUZS"] },
                            { "Devices",AsoRep["IDS_ASODEVICE"] },
                            { "SubParamP16",AsoDataRep["IDS_STRING_SUBSYSTEM_PARAMS"] },
                            { "ViewMessages",AsoDataRep["IDS_STRING_MESSAGE"] },
                            { "CmdBinding",SMP16xRep["IDS_STRING_LINK_CMD"] }
                        });
            }

            if (configStart.STAFF)
            {
                urlForSubsystemList.Add(SubsystemType.SUBSYST_GSO_STAFF, new Dictionary<string, string>{
                            { "SubParamStaff",AsoDataRep["IDS_STRING_SUBSYSTEM_PARAMS"] },
                            { "ViewLocation",AsoDataRep["IDS_STRING_LOCATION"] },
                            { "RegistrationList",Rep["Registration"] },
                            { "ViewReports",AsoDataRep["IDS_STRING_REPORT_SETTINGS"] },
                            { "ViewSituation",AsoDataRep["IDS_STRING_SITUATION"] },
                            { "ViewMessages",AsoDataRep["IDS_STRING_MESSAGE"] }
                        });
            }
            urlForSubsystemList.Add(SubsystemType.SUBSYST_Security, new Dictionary<string, string>{
                            { "Security",AsoDataRep["IDS_STRING_USERS"] }
                        });
            urlForSubsystemList.Add(SubsystemType.SUBSYST_TASKS, new Dictionary<string, string>{
                            { "AutoNotify",DeviceRep["AUTO_NOTIFY"] }
                        });

            if (configStart.UUZS)
            {
                urlForSubsystemList.Add(SubsystemType.SUBSYST_SRS, new Dictionary<string, string>{
                            { "SRSView" ,DeviceRep["SETTING_SRS"]}
                        });
                urlForSubsystemList.Add(SubsystemType.SUBSYST_SZS, new Dictionary<string, string>{
                            { "ViewLocation",AsoDataRep["IDS_STRING_LOCATION"] },
                            { "TestMode",UUZSDataRep["IDS_STRING_TESTING_UUZS"] },
                            { "Devices",AsoRep["IDS_ASODEVICE"] },
                            { "ViewLine",AsoDataRep["IDS_STRING_LINE"] },
                            { "SubParamEdit",AsoDataRep["IDS_STRING_SUBSYSTEM_PARAMS"] },
                            { "ViewMessages",AsoDataRep["IDS_STRING_MESSAGE"] }
                        });
                await GetDeviceSzs();
                urlForSubsystemList[SubsystemType.SUBSYST_SZS].Add("Groups", UUZSDataRep["IDS_STRING_TERMINAL_DEVICES_GROUPS"]);
                urlForSubsystemList[SubsystemType.SUBSYST_SZS].Add("ViewList", GsoRep["IDS_STRING_LIST_DEVICES"]);
                urlForSubsystemList[SubsystemType.SUBSYST_SZS].Add("ViewSituation", AsoDataRep["IDS_STRING_SITUATION"]);
            }

            await Task.Yield();
            ActiveSubMenu = elem?.SubsystemID ?? 0;

            MyNavigationManager.LocationChanged += MyNavigationManager_LocationChanged;

            ChangeUrl();

            //add detect close window
            var reference = DotNetObjectReference.Create(this);
            await JSRuntime.InvokeVoidAsync("CloseWindows", reference);
            await Http.PostAsJsonAsync("api/v1/allow/WriteLog", new WriteLog2Request() { Source = (int)GSOModules.DeviceConsole_Module, EventCode = 132, SubsystemID = 0, UserID = await _User.GetUserId() });
        }

        [JSInvokable]
        public async Task CloseWindows()
        {
            await Http.PostAsJsonAsync("api/v1/allow/WriteLog", new WriteLog2Request() { Source = (int)GSOModules.DeviceConsole_Module, EventCode = 133, SubsystemID = 0, UserID = await _User.GetUserId() });
        }

        private async Task GetConfStart()
        {
            await Http.PostAsync("api/v1/allow/GetConfStart", null).ContinueWith(async (x) =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    configStart = await x.Result.Content.ReadFromJsonAsync<ConfigStart>() ?? new();
                }
            });
        }

        private async Task GetDeviceSzs()
        {
            if (!urlForSubsystemList[SubsystemType.SUBSYST_SZS].Any(x => x.Key.Contains("DevicesSZS")))
            {
                var result = await Http.PostAsync($"api/v1/GetObjects_ITerminalDevice", null);
                if (result.IsSuccessStatusCode)
                {
                    var Model = await result.Content.ReadFromJsonAsync<List<SubsystemObject>>() ?? new();
                    Model = Model.OrderBy(x => x.Name).ToList();

                    foreach (var x in Model)
                    {
                        if (!urlForSubsystemList[SubsystemType.SUBSYST_SZS].ContainsKey($"DevicesSZS/{x.Type}"))
                            urlForSubsystemList[SubsystemType.SUBSYST_SZS].Add($"DevicesSZS/{x.Type}", x.Name);
                    }
                }
            }

        }


        IEnumerable<KeyValuePair<string, string>> GetDevice
        {
            get
            {
                return urlForSubsystemList[SubsystemType.SUBSYST_SZS].Where(x => x.Key.Contains("DevicesSZS"));
            }
        }

        private void MyNavigationManager_LocationChanged(object? sender, LocationChangedEventArgs e)
        {
            ChangeUrl();
        }

        void ChangeUrl()
        {
            var url = MyNavigationManager.Uri.Replace(MyNavigationManager.BaseUri, "");

            if (!string.IsNullOrEmpty(url))
            {
                var b = CheckQuery();
                if (!b && (!urlForSubsystemList.ContainsKey(ActiveSubMenu) || !urlForSubsystemList[ActiveSubMenu].Any(x => url.Contains(x.Key))))
                {
                    var firstElem = urlForSubsystemList.FirstOrDefault(x => x.Value.Any(v => url.Contains(v.Key))).Key;
                    ChangeViewDropDown(firstElem);
                }

                if (url.Contains("DevicesSZS"))
                    IsViewChild = true;
            }
            else
                IsViewSubMenu = ActiveSubMenu = 0;
        }


        bool CheckQuery()
        {
            var uri = MyNavigationManager.Uri;

            if (!string.IsNullOrEmpty(uri))
            {
                string pattern = "(?:SystemId/(\\d{1,2}))";

                var m = Regex.Match(uri, pattern, RegexOptions.IgnoreCase);

                if (m.Success)
                {
                    int.TryParse(m.Groups[1].Value, out int SystemID);

                    if (subsystemList.ContainsKey(SystemID))
                    {
                        ChangeViewDropDown(SystemID);
                        return true;
                    }
                }
            }
            IsViewSubMenu = ActiveSubMenu;
            return false;
        }


        private void ChangeViewDropDown(int item)
        {
            IsViewSubMenu = IsViewSubMenu == item ? 0 : item;
            ActiveSubMenu = item;
            if (subsystemList.ContainsKey(item))
                elem?.ChangeSubSystem(subsystemList[item]);
        }

        public void Dispose()
        {
            MyNavigationManager.LocationChanged -= MyNavigationManager_LocationChanged;
        }
    }
}
