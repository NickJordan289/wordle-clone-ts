using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace WordleMultiplayer.Models
{
    [JsonObject]
    public class ClientContent
    {
        [JsonProperty("from")]
        public string From { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }

        [JsonProperty("is_system_action")]
        public bool IsSystemAction { get; set; } = false;

        [JsonProperty("system_action")]
        public string SystemAction { get; set; } = "";

        public ClientContent()
        {

        }

        public ClientContent(string message)
        {
            From = "[System]";
            Content = message;
            IsSystemAction = false;
            SystemAction = "";
        }

        public ClientContent(string from, string message)
        {
            From = from;
            Content = message;
            IsSystemAction = false;
            SystemAction = "";
        }

        public ClientContent(string from, string message, bool issystemAction, string systemaction)
        {
            From = from;
            Content = message;
            IsSystemAction = issystemAction;
            SystemAction = systemaction;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
