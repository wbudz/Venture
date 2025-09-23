using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Venture.Modules
{
    public class AssetsViewEntry: ModuleEntry
    {
        public string AssetType { get; set; } = "";

        public string CashAccount { get; set; } = "";

        public string CustodyAccount { get; set; } = "";

        public string ValuationClass { get; set; } = "";

        public string InstrumentId { get; set; } = "";

        public DateTime RecognitionDate { get; set; } = DateTime.MinValue;

        public decimal Count { get; set; } = 0;

        public decimal NominalAmount { get; set; } = 0;

        public decimal PurchaseAmount { get; set; } = 0;

        public decimal AmortizedCostValue { get; set; } = 0;

        public decimal MarketValue { get; set; } = 0;

        public decimal AccruedInterest { get; set; } = 0;

        public decimal BookValue { get; set; } = 0;

        public ObservableCollection<RecognitionEvent> Purchases { get; set; } = new ObservableCollection<RecognitionEvent>();

        public ObservableCollection<DerecognitionEvent> Sales { get; set; } = new ObservableCollection<DerecognitionEvent>();

        public ObservableCollection<FlowEvent> Flows { get; set; } = new ObservableCollection<FlowEvent>();

        public ObservableCollection<PaymentEvent> Payments { get; set; } = new ObservableCollection<PaymentEvent>();

        public ObservableCollection<ValuationEvent> Valuations { get; set; } = new ObservableCollection<ValuationEvent>();

        public double YieldToMaturity { get; set; } = 0;

        public AssetsViewEntry(Asset asset, DateTime date)
        {
            TimeArg time = new TimeArg(TimeArgDirection.End, date);
            var events = asset.GetEventsUntil(time);

            UniqueId = asset.UniqueId;
            AssetType = asset.AssetType.ToString();
            PortfolioId = asset.PortfolioId;
            Broker = asset.Broker;
            CashAccount = asset.CashAccount;
            CustodyAccount = asset.CustodyAccount;
            Currency = asset.Currency;
            ValuationClass = Common.ValuationClassToString(asset.ValuationClass);
            InstrumentId = asset is Security ? ((Security)asset).SecurityDefinition.AssetId : "";
            RecognitionDate = asset.GetPurchaseDate();
            Count = asset.GetCount(time);
            NominalAmount = asset.GetNominalAmount(time);
            PurchaseAmount = asset.GetPurchaseAmount(time, true, false);
            AmortizedCostValue = asset.GetAmortizedCostValue(time, true);
            MarketValue = asset.GetMarketValue(time, true);
            AccruedInterest = asset.GetInterestAmount(time);
            BookValue = asset.GetValue(time);
            // Events
            Purchases = new(events.OfType<RecognitionEvent>());
            Sales = new(events.OfType<DerecognitionEvent>());
            Flows = new(events.OfType<FlowEvent>());
            Payments = new(events.OfType<PaymentEvent>());
            Valuations = new(events.OfType<ValuationEvent>());
            // Bond args
            if (asset is Bond b)
            {
                YieldToMaturity = b.GetYieldToMaturity(date) * 100;
            }
        }
    }
}
