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
    }
}
