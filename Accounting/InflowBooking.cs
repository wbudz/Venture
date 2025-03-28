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

                var accountAsset = book.GetAccount(AccountType.Assets, e.ParentAsset.AssetType, e.ParentAsset.Portfolio, e.ParentAsset.Currency);

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

                if (e.FlowType == FlowType.Redemption)
                {
                    TimeArg time = new(TimeArgDirection.Start, e.Timestamp);

                    var nominalAmount = e.ParentAsset.GetNominalAmount(time);
                    var purchaseAmount = e.ParentAsset.GetPurchaseAmount(time, true);
                    if (book.ApplyTaxRules)
                    {
                        /// <summary>
                        /// Cost account for fee recognition
                        /// </summary>
                        var accountFeeCost = book.GetAccount(AccountType.Fees, e.ParentAsset.AssetType, e.ParentAsset.Portfolio, e.ParentAsset.Currency);

                        /// <summary>
                        /// Reserves account which holds purchase fees for unsold assets - these may be book costs but not tax costs
                        /// </summary>
                        var accountUnrealizedFeeDerecognition = book.GetAccount(AccountType.TaxReserves, null, e.ParentAsset.Portfolio, e.ParentAsset.Currency);

                        var purchaseFee = Common.Round(e.Count / e.ParentAsset.GetCount(time) * e.ParentAsset.GetUnrealizedPurchaseFee(time));

                        book.Enqueue(accountCashInflow, e.Timestamp, -1, description + "(cash inflow)", e.Amount);
                        book.Enqueue(accountAsset, e.Timestamp, -1, description + "(asset derecognition)", -purchaseAmount);
                        book.Enqueue(accountNonTaxableResult, e.Timestamp, -1, description + "(non-taxable result)", -(e.GrossAmount - purchaseAmount));
                        book.Enqueue(accountPrechargedTax, e.Timestamp, -1, description + "(pre-charged tax)", e.Tax);
                        book.Enqueue(accountUnrealizedFeeDerecognition, e.Timestamp, -1, description + "(purchase fee deferred tax asset derecognition)", -purchaseFee);
                        book.Enqueue(accountFeeCost, e.Timestamp, -1, description + "(purchase fee cost recognition)", purchaseFee);
                    }
                    else
                    {
                        book.Enqueue(accountCashInflow, e.Timestamp, -1, description + "(cash inflow)", e.Amount);
                        book.Enqueue(accountAsset, e.Timestamp, -1, description + "(asset derecognition)", -nominalAmount);
                        book.Enqueue(accountOrdinaryIncome, e.Timestamp, -1, description + "(ordinary income)", nominalAmount - e.Amount - e.Tax);
                        book.Enqueue(accountTaxRecognition, e.Timestamp, -1, description + "(tax recognition)", e.Tax);
                    }
                }
                else
                {
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
                }
                book.Commit();
            }
        }
    }
}
