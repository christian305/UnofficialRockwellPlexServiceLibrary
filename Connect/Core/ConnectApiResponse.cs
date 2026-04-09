using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace RockwellPlexServiceLibrary.Connect.Core
{
    public class ConnectApiResponse
    {
        public HttpStatusCode StatusCode { get; set; }
        public bool IsSuccessStatusCode { get; set; }
        public string ReasonPhrase { get; set; }
        public string Content { get; set; }
    }
}
