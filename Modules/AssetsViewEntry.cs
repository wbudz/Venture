using Venture.Events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

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

        public decimal AmortizedCostValue { get; set; } = 0;

        public decimal MarketValue { get; set; } = 0;

        public decimal AccruedInterest { get; set; } = 0;

        public decimal BookValue { get; set; } = 0;

        public ObservableCollection<Events.Recognition> Purchases { get; set; } = new ObservableCollection<Events.Recognition>();

        public ObservableCollection<Events.Derecognition> Sales { get; set; } = new ObservableCollection<Events.Derecognition>();

        public ObservableCollection<Events.Flow> Flows { get; set; } = new ObservableCollection<Events.Flow>();

        public ObservableCollection<Events.Payment> Payments { get; set; } = new ObservableCollection<Events.Payment>();
    }
}
