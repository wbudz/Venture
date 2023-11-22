using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Budziszewski.Venture.Events
{
    public class Purchase : Event
    {
        public decimal Price { get; protected set; } = 0;

        public Purchase(Assets.Asset parentAsset, Data.Transaction tr) : base(parentAsset)
        {
            if (tr.TransactionType != Data.TransactionType.Buy) throw new ArgumentException("An attempt was made to create Purchase event with transaction type other than Buy.");

            Direction = PaymentDirection.Inflow;
            TransactionIndex = tr.Index;
            Timestamp = tr.SettlementDate;
            FXRate = tr.FXRate;
            Price = tr.Price;
            Count = tr.Count;
            Amount = tr.Amount;
        }
    }
}
