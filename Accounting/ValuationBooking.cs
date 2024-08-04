using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Venture
{
    public static class ValuationBooking
    {
        public static void Process(IEnumerable<Asset> assets, AssetType assetType, PortfolioDefinition? portfolio, string currency, DateTime currentDate, DateTime previousDate)
        {
            if (portfolio == null) throw new Exception("Cannot process valuation for null portfolio.");

            foreach (var book in Common.Books)
            {
                if (book.ApplyTaxRules) continue;

                /// <summary>
                /// Asset account where change in valuation will be recognized
                /// </summary>
                var accountAssetValuation = book.GetAccount(AccountType.Assets, assetType, portfolio, currency);

                /// <summary>
                /// Account where ordinary income from amortized cost valuation will be recognized
                /// </summary>
                var accountOrdinaryIncomeRecognition = book.GetAccount(AccountType.OrdinaryIncomeValuation, assetType, portfolio, currency);

                /// <summary>
                /// Account where unrealized gains/losses from market valuation will be recognized
                /// </summary>
                var accountUnrealizedResultProfitRecognition = book.GetAccount(AccountType.OtherComprehensiveIncomeProfit, assetType, portfolio, currency);
                var accountUnrealizedResultLossRecognition = book.GetAccount(AccountType.OtherComprehensiveIncomeLoss, assetType, portfolio, currency);

                decimal currentMarketValuation = 0;
                decimal currentAmortizedCostValuation = 0;
                decimal previousMarketValuation = 0;
                decimal previousAmortizedCostValuation = 0;

                foreach (var a in assets.Where(x => x.Portfolio == portfolio && x.AssetType == assetType && x.Currency == currency))
                {
                    ValuationEvent? previousValuationEvent = a.Events.OfType<ValuationEvent>().LastOrDefault(x => x.Timestamp == previousDate);
                    ValuationEvent? currentValuationEvent = a.Events.OfType<ValuationEvent>().LastOrDefault(x => x.Timestamp == currentDate);

                    currentMarketValuation += currentValuationEvent?.CumulativeMarketValuation ?? 0;
                    currentAmortizedCostValuation += currentValuationEvent?.CumulativeAmortizedCostValuation ?? 0;
                    previousMarketValuation += previousValuationEvent?.CumulativeMarketValuation ?? 0;
                    previousAmortizedCostValuation += previousValuationEvent?.CumulativeAmortizedCostValuation ?? 0;
                }

                decimal marketProfitChange = 0;
                decimal marketLossChange = 0;
                decimal amortizedCostChange = currentAmortizedCostValuation - previousAmortizedCostValuation;

                if (currentMarketValuation > previousMarketValuation)
                {
                    marketLossChange = previousMarketValuation >= 0 ? 0 : Math.Min(-previousMarketValuation, currentMarketValuation - previousMarketValuation);
                    marketProfitChange = currentMarketValuation - previousMarketValuation - marketLossChange;
                }

                if (currentMarketValuation < previousMarketValuation)
                {
                    marketProfitChange = previousMarketValuation <= 0 ? 0 : Math.Max(-previousMarketValuation, currentMarketValuation - previousMarketValuation);
                    marketLossChange = currentMarketValuation - previousMarketValuation - marketProfitChange;
                }

                book.Enqueue(accountAssetValuation, currentDate, -1, "Market valuation (change of asset value)", currentMarketValuation - previousMarketValuation);
                book.Enqueue(accountAssetValuation, currentDate, -1, "Amortized cost valuation (change of asset value)", currentAmortizedCostValuation - previousAmortizedCostValuation);
                book.Enqueue(accountUnrealizedResultProfitRecognition, currentDate, -1, "Market valuation (unrealized gains recognition)", -marketProfitChange);
                book.Enqueue(accountUnrealizedResultLossRecognition, currentDate, -1, "Market valuation (unrealized losses recognition)", -marketLossChange);
                book.Enqueue(accountOrdinaryIncomeRecognition, currentDate, -1, "Amortized cost valuation (ordinary income recognition)", -amortizedCostChange);
                book.Commit();
            }
        }
    }
}
