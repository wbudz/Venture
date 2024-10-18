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
            foreach (var book in Common.Books)
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
                var accountFeeRecognition = book.ApplyTaxRules ?
                    book.GetAccount(AccountType.TaxReserves, null, portfolio, btd.Currency) :
                    book.GetAccount(AccountType.Fees, btd.AssetType, portfolio, btd.Currency);

                string description = $"Asset purchase of {btd.AssetId} ";

                DateTime bookingDate = book.ApplyTaxRules ? btd.SettlementDate : btd.Timestamp;

                book.Enqueue(accountAssetRecognition, bookingDate, btd.Index, description + "(asset recognition)", btd.Amount);
                book.Enqueue(accountCashSettlement, bookingDate, btd.Index, description + "(purchase amount payment)", -btd.Amount);
                book.Enqueue(accountCashSettlement, bookingDate, btd.Index, description + "(purchase fee payment)", -btd.Fee);
                book.Enqueue(accountFeeRecognition, bookingDate, btd.Index, description + (book.ApplyTaxRules ? "(purchase fee deferred tax asset)" : "(purchase fee cost recognition)"), btd.Fee);

                book.Commit();
            }
        }
    }
}
