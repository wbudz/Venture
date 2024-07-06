using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Venture
{

    public class PortfolioDefinition : Definition
    {
        public string UniqueId { get { return $"{PortfolioName}_{SubportfolioName}"; } }

        public string PortfolioName { get; private set; } = "";

        public string SubportfolioName { get; private set; } = "";

        public string CashAccount { get; private set; } = "";

        public string CustodyAccount { get; private set; } = "";

        public string Broker { get; private set; } = "";

        public PortfolioDefinition(Dictionary<string, string> data) : base(data)
        {
            PortfolioName = data["portfolio"];
            SubportfolioName = data["subportfolio"];
            CashAccount = data["cashaccount"];
            CustodyAccount = data["custodyaccount"];
            Broker = data["broker"];
        }

        public override string ToString()
        {
            return $"Portfolio: {PortfolioName}_{SubportfolioName}";
        }
    }
}
