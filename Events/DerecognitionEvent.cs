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
        public decimal CleanAmount { get; protected set; } = 0;

        public decimal DirtyPrice { get; protected set; } = 0;

        public decimal CleanPrice { get; protected set; } = 0;

        public decimal Fee { get; set; } = 0;

        public decimal GrossAmount { get { return Amount + Fee; } }

        public decimal PurchaseDirtyPrice { get; protected set; } = 0;

        public decimal PurchaseCleanPrice { get; protected set; } = 0;

        public decimal AmortizedCostDirtyPrice { get; protected set; } = 0;

        public decimal AmortizedCostCleanPrice { get; protected set; } = 0;

        public decimal PurchaseDirtyAmount { get; protected set; } = 0;

        public decimal PurchaseCleanAmount { get; protected set; } = 0;

        public decimal AmortizedCostDirtyAmount { get; protected set; } = 0;

        public decimal AmortizedCostCleanAmount { get; protected set; } = 0;

        public decimal OriginalPurchaseDirtyPrice { get; protected set; } = 0;

        public decimal OriginalPurchaseCleanPrice { get; protected set; } = 0;

        public decimal OriginalPurchaseDirtyAmount { get; protected set; } = 0;

        public decimal OriginalPurchaseCleanAmount { get; protected set; } = 0;

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
            FXRate = std.FXRate;

            var time = new TimeArg(TimeArgDirection.Start, std.Timestamp, std.Index);
            PurchaseDirtyPrice = parentAsset.GetPurchasePrice(true, false);
            PurchaseCleanPrice = parentAsset.GetPurchasePrice(false, false);
            OriginalPurchaseDirtyPrice = parentAsset.GetPurchasePrice(true, true);
            OriginalPurchaseCleanPrice = parentAsset.GetPurchasePrice(false, true);
            AmortizedCostDirtyPrice = parentAsset.GetAmortizedCostPrice(time, true);
            AmortizedCostCleanPrice = parentAsset.GetAmortizedCostPrice(time, false);

            if (parentAsset.IsBond)
            {
                Amount = Common.Round(std.Price / 100 * std.Count * ((Bond)parentAsset).SecurityDefinition.UnitPrice);
                CleanAmount = Common.Round(CleanPrice / 100 * std.Count * ((Bond)parentAsset).SecurityDefinition.UnitPrice);
                PurchaseDirtyAmount = Common.Round(PurchaseDirtyPrice / 100 * std.Count * ((Bond)parentAsset).SecurityDefinition.UnitPrice);
                PurchaseCleanAmount = Common.Round(PurchaseCleanPrice / 100 * std.Count * ((Bond)parentAsset).SecurityDefinition.UnitPrice);
                OriginalPurchaseDirtyAmount = Common.Round(OriginalPurchaseDirtyPrice / 100 * std.Count * ((Bond)parentAsset).SecurityDefinition.UnitPrice);
                OriginalPurchaseCleanAmount = Common.Round(OriginalPurchaseCleanPrice / 100 * std.Count * ((Bond)parentAsset).SecurityDefinition.UnitPrice);
                AmortizedCostDirtyAmount = Common.Round(AmortizedCostDirtyPrice / 100 * std.Count * ((Bond)parentAsset).SecurityDefinition.UnitPrice);
                AmortizedCostCleanAmount = Common.Round(AmortizedCostCleanPrice / 100 * std.Count * ((Bond)parentAsset).SecurityDefinition.UnitPrice);
            }
            else
            {
                Amount = Common.Round(std.Price * std.Count);
                CleanAmount = Common.Round(CleanPrice * std.Count);
                PurchaseDirtyAmount = Common.Round(PurchaseDirtyPrice * std.Count);
                PurchaseCleanAmount = Common.Round(PurchaseCleanPrice * std.Count);
                OriginalPurchaseDirtyAmount = Common.Round(OriginalPurchaseDirtyPrice * std.Count);
                OriginalPurchaseCleanAmount = Common.Round(OriginalPurchaseCleanPrice * std.Count);
                AmortizedCostDirtyAmount = Common.Round(AmortizedCostDirtyPrice * std.Count);
                AmortizedCostCleanAmount = Common.Round(AmortizedCostCleanPrice * std.Count);
            }

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
            FXRate = ttd.FXRate;

            var time = new TimeArg(TimeArgDirection.Start, ttd.Timestamp, ttd.Index);
            PurchaseDirtyPrice = parentAsset.GetPurchasePrice(true, false);
            PurchaseCleanPrice = parentAsset.GetPurchasePrice(false, false);
            OriginalPurchaseDirtyPrice = parentAsset.GetPurchasePrice(true, true);
            OriginalPurchaseCleanPrice = parentAsset.GetPurchasePrice(false, true);
            AmortizedCostDirtyPrice = parentAsset.GetAmortizedCostPrice(time, true);
            AmortizedCostCleanPrice = parentAsset.GetAmortizedCostPrice(time, false);

            if (parentAsset.IsBond)
            {
                Amount = Common.Round(ttd.Price / 100 * ttd.Count * ((Bond)parentAsset).SecurityDefinition.UnitPrice);
                PurchaseDirtyAmount = Common.Round(PurchaseDirtyPrice / 100 * ttd.Count * ((Bond)parentAsset).SecurityDefinition.UnitPrice);
                PurchaseCleanAmount = Common.Round(PurchaseCleanPrice / 100 * ttd.Count * ((Bond)parentAsset).SecurityDefinition.UnitPrice);
                OriginalPurchaseDirtyAmount = Common.Round(OriginalPurchaseDirtyPrice / 100 * ttd.Count * ((Bond)parentAsset).SecurityDefinition.UnitPrice);
                OriginalPurchaseCleanAmount = Common.Round(OriginalPurchaseCleanPrice / 100 * ttd.Count * ((Bond)parentAsset).SecurityDefinition.UnitPrice);
                AmortizedCostDirtyAmount = Common.Round(AmortizedCostDirtyPrice / 100 * ttd.Count * ((Bond)parentAsset).SecurityDefinition.UnitPrice);
                AmortizedCostCleanAmount = Common.Round(AmortizedCostCleanPrice / 100 * ttd.Count * ((Bond)parentAsset).SecurityDefinition.UnitPrice);
            }
            else
            {
                Amount = Common.Round(ttd.Price * ttd.Count);
                PurchaseDirtyAmount = Common.Round(PurchaseDirtyPrice * ttd.Count);
                PurchaseCleanAmount = Common.Round(PurchaseCleanPrice * ttd.Count);
                OriginalPurchaseDirtyAmount = Common.Round(OriginalPurchaseDirtyPrice * ttd.Count);
                OriginalPurchaseCleanAmount = Common.Round(OriginalPurchaseCleanPrice * ttd.Count);
                AmortizedCostDirtyAmount = Common.Round(AmortizedCostDirtyPrice * ttd.Count);
                AmortizedCostCleanAmount = Common.Round(AmortizedCostCleanPrice * ttd.Count);
            }

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
            PurchaseDirtyPrice = parentAsset.GetPurchasePrice(true, false);
            PurchaseCleanPrice = parentAsset.GetPurchasePrice(false, false);
            OriginalPurchaseDirtyPrice = parentAsset.GetPurchasePrice(true, true);
            OriginalPurchaseCleanPrice = parentAsset.GetPurchasePrice(false, true);
            AmortizedCostDirtyPrice = parentAsset.GetAmortizedCostPrice(time, true);
            AmortizedCostCleanPrice = parentAsset.GetAmortizedCostPrice(time, false);
            PurchaseDirtyAmount = Common.Round(PurchaseDirtyPrice * count);
            PurchaseCleanAmount = Common.Round(PurchaseCleanPrice * count);
            OriginalPurchaseDirtyAmount = Common.Round(OriginalPurchaseDirtyPrice * count);
            OriginalPurchaseCleanAmount = Common.Round(OriginalPurchaseCleanPrice * count);
            AmortizedCostDirtyAmount = Common.Round(AmortizedCostDirtyPrice * count);
            AmortizedCostCleanAmount = Common.Round(AmortizedCostCleanPrice * count);

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
            PurchaseDirtyPrice = parentAsset.GetPurchasePrice(true, false);
            PurchaseCleanPrice = parentAsset.GetPurchasePrice(false, false);
            OriginalPurchaseDirtyPrice = parentAsset.GetPurchasePrice(true, true);
            OriginalPurchaseCleanPrice = parentAsset.GetPurchasePrice(false, true);
            AmortizedCostDirtyPrice = parentAsset.GetAmortizedCostPrice(time, true);
            AmortizedCostCleanPrice = parentAsset.GetAmortizedCostPrice(time, false);
            PurchaseDirtyAmount = Common.Round(PurchaseDirtyPrice * count);
            PurchaseCleanAmount = Common.Round(PurchaseCleanPrice * count);
            OriginalPurchaseDirtyAmount = Common.Round(OriginalPurchaseDirtyPrice * count);
            OriginalPurchaseCleanAmount = Common.Round(OriginalPurchaseCleanPrice * count);
            AmortizedCostDirtyAmount = Common.Round(AmortizedCostDirtyPrice * count);
            AmortizedCostCleanAmount = Common.Round(AmortizedCostCleanPrice * count);

            IsTotal = true;
        }
    }
}
