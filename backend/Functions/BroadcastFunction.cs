using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.WebPubSub;
using Microsoft.Azure.WebPubSub.Common;
using Newtonsoft.Json;
using WordleMultiplayer.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WordleMultiplayer.Models;

namespace WordleMultiplayer.Functions
{
    public class BroadcastFunction
    {
        /// <summary>
        /// TODO: Needs to be deconstructed and implemented with cosmosdb
        /// </summary>
        /// <param name="request"></param>
        /// <param name="connectionContext"></param>
        /// <param name="data"></param>
        /// <param name="dataType"></param>
        /// <param name="actions"></param>
        /// <returns></returns>
        [FunctionName("broadcast")]
        public static async Task<WebPubSubEventResponse> Broadcast(
            [WebPubSubTrigger("%WebPubSubHub%", WebPubSubEventType.User, "message")] // another way to resolve Hub name from settings.
            UserEventRequest request,
            WebPubSubConnectionContext connectionContext,
            BinaryData data,
            WebPubSubDataType dataType,
            [WebPubSub(Hub = "%WebPubSubHub%")] IAsyncCollector<WebPubSubAction> actions)
        {
            var states = new GroupState("lobby");
            connectionContext.ConnectionStates.TryGetValue(nameof(GroupState), out var currentState);
            if (currentState != null)
            {
                states = currentState.ToObjectFromJson<GroupState>();
            }

            ClientContent content = JsonConvert.DeserializeObject<ClientContent>(data.ToString());
            


            var response = request.CreateResponse(BinaryData.FromString(JsonConvert.ToString(states)), WebPubSubDataType.Json);
            return response;
        }
    }
}
