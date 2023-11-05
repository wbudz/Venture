using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Budziszewski.Venture.Modules
{
    public class AssetsViewEntry
    {
        public string UniqueId { get; set; } = "";

        public string Portfolio { get; set; } = "";

        //public string CustodyAccount { get; set; } = "";

        public string CashAccount { get; set; } = "";

        public string Currency { get; set; } = "PLN";

        public ValuationClass ValuationClass { get; set; } = ValuationClass.AvailableForSale;
    }
}
