using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Budziszewski.Venture.Modules
{
    public class AssetsViewEntry
    {
        public string UniqueId { get; set; } = "";

        public string AssetType { get; set; } = "";

        public string Portfolio { get; set; } = "";

        public string CashAccount { get; set; } = "";

        public string CustodyAccount { get; set; } = "";

        public string Currency { get; set; } = "PLN";

        public ValuationClass ValuationClass { get; set; } = ValuationClass.AvailableForSale;

        public decimal Count { get; set; } = 0;

        public decimal NominalAmount { get; set; } = 0;

        public decimal AmortizedCostValue { get; set; } = 0;

        public decimal MarketValue { get; set; } = 0;

        public decimal AccruedInterest { get; set; } = 0;

        public ObservableCollection<Events.Event> Events { get; set; } = new ObservableCollection<Events.Event>();
    }
}
