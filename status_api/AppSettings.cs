using System;

namespace status_api
{
    public class AppSettings
    {
        public string Environment {get; set;} = String.Empty;
        public int MaxPayloadSize {get; set;} = 10000000;
    }
}
