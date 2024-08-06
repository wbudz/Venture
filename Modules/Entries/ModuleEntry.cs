using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Venture.Modules
{
    public abstract class ModuleEntry : IFilterable
    {
        public string UniqueId { get; set; } = "";

        public string PortfolioId { get; protected set; } = "";

        public string Broker { get; protected set; } = "";

        public string Currency { get; set; } = "PLN";
    }
}
