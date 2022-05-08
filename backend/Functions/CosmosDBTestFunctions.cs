using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using SimpleChat.Models;
using WordleMultiplayer.Models;

namespace WordleMultiplayer.Functions
{
    public static class CosmosDBTestFunctions
    {
        static Uri collectionUri = UriFactory.CreateDocumentCollectionUri("wordle", "games");

        [FunctionName("Function1")]
        public static void Run([CosmosDBTrigger(
            databaseName: "wordle",
            collectionName: "games",
            ConnectionStringSetting = "ConnectionStrings:cosmosdbConnection",
            LeaseCollectionName = "leases")]IReadOnlyList<Document> input, ILogger log)
        {
            if (input != null && input.Count > 0)
            {
                log.LogInformation("Documents modified " + input.Count);
                log.LogInformation("First document Id " + input[0].Id);
            }
        }

        [FunctionName("test")]
        public static IActionResult Test([HttpTrigger(AuthorizationLevel.Function)] HttpRequest req, ILogger log, [CosmosDB(
                databaseName: "wordle",
                collectionName: "games",
                ConnectionStringSetting = "CosmosDBConnection")] DocumentClient client)
        {
            var responseContent = new ResponseContent
            {
                Action = ActionDefinition.Guess,
                Content = "Test"
            };
            return new OkObjectResult(responseContent.ToString());
        }

        [FunctionName("GetGames")]
        public static async Task GetGamesAsync([HttpTrigger(AuthorizationLevel.Function)] HttpRequest req, ILogger log, [CosmosDB(
                databaseName: "wordle",
                collectionName: "games",
                ConnectionStringSetting = "CosmosDBConnection")] DocumentClient client)
        {
            var option = new FeedOptions { EnableCrossPartitionQuery = true };
            IDocumentQuery<Game> query = client.CreateDocumentQuery<Game>(collectionUri, option)
                .AsDocumentQuery();
            
            while (query.HasMoreResults)
            {
                foreach (Game result in await query.ExecuteNextAsync())
                {
                    log.LogInformation(result.Name);
                }
            }
        }

        [FunctionName("CreateGame")]
        public static async Task CreateGameAsync([HttpTrigger(AuthorizationLevel.Function)] HttpRequest req, ILogger log, [CosmosDB(
                databaseName: "wordle",
                collectionName: "games",
                ConnectionStringSetting = "CosmosDBConnection")] DocumentClient client)
        {
            Uri collectionUri = UriFactory.CreateDocumentCollectionUri("wordle", "games");

            var response = await client.CreateDocumentAsync(collectionUri, new Game
            {
            });

        }

        [FunctionName("GetGame")]
        public static async Task<IActionResult> GetGameAsync([HttpTrigger(AuthorizationLevel.Function, Route="GetGame/{id}")] HttpRequest req, ILogger log, [CosmosDB(
                databaseName: "wordle",
                collectionName: "games",
                ConnectionStringSetting = "CosmosDBConnection")] DocumentClient client, string id)
        {
            var option = new FeedOptions { EnableCrossPartitionQuery = true };
            IDocumentQuery<Game> query = client.CreateDocumentQuery<Game>(collectionUri, option)
                .AsDocumentQuery();

            while (query.HasMoreResults)
            {
                foreach (Game result in await query.ExecuteNextAsync())
                {
                    if (result.Name == id)
                        return new OkObjectResult(result);
                }
            }

            return new BadRequestResult();
        }
    }
}
