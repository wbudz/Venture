using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace Venture
{
    public class FuturesDerecognitionEvent : FuturesTransactionEvent
    {

        public FuturesDerecognitionEvent(Futures parentAsset, TransactionDefinition td, decimal count) : base(parentAsset, td.Timestamp)
        {
            UniqueId = $"FuturesDerecognition{parentAsset.UniqueId}_{td.Index}_{td.Timestamp.ToString("yyyyMMdd")}";
            TransactionIndex = td.Index;

            Count = ((td is SellTransactionDefinition) ? -1 : 1) * count;
            Price = td.Price;
            Fee = td.Fee;

            Amount = 0; // -Fee;?
            FXRate = td.FXRate;
        }
    }
}
