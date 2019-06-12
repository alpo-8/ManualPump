using System;
using System.Collections.Generic;
using MessagePack;
using static System.Math;

namespace ManualPump
{
    [MessagePackObject(false)]
    public sealed class Orderbook
    {
        public Orderbook(string assetPairId, decimal bid, decimal ask, int depth)
        {
            AssetPairId = assetPairId;
            
            var jitter = TheRandom.Range(1,Program.Settings.books.bestPriceDeviation);
            bid *= jitter;
            ask *= jitter;
            
            for (var (i, offset) = (0, 0.001m * (ask - bid)); i < depth; offset = Max(0.001m, Min((ask - bid) * i / depth, offset * i++)))
            {
                // volume = 10 * (i * i + 1)
                Bids.Add(Level(bid - i * offset, Vol(i)));
                Asks.Add(Level(ask + i * offset, Vol(i)));
            }
        }

        private static decimal Vol(int i) 
            => TheRandom.In(10 * ((i - 1) * (i - 1) + 1), 10 * (i * i + 1));

        private static VolumePrice Level(decimal price, decimal volume) 
            => new VolumePrice(price, volume);
        
        public Orderbook() { }
        
        [Key(0)]
        public string Source = Program.Settings.books.sourceName;
        
        [Key(1)]
        public string AssetPairId { get; set; }
        
        [Key(2)]
        public DateTime Timestamp => DateTime.UtcNow;
        
        [Key(3)]
        public List<VolumePrice> Asks = new List<VolumePrice>();
        
        [Key(4)]
        public List<VolumePrice> Bids = new List<VolumePrice>();
    }
    
    [MessagePackObject(false)]
    public sealed class VolumePrice
    {
        public VolumePrice(decimal price, decimal volume)
        {
            Price = decimal.Round(price, Program.Settings.books.pricePrecision);
            Volume = Max(1.0m, decimal.Round(volume, 0));
        }
        
        public VolumePrice() { }
        
        [Key(0)]
        public decimal Volume { get; set; }
        
        [Key(1)]
        public decimal Price { get; set; }
    }
}