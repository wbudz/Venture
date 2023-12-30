using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Venture.Data;

namespace Venture.Events
{
    public class Recognition : Event
    {
        public decimal Count { get; protected set; } = 0;

        public decimal Price { get; protected set; } = 0;

        public decimal Fee { get; protected set; } = 0;

        public Recognition(Assets.Asset parentAsset, Data.Transaction tr, DateTime date) : base(parentAsset)
        {
            if (tr.TransactionType != Data.TransactionType.Buy) throw new ArgumentException("An attempt was made to create Recognition event with transaction type other than Buy.");

            TransactionIndex = tr.Index;
            Timestamp = date;
            Price = tr.Price;
            Fee = tr.Fee;
            Count = tr.Count;
            if (parentAsset.IsBond)
            {
                Amount = Math.Round(tr.Price / 100 * tr.Count * tr.NominalAmount, 2);
            }
            else
            {
                Amount = Math.Round(tr.Price * tr.Count, 2);
            }
            FXRate = tr.FXRate;
        }

        public Recognition(Assets.Asset parentAsset, Manual manual, decimal count, decimal price) : base(parentAsset)
        {
            switch (manual.AdjustmentType)
            {
                case ManualAdjustmentType.CouponTaxAdjustment:
                case ManualAdjustmentType.DividendTaxAdjustment:
                    throw new ArgumentException("Unexpected source for Recognition event.");
                case ManualAdjustmentType.EquitySpinOff:
                    Timestamp = manual.Timestamp;
                    Price = price;
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
