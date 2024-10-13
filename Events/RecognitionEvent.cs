using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace Venture
{
    public class RecognitionEvent : StandardAssetEvent
    {
        public decimal DirtyPrice { get; protected set; } = 0;

        public decimal CleanPrice { get; protected set; } = 0;

        public decimal Fee { get; protected set; } = 0;

        public decimal OriginalDirtyPrice { get; protected set; } = 0;

        public decimal OriginalCleanPrice { get; protected set; } = 0;

        public decimal OriginalFee { get; protected set; } = 0;

        public decimal GrossAmount { get { return Amount + Fee; } }

        private RecognitionEvent(StandardAsset parentAsset, TransactionDefinition tr) : base(parentAsset, tr.Timestamp)
        {
            UniqueId = $"RecognitionEvent_{parentAsset.InstrumentUniqueId}_{tr.Index}_{tr.Timestamp.ToString("yyyyMMdd")}";
            TransactionIndex = tr.Index;

            DirtyPrice = tr.Price;
            CleanPrice = tr.Price - parentAsset.GetAccruedInterest(tr.Timestamp);
            Fee = tr.Fee;

            Count = tr.Count;
            FXRate = tr.FXRate;
        }

        public RecognitionEvent(StandardAsset parentAsset, BuyTransactionDefinition btd) : this(parentAsset,(TransactionDefinition)btd)
        {
            OriginalDirtyPrice = DirtyPrice;
            OriginalCleanPrice = CleanPrice;
            OriginalFee = Fee;

            if (parentAsset.IsBond)
            {
                Amount = Common.Round(btd.Price / 100 * btd.Count * ((Bond)parentAsset).SecurityDefinition.UnitPrice);
            }
            else
            {
                Amount = Common.Round(btd.Price * btd.Count);
            }
        }

        public RecognitionEvent(StandardAsset parentAsset, TransferTransactionDefinition ttd, StandardAsset originalAsset) : this(parentAsset, (TransactionDefinition)ttd)
        {
            OriginalDirtyPrice = originalAsset.GetPurchasePrice(true);
            OriginalCleanPrice = originalAsset.GetPurchasePrice(false);
            OriginalFee = originalAsset.GetUnrealizedPurchaseFee(new TimeArg(TimeArgDirection.Start, ttd.Timestamp, ttd.Index));

            if (parentAsset.IsBond)
            {
                Amount = Common.Round(ttd.Price / 100 * ttd.Count * ((Bond)parentAsset).SecurityDefinition.UnitPrice);
            }
            else
            {
                Amount = Common.Round(ttd.Price * ttd.Count);
            }
        }

        public RecognitionEvent(StandardAsset parentAsset, EquitySpinOffEventDefinition manual, decimal count, decimal price, Asset originalAsset, bool includeFee) : base(parentAsset, manual.Timestamp)
        {
            UniqueId = $"RecognitionEvent_{parentAsset.InstrumentUniqueId}_EquitySpinOff_{manual.UniqueId}_{manual.Timestamp.ToString("yyyyMMdd")}";
            TransactionIndex = -1;

            DirtyPrice = price;
            CleanPrice = price;
            Fee = 0;

            OriginalDirtyPrice = originalAsset.GetPurchasePrice(true);
            OriginalCleanPrice = originalAsset.GetPurchasePrice(false);
            if (includeFee)
                OriginalFee = originalAsset.GetUnrealizedPurchaseFee(new TimeArg(TimeArgDirection.Start, manual.Timestamp));
            else
                OriginalFee = 0;

            Count = count;
            Amount = price * count;
            FXRate = FX.GetRate(manual.Timestamp, parentAsset.Currency);
        }
    }
}
