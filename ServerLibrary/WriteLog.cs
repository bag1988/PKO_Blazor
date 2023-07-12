//using Dapr.Client;
using System.Text.RegularExpressions;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ServerLibrary.Extensions;
using SMSSGsoProto.V1;
using static SMSSGsoProto.V1.SMSSGso;

namespace ServerLibrary
{
    public class WriteLog
    {
        private readonly SMSSGsoClient _SMSSgso;
        private readonly ILogger<WriteLog> _logger;
        private readonly IHttpContextAccessor _httpContext;

        public WriteLog(ILogger<WriteLog> logger, SMSSGsoClient SMSSgso, IHttpContextAccessor httpContext)
        {
            _logger = logger;
            _SMSSgso = SMSSgso;
            _httpContext = httpContext;
        }

        public async Task<bool> Write(int Source, int EventCode, int? SubsystemID = 0, int? UserID = 0, string? Info = null, int? Type = 0)
        {
            try
            {
                var result = await Write(new WriteLog2Request() { Source = Source, EventCode = EventCode, Info = Info ?? "", SubsystemID = SubsystemID ?? 0, UserID = UserID ?? 0, Type = Type ?? 0 });
                return result;
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, "WriteLog");
                return false;
            }
        }

        public async Task<bool> Write(WriteLog2Request request)
        {
            try
            {
                request.RegTime = DateTime.Now.ToUniversalTime().ToTimestamp();


                var r = new Regex("(\\d{1,3}).(\\d{1,3}).(\\d{1,3}).(\\d{1,3})");

                var localIp = r.Match(_httpContext.HttpContext?.Connection.LocalIpAddress?.ToString() ?? "").Groups.Values.Select(x => x.Value);

                var remoteIp = r.Match(_httpContext.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "").Groups.Values.Select(x => x.Value);

                var ip = remoteIp.FirstOrDefault();

                if (remoteIp.FirstOrDefault() == localIp.FirstOrDefault() || remoteIp.Skip(1).Take(3).SequenceEqual(localIp.Skip(1).Take(3)))
                {
                    ip = string.Empty;                    
                }

                if (request.Type == 0)
                    request.Type = 3;
                request.Info = $"{(!string.IsNullOrEmpty(ip) ? $"{ip} -> " : "")}{_httpContext.HttpContext?.Request.Host} {(string.IsNullOrEmpty(request.Info) ? "" : " - " + request.Info)}";// _httpContext.HttpContext?.Request.Host + (string.IsNullOrEmpty(request.Info) ? "" : " - " + request.Info);
                await _SMSSgso.WriteLog_2Async(request);
                return true;
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(Write));
                return false;
            }

        }
    }


}
