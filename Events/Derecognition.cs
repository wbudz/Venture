using Microsoft.SolverFoundation.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Venture.Data;

namespace Venture.Events
{
    public class Derecognition : Event
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

        public Derecognition(Assets.Asset parentAsset, Data.Transaction tr, decimal count, decimal fee, DateTime date) : base(parentAsset, date)
        {
            UniqueId = $"Derecognition_{parentAsset.UniqueId}_{tr.Index}_{tr.Timestamp.ToString("yyyyMMdd")}";
            ParentAsset = parentAsset;

            TransactionIndex = tr.Index;
            DirtyPrice = tr.Price;
            CleanPrice = tr.Price - parentAsset.GetAccruedInterest(tr.Timestamp);
            Fee = fee;
            Count = count;
            if (parentAsset.IsBond)
            {
                Amount = Common.Round(tr.Price / 100 * count * tr.NominalAmount);
            }
            else
            {
                Amount = Common.Round(tr.Price * count);
            }
            FXRate = tr.FXRate;

            var time = new TimeArg(TimeArgDirection.Start, tr.Timestamp, tr.Index);
            PurchaseDirtyPrice = parentAsset.GetPurchasePrice(true);
            PurchaseCleanPrice = parentAsset.GetPurchasePrice(false);
            AmortizedCostDirtyPrice = parentAsset.GetAmortizedCostPrice(time, true);
            AmortizedCostCleanPrice = parentAsset.GetAmortizedCostPrice(time, false);

            // Calculate income

            //decimal originalCount = parentAsset.GetCount(time);
            //decimal purchaseAmount = parentAsset.GetPurchaseAmount(time, true);
            //IncomeVsPurchasePrice = Common.Round((Amount - purchaseAmount) * Count / originalCount);
            //decimal amortizedCostAmount = parentAsset.GetAmortizedCostValue(time, true);
            //IncomeVsAmortizedCost = Common.Round((Amount - amortizedCostAmount) * Count / originalCount);

            // Calculate tax, if applicable

            //if (parentAsset.IsFund && Timestamp <= Globals.TaxableFundSaleEndDate)
            //{
            //    Tax = TaxCalculations.CalculateFromIncome(IncomeVsPurchasePrice);
            //}
            //else
            //{
            //    Tax = 0;
            //}

            // Decide if derecognition is partial or does it apply to the whole asset

            if (count >= parentAsset.GetCount(new TimeArg(TimeArgDirection.Start, tr.Timestamp, tr.Index)))
            {
                IsTotal = true;
            }
        }

        public Derecognition(Assets.Asset parentAsset, Manual manual, decimal count, decimal price) : base(parentAsset, manual.Timestamp)
        {
            UniqueId = $"Derecognition_{parentAsset.UniqueId}_MANUAL_{manual.UniqueId}_{manual.Timestamp.ToString("yyyyMMdd")}";
            switch (manual.AdjustmentType)
            {
                case ManualAdjustmentType.CouponTaxAdjustment:
                case ManualAdjustmentType.DividendTaxAdjustment:
                    throw new ArgumentException("Unexpected source for Derecognition event.");
                case ManualAdjustmentType.EquitySpinOff:
                    DirtyPrice = price;
                    CleanPrice = price;
                    Count = count;
                    Amount = price * count;
                    //TODO: FXRate = tr.FXRate;
                    break;
                default:
                    throw new ArgumentException("Undefined source for Derecognition event.");
            }
        }
    }
}
