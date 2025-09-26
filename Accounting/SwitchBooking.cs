using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Venture
{
    public static class SwitchBooking
    {
        public static void Process(AssetSwitchTransactionDefinition wtd, IEnumerable<DerecognitionEvent> events)
        {
            PortfolioDefinition? portfolioSrc = Definitions.Portfolios.Single(x => x.UniqueId == wtd.PortfolioSrc);
            PortfolioDefinition? portfolioDst = Definitions.Portfolios.Single(x => x.UniqueId == wtd.PortfolioDst);

            /// <summary>
            /// Source asset account
            /// </summary>
            var accountAssetDerecognition = Common.MainBook.GetAccount(AccountType.Assets, wtd.AssetType, portfolioSrc, wtd.Currency);

            /// <summary>
            /// Destination asset account
            /// </summary>
            var accountAssetRecognition = Common.MainBook.GetAccount(AccountType.Assets, wtd.AssetTypeTarget, portfolioDst, wtd.Currency);

            /// <summary>
            /// Accounts from where unrealized FX result will be derecognized.
            /// </summary>
            //var accountUnrealizedResultOnFXDerecognition = Book.GetAccountingActivity("AssetDerecognitionOnSale", "UnrealizedResultOnFXDerecognition", inv);

            /// <summary>
            /// Accounts where realized FX result will be recognized.
            /// </summary>
            //var accountRealizedResultOnFXRecognition = Book.GetAccountingActivity("AssetDerecognitionOnSale", "RealizedResultOnFXRecognition", inv);

            string description = $"Asset switch of {wtd.AssetId} to {wtd.AssetIdTarget} ";

            decimal assetDerecognitionAmount = 0;
            foreach (var e in events)
            {
                assetDerecognitionAmount += e.AmortizedCostDirtyAmount;
            }

            /// <summary>
            /// Account where realized gains/losses accrued before transfer will be booked
            /// </summary>
            var accountRealizedProfitRecognition = Common.MainBook.GetAccount(AccountType.RealizedProfit, wtd.AssetType, portfolioSrc, wtd.Currency);
            var accountRealizedLossRecognition = Common.MainBook.GetAccount(AccountType.RealizedLoss, wtd.AssetType, portfolioSrc, wtd.Currency);

            decimal realizedResult = wtd.Amount - assetDerecognitionAmount;

            Common.MainBook.Enqueue(accountAssetDerecognition, wtd.Timestamp, wtd.Index, description + "(asset derecognition)", -assetDerecognitionAmount);
            Common.MainBook.Enqueue(accountAssetRecognition, wtd.Timestamp, wtd.Index, description + "(asset recognition)", wtd.Amount);

            if (realizedResult > 0)
            {
                Common.MainBook.Enqueue(accountRealizedProfitRecognition, wtd.Timestamp, wtd.Index, description + "(profit)", -realizedResult);
            }
            else if (realizedResult < 0)
            {
                Common.MainBook.Enqueue(accountRealizedLossRecognition, wtd.Timestamp, wtd.Index, description + "(loss)", -realizedResult);
            }

            Common.MainBook.Commit();
        }
    }
}
