using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Venture.Modules
{
    public partial class Module : UserControl
    {
        public Module()
        {
            DataContext = this;
        }     
        
        public static bool Filter(ModuleEntry entry, string portfolio, string broker)
        {
            if (portfolio == "*")
            {
                // fall-through
            }
            else if (portfolio.EndsWith("_*"))
            {
                string portfolioName = portfolio.Substring(0, portfolio.IndexOf("_*"));
                if (!entry.PortfolioId.StartsWith(portfolioName)) return false;
            }
            else
            {
                if (entry.PortfolioId != portfolio) return false;
            }

            if (broker == "*")
            {
                // fall-through
            }
            else
            {
                if (entry.Broker != broker) return false;
            }

            return true;
        }

        public static bool Filter(Asset asset, string portfolio, string broker)
        {
            if (portfolio == "*")
            {
                // fall-through
            }
            else if (portfolio.EndsWith("_*"))
            {
                string portfolioName = portfolio.Substring(0, portfolio.IndexOf("_*"));
                if (!asset.PortfolioId.StartsWith(portfolioName)) return false;
            }
            else
            {
                if (asset.PortfolioId != portfolio) return false;
            }

            if (broker == "*")
            {
                // fall-through
            }
            else
            {
                if (asset.Broker != broker) return false;
            }

            return true;
        }
    }
}
