using Microsoft.SolverFoundation.Services;
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
            UniqueId = $"Derecognition_{parentAsset.UniqueId}_{tr.Index}_{tr.Timestamp.ToString("yyyyMMdd")}";
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

        public Derecognition(Assets.Asset parentAsset, Manual manual, decimal count, decimal price) : base(parentAsset)
        {
            UniqueId = $"Derecognition_{parentAsset.UniqueId}_MANUAL_{manual.UniqueId}_{manual.Timestamp.ToString("yyyyMMdd")}";
            switch (manual.AdjustmentType)
            {
                case ManualAdjustmentType.CouponTaxAdjustment:
                case ManualAdjustmentType.DividendTaxAdjustment:
                    throw new ArgumentException("Unexpected source for Derecognition event.");
                case ManualAdjustmentType.EquitySpinOff:
                    Timestamp = manual.Timestamp;
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
