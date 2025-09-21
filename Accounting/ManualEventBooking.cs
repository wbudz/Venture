using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Venture
{
    public static class ManualEventBooking
    {
        public static void Process(AdditionalPremiumEventDefinition ape)
        {
            foreach (var book in Common.Books)
            {
                PortfolioDefinition? portfolio = Definitions.Portfolios.SingleOrDefault(x => x.UniqueId == ape.Portfolio);

                var accountCashSettlement = book.GetAccount(AccountType.Assets, AssetType.Cash, portfolio, ape.Currency);
                var accountIncome = book.GetAccount(AccountType.OrdinaryIncomeInflows, AssetType.Cash, portfolio, ape.Currency);
                var accountNonTaxableResult = book.GetAccount(AccountType.NonTaxableResult, AssetType.Cash, portfolio, ape.Currency);

                string description = ape.Description;

                book.Enqueue(accountCashSettlement, ape.Timestamp, -1, "Additional premium " + description, ape.Amount);
                if (book.ApplyTaxRules && !ape.IncomeTaxable)
                {
                    book.Enqueue(accountNonTaxableResult, ape.Timestamp, -1, "Additional premium " + description, -ape.Amount);
                }
                else
                {
                    book.Enqueue(accountIncome, ape.Timestamp, -1, "Additional premium " + description, -ape.Amount);
                }

                book.Commit();
            }
        }

        public static void Process(AdditionalChargeEventDefinition ace)
        {
            foreach (var book in Common.Books)
            {
                PortfolioDefinition? portfolio = Definitions.Portfolios.SingleOrDefault(x => x.UniqueId == ace.Portfolio);

                var accountCashSettlement = book.GetAccount(AccountType.Assets, AssetType.Cash, portfolio, ace.Currency);
                var accountExpense = book.GetAccount(AccountType.Fees, AssetType.Cash, portfolio, ace.Currency);
                var accountNonTaxableResult = book.GetAccount(AccountType.NonTaxableResult, AssetType.Cash, portfolio, ace.Currency);

                string description = ace.Description;

                book.Enqueue(accountCashSettlement, ace.Timestamp, -1, "Additional charge " + description, -ace.Amount);
                if (book.ApplyTaxRules && !ace.IncomeTaxable)
                {
                    book.Enqueue(accountNonTaxableResult, ace.Timestamp, -1, "Additional charge " + description, ace.Amount);
                }
                else
                {
                    book.Enqueue(accountExpense, ace.Timestamp, -1, "Additional charge " + description, ace.Amount);
                }

                book.Commit();
            }
        }

        public static void Process(EquityRedemptionEventDefinition ere, DerecognitionEvent de)
        {
            foreach (var book in Common.Books)
            {
                PortfolioDefinition? portfolio = Definitions.Portfolios.SingleOrDefault(x => x.UniqueId == de.ParentAsset.PortfolioId);

                /// <summary>
                /// Asset account from which the asset will be derecognized upon sale / liquidation
                /// </summary>
                var accountAssetDerecognition = book.GetAccount(AccountType.Assets, de.ParentAsset.AssetType, portfolio, de.Currency);

                /// <summary>
                /// Cash account to which payment would be made
                /// </summary>
                var accountCashSettlement = book.GetAccount(AccountType.Assets, AssetType.Cash, portfolio, de.Currency);

                /// <summary>
                /// Accounts from where unrealized FX result will be derecognized.
                /// </summary>
                //var accountUnrealizedResultOnFXDerecognition = Book.GetAccountingActivity("AssetDerecognitionOnSale", "UnrealizedResultOnFXDerecognition", inv);

                /// <summary>
                /// Accounts where realized FX result will be recognized.
                /// </summary>
                //var accountRealizedResultOnFXRecognition = Book.GetAccountingActivity("AssetDerecognitionOnSale", "RealizedResultOnFXRecognition", inv);

                var time = new TimeArg(TimeArgDirection.Start, de.Timestamp);

                decimal assetDerecognitionAmount = book.ApplyTaxRules ? de.PurchaseDirtyAmount : de.AmortizedCostDirtyAmount;
                decimal purchaseFee = book.ApplyTaxRules ? Common.Round(de.Count / de.ParentAsset.GetCount(time) * de.ParentAsset.GetUnrealizedPurchaseFee(time)) : 0;
                decimal tax = de.Tax;

                string description = $"Equity redemption of {de.ParentAsset.InstrumentId} ";

                if (book.ApplyTaxRules)
                {
                    // REALIZED PROFIT: SALE AMOUNT - PURCHASE AMOUNT

                    /// <summary>
                    /// Reserves account which holds purchase fees for unsold assets - these may be book costs but not tax costs
                    /// </summary>
                    var accountUnrealizedFeeDerecognition = book.GetAccount(AccountType.TaxReserves, null, portfolio, de.Currency);

                    /// <summary>
                    /// Cost account for fee recognition
                    /// </summary>
                    var accountFeeCost = book.GetAccount(AccountType.Fees, de.ParentAsset.AssetType, portfolio, de.Currency);

                    /// <summary>
                    /// Account where realized gains/losses from sale (market valuation) will be booked
                    /// </summary>
                    var accountRealizedIncomeRecognition = book.GetAccount(AccountType.RealizedIncome, de.ParentAsset.AssetType, portfolio, de.Currency);
                    var accountRealizedExpenseRecognition = book.GetAccount(AccountType.RealizedExpense, de.ParentAsset.AssetType, portfolio, de.Currency);

                    book.Enqueue(accountAssetDerecognition, de.Timestamp, -1, description + "(asset derecognition)", -assetDerecognitionAmount);
                    book.Enqueue(accountCashSettlement, de.Timestamp, -1, description + "(redemption payment)", de.Amount);
                    book.Enqueue(accountUnrealizedFeeDerecognition, de.Timestamp, -1, description + "(purchase fee deferred tax asset derecognition)", -purchaseFee);

                    // Always non-taxable
                    var accountPrechargedTax = book.GetAccount(AccountType.PrechargedTax, de.ParentAsset.AssetType, portfolio, de.Currency);
                    var accountNonTaxableResult = book.GetAccount(AccountType.NonTaxableResult, de.ParentAsset.AssetType, portfolio, de.Currency);
                    book.Enqueue(accountCashSettlement, de.Timestamp, -1, description + "(pre-charged tax)", -tax);
                    book.Enqueue(accountPrechargedTax, de.Timestamp, -1, description + "(pre-charged tax)", tax);
                    book.Enqueue(accountNonTaxableResult, de.Timestamp, -1, description + "(expense)", assetDerecognitionAmount - de.Amount);

                    book.Enqueue(accountNonTaxableResult, de.Timestamp, -1, description + "(purchase fee)", purchaseFee);
                }
                else
                {
                    // REALIZED PROFIT: SALE AMOUNT - AMORTIZED DIRTY AMOUNT ON THE DAY OF SALE

                    /// <summary>
                    /// Account where realized gains/losses from sale (market valuation) will be booked
                    /// </summary>
                    var accountRealizedProfitRecognition = book.GetAccount(AccountType.RealizedProfit, de.ParentAsset.AssetType, portfolio, de.Currency);
                    var accountRealizedLossRecognition = book.GetAccount(AccountType.RealizedLoss, de.ParentAsset.AssetType, portfolio, de.Currency);

                    decimal realizedResult = de.Amount - assetDerecognitionAmount;

                    book.Enqueue(accountAssetDerecognition, de.Timestamp, -1, description + "(asset derecognition)", -assetDerecognitionAmount);
                    book.Enqueue(accountCashSettlement, de.Timestamp, -1, description + "(redemption payment)", de.Amount);

                    // Always non-taxable
                    var accountPrechargedTax = book.GetAccount(AccountType.Tax, de.ParentAsset.AssetType, portfolio, de.Currency);
                    var accountNonTaxableResult = book.GetAccount(AccountType.NonTaxableResult, de.ParentAsset.AssetType, portfolio, de.Currency);
                    // Funds (pre-charged tax)      
                    book.Enqueue(accountCashSettlement, de.Timestamp, -1, description + "(pre-charged tax)", -tax);
                    book.Enqueue(accountPrechargedTax, de.Timestamp, -1, description + "(pre-charged tax)", tax);
                    book.Enqueue(accountNonTaxableResult, de.Timestamp, -1, description + "(expense)", assetDerecognitionAmount - de.Amount);
                }

                book.Commit();
            }
        }

        public static void Process(AdditionalTaxableIncomeEventDefinition atie)
        {
            PortfolioDefinition? portfolio = Definitions.Portfolios.SingleOrDefault(x => x.UniqueId == atie.Portfolio);

            object? at;
            AssetType assetType;
            Enum.TryParse(typeof(AssetType), atie.AssetType, out at);
            if (at!=null)
            {
                assetType = (AssetType)at;
            }
            else
            {
                assetType = AssetType.Cash;
            }

                var accountIncome = Common.TaxBook.GetAccount(AccountType.RealizedIncome, assetType, portfolio, Globals.LocalCurrency);
            var accountNonTaxableResult = Common.TaxBook.GetAccount(AccountType.NonTaxableResult, assetType, portfolio, Globals.LocalCurrency);

            string description = atie.Description;

            Common.TaxBook.Enqueue(accountNonTaxableResult, atie.Timestamp, -1, "Additional taxable income: " + description, atie.Amount);
            Common.TaxBook.Enqueue(accountIncome, atie.Timestamp, -1, "Additional taxable income: " + description, -atie.Amount);

            Common.TaxBook.Commit();
        }

        public static void Process(AdditionalTaxableExpenseEventDefinition atee)
        {
            PortfolioDefinition? portfolio = Definitions.Portfolios.SingleOrDefault(x => x.UniqueId == atee.Portfolio);

            object? at;
            AssetType assetType;
            Enum.TryParse(typeof(AssetType), atee.AssetType, out at);
            if (at != null)
            {
                assetType = (AssetType)at;
            }
            else
            {
                assetType = AssetType.Cash;
            }

            var accountExpense = Common.TaxBook.GetAccount(AccountType.RealizedExpense, assetType, portfolio, Globals.LocalCurrency);
            var accountNonTaxableResult = Common.TaxBook.GetAccount(AccountType.NonTaxableResult, assetType, portfolio, Globals.LocalCurrency);

            string description = atee.Description;

            Common.TaxBook.Enqueue(accountNonTaxableResult, atee.Timestamp, -1, "Additional taxable expense: " + description, -atee.Amount);
            Common.TaxBook.Enqueue(accountExpense, atee.Timestamp, -1, "Additional taxable expense: " + description, atee.Amount);

            Common.TaxBook.Commit();
        }
    }
}
