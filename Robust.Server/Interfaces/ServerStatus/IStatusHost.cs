using System;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;

namespace Robust.Server.Interfaces.ServerStatus
{
    public delegate bool StatusHostHandler(HttpMethod method, HttpRequest request, HttpResponse response);

    public interface IStatusHost
    {
        void Start();

        void AddHandler(StatusHostHandler handler);

        /// <summary>
        ///     Invoked when a client queries a status request from the server.
        ///     THIS IS INVOKED FROM ANOTHER THREAD.
        ///     I REPEAT, THIS DOES NOT RUN ON THE MAIN THREAD.
        ///     MAKE TRIPLE SURE EVERYTHING IN HERE IS THREAD SAFE DEAR GOD.
        /// </summary>
        event Action<JObject> OnStatusRequest;

        /// <summary>
        ///     Invoked when a client queries an info request from the server.
        ///     THIS IS INVOKED FROM ANOTHER THREAD.
        ///     I REPEAT, THIS DOES NOT RUN ON THE MAIN THREAD.
        ///     MAKE TRIPLE SURE EVERYTHING IN HERE IS THREAD SAFE DEAR GOD.
        /// </summary>
        event Action<JObject> OnInfoRequest;
    }
}
