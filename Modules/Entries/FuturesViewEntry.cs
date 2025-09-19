using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Venture.Modules
{
    public class FuturesViewEntry : ModuleEntry
    {
        public string CashAccount { get; set; } = "";

        public string CustodyAccount { get; set; } = "";

        public string InstrumentId { get; set; } = "";

        public DateTime RecognitionDate { get; set; } = DateTime.MinValue;

        public DateTime? DerecognitionDate { get; set; } = DateTime.MinValue;

        public decimal Result { get; set; }

        public decimal Fees { get; set; }

        public ObservableCollection<FuturesEventViewEntry> Events { get; set; } = new ObservableCollection<FuturesEventViewEntry>();

        public FuturesViewEntry(Futures futures, DateTime start, DateTime end)
        {
            var events = futures.Events.OfType<FuturesEvent>().Where(x => x.Timestamp >= start && x.Timestamp <= end);

            UniqueId = futures.UniqueId;
            PortfolioId = futures.PortfolioId;
            Broker = futures.Portfolio?.Broker ?? "";
            CashAccount = futures.CashAccount;
            CustodyAccount = futures.CustodyAccount;
            Currency = futures.Currency;
            InstrumentId = futures.SecurityDefinition.AssetId;
            RecognitionDate = futures.GetPurchaseDate();
            DerecognitionDate = futures.Events.OfType<FuturesEvent>().FirstOrDefault(x => x.IsTotalDerecognition)?.Timestamp;
            Result = events.Sum(x => x.Amount);
            Fees = events.OfType<FuturesTransactionEvent>().Sum(x => x.Fee);
            // Events
            foreach (var e in events)
            {
                Events.Add(new FuturesEventViewEntry(e, futures.GetCount(new TimeArg(TimeArgDirection.Start, e.Timestamp, e.TransactionIndex)), futures.Multiplier));
            }
        }
    }
}
