using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace WordleMultiplayer.Models
{
    [JsonObject]
    public class GroupState
    {
        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }
        [JsonProperty("group")]
        public string Group { get; set; }
        [JsonProperty("id")]
        public string DbId { get; set; } = null;

        public GroupState()
        { }

        public GroupState(string group)
        {
            Group = group;
            Timestamp = DateTime.Now;
        }

        public void Update(string group)
        {
            Timestamp = DateTime.Now;
            Group = group;
        }
    }
}
