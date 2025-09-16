using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Venture
{
    public static class FuturesBooking
    {
        public static void Process(FuturesEvent fe)
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
                var accountFeeCost = book.GetAccount(AccountType.Fees, AssetType.Futures, portfolio, fe.Currency);

                /// <summary>
                /// Result account for net gain
                /// </summary>
                var accountRealizedIncomeRecognition = book.GetAccount(AccountType.RealizedIncome, AssetType.Futures, portfolio, fe.Currency);

                /// <summary>
                /// Result account for net loss
                /// </summary>
                var accountRealizedExpenseRecognition = book.GetAccount(AccountType.RealizedExpense, AssetType.Futures, portfolio, fe.Currency);

                string description = $"Futures transaction of {fe.ParentAsset.InstrumentId} ";

                decimal fee = 0;
                if (fe is FuturesRecognitionEvent fre)
                {
                    fee = fre.Fee;
                }

                book.Enqueue(accountCashSettlement, fe.Timestamp, fe.TransactionIndex, description + "(fee payment)", -fee);
                book.Enqueue(accountFeeCost, fe.Timestamp, fe.TransactionIndex, description + "(fee cost recognition)", fee);
                if (fe.Amount >= 0)
                {
                    book.Enqueue(accountCashSettlement, fe.Timestamp, fe.TransactionIndex, description + "(income)", fe.Amount);
                    book.Enqueue(accountRealizedIncomeRecognition, fe.Timestamp, fe.TransactionIndex, description + "(income)", -fe.Amount);
                }
                else
                {
                    book.Enqueue(accountCashSettlement, fe.Timestamp, fe.TransactionIndex, description + "(expense)", fe.Amount);
                    book.Enqueue(accountRealizedExpenseRecognition, fe.Timestamp, fe.TransactionIndex, description + "(expense)", -fe.Amount);

                }

                book.Commit();
            }
        }
    }
}
