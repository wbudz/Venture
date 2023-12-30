using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Venture.Data;

namespace Venture.Events
{
    public class Derecognition: Event
    {
        public decimal Count { get; protected set; } = 0;

        public decimal Price { get; protected set; } = 0;

        public decimal Fee { get; protected set; } = 0;

        public Derecognition(Assets.Asset parentAsset, Data.Transaction tr, decimal count, DateTime date) : base(parentAsset)
        {
            ParentAsset = parentAsset;

            TransactionIndex = tr.Index;
            Timestamp = date;
            Price = tr.Price;
            Fee = tr.Fee;
            Count = count;
            if (tr.NominalAmount != 0)
            {
                Amount = tr.Price * tr.Count / tr.NominalAmount;
            }
            else
            {
                Amount = tr.Price * tr.Count;
            }
            FXRate = tr.FXRate;
        }

        public Derecognition(Assets.Asset parentAsset, Manual manual, DateTime date, decimal count, decimal price) : base(parentAsset)
        {
            switch (manual.AdjustmentType)
            {
                case ManualAdjustmentType.CouponTaxAdjustment:
                case ManualAdjustmentType.DividendTaxAdjustment:
                    throw new ArgumentException("Unexpected source for Derecognition event.");
                case ManualAdjustmentType.EquitySpinOff:
                    Timestamp = date;
                    Price = price;
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
