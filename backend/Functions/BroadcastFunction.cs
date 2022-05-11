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
                responseContent = await CreateGameAsync(connectionContext, client);
                states.Update(responseContent.Content);
            }
            else if (content.Action == ActionDefinition.Join)
            {
                Game game = await GetGameByIdAsync(content.Content, client);
                if (game != null && game.Players.Count < 2)
                {
                    responseContent = new ResponseContent
                    {
                        Action = ActionDefinition.Join,
                        Content = content.Content
                    };

                    game.Players.Add(connectionContext.UserId);
                    if (game.Players.Count == 2) game.Status = GameStatus.InProgress;

                    var upsertResponse = await client.UpsertDocumentAsync(collectionUri, game);

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
                Game game = await GetGameByIdAsync(content.Content, client);
                if (game != null)
                {
                    game.Players.Remove(connectionContext.UserId);
                    var upsertResponse = await client.UpsertDocumentAsync(collectionUri, game);
                }

                responseContent = new ResponseContent
                {
                    Action = ActionDefinition.Join,
                    Content = "lobby"
                };
                states.Update(responseContent.Content);
            }
            else if (content.Action == ActionDefinition.Guess)
            {
                Game game = await GetGameByIdAsync(states.Group, client);
                if (game != null)
                {
                    GuessRecord guessRecord;
                    if (content.Content == game.TargetWord)
                    {
                        guessRecord = new GuessRecord
                        {
                            Score = new List<int>(Enumerable.Repeat(1, game.TargetWord.Length)),
                            Word = content.Content.ToLower(),
                            Winner = true
                        };
                    }
                    else
                    {
                        var score = new List<int>(game.TargetWord.Length);
                        for (int i = 0; i < game.TargetWord.Length; i++)
                        {
                            var targetLetter = game.TargetWord[i];
                            var letterGuess = content.Content.ToLower()[i];
                            if (letterGuess == targetLetter)
                            {
                                score.Add(1); // Green
                            }
                            else if (game.TargetWord.Contains(letterGuess))
                            {
                                score.Add(2); // Yellow
                            }
                            else
                            {
                                score.Add(0); // Nothing
                            }
                        }

                        guessRecord = new GuessRecord
                        {
                            Score = score,
                            Word = content.Content.ToLower()
                        };
                    }

                    game.Guesses.Add(guessRecord);
                    if (guessRecord.Winner) game.Status = GameStatus.Finished;

                    // Upsert updated guess array + Status if changed
                    var upsertResponse = await client.UpsertDocumentAsync(collectionUri, game);

                    // Return just the current guess to the caller 
                    responseContent = new ResponseContent
                    {
                        Content = JsonConvert.SerializeObject(guessRecord),
                        Action = ActionDefinition.Guess
                    };

                    // If two players in this game send other player your guess
                    if (game.Players.Count > 1)
                    {
                        var other = game.Players.Where(q => q != connectionContext.UserId).FirstOrDefault();
                        await actions.AddAsync(new SendToUserAction
                        {
                            UserId = other,
                            Data = BinaryData.FromString(JsonConvert.SerializeObject(new ResponseContent
                            {
                                Content = JsonConvert.SerializeObject(new GuessRecord
                                {
                                    Score = guessRecord.Score,
                                    Word = null,
                                    Winner = guessRecord.Winner
                                }),
                                Action = ActionDefinition.Guess
                            })),
                            DataType = WebPubSubDataType.Json
                        });
                    }
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

        private static async Task<ResponseContent> CreateGameAsync(WebPubSubConnectionContext creator, DocumentClient service)
        {
            string randomName = Guid.NewGuid().ToString().Replace("-", "")[..10];
            string randomWord = await GetRandomWordAsync();

            var documentResponse = await service.CreateDocumentAsync(collectionUri, new Game
            {
                Name = randomName,
                TargetWord = randomWord,
                Guesses = new List<GuessRecord>(),
                Players = new List<string> { creator.UserId }
            });

            return new ResponseContent
            {
                Action = ActionDefinition.Join,
                Content = randomName
            };
        }

        private static async Task<string> GetRandomWordAsync(int wordDifficulty = 5)
        {
            var url = $"https://random-word-api.herokuapp.com/word?length={wordDifficulty}";
            var client = new HttpClient();
            var response = await client.GetAsync(url);
            var words = await response.Content.ReadAsAsync<List<string>>();
            return words.FirstOrDefault();
        }
    }
}
