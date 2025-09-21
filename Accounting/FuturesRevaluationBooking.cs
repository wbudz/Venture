using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Venture
{
    public static class FuturesRevaluationBooking
    {
        public static void Process(FuturesRevaluationEvent fr)
        {
            foreach (var book in Common.Books)
            {
                PortfolioDefinition? portfolio = Definitions.Portfolios.Single(x => x.UniqueId == fr.ParentAsset.PortfolioId);

                /// <summary>
                /// Cash account
                /// </summary>
                var accountCashSettlement = book.GetAccount(AccountType.Assets, AssetType.Cash, portfolio, fr.Currency);

                /// <summary>
                /// Result account for net gain
                /// </summary>
                var accountRealizedIncomeRecognition = book.GetAccount(AccountType.RealizedIncome, AssetType.Futures, portfolio, fr.Currency);

                /// <summary>
                /// Result account for net loss
                /// </summary>
                var accountRealizedExpenseRecognition = book.GetAccount(AccountType.RealizedExpense, AssetType.Futures, portfolio, fr.Currency);

                string description = $"Futures revaluation of {fr.ParentAsset.InstrumentId} ";

                if (fr.Amount >= 0)
                {
                    book.Enqueue(accountCashSettlement, fr.Timestamp, fr.TransactionIndex, description + "(income)", fr.Amount);
                    book.Enqueue(accountRealizedIncomeRecognition, fr.Timestamp, fr.TransactionIndex, description + "(income)", -fr.Amount);
                }
                else
                {
                    book.Enqueue(accountCashSettlement, fr.Timestamp, fr.TransactionIndex, description + "(expense)", fr.Amount);
                    book.Enqueue(accountRealizedExpenseRecognition, fr.Timestamp, fr.TransactionIndex, description + "(expense)", -fr.Amount);
                }

                book.Commit();
            }
        }
    }
}
