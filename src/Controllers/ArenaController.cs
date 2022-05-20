using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Dapr;
using Dapr.Client;
using PusherServer;

using toc_arena.Models;

namespace toc_arena.Controllers
{
    [ApiController]
    [Route("/")]
    public class ArenaController : ControllerBase
    {
        private readonly ILogger<ArenaController> _logger;
        private readonly Pusher _pusher;
        private const string GAME_DATA_KEY = "GAME_DATA_KEY";

        public ArenaController(ILogger<ArenaController> logger)
        {
            _logger = logger;

            var options = new PusherOptions
            {
                Cluster = "eu",
                Encrypted = true
            };

            _pusher = new Pusher(
                "1411223",
                "2d282419724d865daded",
                "a63df2f9310f999f45e7",
                options
            );

        }

        [Topic("pubsub", "battle")]
        [HttpPost("punches")]
        async public void Punches([FromBody] CloudEvent<PunchData> message)
        {
            _logger.LogInformation("Receiving message...");

            // Get dpr client
            var client = new DaprClientBuilder().Build();

            // Get game data
            GameData gdata = await RetrieveGameData(client);

            // Get current valyes
            int punches = gdata.Standings.GetValueOrDefault(message.Data.Champion);
            if (gdata.Running)
            {
                // Update data
                _logger.LogInformation($"Going to add {message.Data.Punches} punches to champion {(ChampionEnum)(message.Data.Champion)}!");
                gdata.Standings[message.Data.Champion] = punches + message.Data.Punches;

                // Save state and broadcast
                await SaveGameData(gdata, client);
                await BroadcastGameData(gdata);
            }
        }

        [HttpPost("start")]
        async public void Start()
        {
            // Get dpr client
            var client = new DaprClientBuilder().Build();

            _logger.LogInformation($"Going to reset and start new game!");

            // Get client info from state
            GameData data = await RetrieveGameData(client);

            // Reset data
            data.Running = true;
            data.Standings = new Dictionary<ChampionEnum, int>();

            // Save state
            await SaveGameData(data, client);
        }

        [HttpPost("stop")]
        async public Task<GameData> Stop()
        {
            // Get dpr client
            var client = new DaprClientBuilder().Build();

            // Get client info from state
            GameData data =  await RetrieveGameData(client);

            // Update data and save state back
            data.Running = false;
            await SaveGameData(data, client);

            _logger.LogInformation($"Stopping game!");
            LogGameData(data);

            return data;
        }

        [HttpGet("totals")]
        async public Task<GameData> Totals()
        {
            // Get client info from state
            var data =  await RetrieveGameData();
            LogGameData(data);

            return data;
        }

        private void LogGameData(GameData data)
        { 
            _logger.LogInformation($"Running: {data.Running}");
            foreach (var item in data.Standings)
            {
                _logger.LogInformation($"{item.Key}: {item.Value} points");
            }
        }

        static async private Task<GameData> RetrieveGameData(DaprClient client = null)
        {
            if (client == null)
            { 
                client = new DaprClientBuilder().Build();
            }

            GameData data = await client.GetStateAsync<GameData>("statestore", GAME_DATA_KEY);
            if (data == null)
            {
                data = new GameData() { 
                    Running = false,
                    Standings = new Dictionary<ChampionEnum, int>()
                };
            }
            return data;
        }

        static async private Task SaveGameData(GameData data, DaprClient client = null)
        {
            if (client == null)
            { 
                client = new DaprClientBuilder().Build();
            }
            await client.SaveStateAsync<GameData>("statestore", GAME_DATA_KEY, data);
        }

        async private Task BroadcastGameData(GameData data)
        { 
            var result = await _pusher.TriggerAsync("arena", "gameData", data);
            if (result.StatusCode != System.Net.HttpStatusCode.OK)
            {
                _logger.LogError($"Error broadcasting game data: {result.ToString()}");
            }
        }
    }
}
