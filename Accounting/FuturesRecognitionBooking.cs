using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Venture
{
    public static class FuturesRecognitionBooking
    {
        public static void Process(FuturesRecognitionEvent fe)
        {
            foreach (var book in Common.Books)
            {
                PortfolioDefinition? portfolio = Definitions.Portfolios.Single(x => x.UniqueId == fe.ParentAsset.PortfolioId);

                /// <summary>
                /// Cash account
                /// </summary>
                var accountCashSettlement = book.GetAccount(AccountType.Assets, AssetType.Cash, portfolio, fe.Currency);

                /// <summary>
                /// Cost account for fee recognition
                /// </summary>
                var accountFeeRecognition = book.ApplyTaxRules ?
                    book.GetAccount(AccountType.TaxReserves, null, portfolio, fe.Currency) :
                    book.GetAccount(AccountType.Fees, AssetType.Futures, portfolio, fe.Currency);

                string description = $"Futures transaction of {fe.ParentAsset.InstrumentId} ";

                book.Enqueue(accountCashSettlement, fe.Timestamp, fe.TransactionIndex, description + "(fee payment)", -fe.Fee);
                book.Enqueue(accountFeeRecognition, fe.Timestamp, fe.TransactionIndex, description + (book.ApplyTaxRules ? "(fee deferred tax asset)" : "(fee cost recognition)"), fe.Fee);

                book.Commit();
            }
        }
    }
}
