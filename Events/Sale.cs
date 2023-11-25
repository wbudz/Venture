using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Budziszewski.Venture.Events
{
    public class Sale: Event
    {
        public decimal Count { get; protected set; } = 0;

        public decimal Price { get; protected set; } = 0;

        public decimal Fee { get; protected set; } = 0;

        public Sale(Assets.Asset parentAsset, Data.Transaction tr, decimal count, DateTime date) : base(parentAsset)
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
    }
}
