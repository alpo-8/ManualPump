namespace ManualPump
{
    public sealed class Settings
    {
        public string mtSettingsService { get; set; }
        public string mtTradingCore { get; set; }
        public RabbitMq rabbitMq { get; set; }
        public Books books { get; set; }
        public PubRate publishingRate { get; set; }
        public DefaultQuote defaults { get; set; }
        public string? instrumentRegex { get; set; }
    }
    
    public sealed class RabbitMq
    {
        public string connString { get; set; }
        public string orderBooksExchange { get; set; }
        public string fxRatesExchange { get; set; }
    }

    public sealed class Books
    {
        public string sourceName { get; set; }
        public int depth { get; set; }
        public int pricePrecision { get; set; }
        public decimal bestPriceDeviation { get; set; }
    }
    
    public sealed class PubRate
    {
        public decimal initial { get; set; }
        public decimal increment { get; set; }
        public decimal target { get; set; }
    }
    
    public sealed class DefaultQuote
    {
        public decimal bid { get; set; }
        public decimal ask { get; set; }
    }
}