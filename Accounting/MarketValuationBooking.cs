using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Venture
{
    public static class MarketValuationBooking
    {
        public static void Process(IEnumerable<Asset> assets, AssetType assetType, PortfolioDefinition? portfolio, string currency, DateTime date)
        {
            if (portfolio == null) throw new Exception("Cannot process market valuation for null portfolio.");

            foreach (var book in new Book[] { Common.MainBook })
            {
                /// <summary>
                /// Asset account where change in market valuation will be recognized
                /// </summary>
                var accountAssetValuation = book.GetAccount(AccountType.Assets, assetType, portfolio, currency);

                /// <summary>
                /// Account where unrealized gains/losses from market valuation will be recognized
                /// </summary>
                var accountUnrealizedResultProfitRecognition = book.GetAccount(AccountType.OtherComprehensiveIncomeProfit, assetType, portfolio, currency);
                var accountUnrealizedResultLossRecognition = book.GetAccount(AccountType.OtherComprehensiveIncomeLoss, assetType, portfolio, currency);

                decimal currentValuation = 0;
                decimal previousValuation = 0;

                foreach (var a in assets.Where(x => x.Portfolio == portfolio && x.AssetType == assetType && x.Currency == currency))
                {
                    ValuationEvent? previousValuationEvent = a.Events.OfType<ValuationEvent>().LastOrDefault(x => x.Timestamp < date);
                    ValuationEvent? currentValuationEvent = a.Events.OfType<ValuationEvent>().LastOrDefault(x => x.Timestamp == date);

                    decimal assetValuationIncomePrevious = previousValuationEvent?.Amount ?? 0;
                    decimal assetValuationIncomeCurrent = currentValuationEvent?.Amount ?? 0;
                    currentValuation += assetValuationIncomeCurrent;
                    previousValuation += assetValuationIncomePrevious;
                }

                decimal profitChange = 0;
                decimal lossChange = 0;

                if (currentValuation > previousValuation)
                {
                    lossChange = previousValuation >= 0 ? 0 : Math.Min(-previousValuation, currentValuation - previousValuation);
                    profitChange = currentValuation - previousValuation - lossChange;
                }

                if (currentValuation < previousValuation)
                {
                    profitChange = previousValuation <= 0 ? 0 : Math.Max(-previousValuation, currentValuation - previousValuation);
                    lossChange = currentValuation - previousValuation - profitChange;
                }

                book.Enqueue(accountAssetValuation, date, -1, "Market valuation (change of asset value)", currentValuation - previousValuation);
                book.Enqueue(accountUnrealizedResultProfitRecognition, date, -1, "Market valuation (unrealized gains recognition)", -profitChange);
                book.Enqueue(accountUnrealizedResultLossRecognition, date, -1, "Market valuation (unrealized losses recognition)", -lossChange);
                book.Commit();
            }
        }
    }
}
