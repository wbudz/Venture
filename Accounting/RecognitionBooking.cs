using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Venture
{
    public static class RecognitionBooking
    {
        public static void Process(BuyTransactionDefinition btd)
        {
            foreach (var book in new Book[] { Common.MainBook })
            {
                PortfolioDefinition? portfolio = Definitions.Portfolios.Single(x => x.UniqueId == btd.PortfolioDst);

                /// <summary>
                /// Assets account for recognition of purchased (created) asset
                /// </summary>
                var accountAssetRecognition = book.GetAccount(AccountType.Assets, btd.AssetType, portfolio, btd.Currency);

                /// <summary>
                /// Cash account from which payments would be made
                /// </summary>
                var accountCashSettlement = book.GetAccount(AccountType.Assets, AssetType.Cash, portfolio, btd.Currency);

                /// <summary>
                /// Cost account for fee recognition
                /// </summary>
                var accountFeeCost = book.GetAccount(AccountType.Fees, btd.AssetType, portfolio, btd.Currency);

                string description = $"Asset purchase of {btd.AssetId} ";

                book.Enqueue(accountAssetRecognition, btd.Timestamp, btd.Index, description + "(asset recognition)", btd.Amount);
                book.Enqueue(accountCashSettlement, btd.Timestamp, btd.Index, description + "(purchase amount payment)", -btd.Amount);
                book.Enqueue(accountCashSettlement, btd.Timestamp, btd.Index, description + "(fee payment)", -btd.Fee);
                book.Enqueue(accountFeeCost, btd.Timestamp, btd.Index, description + "(fee cost recognition)", btd.Fee);

                book.Commit();
            }
        }
    }
}
