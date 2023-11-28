using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Venture.Events
{
    public class Purchase : Event
    {
        public decimal Count { get; protected set; } = 0;

        public decimal Price { get; protected set; } = 0;

        public decimal Fee { get; protected set; } = 0;

        public Purchase(Assets.Asset parentAsset, Data.Transaction tr, DateTime date) : base(parentAsset)
        {
            if (tr.TransactionType != Data.TransactionType.Buy) throw new ArgumentException("An attempt was made to create Purchase event with transaction type other than Buy.");

            TransactionIndex = tr.Index;
            Timestamp = date;
            Price = tr.Price;
            Fee = tr.Fee;
            Count = tr.Count;
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
