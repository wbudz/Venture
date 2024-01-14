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

        public decimal Price { get; protected set; } = 0;

        public decimal Fee { get; protected set; } = 0;

        public Recognition(Assets.Asset parentAsset, Data.Transaction tr, DateTime date) : base(parentAsset)
        {
            UniqueId = $"recognition_{parentAsset.UniqueId}_{tr.Index}_{tr.Timestamp.ToString("yyyyMMdd")}";
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
            UniqueId = $"Derecognition_{parentAsset.UniqueId}_MANUAL_{manual.UniqueId}_{manual.Timestamp.ToString("yyyyMMdd")}";
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
