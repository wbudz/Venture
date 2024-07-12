using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace Venture
{
    public class FuturesRecognitionEvent : FuturesEvent
    {
        public decimal Count { get; protected set; } = 0;

        public decimal Fee { get; protected set; } = 0;

        public FuturesRecognitionEvent(Futures parentAsset, BuyTransactionDefinition btd) : base(parentAsset, btd.Timestamp)
        {
            UniqueId = $"FuturesRecognition_{parentAsset.UniqueId}_{btd.Index}_{btd.Timestamp.ToString("yyyyMMdd")}";
            TransactionIndex = btd.Index;

            Count = btd.Count;
            Price = btd.Price;
            Fee = btd.Fee;

            Amount = 0; // -Fee;?
            FXRate = btd.FXRate;
        }

        public FuturesRecognitionEvent(Futures parentAsset, SellTransactionDefinition std) : base(parentAsset, std.Timestamp)
        {
            UniqueId = $"FuturesRecognition_{parentAsset.UniqueId}_{std.Index}_{std.Timestamp.ToString("yyyyMMdd")}";
            TransactionIndex = std.Index;

            Count = -std.Count; // negative
            Price = std.Price;
            Fee = std.Fee;

            Amount = 0; // -Fee;?
            FXRate = std.FXRate;
        }
    }
}
