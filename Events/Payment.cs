using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Budziszewski.Venture.Events
{
    public class Payment: Event
    {
        public decimal Amount { get; protected set; }

        public decimal FXRate { get; protected set; } = 1;

        public Payment(Assets.Asset parentAsset, Data.Transaction tr) : base(parentAsset)
        {
            if (tr.TransactionType != Data.TransactionType.Cash) throw new ArgumentException("An attempt was made to create Payment event with transaction type other than Cash.");

            Timestamp = tr.SettlementDate;
            Amount = tr.NominalAmount;
            FXRate = tr.FXRate;
        }

        public Payment(Assets.Asset parentAsset, Events.Flow fl) : base(parentAsset)
        {
            Timestamp = fl.Timestamp;
            Amount = fl.Amount;
            FXRate = fl.FXRate;
        }

        public override string ToString()
        {
            return $"Event:Payment @{Timestamp:yyyy-MM-dd}, amount: {Amount:0.00}";
        }
    }
}
