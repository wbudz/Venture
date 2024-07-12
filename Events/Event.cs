using System;
using System.Linq;

namespace Venture
{
    public abstract class Event
    {
        public string UniqueId { get; protected set; } = $"Event_{Guid.NewGuid()}";

        public int TransactionIndex { get; protected set; } = -1;

        public DateTime Timestamp { get; set; }

        public Asset ParentAsset { get; set; }

        public decimal Amount { get; set; } = 0;

        public string Currency { get; protected set; } = "PLN";

        public decimal FXRate { get; protected set; } = 1;

        public Event(Asset parentAsset, DateTime timestamp)
        {
            ParentAsset = parentAsset;
            Timestamp = timestamp;
            Currency = parentAsset.Currency;
        }

        public override string ToString()
        {
            return UniqueId;
        }
    }

    public abstract class StandardAssetEvent: Event
    {
        public StandardAssetEvent(StandardAsset parentAsset, DateTime timestamp): base(parentAsset, timestamp)
        { 
        }

    }

    public abstract class FuturesEvent: Event
    {
        public decimal Price { get; protected set; } = 0;

        public bool IsTotalDerecognition { get; set; } = false;

        public FuturesEvent(Futures parentAsset, DateTime timestamp) : base(parentAsset, timestamp)
        {
        }

    }
}
