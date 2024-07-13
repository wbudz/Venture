using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Venture.Modules
{
    public abstract class ModuleEntry
    {
        public string UniqueId { get; set; } = "";

        public string PortfolioId { get; set; } = "";

        public string Broker { get; set; } = "";

        public string Currency { get; set; } = "PLN";
    }
}
