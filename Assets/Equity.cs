using Budziszewski.Venture.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Budziszewski.Venture.Assets
{
    public class Equity : Security
    {
        public Equity(Data.Transaction tr, Data.Instrument definition) : base(tr, definition)
        {
            AddEvent(new Events.Purchase(this, tr, definition.RecognitionOnTradeDate ? tr.TradeDate : tr.SettlementDate));
            GenerateFlows();
        }

        protected override void GenerateFlows()
        {
            if (events.Count == 0) return;
            foreach (var d in Data.Definitions.Dividends)
            {
                if (this.SecurityDefinition.InstrumentId == d.InstrumentId && d.RecordDate >= events.First().Timestamp)
                {
                    TimeArg time = new TimeArg(TimeArgDirection.End, d.RecordDate);
                    decimal count = GetCount(time);
                    decimal amount = Math.Round(count * d.PaymentPerShare, 2);
                    AddEvent(new Events.Flow(this, d.PaymentDate, Venture.Events.FlowType.Dividend, amount, 1));
                }
            }
        }

        public override decimal GetCouponRate(DateTime date)
        {
            return 0;
        }

        public override decimal GetMarketPrice(TimeArg time, bool dirty)
        {
            if (!IsActive(time)) return 0;

            Data.Price? price = Data.Definitions.Prices.LastOrDefault(x => x.InstrumentId == this.SecurityDefinition.InstrumentId && x.Timestamp <= time.Date);
            if (price == null)
            {
                throw new Exception($"No price for: {UniqueId} at date: {time.Date:yyyy-MM-dd}.");
                //return GetAmortizedCostPrice(time, dirty);
            }
            else
            {
                return price.Value;
            }
        }

        public override decimal GetAmortizedCostPrice(TimeArg time, bool dirty)
        {
            return GetPurchasePrice(time, dirty);
        }

        public override decimal GetAccruedInterest(DateTime date)
        {
            return 0;
        }

        public override decimal GetNominalAmount(TimeArg time)
        {
            return Math.Round(GetPurchaseAmount(time, false), 2);
        }

        public override decimal GetNominalAmount()
        {
            var evt = Events.OfType<Events.Purchase>().FirstOrDefault();
            if (evt!=null)
            {
                return GetNominalAmount(new TimeArg(TimeArgDirection.End, evt.Timestamp, evt.TransactionIndex));
            }
            else
            {
                return 0;
            }
        }

        public override decimal GetInterestAmount(TimeArg time)
        {
            return 0;
        }

        public override decimal GetPurchaseAmount(TimeArg time, bool dirty)
        {
            return Math.Round(GetPurchasePrice(time, dirty) * GetCount(time), 2);
        }

        public override decimal GetMarketValue(TimeArg time, bool dirty)
        {
            return Math.Round(GetMarketPrice(time, dirty) * GetCount(time), 2);
        }

        public override decimal GetAmortizedCostValue(TimeArg time, bool dirty)
        {
            return Math.Round(GetAmortizedCostPrice(time, dirty) * GetCount(time), 2);
        }

    }
}
