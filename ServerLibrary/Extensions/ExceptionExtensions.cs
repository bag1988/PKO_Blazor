using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace ServerLibrary.Extensions
{
    public static class ExceptionExtensions
    {
        public static StatusCodeResult GetResultStatusCode(this Exception ex)
        {
            int result = (int)HttpStatusCode.BadRequest;

            if (ex is Grpc.Core.RpcException && (ex as Grpc.Core.RpcException)?.StatusCode == Grpc.Core.StatusCode.Unauthenticated)
            {
                result = (int)HttpStatusCode.Unauthorized;
            }
            else if (ex is Grpc.Core.RpcException && (ex as Grpc.Core.RpcException)?.StatusCode == Grpc.Core.StatusCode.Cancelled)
            {
                result = 299;
            }

            return new StatusCodeResult(result);
        }
    }
}
