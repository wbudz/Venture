using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Budziszewski.Venture.Events
{
    public class Sale: Event
    {
        public decimal Price { get; protected set; } = 0;

        public Sale(Assets.Asset parentAsset, Data.Transaction tr, decimal amount) : base(parentAsset)
        {
            ParentAsset = parentAsset;

            Direction = PaymentDirection.Outflow;
            TransactionIndex = tr.Index;
            Timestamp = tr.SettlementDate;
            FXRate = tr.FXRate;
            Price = tr.Price;
            Count = tr.Count;
            Amount = amount;
        }
    }
}
