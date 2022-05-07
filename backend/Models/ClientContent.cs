using Newtonsoft.Json;
using SimpleChat.Models;
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

        [JsonProperty("action")]
        public ActionDefinition Action { get; set; }

        public ClientContent()
        {

        }

        public ClientContent(string message)
        {
            From = "[System]";
            Content = message;
        }

        public ClientContent(string from, string message)
        {
            From = from;
            Content = message;
        }

        public ClientContent(string from, string message, ActionDefinition action)
        {
            From = from;
            Content = message;
            Action = action;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
