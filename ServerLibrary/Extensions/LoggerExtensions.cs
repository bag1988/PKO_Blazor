using System.Data;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Dapr;

namespace ServerLibrary.Extensions
{
    public static class LoggerExtensions
    {
        public static void WriteLogError(this ILogger logger, Exception exception, string? action, params object?[] args)
        {
            if (exception is RpcException)
                logger.LogError("RPC exception in {Action}: StatusCode - {StatusCode}, Detail - {Detail}", action, ((RpcException)exception).StatusCode, ((RpcException)exception).Status.Detail);
            else if (exception is DaprException)
            {
                logger.LogError("DaprException exception in {Action}: {Status}", action, ((DaprException)exception).InnerException?.InnerException?.Message ?? exception.HResult.ToString());
            }
            else
                logger.LogError("Exception in {Action}: {Status}", action, exception.InnerException?.InnerException?.Message ?? exception.Source ?? exception.HResult.ToString());
        }
    }
}
