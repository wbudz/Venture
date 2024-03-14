using Microsoft.SolverFoundation.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Venture
{
    public class DerecognitionEvent : StandardAssetEvent
    {
        public decimal Count { get; protected set; } = 0;

        public decimal DirtyPrice { get; protected set; } = 0;

        public decimal CleanPrice { get; protected set; } = 0;

        public decimal Fee { get; set; } = 0;

        public decimal GrossAmount { get { return Amount + Fee; } }

        public decimal PurchaseDirtyPrice { get; protected set; } = 0;

        public decimal PurchaseCleanPrice { get; protected set; } = 0;

        public decimal AmortizedCostDirtyPrice { get; protected set; } = 0;

        public decimal AmortizedCostCleanPrice { get; protected set; } = 0;

        public decimal Tax { get; set; } = 0;

        public bool IsTotal { get; protected set; } = false;

        public DerecognitionEvent(StandardAsset parentAsset, SellTransactionDefinition std) : base(parentAsset, std.Timestamp)
        {
            UniqueId = $"DerecognitionEvent_{parentAsset.UniqueId}_{std.Index}_{std.Timestamp.ToString("yyyyMMdd")}";
            TransactionIndex = std.Index;

            DirtyPrice = std.Price;
            CleanPrice = std.Price - parentAsset.GetAccruedInterest(std.Timestamp);
            Fee = std.Fee;
            Count = std.Count;
            if (parentAsset.IsBond)
            {
                Amount = Common.Round(std.Price / 100 * std.Count * ((Bond)parentAsset).SecurityDefinition.UnitPrice);
            }
            else
            {
                Amount = Common.Round(std.Price * std.Count);
            }
            FXRate = std.FXRate;

            var time = new TimeArg(TimeArgDirection.Start, std.Timestamp, std.Index);
            PurchaseDirtyPrice = parentAsset.GetPurchasePrice(true);
            PurchaseCleanPrice = parentAsset.GetPurchasePrice(false);
            AmortizedCostDirtyPrice = parentAsset.GetAmortizedCostPrice(time, true);
            AmortizedCostCleanPrice = parentAsset.GetAmortizedCostPrice(time, false);

            // Decide if derecognition is partial or does it apply to the whole asset
            if (std.Count >= parentAsset.GetCount(new TimeArg(TimeArgDirection.Start, std.Timestamp, std.Index)))
            {
                IsTotal = true;
            }
        }

        public DerecognitionEvent(StandardAsset parentAsset, TransferTransactionDefinition ttd) : base(parentAsset, ttd.Timestamp)
        {
            UniqueId = $"DerecognitionEvent_{parentAsset.UniqueId}_{ttd.Index}_{ttd.Timestamp.ToString("yyyyMMdd")}";
            TransactionIndex = ttd.Index;

            DirtyPrice = ttd.Price;
            CleanPrice = ttd.Price - parentAsset.GetAccruedInterest(ttd.Timestamp);
            Fee = ttd.Fee;
            Count = ttd.Count;
            if (parentAsset.IsBond)
            {
                Amount = Common.Round(ttd.Price / 100 * ttd.Count * ((Bond)parentAsset).SecurityDefinition.UnitPrice);
            }
            else
            {
                Amount = Common.Round(ttd.Price * ttd.Count);
            }
            FXRate = ttd.FXRate;

            var time = new TimeArg(TimeArgDirection.Start, ttd.Timestamp, ttd.Index);
            PurchaseDirtyPrice = parentAsset.GetPurchasePrice(true);
            PurchaseCleanPrice = parentAsset.GetPurchasePrice(false);
            AmortizedCostDirtyPrice = parentAsset.GetAmortizedCostPrice(time, true);
            AmortizedCostCleanPrice = parentAsset.GetAmortizedCostPrice(time, false);

            // Decide if derecognition is partial or does it apply to the whole asset
            if (ttd.Count >= parentAsset.GetCount(new TimeArg(TimeArgDirection.Start, ttd.Timestamp, ttd.Index)))
            {
                IsTotal = true;
            }
        }

        public DerecognitionEvent(StandardAsset parentAsset, EquitySpinOffEventDefinition manual, decimal count, decimal price) : base(parentAsset, manual.Timestamp)
        {
            UniqueId = $"DerecognitionEvent_{parentAsset.UniqueId}_EquitySpinOff_{manual.UniqueId}_{manual.Timestamp.ToString("yyyyMMdd")}";
            TransactionIndex = -1;

            DirtyPrice = price;
            CleanPrice = price;
            Fee = 0;

            Count = count;
            Amount = Common.Round(price * count);
            FXRate = FX.GetRate(manual.Timestamp, parentAsset.Currency);

            var time = new TimeArg(TimeArgDirection.Start, manual.Timestamp);
            PurchaseDirtyPrice = parentAsset.GetPurchasePrice(true);
            PurchaseCleanPrice = parentAsset.GetPurchasePrice(false);
            AmortizedCostDirtyPrice = parentAsset.GetAmortizedCostPrice(time, true);
            AmortizedCostCleanPrice = parentAsset.GetAmortizedCostPrice(time, false);

            IsTotal = (manual.OriginalInstrumentCountMultiplier == 0);
        }

        public DerecognitionEvent(StandardAsset parentAsset, EquityRedemptionEventDefinition manual, decimal count, decimal price) : base(parentAsset, manual.Timestamp)
        {
            UniqueId = $"DerecognitionEvent_{parentAsset.UniqueId}_EquityRedemption_{manual.UniqueId}_{manual.Timestamp.ToString("yyyyMMdd")}";
            TransactionIndex = -1;

            DirtyPrice = price;
            CleanPrice = price;
            Fee = 0;

            Count = count;
            Amount = Common.Round(price * count);
            FXRate = FX.GetRate(manual.Timestamp, parentAsset.Currency);

            var time = new TimeArg(TimeArgDirection.Start, manual.Timestamp);
            PurchaseDirtyPrice = parentAsset.GetPurchasePrice(true);
            PurchaseCleanPrice = parentAsset.GetPurchasePrice(false);
            AmortizedCostDirtyPrice = parentAsset.GetAmortizedCostPrice(time, true);
            AmortizedCostCleanPrice = parentAsset.GetAmortizedCostPrice(time, false);

            IsTotal = true;
        }
    }
}
