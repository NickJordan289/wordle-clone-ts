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
using SimpleChat.Models;
using System.Net.Http;
using System.Linq;

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

            ResponseContent responseContent = new ResponseContent();
            if (content.Action == ActionDefinition.Create)
            {
                // Create game in cosmos db table
                //

                responseContent = await CreateGameAsync(content);
                states.Update(responseContent.Content);
            }
            else if (content.Action == ActionDefinition.Join)
            {
                // Check if can join this lobby
                //

                responseContent = new ResponseContent
                {
                    Action = ActionDefinition.Join,
                    Content = content.Content
                };
                states.Update(responseContent.Content);
            }
            else if (content.Action == ActionDefinition.Leave)
            {
                // broadcast to opponent that user is leaving
                //

                responseContent = new ResponseContent
                {
                    Action = ActionDefinition.Join,
                    Content = "lobby"
                };
                states.Update(responseContent.Content);
            }
            else if (content.Action == ActionDefinition.Guess)
            {
                // Read group cosmosdb table
                // Check guess against target word
                // Update cosmosdb table
                // Broadcast back to group
            }

            var response = request.CreateResponse(BinaryData.FromString(responseContent.ToString()), WebPubSubDataType.Json);
            response.SetState(nameof(GroupState), BinaryData.FromObjectAsJson(states));
            return response;

            //var response = request.CreateResponse(BinaryData.FromString(JsonConvert.ToString(states)), WebPubSubDataType.Json);
            //return response;
        }

        private static async Task<ResponseContent> CreateGameAsync(ClientContent content)
        {
            string randomName = Guid.NewGuid().ToString().Replace("-","")[..10];
            string randomWord = await GetRandomWordAsync();
            return new ResponseContent
            {
                Action = ActionDefinition.Join,
                Content = randomName
            };
        }

        private static async Task<string> GetRandomWordAsync(int wordDifficulty=5)
        {
            var url = $"https://random-word-api.herokuapp.com/word?length={wordDifficulty}";
            var client = new HttpClient();
            var response = await client.GetAsync(url);
            var words = await response.Content.ReadAsAsync<List<string>>();
            return words.FirstOrDefault();
        }
    }
}
