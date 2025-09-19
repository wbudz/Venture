using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Venture
{
    public static class FuturesDerecognitionBooking
    {
        public static void Process(FuturesDerecognitionEvent fe)
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

                decimal purchaseFee = 0;

                var time = new TimeArg(TimeArgDirection.Start, fe.Timestamp, fe.TransactionIndex);

                if (book.ApplyTaxRules)
                {
                    /// <summary>
                    /// Reserves account which holds purchase fees for unsold assets - these may be book costs but not tax costs
                    /// </summary>
                    var accountUnrealizedFeeDerecognition = book.GetAccount(AccountType.TaxReserves, null, portfolio, fe.Currency);
                    purchaseFee = Common.Round(Math.Abs(fe.Count / fe.ParentAsset.GetCount(time)) * fe.ParentAsset.GetUnrealizedPurchaseFee(time));
                    book.Enqueue(accountUnrealizedFeeDerecognition, fe.Timestamp, fe.TransactionIndex, description + "(purchase fee deferred tax asset derecognition)", -purchaseFee);
                    book.Enqueue(accountFeeCost, fe.Timestamp, fe.TransactionIndex, description + "(purchase fee cost recognition)", purchaseFee);
                }

                book.Enqueue(accountCashSettlement, fe.Timestamp, fe.TransactionIndex, description + "(sale fee payment)", -fe.Fee);
                book.Enqueue(accountFeeCost, fe.Timestamp, fe.TransactionIndex, description + "(sale fee cost recognition)", fe.Fee);

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
