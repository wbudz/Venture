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
    }
}
