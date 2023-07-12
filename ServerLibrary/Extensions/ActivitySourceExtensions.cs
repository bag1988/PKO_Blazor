using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace ServerLibrary.Extensions
{
    public static class ActivitySourceExtensions
    {
        public static ActivitySource? ActivitySourceForHub(this Hub obj)
        {
            return new ActivitySource(obj.GetType().FullName ?? throw new ArgumentOutOfRangeException());
        }

        public static ActivitySource? ActivitySourceForController(this Controller obj)
        {
            return new ActivitySource(obj.GetType().FullName ?? throw new ArgumentOutOfRangeException());
        }
    }
}
