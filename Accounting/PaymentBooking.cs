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
                        var accountShareCapital = book.GetAccount(AccountType.ShareCapital, null, srcPortfolio, ptd.Currency);
                        book.Enqueue(accountShareCapital, ptd.SettlementDate, ptd.Index, "Share capital decrease", ptd.Amount);
                    }
                }

                if (dstPortfolio != null)
                {
                    var accountCashSettlement = book.GetAccount(AccountType.Assets, AssetType.Cash, dstPortfolio, ptd.Currency);
                    book.Enqueue(accountCashSettlement, ptd.SettlementDate, ptd.Index, "Cash payment (inflow)", ptd.Amount);

                    if (srcPortfolio == null)
                    {
                        var accountShareCapital = book.GetAccount(AccountType.ShareCapital, null, dstPortfolio, ptd.Currency);
                        book.Enqueue(accountShareCapital, ptd.SettlementDate, ptd.Index, "Share capital increase", -ptd.Amount);
                    }
                }

                book.Commit();
            }

        }
    }
}
