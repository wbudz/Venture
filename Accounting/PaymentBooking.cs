using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Venture
{
    public static class PaymentBooking
    {
        public static void Process(PayTransactionDefinition ptd)
        {
            foreach (var book in Common.Books)
            {
                PortfolioDefinition? srcPortfolio = Definitions.Portfolios.SingleOrDefault(x => x.UniqueId == ptd.PortfolioSrc);
                PortfolioDefinition? dstPortfolio = Definitions.Portfolios.SingleOrDefault(x => x.UniqueId == ptd.PortfolioDst);

                if (srcPortfolio == null && dstPortfolio == null) throw new Exception("Both portfolios are null in payment transaction.");

                if (srcPortfolio != null)
                {
                    var accountCashSettlement = book.GetAccount(AccountType.Assets, AssetType.Cash, srcPortfolio, ptd.Currency);
                    book.Enqueue(accountCashSettlement, ptd.SettlementDate, ptd.Index, "Cash payment (outflow)", -ptd.Amount);

                    if (dstPortfolio == null)
                    {
                        AccountType type = AccountType.ShareCapital;
                        string description = "Share capital decrease";
                        switch (ptd.PaymentType)
                        {
                            case PaymentType.ShareCapital: type = AccountType.ShareCapital; description = "Share capital decrease"; break;
                            //case PaymentType.OtherCapital: type = AccountType.OtherCapital; description = "Other capital decrease"; break;
                            case PaymentType.Tax: type = AccountType.TaxLiabilities; description = "Tax liabilities payment"; break;
                            //case PaymentType.Receivables: type = AccountType.OtherLiabilities; description = "Other liabilities payment"; break;
                            default: break;
                        }
                        var account2 = book.GetAccount(type, null, srcPortfolio, ptd.Currency);
                        book.Enqueue(account2, ptd.SettlementDate, ptd.Index, description, ptd.Amount);
                    }
                }

                if (dstPortfolio != null)
                {
                    var accountCashSettlement = book.GetAccount(AccountType.Assets, AssetType.Cash, dstPortfolio, ptd.Currency);
                    book.Enqueue(accountCashSettlement, ptd.SettlementDate, ptd.Index, "Cash payment (inflow)", ptd.Amount);

                    if (srcPortfolio == null)
                    {
                        AccountType type = AccountType.ShareCapital;
                        string description = "Share capital increase";
                        switch (ptd.PaymentType)
                        {
                            case PaymentType.ShareCapital: type = AccountType.ShareCapital; description = "Share capital increase"; break;
                            //case PaymentType.OtherCapital: type = AccountType.OtherCapital; description = "Other capital increase"; break;
                            case PaymentType.Tax: type = AccountType.Tax; description = "Tax benefit"; break;
                            //case PaymentType.Receivables: type = AccountType.OtherReceivables; description = "Other receivables payment"; break;
                            default: break;
                        }
                        var account2 = book.GetAccount(type, null, dstPortfolio, ptd.Currency);
                        book.Enqueue(account2, ptd.SettlementDate, ptd.Index, description, -ptd.Amount);
                    }
                }

                book.Commit();
            }

        }
    }
}
