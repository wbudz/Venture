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

                var accountNonTaxableResult = book.GetAccount(AccountType.NonTaxableResult, btd.AssetType, portfolio, btd.Currency);

                string description = $"Asset purchase of {btd.AssetId} ";
                DateTime bookingDate = btd.Timestamp;

                if (book.ApplyTaxRules)
                {
                    /// <summary>
                    /// Cost account for fee recognition
                    /// </summary>
                    var accountFeeRecognition = book.GetAccount(AccountType.TaxReserves, null, portfolio, btd.Currency);
                    bookingDate = btd.SettlementDate;

                    if (Globals.TaxFreePortfolios.Contains(portfolio.UniqueId))
                    {
                        book.Enqueue(accountNonTaxableResult, bookingDate, btd.Index, description + "(purchase fee cost recognition)", btd.Fee);
                    }
                    else
                    {
                        book.Enqueue(accountFeeRecognition, bookingDate, btd.Index, description + "(purchase fee deferred tax asset)", btd.Fee);
                    }
                }
                else
                {
                    /// <summary>
                    /// Cost account for fee recognition
                    /// </summary>
                    var accountFeeRecognition = book.GetAccount(AccountType.Fees, btd.AssetType, portfolio, btd.Currency);
                    bookingDate = btd.Timestamp;

                    book.Enqueue(accountFeeRecognition, bookingDate, btd.Index, description + "(purchase fee cost recognition)", btd.Fee);

                }

                book.Enqueue(accountAssetRecognition, bookingDate, btd.Index, description + "(asset recognition)", btd.Amount);
                book.Enqueue(accountCashSettlement, bookingDate, btd.Index, description + "(purchase amount payment)", -btd.Amount);
                book.Enqueue(accountCashSettlement, bookingDate, btd.Index, description + "(purchase fee payment)", -btd.Fee);

                book.Commit();
            }
        }
    }
}
