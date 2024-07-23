using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Venture
{
    public static class AmortizedValuationBooking
    {
        public static void Process(IEnumerable<Asset> assets, AssetType assetType, PortfolioDefinition? portfolio, string currency, DateTime date)
        {
            if (portfolio == null) throw new Exception("Cannot process amortized valuation for null portfolio.");

            foreach (var book in new Book[] { Common.MainBook })
            {
                /// <summary>
                /// Asset account where change in amortized cost valuation will be recognized
                /// </summary>
                var accountAssetValuation = book.GetAccount(AccountType.Assets, assetType, portfolio, currency);

                /// <summary>
                /// Account where ordinary income from amortized cost valuation will be recognized
                /// </summary>
                var accountOrdinaryIncomeRecognition = book.GetAccount(AccountType.OrdinaryIncomeValuation, assetType, portfolio, currency);

                decimal result = 0;
                decimal previousPrice = 0;
                decimal currentPrice = 0;

                foreach (var a in assets.Where(x => x.Portfolio == portfolio && x.AssetType == assetType && x.Currency == currency))
                {
                    ValuationEvent? previousValuationEvent = a.Events.OfType<ValuationEvent>().LastOrDefault(x => x.Timestamp < date);
                    ValuationEvent? currentValuationEvent = a.Events.OfType<ValuationEvent>().LastOrDefault(x => x.Timestamp == date);

                    if (currentValuationEvent != null)
                    {
                        currentPrice = currentValuationEvent.AmortizedCostDirtyPrice;
                    }
                    else
                    {
                        continue;
                    }

                    if (previousValuationEvent != null)
                    {
                        previousPrice = previousValuationEvent.AmortizedCostDirtyPrice;
                    }
                    else
                    {
                        previousPrice = a.GetPurchasePrice(true);
                    }

                    result += Common.Round(currentValuationEvent.Count * (currentPrice - previousPrice));
                }

                book.Enqueue(accountAssetValuation, date, -1, "Amortized cost valuation (change of asset value)", result);
                book.Enqueue(accountOrdinaryIncomeRecognition, date, -1, "Amortized cost valuation (ordinary income recognition)", -result);
                book.Commit();
            }
        }
    }
}
