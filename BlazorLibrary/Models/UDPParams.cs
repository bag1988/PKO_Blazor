using SharedLibrary.Utilities;
using System.Net;

namespace BlazorLibrary.Models
{
    public class UDPParams
    {
        //public bool IsReset { get; set; }
        public string? RemoteIpAddress { get; set; }
        public int RemotePort { get; set; } = 0;
        public string? LocalIpAddress { get; set; }
        public int LocalPort { get; set; } = 0;
        //public byte SoundFormat { get; set; }
        //public byte HardwareMonitorType { get; set; }
        //public int PortNo { get; set; }


        public UDPParams()
        {

        }
        public UDPParams(string RemoteIp, string LocalIp)
        {
            if (!string.IsNullOrEmpty(RemoteIp))
            {
                var s = RemoteIp.Split(":");

                RemoteIpAddress = s[0];

                if (s.Length >= 1)
                {
                    int.TryParse(s[1], out int p);
                    RemotePort = p;
                }
            }

            if (!string.IsNullOrEmpty(LocalIp))
            {
                var s = LocalIp.Split(":");

                LocalIpAddress = s[0];

                if (s.Length >= 1)
                {
                    int.TryParse(s[1], out int p);
                    LocalPort = p;
                }
            }
        }

        public string GetRemoteIP()
        {
            RemoteIpAddress = IpAddressUtilities.ParseEndPoint(RemoteIpAddress);
            if (string.IsNullOrEmpty(RemoteIpAddress) || RemoteIpAddress == "0.0.0.0")
                return string.Empty;
            return $"{RemoteIpAddress}:{RemotePort}";
        }
        public string GetLocalIP()
        {
            LocalIpAddress = IpAddressUtilities.ParseEndPoint(LocalIpAddress);
            if (string.IsNullOrEmpty(LocalIpAddress) || LocalIpAddress == "0.0.0.0")
                return string.Empty;
            return $"{LocalIpAddress}:{LocalPort}";
        }
    }
}
