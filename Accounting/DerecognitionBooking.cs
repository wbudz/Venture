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
            foreach (var book in Common.Books)
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
                /// Accounts from where unrealized FX result will be derecognized.
                /// </summary>
                //var accountUnrealizedResultOnFXDerecognition = Book.GetAccountingActivity("AssetDerecognitionOnSale", "UnrealizedResultOnFXDerecognition", inv);

                /// <summary>
                /// Accounts where realized FX result will be recognized.
                /// </summary>
                //var accountRealizedResultOnFXRecognition = Book.GetAccountingActivity("AssetDerecognitionOnSale", "RealizedResultOnFXRecognition", inv);

                decimal assetDerecognitionAmount = 0;
                decimal purchaseFee = 0;
                decimal tax = 0;

                var time = new TimeArg(TimeArgDirection.Start, std.Timestamp, std.Index);

                foreach (var e in events)
                {
                    assetDerecognitionAmount += book.ApplyTaxRules ? e.PurchaseDirtyAmount : e.AmortizedCostDirtyAmount;
                    purchaseFee += book.ApplyTaxRules ? Common.Round(e.Count / e.ParentAsset.GetCount(time) * e.ParentAsset.GetUnrealizedPurchaseFee(time)) : 0;
                    tax += e.Tax;
                }

                string description = $"Asset sale of {std.AssetId} ";

                DateTime bookingDate;
                if (book.ApplyTaxRules)
                {
                    // REALIZED PROFIT: SALE AMOUNT - PURCHASE AMOUNT

                    /// <summary>
                    /// Reserves account which holds purchase fees for unsold assets - these may be book costs but not tax costs
                    /// </summary>
                    var accountUnrealizedFeeDerecognition = book.GetAccount(AccountType.TaxReserves, null, portfolio, std.Currency);

                    /// <summary>
                    /// Account where realized gains/losses from sale (market valuation) will be booked
                    /// </summary>
                    var accountRealizedIncomeRecognition = book.GetAccount(AccountType.RealizedIncome, std.AssetType, portfolio, std.Currency);
                    var accountRealizedExpenseRecognition = book.GetAccount(AccountType.RealizedExpense, std.AssetType, portfolio, std.Currency);

                    bookingDate = std.SettlementDate;

                    book.Enqueue(accountAssetDerecognition, bookingDate, std.Index, description + "(asset derecognition)", -assetDerecognitionAmount);
                    book.Enqueue(accountCashSettlement, bookingDate, std.Index, description + "(sale amount payment)", std.Amount);
                    book.Enqueue(accountCashSettlement, bookingDate, std.Index, description + "(sale fee payment)", -std.Fee);
                    book.Enqueue(accountFeeCost, bookingDate, std.Index, description + "(sale fee cost recognition)", std.Fee);
                    book.Enqueue(accountUnrealizedFeeDerecognition, bookingDate, std.Index, description + "(purchase fee deferred tax asset derecognition)", -purchaseFee);
                    book.Enqueue(accountFeeCost, bookingDate, std.Index, description + "(purchase fee cost recognition)", purchaseFee);

                    if (std.AssetType == AssetType.MoneyMarketFund || std.AssetType == AssetType.EquityMixedFund || std.AssetType == AssetType.TreasuryBondsFund || std.AssetType == AssetType.CorporateBondsFund
                        && bookingDate <= Globals.TaxableFundSaleEndDate)
                    {
                        // Funds (pre-charged tax)      
                        var accountPrechargedTax = book.GetAccount(AccountType.PrechargedTax, std.AssetType, portfolio, std.Currency);
                        var accountNonTaxableResult = book.GetAccount(AccountType.NonTaxableResult, std.AssetType, portfolio, std.Currency);
                        book.Enqueue(accountCashSettlement, bookingDate, std.Index, description + "(pre-charged tax)", -tax);
                        book.Enqueue(accountPrechargedTax, bookingDate, std.Index, description + "(pre-charged tax)", tax);
                        book.Enqueue(accountNonTaxableResult, bookingDate, std.Index, description + "(expense)", assetDerecognitionAmount - std.Amount);
                    }
                    else
                    {
                        // Non-funds (result impacts taxable income)
                        book.Enqueue(accountRealizedIncomeRecognition, bookingDate, std.Index, description + "(income)", -std.Amount);
                        book.Enqueue(accountRealizedExpenseRecognition, bookingDate, std.Index, description + "(expense)", assetDerecognitionAmount);
                    }
                }
                else
                {
                    // REALIZED PROFIT: SALE AMOUNT - AMORTIZED DIRTY AMOUNT ON THE DAY OF SALE

                    bookingDate = std.Timestamp;

                    /// <summary>
                    /// Account where realized gains/losses from sale (market valuation) will be booked
                    /// </summary>
                    var accountRealizedProfitRecognition = book.GetAccount(AccountType.RealizedProfit, std.AssetType, portfolio, std.Currency);
                    var accountRealizedLossRecognition = book.GetAccount(AccountType.RealizedLoss, std.AssetType, portfolio, std.Currency);

                    decimal realizedResult = std.Amount - assetDerecognitionAmount;

                    book.Enqueue(accountAssetDerecognition, bookingDate, std.Index, description + "(asset derecognition)", -assetDerecognitionAmount);
                    book.Enqueue(accountCashSettlement, bookingDate, std.Index, description + "(sale amount payment)", std.Amount);
                    book.Enqueue(accountCashSettlement, bookingDate, std.Index, description + "(sale fee payment)", -std.Fee);
                    book.Enqueue(accountFeeCost, bookingDate, std.Index, description + "(sale fee cost recognition)", std.Fee);

                    if (std.AssetType == AssetType.MoneyMarketFund || std.AssetType == AssetType.EquityMixedFund || std.AssetType == AssetType.TreasuryBondsFund || std.AssetType == AssetType.CorporateBondsFund
                        && bookingDate <= Globals.TaxableFundSaleEndDate)
                    {
                        var accountPrechargedTax = book.GetAccount(AccountType.Tax, std.AssetType, portfolio, std.Currency);
                        var accountNonTaxableResult = book.GetAccount(AccountType.NonTaxableResult, std.AssetType, portfolio, std.Currency);
                        // Funds (pre-charged tax)      
                        book.Enqueue(accountCashSettlement, bookingDate, std.Index, description + "(pre-charged tax)", -tax);
                        book.Enqueue(accountPrechargedTax, bookingDate, std.Index, description + "(pre-charged tax)", tax);
                        book.Enqueue(accountNonTaxableResult, bookingDate, std.Index, description + "(expense)", assetDerecognitionAmount - std.Amount);
                    }
                    else
                    {
                        // Non-funds (result impacts taxable income)
                        if (realizedResult > 0)
                        {
                            book.Enqueue(accountRealizedProfitRecognition, bookingDate, std.Index, description + "(profit)", -realizedResult);
                        }
                        else if (realizedResult < 0)
                        {
                            book.Enqueue(accountRealizedLossRecognition, bookingDate, std.Index, description + "(loss)", -realizedResult);
                        }
                    }
                }

                book.Commit();
            }
        }
    }
}
