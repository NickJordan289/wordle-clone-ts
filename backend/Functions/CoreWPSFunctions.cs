using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.WebPubSub;
using Microsoft.Azure.WebPubSub.Common;
using System;
using System.Threading.Tasks;
using WordleMultiplayer.Models;

namespace WordleMultiplayer.Functions
{
    public static class CoreWPSFunctions
    {
        [FunctionName("login")]
        public static WebPubSubConnection GetClientConnection(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req,
            [WebPubSubConnection(UserId = "{query.userid}", Hub = "%WebPubSubHub%")] WebPubSubConnection connection)
        {
            Console.WriteLine("login");
            return connection;
        }

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

        [FunctionName("disconnect")]
        [return: WebPubSub(Hub = "%WebPubSubHub%")]
        public static WebPubSubAction Disconnect(
    [WebPubSubTrigger("%WebPubSubHub%", WebPubSubEventType.System, "disconnected")] WebPubSubConnectionContext connectionContext)
        {
            Console.WriteLine("Disconnect.");
            return new SendToAllAction
            {
                Data = BinaryData.FromString(new ClientContent($"{connectionContext.UserId} disconnect.").ToString()),
                DataType = WebPubSubDataType.Text
            };
        }
    }
}
