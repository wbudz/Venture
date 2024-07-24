using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Venture
{
    public static class InflowBooking
    {
        public static void Process(FlowEvent e)
        {
            foreach (var book in Common.Books)
            {
                /// <summary>
                /// Asset account for cash settlement of dividend/coupon/redemption (inflow)
                /// </summary>
                var accountCashInflow = book.GetAccount(AccountType.Assets, AssetType.Cash, e.ParentAsset.Portfolio, e.ParentAsset.Currency);

                /// <summary>
                /// Result account for recognition of ordinary income.
                /// </summary>
                var accountOrdinaryIncome = book.GetAccount(AccountType.OrdinaryIncomeInflows, e.ParentAsset.AssetType, e.ParentAsset.Portfolio, e.ParentAsset.Currency);


                var accountNonTaxableResult = book.GetAccount(AccountType.NonTaxableResult, e.ParentAsset.AssetType, e.ParentAsset.Portfolio, e.ParentAsset.Currency);

                var accountPrechargedTax = book.GetAccount(AccountType.PrechargedTax, e.ParentAsset.AssetType, e.ParentAsset.Portfolio, e.ParentAsset.Currency);

                /// <summary>
                /// Tax account for recognition of income tax on dividend.
                /// </summary>
                var accountTaxRecognition = book.GetAccount(AccountType.Tax, e.ParentAsset.AssetType, e.ParentAsset.Portfolio, e.ParentAsset.Currency);

                string description = "";

                switch (e.FlowType)
                {
                    case FlowType.Dividend: description = "Dividend "; break;
                    case FlowType.Coupon: description = "Coupon "; break;
                    case FlowType.Redemption: description = "Redemption "; break;
                    default: break;
                }

                description += $"from {e.ParentAsset.InstrumentId} ";

                book.Enqueue(accountCashInflow, e.Timestamp, -1, description + "(cash inflow)", e.Amount);
                if (book.ApplyTaxRules)
                {
                    book.Enqueue(accountNonTaxableResult, e.Timestamp, -1, description + "(non-taxable result)", -e.GrossAmount);
                    book.Enqueue(accountPrechargedTax, e.Timestamp, -1, description + "(pre-charged tax)", e.Tax);
                }
                else
                {
                    book.Enqueue(accountOrdinaryIncome, e.Timestamp, -1, description + "(ordinary income)", -e.GrossAmount);
                    book.Enqueue(accountTaxRecognition, e.Timestamp, -1, description + "(tax recognition)", e.Tax);
                }
                book.Commit();
            }
        }
    }
}
