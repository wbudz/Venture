using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Venture.Modules
{
    public class ClassesViewEntry : ModuleEntry
    {
        public string AssetType { get; set; } = "";

        public decimal AmortizedCostValue { get; set; } = 0;

        public decimal MarketValue { get; set; } = 0;

        public decimal AccruedInterest { get; set; } = 0;

        public decimal BookValue { get; set; } = 0;

        public decimal PercentOfSubportfolio { get; set; } = 0;

        public decimal PercentOfPortfolio { get; set; } = 0;

        public decimal PercentOfTotal { get; set; } = 0;

        public ClassesViewEntry(IEnumerable<Asset> assets, string portfolioId, string assetType, string currency, DateTime date)
        {
            TimeArg time = new TimeArg(TimeArgDirection.End, date);

            foreach (var a in assets.Where(x => x.Portfolio.UniqueId == portfolioId && x.AssetType.ToString() == assetType && x.Currency == currency))
            {
                if (!a.IsActive(new TimeArg(TimeArgDirection.End, Common.CurrentDate))) continue;

                AmortizedCostValue += a.GetAmortizedCostValue(time, true);
                MarketValue += a.GetMarketValue(time, true);
                AccruedInterest += a.GetInterestAmount(time);
                BookValue += a.GetValue(time);
            }

            UniqueId = $"{assetType}_{portfolioId}_{currency}";
            AssetType = assetType;

            PortfolioDefinition portfolio = Definitions.Portfolios.Single(x => x.UniqueId == portfolioId);
            PortfolioId = portfolio.UniqueId;
            Broker = portfolio.Broker;

            Currency = currency;

        }
    }
}
