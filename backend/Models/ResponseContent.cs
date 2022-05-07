using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleChat.Models
{
    [JsonObject]
    public class ResponseContent
    {
        [JsonProperty("content")]
        public string Content { get; set; }
        [JsonProperty("action")]
        public ActionDefinition Action { get; set; } = ActionDefinition.Default;

        public ResponseContent()
        {

        }

        public ResponseContent(string Content, ActionDefinition Action)
        {
            this.Content = Content;
            this.Action = Action;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
