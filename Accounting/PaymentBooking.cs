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
            //foreach (var book in new Book[] { Common.MainBook })
            //{
            //    var portfolioGroups = events.GroupBy(x => x.ParentAsset.Portfolio);

            //    foreach (var p in portfolioGroups)
            //    {
            //        PortfolioDefinition portfolio = Booking.GetPortfolio(p);
            //        string currency = Booking.GetCurrency(p);
            //        decimal amount = Booking.GetAmount(p);
            //        DateTime date = Booking.GetDate(p);
            //        int transactionIndex = Booking.GetTransactionIndex(p);
            //        PaymentType paymentType = Booking.GetPaymentType(p);

            //        var accountCashSettlement = book.GetAccount(AccountType.Assets, AssetType.Cash, portfolio, currency);
            //        string description = amount >= 0 ? "Cash payment (inflow)" : "Cash payment (outflow)";
            //        book.Enqueue(accountCashSettlement, date, transactionIndex, description, portfolio, amount);
            //        if (paymentType == PaymentType.ShareCapital)
            //        {
            //            var accountShareCapital = book.GetAccount(AccountType.ShareCapital, null, portfolio, currency);
            //            description = amount >= 0 ? "Share capital increase" : "Share capital decrease";
            //            book.Enqueue(accountShareCapital, date, transactionIndex, description, portfolio, -amount);
            //        }
            //    }                

            //    book.Commit();
            //}

            foreach (var book in new Book[] { Common.MainBook })
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
