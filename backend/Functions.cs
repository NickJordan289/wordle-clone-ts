using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.WebPubSub;
using Microsoft.Azure.WebPubSub.Common;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SimpleChat
{
    public static class Functions
    {
        [FunctionName("index")]
        public static IActionResult Home([HttpTrigger(AuthorizationLevel.Anonymous)] HttpRequest req, ILogger log)
        {
            string indexFile = "index.html";
            // detect Azure env.
            if (Environment.GetEnvironmentVariable("HOME") != null)
            {
                indexFile = Path.Join(Environment.GetEnvironmentVariable("HOME"), "site", "wwwroot", indexFile);
            }
            log.LogInformation($"index.html path: {indexFile}.");
            return new ContentResult
            {
                Content = File.ReadAllText(indexFile),
                ContentType = "text/html",
            };
        }

        [FunctionName("login")]
        public static WebPubSubConnection GetClientConnection(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req,
            [WebPubSubConnection(UserId = "{query.userid}", Hub = "%WebPubSubHub%")] WebPubSubConnection connection)
        {
            Console.WriteLine("login");
            return connection;
        }

        #region Work with WebPubSubTrigger
        [FunctionName("connect")]
        public static WebPubSubEventResponse Connect(
            [WebPubSubTrigger("%WebPubSubHub%", WebPubSubEventType.System, "connect")] ConnectEventRequest request)
        {
            Console.WriteLine($"Received client connect with connectionId: {request.ConnectionContext.ConnectionId}");
            if (request.ConnectionContext.UserId == "attacker")
            {
                return request.CreateErrorResponse(WebPubSubErrorCode.Unauthorized, null);
            }
            return request.CreateResponse(request.ConnectionContext.UserId, null, null, null);
        }

        // multi tasks sample
        [FunctionName("connected")]
        public static async Task Connected(
            [WebPubSubTrigger(WebPubSubEventType.System, "connected")] WebPubSubConnectionContext connectionContext,
            [WebPubSub] IAsyncCollector<WebPubSubAction> actions)
        //    [CosmosDB(
        //databaseName: "Wordle",
        //collectionName: "Users",
        //ConnectionStringSetting = "CosmosDbConnectionString")]
        //IAsyncCollector<dynamic> documentsOut)
        {
            await actions.AddAsync(new SendToAllAction
            {
                Data = BinaryData.FromString(new ClientContent($"{connectionContext.UserId} connected.").ToString()),
                DataType = WebPubSubDataType.Json
            });

            await actions.AddAsync(WebPubSubAction.CreateAddUserToGroupAction(connectionContext.UserId, "lobby"));
            await actions.AddAsync(new SendToUserAction
            {
                UserId = connectionContext.UserId,
                Data = BinaryData.FromString(new ClientContent($"{connectionContext.UserId} joined group: lobby.").ToString()),
                DataType = WebPubSubDataType.Json
            });
        }

        // single message sample
        [FunctionName("broadcast")]
        public static async Task<WebPubSubEventResponse> Broadcast(
            [WebPubSubTrigger("%WebPubSubHub%", WebPubSubEventType.User, "message")] // another way to resolve Hub name from settings.
            UserEventRequest request,
            WebPubSubConnectionContext connectionContext,
            BinaryData data,
            WebPubSubDataType dataType,
            [WebPubSub(Hub = "%WebPubSubHub%")] IAsyncCollector<WebPubSubAction> actions)
            //[CosmosDB(
            //    databaseName: "Wordle",
            //    collectionName: "Users",
            //    ConnectionStringSetting = "CosmosDBConnection")] DocumentClient client)
        {
            var response = request.CreateResponse(BinaryData.FromString(new ClientContent($"ack").ToString()), WebPubSubDataType.Json);
            var states = new GroupState("lobby");
            connectionContext.ConnectionStates.TryGetValue(nameof(GroupState), out var currentState);
            if (currentState != null)
            {
                states = currentState.ToObjectFromJson<GroupState>();
            }

            ClientContent content = JsonConvert.DeserializeObject<ClientContent>(data.ToString());
            if (content != null && (content.Content.Contains("SystemAction") || content.IsSystemAction))
            {
                if (content.SystemAction.Contains("handshake"))
                {
                    await actions.AddAsync(new SendToGroupAction
                    {
                        Data = BinaryData.FromString(new ClientContent("[System]", "", true, $"handshake:{connectionContext.UserId}").ToString()),
                        DataType = WebPubSubDataType.Json,
                        Group = states.Group
                    });
                }
                else if (content.SystemAction.Contains("sync"))
                {
                    var targetWord = content.SystemAction.Split("sync:")[1];
                    await actions.AddAsync(new SendToGroupAction
                    {
                        Data = BinaryData.FromString(new ClientContent("[System]", "", true, $"sync:{targetWord}").ToString()),
                        DataType = WebPubSubDataType.Json,
                        Group = states.Group
                    });
                }
                else if (content.Content.Contains("join") || content.Content.Contains("leave") || content.SystemAction.Contains("join") || content.SystemAction.Contains("leave"))
                {
                    var leaving = new RemoveUserFromGroupAction();
                    var joining = new AddUserToGroupAction();
                    if (content.Content.Contains("join") || content.SystemAction.Contains("join"))
                    {
                        string groupName;
                        if (content.Content.Contains("join")) 
                            groupName = content.Content.Split("join:")[1];
                        else
                            groupName = content.SystemAction.Split("join:")[1];

                        // Remove user from current group
                        leaving.Group = states.Group;
                        leaving.UserId = connectionContext.UserId;
                        // Add user to new group
                        joining.Group = groupName;
                        joining.UserId = connectionContext.UserId;

                        await actions.AddAsync(leaving);
                        await actions.AddAsync(joining);

                        // Message old group to say user has left
                        await actions.AddAsync(new SendToGroupAction
                        {
                            Data = BinaryData.FromString(new ClientContent($"{connectionContext.UserId} left group.").ToString()),
                            DataType = WebPubSubDataType.Json,
                            Group = leaving.Group
                        });

                        // Message new group to say new user has joined
                        await actions.AddAsync(new SendToGroupAction
                        {
                            Data = BinaryData.FromString(new ClientContent($"{connectionContext.UserId} joined group: {joining.Group}.").ToString()),
                            DataType = WebPubSubDataType.Json,
                            Group = joining.Group
                        });

                        // Update group state
                        states.Update(joining.Group);
                        response.SetState(nameof(GroupState), BinaryData.FromObjectAsJson(states));
                    }
                    else if ((content.Content.Contains("leave") || content.SystemAction.Contains("leave")) && states.Group != "lobby")
                    {
                        // Remove user from current group
                        leaving.Group = states.Group;
                        leaving.UserId = connectionContext.UserId;
                        // Add user to new group
                        joining.Group = "lobby";
                        joining.UserId = connectionContext.UserId;

                        await actions.AddAsync(leaving);
                        await actions.AddAsync(joining);

                        // Message old group to say user has left
                        await actions.AddAsync(new SendToGroupAction
                        {
                            Data = BinaryData.FromString(new ClientContent($"{connectionContext.UserId} left group.").ToString()),
                            DataType = WebPubSubDataType.Json,
                            Group = leaving.Group
                        });

                        // Message new group to say new user has joined
                        await actions.AddAsync(new SendToGroupAction
                        {
                            Data = BinaryData.FromString(new ClientContent($"{connectionContext.UserId} joined group: group2.").ToString()),
                            DataType = WebPubSubDataType.Json,
                            Group = joining.Group
                        });

                        // Update group state
                        states.Update(joining.Group);
                        response.SetState(nameof(GroupState), BinaryData.FromObjectAsJson(states));
                    }

                    
                }
            }
            else
            {
                await actions.AddAsync(new SendToGroupAction
                {
                    Data = request.Data,
                    DataType = request.DataType,
                    Group = states.Group
                });
            }
            return response;
        }

        [FunctionName("disconnect")]
        [return: WebPubSub(Hub = "%WebPubSubHub%")]
        public static WebPubSubAction Disconnect(
            [WebPubSubTrigger("sample_funcchat", WebPubSubEventType.System, "disconnected")] WebPubSubConnectionContext connectionContext)
        {
            Console.WriteLine("Disconnect.");
            return new SendToAllAction
            {
                Data = BinaryData.FromString(new ClientContent($"{connectionContext.UserId} disconnect.").ToString()),
                DataType = WebPubSubDataType.Text
            };
        }

        #endregion

        [JsonObject]
        private sealed class ClientContent
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

        [JsonObject]
        private sealed class GroupState
        {
            [JsonProperty("timestamp")]
            public DateTime Timestamp { get; set; }
            [JsonProperty("group")]
            public string Group { get; set; }

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
}
