using SharedLibrary.Models;

namespace BBImport.Client.Shared
{
    partial class MainLayout
    {
        MenuBBImport? menu { get; set; }

        public ReadFileRequestInfo request => menu?.request ?? new();

        public Dictionary<int, string> ThListFile
        {
            get
            {
                return menu?.ThListFile ?? new();
            }
            set
            {
                if (menu != null)
                {
                    menu.ThListFile = value;
                }
            }
        }

        public Action? StartUpdateFile { get; set; }
        public Action? StartUpdateInfo { get; set; }

        void UpdateFile()
        {
            StartUpdateFile?.Invoke();
        }
        void UpdateInfo()
        {
            StartUpdateInfo?.Invoke();
        }
    }
}
