using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Venture
{
    public static class DerecognitionBooking
    {
        public static void Process(SellTransactionDefinition std, IEnumerable<DerecognitionEvent> events)
        {
            foreach (var book in new Book[] { Common.MainBook })
            {
                PortfolioDefinition? portfolio = Definitions.Portfolios.Single(x => x.UniqueId == std.PortfolioSrc);

                /// <summary>
                /// Asset account from which the asset will be derecognized upon sale / liquidation
                /// </summary>
                var accountAssetDerecognition = book.GetAccount(AccountType.Assets, std.AssetType, portfolio, std.Currency);

                /// <summary>
                /// Cash account to which payment would be made
                /// </summary>
                var accountCashSettlement = book.GetAccount(AccountType.Assets, AssetType.Cash, portfolio, std.Currency);

                /// <summary>
                /// Cost account for fee recognition
                /// </summary>
                var accountFeeCost = book.GetAccount(AccountType.Fees, std.AssetType, portfolio, std.Currency);

                /// <summary>
                /// Reserves account which holds purchase fees for unsold assets - these may be book costs but not tax costs
                /// </summary>
                //var accountUnrealizedFeeDerecognition = Book.GetAccountingActivity("AssetDerecognitionOnSale", "UnrealizedFeeDerecognition", inv);

                /// <summary>
                /// Account where realized gains/losses from sale (market valuation) will be booked
                /// </summary>
                var accountRealizedProfitRecognition = book.GetAccount(AccountType.RealizedProfit, std.AssetType, portfolio, std.Currency);
                var accountRealizedLossRecognition = book.GetAccount(AccountType.RealizedLoss, std.AssetType, portfolio, std.Currency);

                /// <summary>
                /// Accounts from where unrealized FX result will be derecognized.
                /// </summary>
                //var accountUnrealizedResultOnFXDerecognition = Book.GetAccountingActivity("AssetDerecognitionOnSale", "UnrealizedResultOnFXDerecognition", inv);

                /// <summary>
                /// Accounts where realized FX result will be recognized.
                /// </summary>
                //var accountRealizedResultOnFXRecognition = Book.GetAccountingActivity("AssetDerecognitionOnSale", "RealizedResultOnFXRecognition", inv);

                decimal assetDerecognitionAmount = 0;
                decimal realizedResult = 0;

                foreach (var e in events)
                {
                    assetDerecognitionAmount += e.AmortizedCostDirtyAmount;
                }

                realizedResult = std.Amount - assetDerecognitionAmount;

                book.Enqueue(accountAssetDerecognition, std.Timestamp, std.Index, "Asset sale (asset derecognition)", -assetDerecognitionAmount);
                if (realizedResult > 0)
                {
                    book.Enqueue(accountRealizedProfitRecognition, std.Timestamp, std.Index, "Asset sale (profit)", -realizedResult);
                }
                else if (realizedResult < 0)
                {
                    book.Enqueue(accountRealizedLossRecognition, std.Timestamp, std.Index, "Asset sale (loss)", -realizedResult);
                }
                book.Enqueue(accountCashSettlement, std.Timestamp, std.Index, "Asset sale (sale amount payment)", std.Amount);
                book.Enqueue(accountCashSettlement, std.Timestamp, std.Index, "Asset purchase (fee payment)", -std.Fee);
                book.Enqueue(accountFeeCost, std.Timestamp, std.Index, "Asset purchase (fee cost recognition)", std.Fee);

                book.Commit();
            }
        }
    }
}
