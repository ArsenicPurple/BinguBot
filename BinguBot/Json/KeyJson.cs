using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BinguBot
{
    class KeyJson
    {
        [JsonProperty("key")]
        public string Key { get; private set; }
    }
}
