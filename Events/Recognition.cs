using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Venture.Data;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Venture.Events
{
    public class Recognition : Event
    {
        public decimal Count { get; protected set; } = 0;

        public decimal DirtyPrice { get; protected set; } = 0;

        public decimal CleanPrice { get; protected set; } = 0;

        public decimal Fee { get; protected set; } = 0;

        public decimal GrossAmount { get { return Amount + Fee; } }

        public Recognition(Assets.Asset parentAsset, Data.Transaction tr, DateTime date) : base(parentAsset, date)
        {
            UniqueId = $"Recognition_{parentAsset.UniqueId}_{tr.Index}_{tr.Timestamp.ToString("yyyyMMdd")}";
            if (tr.TransactionType != Data.TransactionType.Buy) throw new ArgumentException("An attempt was made to create Recognition event with transaction type other than Buy.");

            TransactionIndex = tr.Index;
            DirtyPrice = tr.Price;
            CleanPrice = tr.Price - parentAsset.GetAccruedInterest(tr.Timestamp);
            Fee = tr.Fee;
            Count = tr.Count;
            if (parentAsset.IsBond)
            {
                Amount = Common.Round(tr.Price / 100 * tr.Count * tr.NominalAmount);
            }
            else
            {
                Amount = Common.Round(tr.Price * tr.Count);
            }
            FXRate = tr.FXRate;
        }

        public Recognition(Assets.Asset parentAsset, Manual manual, decimal count, decimal price) : base(parentAsset, manual.Timestamp)
        {
            UniqueId = $"Recognition_{parentAsset.UniqueId}_MANUAL_{manual.UniqueId}_{manual.Timestamp.ToString("yyyyMMdd")}";
            switch (manual.AdjustmentType)
            {
                case ManualAdjustmentType.CouponTaxAdjustment:
                case ManualAdjustmentType.DividendTaxAdjustment:
                    throw new ArgumentException("Unexpected source for Recognition event.");
                case ManualAdjustmentType.EquitySpinOff:
                    DirtyPrice = price;
                    CleanPrice = price;
                    Count = count;
                    Amount = price * count;
                    //TODO: FXRate = tr.FXRate;
                    break;
                default:
                    throw new ArgumentException("Undefined source for Recognition event.");
            }
        }
    }
}
