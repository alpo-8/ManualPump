namespace ManualPump
{
    public abstract class Settings
    {
        public string mtSettingsService { get; set; }
        public string mtTradingCore { get; set; }
        public RabbitMq rabbitMq { get; set; }
        public Books books { get; set; }
        public PubRate publishingRate { get; set; }
        public DefaultQuote defaults { get; set; }
        public string instrumentRegex { get; set; }
    }
    
    public abstract class RabbitMq
    {
        public string connString { get; set; }
        public string orderBooksExchange { get; set; }
        public string fxRatesExchange { get; set; }
    }

    public abstract class Books
    {
        public string sourceName { get; set; }
        public int depth { get; set; }
        public int pricePrecision { get; set; }
        public decimal bestPriceDeviation { get; set; }
    }
    
    public abstract class PubRate
    {
        public int initial { get; set; }
        public int target { get; set; }
    }
    
    public abstract class DefaultQuote
    {
        public decimal bid { get; set; }
        public decimal ask { get; set; }
    }
}