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

        public ObservableCollection<Events.Purchase> Purchases { get; set; } = new ObservableCollection<Events.Purchase>();

        public ObservableCollection<Events.Sale> Sales { get; set; } = new ObservableCollection<Events.Sale>();

        public ObservableCollection<Events.Flow> Flows { get; set; } = new ObservableCollection<Events.Flow>();

        public ObservableCollection<Events.Payment> Payments { get; set; } = new ObservableCollection<Events.Payment>();
    }
}
