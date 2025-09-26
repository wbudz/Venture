using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Venture
{
    public static class TransferBooking
    {
        public static void Process(PortfolioTransferTransactionDefinition ttd, IEnumerable<DerecognitionEvent> events)
        {
            PortfolioDefinition? portfolioSrc = Definitions.Portfolios.Single(x => x.UniqueId == ttd.PortfolioSrc);
            PortfolioDefinition? portfolioDst = Definitions.Portfolios.Single(x => x.UniqueId == ttd.PortfolioDst);

            /// <summary>
            /// Source asset account
            /// </summary>
            var accountAssetDerecognition = Common.MainBook.GetAccount(AccountType.Assets, ttd.AssetType, portfolioSrc, ttd.Currency);

            /// <summary>
            /// Destination asset account
            /// </summary>
            var accountAssetRecognition = Common.MainBook.GetAccount(AccountType.Assets, ttd.AssetType, portfolioDst, ttd.Currency);

            /// <summary>
            /// Accounts from where unrealized FX result will be derecognized.
            /// </summary>
            //var accountUnrealizedResultOnFXDerecognition = Book.GetAccountingActivity("AssetDerecognitionOnSale", "UnrealizedResultOnFXDerecognition", inv);

            /// <summary>
            /// Accounts where realized FX result will be recognized.
            /// </summary>
            //var accountRealizedResultOnFXRecognition = Book.GetAccountingActivity("AssetDerecognitionOnSale", "RealizedResultOnFXRecognition", inv);

            string description = $"Portfolio transfer of {ttd.AssetId} ";

            decimal assetDerecognitionAmount = 0;
            foreach (var e in events)
            {
                assetDerecognitionAmount += e.AmortizedCostDirtyAmount;
            }

            /// <summary>
            /// Account where realized gains/losses accrued before transfer will be booked
            /// </summary>
            var accountRealizedProfitRecognition = Common.MainBook.GetAccount(AccountType.RealizedProfit, ttd.AssetType, portfolioSrc, ttd.Currency);
            var accountRealizedLossRecognition = Common.MainBook.GetAccount(AccountType.RealizedLoss, ttd.AssetType, portfolioSrc, ttd.Currency);

            decimal realizedResult = ttd.Amount - assetDerecognitionAmount;

            Common.MainBook.Enqueue(accountAssetDerecognition, ttd.Timestamp, ttd.Index, description + "(asset derecognition)", -assetDerecognitionAmount);
            Common.MainBook.Enqueue(accountAssetRecognition, ttd.Timestamp, ttd.Index, description + "(asset recognition)", ttd.Amount);

            if (realizedResult > 0)
            {
                Common.MainBook.Enqueue(accountRealizedProfitRecognition, ttd.Timestamp, ttd.Index, description + "(profit)", -realizedResult);
            }
            else if (realizedResult < 0)
            {
                Common.MainBook.Enqueue(accountRealizedLossRecognition, ttd.Timestamp, ttd.Index, description + "(loss)", -realizedResult);
            }

            Common.MainBook.Commit();
        }
    }
}
