using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Venture
{
    public interface IFilterable
    {
        public string PortfolioId { get; }

        public string Broker { get; }

        public bool Filter(string portfolio, string broker)
        {
            if (portfolio == "*")
            {
                // fall-through
            }
            else if (portfolio.EndsWith("_*"))
            {
                string portfolioName = portfolio.Substring(0, portfolio.IndexOf("_*"));
                if (!PortfolioId.StartsWith(portfolioName)) return false;
            }
            else
            {
                if (PortfolioId != portfolio) return false;
            }

            if (broker == "*")
            {
                // fall-through
            }
            else
            {
                if (Broker != broker) return false;
            }

            return true;
        }
    }
}
