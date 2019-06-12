using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Flurl;
using Flurl.Http;
using Newtonsoft.Json;
using static System.Math;

namespace ManualPump
{
    public static class Program
    {
        public static Settings Settings;

        private static void Log(string rec) => Console.WriteLine($"[ {DateTime.UtcNow.ToString()} ] : {rec}");
        
        public static void Main(string[] args)
        {
            Settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(@"./input.json"));
            RabbitMqPublisher.ConnectionString = new Uri(Settings.rabbitMq.connString.AppendPathSegment("%2f"));
            
            if (!string.IsNullOrEmpty(Settings.rabbitMq.fxRatesExchange))
            {
                Log("Pre-populating FxRates");
                
                var pairs = Settings.mtSettingsService
                    .AppendPathSegment("api/assetPairs")
                    .WithHeader("api-key", "margintrading")
                    .WithHeader("Accept", "application/json")
                    .GetJsonListAsync().Result
                    .Select(x => (string) x.Id).OrderBy(x => x).ToArray();
                
                var oldFx = Settings.mtTradingCore
                    .AppendPathSegment("api/prices/bestFx")
                    .WithHeader("api-key", "margintrading")
                    .WithHeader("Content-Type", "application/json")
                    .PostJsonAsync(new { pairs }).ReceiveJson<Dictionary<string, dynamic>>().Result;
                
                var fxRates = pairs.Select(i => (id: i,
                    mid: oldFx.ContainsKey(i)
                        ? (decimal) oldFx[i].Bid
                        : TheRandom.In(Settings.defaults.bid, Settings.defaults.ask))).ToList();
                
                fxRates.ForEach(p => RabbitMqPublisher.Publish(
                    exchange: Settings.rabbitMq.fxRatesExchange,
                    message: new Orderbook(
                        assetPairId: p.id,
                        bid: p.mid,
                        ask: p.mid,
                        depth: 1)));
                
                Log("FxRates have been pre-populated");
                Log("Waiting 5 seconds after burst");
                Thread.Sleep(5000);
            }
            
            var instruments = Settings.mtSettingsService
                .AppendPathSegment("api/tradingInstruments")
                .WithHeader("api-key", "margintrading")
                .WithHeader("Accept", "application/json")
                .GetJsonListAsync().Result
                .Select(x => (string) x.Instrument).OrderBy(x => x).ToArray();
            Log($"Trading instruments discovered: {instruments.Length}");

            if (!string.IsNullOrEmpty(Settings.instrumentRegex))
                instruments = instruments.Where(x => new Regex(Settings.instrumentRegex).IsMatch(x)).ToArray();
            
            var oldQuotes = Settings.mtTradingCore
                .AppendPathSegment("api/prices/best")
                .WithHeader("api-key", "margintrading")
                .WithHeader("Content-Type", "application/json")
                .PostJsonAsync(new { instruments }).ReceiveJson<Dictionary<string, dynamic>>().Result;
            Log($"Trading quote history exists for {oldQuotes.Count} pairs");
            
            var quotes = instruments.Select(i =>
                oldQuotes.ContainsKey(i)
                    ? (id: i, bid: (decimal)oldQuotes[i].Bid, ask: (decimal)oldQuotes[i].Ask)
                    : (id: i, Settings.defaults.bid, Settings.defaults.ask)).ToList();
            Log($"Operational instruments: {quotes.Count}");
            
            if (quotes.Count.Equals(0)) return;
            
            var cycleCount = 0;
            var ramp = 0m;
            while (!(Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Enter))
            {
                var sw = Stopwatch.StartNew();
                var target = Stopwatch.Frequency / (ramp
                                 += 0.1m * Settings.publishingRate.initial *
                                    (Min(Settings.publishingRate.target, ++cycleCount) <= Floor(ramp)
                                        ? 0
                                        : cycleCount = 1));
                
                Log($"Current publishing rate: {ramp}");
                for (var i = 0; i < quotes.Count; sw.Blink(target * i++ / quotes.Count))
                    RabbitMqPublisher.Publish(
                        exchange: Settings.rabbitMq.orderBooksExchange,
                        message: new Orderbook(
                            quotes[i].id,
                            quotes[i].bid,
                            quotes[i].ask,
                            depth: Settings.books.depth
                            ));
                
                sw.Blink(target);
            }
            Console.ReadKey();
        }
        
        private static void Blink(this Stopwatch swatch, decimal ticks) =>
            Thread.Sleep(TimeSpan.FromTicks(Max(0, (int)ticks - swatch.ElapsedTicks)));
    }
}