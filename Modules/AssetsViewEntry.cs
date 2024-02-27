using Venture.Events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Venture.Assets;

namespace Venture.Modules
{
    public class AssetsViewEntry
    {
        public string UniqueId { get; set; } = "";

        public string AssetType { get; set; } = "";

        public string Portfolio { get; set; } = "";

        public string CashAccount { get; set; } = "";

        public string CustodyAccount { get; set; } = "";

        public string Currency { get; set; } = "PLN";

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

        public ObservableCollection<Events.Recognition> Purchases { get; set; } = new ObservableCollection<Events.Recognition>();

        public ObservableCollection<Events.Derecognition> Sales { get; set; } = new ObservableCollection<Events.Derecognition>();

        public ObservableCollection<Events.Flow> Flows { get; set; } = new ObservableCollection<Events.Flow>();

        public ObservableCollection<Events.Payment> Payments { get; set; } = new ObservableCollection<Events.Payment>();

        public double YieldToMaturity { get; set; } = 0;

        public AssetsViewEntry(Asset asset, DateTime date)
        {
            TimeArg time = new TimeArg(TimeArgDirection.End, date);
            var events = asset.GetEvents(time);

            UniqueId = asset.UniqueId;
            AssetType = asset.AssetType.ToString();
            Portfolio = asset.Portfolio;
            CashAccount = asset.CashAccount;
            CustodyAccount = asset.CustodyAccount;
            Currency = asset.Currency;
            ValuationClass = Common.ValuationClassToString(asset.ValuationClass);
            InstrumentId = asset is Security ? ((Security)asset).SecurityDefinition.AssetId : "";
            RecognitionDate = asset.GetPurchaseDate();
            Count = asset.GetCount(time);
            NominalAmount = asset.GetNominalAmount(time);
            PurchaseAmount = asset.GetPurchaseAmount(time, true);
            AmortizedCostValue = asset.GetAmortizedCostValue(time, true);
            MarketValue = asset.GetMarketValue(time, true);
            AccruedInterest = asset.GetInterestAmount(time);
            BookValue = asset.GetValue(time);
            // Events
            Purchases = new(events.OfType<Events.Recognition>());
            Sales = new(events.OfType<Events.Derecognition>());
            Flows = new(events.OfType<Events.Flow>());
            Payments = new(events.OfType<Events.Payment>());
            // Bond args
            if (asset is Bond b)
            {
                YieldToMaturity = b.GetYieldToMaturity(date) * 100;
            }
        }
    }
}
