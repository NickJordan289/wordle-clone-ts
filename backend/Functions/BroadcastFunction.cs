using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.WebPubSub;
using Microsoft.Azure.WebPubSub.Common;
using Newtonsoft.Json;
using WordleMultiplayer.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SimpleChat.Models;
using System.Net.Http;
using System.Linq;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Extensions.Logging;

namespace WordleMultiplayer.Functions
{
    public class BroadcastFunction
    {
        static Uri collectionUri = UriFactory.CreateDocumentCollectionUri("wordle", "games");
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
            [WebPubSub(Hub = "%WebPubSubHub%")] IAsyncCollector<WebPubSubAction> actions,
            [CosmosDB(
                databaseName: "wordle",
                collectionName: "games",
                ConnectionStringSetting = "CosmosDBConnection")] DocumentClient client,
             ILogger log)
        {
            // Retrieve group stored in state
            var states = new GroupState("lobby");
            connectionContext.ConnectionStates.TryGetValue(nameof(GroupState), out var currentState);
            if (currentState != null)
            {
                states = currentState.ToObjectFromJson<GroupState>();
            }


            // Deserialize the client input
            ClientContent content = JsonConvert.DeserializeObject<ClientContent>(data.ToString());

            ResponseContent responseContent = new ResponseContent();
            if (content.Action == ActionDefinition.Create)
            {
                responseContent = await CreateGameAsync(content, client);
                states.Update(responseContent.Content);
            }
            else if (content.Action == ActionDefinition.Join)
            {
                // Check if can join this lobby
                //
                Game game = await GetGameByIdAsync(content.Content, client);
                if (game != null)
                {
                    responseContent = new ResponseContent
                    {
                        Action = ActionDefinition.Join,
                        Content = content.Content
                    };
                    states.Update(responseContent.Content);
                }
                else
                {
                    responseContent = new ResponseContent { Action = ActionDefinition.Default };
                }
            }
            else if (content.Action == ActionDefinition.Leave)
            {
                // TODO:
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
                // TODO:
                // Broadcast back to group / opponent

                Game game = await GetGameByIdAsync(states.Group, client);
                if (game != null)
                {
                    var matches = new List<int>(game.TargetWord.Length);
                    for (int i = 0; i < game.TargetWord.Length; i++)
                    {
                        var targetLetter = game.TargetWord[i];
                        var letterGuess = content.Content.ToLower()[i];
                        if (letterGuess == targetLetter)
                        {
                            matches.Add(1); // Green
                        }
                        else if (game.TargetWord.Contains(letterGuess))
                        {
                            matches.Add(2); // Yellow
                        }
                        else
                        {
                            matches.Add(0); // Nothing
                        }
                    }

                    var guessRecord = new GuessRecord
                    {
                        Score = matches,
                        Word = content.Content.ToLower()
                    };

                    // Upsert updated guess array
                    game.Guesses.Add(guessRecord);
                    var upsertResponse = await client.UpsertDocumentAsync(collectionUri, game);

                    // Return just the current guess to the caller 
                    responseContent = new ResponseContent
                    {
                        Content = JsonConvert.SerializeObject(guessRecord),
                        Action = ActionDefinition.Guess
                    };
                }
                else
                {
                    // If lost connection to game
                    // Join whats in the state object (probably lobby)
                    responseContent = new ResponseContent
                    {
                        Content = states.Group,
                        Action = ActionDefinition.Join
                    };
                }
            }

            // Add WPS Action to swap groups
            if (responseContent.Action == ActionDefinition.Join)
            {
                await actions.AddAsync(WebPubSubAction.CreateAddUserToGroupAction(connectionContext.UserId, responseContent.Content));
            }

            // Return response as json and store Group State
            // goes back to the caller
            var response = request.CreateResponse(BinaryData.FromString(responseContent.ToString()), WebPubSubDataType.Json);
            response.SetState(nameof(GroupState), BinaryData.FromObjectAsJson(states));
            return response;
        }

        private static async Task<Game> GetGameByIdAsync(string gameid, DocumentClient service)
        {
            // This needs fixing to not use a while loop
            // Couldn't get the .Where filter to work
            var option = new FeedOptions { EnableCrossPartitionQuery = true };
            IDocumentQuery<Game> query = service.CreateDocumentQuery<Game>(collectionUri, option)
                .AsDocumentQuery();

            while (query.HasMoreResults)
            {
                foreach (Game result in await query.ExecuteNextAsync())
                {
                    if (result.Name == gameid)
                        return result;
                }
            }

            return null;
        }

        private static async Task<ResponseContent> CreateGameAsync(ClientContent content, DocumentClient service)
        {
            string randomName = Guid.NewGuid().ToString().Replace("-","")[..10];
            string randomWord = await GetRandomWordAsync();

            var documentResponse = await service.CreateDocumentAsync(collectionUri, new Game
            {
                Name = randomName,
                TargetWord = randomWord,
                Guesses = new List<GuessRecord>()
            });

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
