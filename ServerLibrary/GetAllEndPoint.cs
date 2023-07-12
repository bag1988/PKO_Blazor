using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace ServerLibrary
{
    public static class GetAllEndPoint
    {
        public static string[] Get
        {
            get
            {
                string[] names = Array.Empty<string>();
                var cal = Assembly.GetEntryAssembly();
                List<string> list = new();
                list.AddRange(cal?.GetTypes().Where(type => typeof(Controller).IsAssignableFrom(type) || typeof(Hub).IsAssignableFrom(type)).Select(x => x.FullName ?? "No name") ?? Array.Empty<string>());

                Assembly? exec = Assembly.GetExecutingAssembly();
                list.AddRange(exec?.GetTypes().Where(type => typeof(Controller).IsAssignableFrom(type) || typeof(Hub).IsAssignableFrom(type)).Select(x => x.FullName ?? "No name") ?? Array.Empty<string>());

                return list.ToArray();
            }

        }
    }
}
