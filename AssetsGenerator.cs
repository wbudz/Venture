using System;
using System.Collections.Generic;
using System.Linq;

namespace Venture
{
    public static class AssetsGenerator
    {
        public static List<Asset> GenerateAssets()
        {
            Common.Books.ForEach(x => x.Clear());
            List<Asset> output = new List<Asset>();
            Queue<TransactionDefinition> transactions = new Queue<TransactionDefinition>(Definitions.Transactions);
            HashSet<ManualEventDefinition> manuals = new HashSet<ManualEventDefinition>(Definitions.ManualEvents.OfType<NonModifyingManualEventDefinition>());

            if (transactions.Count() == 0) return output;

            DateTime currentDate = new DateTime(transactions.Peek().Timestamp.Year - 1, 12, 31);
            Queue<DateTime> reportingDates = new Queue<DateTime>(Financial.Calendar.GenerateReportingDates(currentDate, Common.EndDate, Financial.Calendar.TimeStep.Monthly).ToList());

            while (transactions.Count > 0)
            {
                TransactionDefinition tr = transactions.Dequeue();
                if (tr.Amount == 0 && tr.AssetType != AssetType.Futures) throw new Exception($"Transaction with amount equal to 0 encountered: {tr}.");

                // Go through pending events that come before (influence) current transaction - e.g. dividends, coupons that may add new cash    
                // Also, Process manual entries that influence assets
                ProcessPrecedingEvents(output, manuals, tr, currentDate);

                // Go through preceding ends of months
                while (reportingDates.Peek() < tr.Timestamp)
                {
                    ProcessEndOfPeriod(output, reportingDates.Dequeue());
                }

                // Process the transaction
                if (tr is PayTransactionDefinition ptd)
                {
                    // Register cash addition
                    if (!String.IsNullOrEmpty(ptd.AccountDst))
                    {
                        var cash = new Cash(ptd);
                        AddAsset(output, cash, tr.Timestamp);
                    }
                    // Register cash deduction
                    if (!String.IsNullOrEmpty(ptd.AccountSrc))
                    {
                        RegisterCashDeduction(output, ptd);
                    }
                    PaymentBooking.Process(ptd);
                }
                else
                {
                    InstrumentDefinition definition;
                    try
                    {
                        definition = Definitions.Instruments.Single(x => x.UniqueId == tr.InstrumentUniqueId);
                    }
                    catch
                    {
                        throw new Exception($"Transaction {tr} definition pointed to unknown instrument id: {tr.InstrumentUniqueId}.");
                    }

                    if (tr is TransferTransactionDefinition ttd)
                    {
                        if (definition.AssetType == AssetType.Futures)
                        {
                            ProcessFuturesTransaction(output, definition, ttd);
                        }
                        else
                        {
                            var sales = RegisterSale(output, definition, ttd);
                            foreach (var s in sales)
                            {
                                var mtr = TransactionDefinition.CreateModifiedTransaction<TransferTransactionDefinition>(ttd, s.Count, s.DirtyPrice, s.Fee);
                                Asset asset = Asset.CreateFromTransferTransaction(mtr, (Security)s.ParentAsset);
                                AddAsset(output, asset, mtr.Timestamp);
                            }
                            TransferBooking.Process(ttd, sales);
                        }
                    }
                    if (tr is BuyTransactionDefinition btd)
                    {
                        if (definition.AssetType == AssetType.Futures)
                        {
                            ProcessFuturesTransaction(output, definition, btd);
                        }
                        else
                        {
                            Asset asset = Asset.CreateFromBuyTransaction(btd, definition);
                            AddAsset(output, asset, btd.Timestamp);
                            var purchase = asset.Events.OfType<RecognitionEvent>().First();
                            // Subtract cash used for purchase
                            RegisterCashDeduction(output, btd, purchase);
                            RecognitionBooking.Process(btd);
                        }
                    }
                    if (tr is SellTransactionDefinition std)
                    {
                        if (definition.AssetType == AssetType.Futures)
                        {
                            ProcessFuturesTransaction(output, definition, std);
                        }
                        else
                        {
                            var sales = RegisterSale(output, definition, std);
                            if (sales.All(x => x.ParentAsset.IsFund) && std.Timestamp <= Globals.TaxableFundSaleEndDate)
                            {
                                CalculateSalesTax(std, sales);
                            }
                            // Add cash gained from sale(s)
                            AddAsset(output, new Cash(std, sales), std.Timestamp);
                            DerecognitionBooking.Process(std, sales);
                        }
                    }
                }
                currentDate = tr.Timestamp;
            }

            ProcessPrecedingEvents(output, manuals, null, currentDate);
            while (reportingDates.Count > 0)
            {
                ProcessEndOfPeriod(output, reportingDates.Dequeue());
            }

            return output;
        }

        private static void RegisterCashDeduction(List<Asset> list, BuyTransactionDefinition btd, Event e)
        {
            if (e is FuturesEvent) throw new Exception("Not intended to be used with futures.");

            decimal remainingAmount = (btd.AssetType != AssetType.Futures ? btd.Amount : 0) + btd.Fee;
            if (remainingAmount == 0) throw new Exception("Cash deduction attempted with amount equal to 0.");

            string portfolio = btd.PortfolioDst;
            if (String.IsNullOrEmpty(portfolio)) throw new Exception("Portfolio not specified for cash deduction.");

            var cash = list.OfType<Cash>().Where(x => x.IsActive(btd.Timestamp) && x.Currency == btd.Currency && x.CashAccount == btd.AccountSrc && x.PortfolioId == portfolio);
            var allCash = list.OfType<Cash>().Where(x => x.Currency == btd.Currency && x.CashAccount == btd.AccountSrc && x.PortfolioId == portfolio);

            foreach (var c in cash)
            {
                var currentAmount = c.GetNominalAmount(new TimeArg(TimeArgDirection.StartIncludingRedemptions, btd.SettlementDate, btd.Index));
                if (currentAmount == 0) continue;
                var evt = new PaymentEvent(c, btd, Math.Min(currentAmount, remainingAmount), e);
                c.AddEvent(evt);
                remainingAmount -= evt.Amount;
                if (remainingAmount <= 0) break;
            }

            if (remainingAmount > 0)
            {
                throw new Exception($"No possible source for cash deduction: {btd}.");
            }
        }

        private static void RegisterCashDeduction(List<Asset> list, SellTransactionDefinition std, Event e)
        {
            if (e is FuturesEvent) throw new Exception("Not intended to be used with futures.");

            // In case of sale transaction only fee is deducted.
            // This happens only for sales involving futures.
            // Otherwise, fee is deducted from amount gained in sale.
            decimal remainingAmount = (std.AssetType != AssetType.Futures ? std.Amount : 0) + std.Fee;
            if (remainingAmount == 0) throw new Exception("Cash deduction attempted with amount equal to 0.");

            string portfolio = std.PortfolioSrc;
            if (String.IsNullOrEmpty(portfolio)) throw new Exception("Portfolio not specified for cash deduction.");

            var cash = list.OfType<Cash>().Where(x => x.IsActive(std.Timestamp) && x.Currency == std.Currency && x.CashAccount == std.AccountDst && x.PortfolioId == portfolio);
            foreach (var c in cash)
            {
                var currentAmount = c.GetNominalAmount(new TimeArg(TimeArgDirection.StartIncludingRedemptions, std.SettlementDate, std.Index));
                if (currentAmount == 0) continue;
                var evt = new PaymentEvent(c, std, Math.Min(currentAmount, remainingAmount), e);
                c.AddEvent(evt);
                remainingAmount -= evt.Amount;
                if (remainingAmount <= 0) break;
            }

            if (remainingAmount > 0)
            {
                throw new Exception($"No possible source for cash deduction: {std}.");
            }
        }

        private static void RegisterCashDeduction(List<Asset> list, PayTransactionDefinition ptd)
        {
            decimal remainingAmount = ptd.Amount + ptd.Fee;
            if (remainingAmount == 0) throw new Exception("Cash deduction attempted with amount equal to 0.");

            string portfolio = ptd.PortfolioSrc;
            if (String.IsNullOrEmpty(portfolio)) throw new Exception("Portfolio not specified for cash deduction.");

            var cash = list.OfType<Cash>().Where(x => x.IsActive(ptd.Timestamp) && x.Currency == ptd.Currency && x.CashAccount == ptd.AccountSrc && x.PortfolioId == portfolio);
            foreach (var c in cash)
            {
                var currentAmount = c.GetNominalAmount(new TimeArg(TimeArgDirection.StartIncludingRedemptions, ptd.SettlementDate, ptd.Index));
                if (currentAmount == 0) continue;
                var evt = new PaymentEvent(c, ptd, Math.Min(currentAmount, remainingAmount), PaymentDirection.Outflow);
                c.AddEvent(evt);
                remainingAmount -= evt.Amount;
                if (remainingAmount <= 0) break;
            }

            if (remainingAmount > 0)
            {
                throw new Exception($"No possible source for cash deduction: {ptd}.");
            }
        }

        private static void RegisterCashDeduction(List<Asset> list, FuturesTransactionEvent fr)
        {
            decimal remainingAmount = 0;
            if (fr.Amount - fr.Fee >= 0)
            {
                remainingAmount = fr.Fee;
            }
            else
            {
                remainingAmount = Math.Abs(fr.Amount) + fr.Fee;
            }

            string portfolio = fr.ParentAsset.PortfolioId;
            string currency = fr.ParentAsset.Currency;
            string account = fr.ParentAsset.CashAccount;

            var cash = list.OfType<Cash>().Where(x => x.IsActive(fr.Timestamp) && x.Currency == currency && x.CashAccount == account && x.PortfolioId == portfolio);
            foreach (var c in cash)
            {
                var currentAmount = c.GetNominalAmount(new TimeArg(TimeArgDirection.StartIncludingRedemptions, fr.Timestamp, fr.TransactionIndex));
                if (currentAmount == 0) continue;
                PaymentEvent payment = new PaymentEvent(c, fr, Math.Min(currentAmount, remainingAmount), PaymentDirection.Outflow);
                c.AddEvent(payment);
                remainingAmount -= Math.Abs(payment.Amount);
                if (remainingAmount <= 0) break;
            }

            if (remainingAmount > 0)
            {
                throw new Exception($"No possible source for cash deduction: {fr}.");
            }
        }

        private static void RegisterCashDeduction(List<Asset> list, FuturesRevaluationEvent fs)
        {
            if (fs.Amount >= 0) throw new Exception("Cash deduction can be created only from event with negative amount.");

            decimal remainingAmount = Math.Abs(fs.Amount);

            string portfolio = fs.ParentAsset.PortfolioId;
            string currency = fs.ParentAsset.Currency;
            string account = fs.ParentAsset.CashAccount;

            var cash = list.OfType<Cash>().Where(x => x.IsActive(fs.Timestamp) && x.Currency == currency && x.CashAccount == account && x.PortfolioId == portfolio);
            foreach (var c in cash)
            {
                var currentAmount = c.GetNominalAmount(new TimeArg(TimeArgDirection.End, fs.Timestamp, -1));
                if (currentAmount == 0) continue;
                PaymentEvent payment = new PaymentEvent(c, fs, Math.Min(currentAmount, remainingAmount), PaymentDirection.Outflow);
                c.AddEvent(payment);
                remainingAmount -= Math.Abs(payment.Amount);
                if (remainingAmount <= 0) break;
            }

            if (remainingAmount > 0)
            {
                throw new Exception($"No possible source for cash deduction: {fs}.");
            }
        }

        private static List<DerecognitionEvent> RegisterSale(List<Asset> list, InstrumentDefinition definition, SellTransactionDefinition std)
        {
            List<DerecognitionEvent> output = new List<DerecognitionEvent>();
            decimal remainingCount = std.Count;
            decimal remainingFee = std.Fee;

            Type assetType = Asset.GetAssetType(definition.AssetType);
            if (assetType == typeof(Cash)) throw new Exception("Tried selling an asset with cash type; cash transaction should be used instead.");
            if (assetType == typeof(Futures)) throw new Exception("Tried registering sale for futures instrument.");

            if (String.IsNullOrEmpty(std.PortfolioSrc)) throw new Exception("Portfolio not specified for sale.");

            var src = list.OfType<Security>().Where(x => x.GetType() == assetType &&
                x.SecurityDefinition.UniqueId == std.InstrumentUniqueId &&
                x.Currency == std.Currency &&
                x.CustodyAccount == std.AccountSrc &&
                x.PortfolioId == std.PortfolioSrc);

            foreach (var s in src)
            {
                var currentCount = s.GetCount(new TimeArg(TimeArgDirection.Start, std.SettlementDate, std.Index));
                if (currentCount == 0) continue;
                decimal soldCount = Math.Min(currentCount, remainingCount);
                decimal fee = Common.Round(remainingFee * soldCount / remainingCount);
                SellTransactionDefinition mtd = TransactionDefinition.CreateModifiedTransaction<SellTransactionDefinition>(std, soldCount, std.Price, fee);
                var evt = new DerecognitionEvent(s, mtd);
                s.AddEvent(evt);
                output.Add(evt);
                remainingCount -= soldCount;
                remainingFee -= fee;

                if (remainingCount <= 0) break;
            }

            ReconcileDerecognitionAmounts(std, output);

            if (remainingCount > 0)
            {
                throw new Exception($"No possible source for sale: {std}.");
            }

            return output;
        }

        private static List<DerecognitionEvent> RegisterSale(List<Asset> list, InstrumentDefinition definition, TransferTransactionDefinition ttd)
        {
            List<DerecognitionEvent> output = new List<DerecognitionEvent>();
            decimal remainingCount = ttd.Count;
            decimal remainingFee = ttd.Fee;

            Type assetType = Asset.GetAssetType(definition.AssetType);
            if (assetType == typeof(Cash)) throw new Exception("Tried transferring an asset with cash type; cash transaction should be used instead.");
            if (assetType == typeof(Futures)) throw new Exception("Tried registering transfer for futures instrument.");

            if (String.IsNullOrEmpty(ttd.PortfolioSrc)) throw new Exception("Portfolio not specified for sale.");

            var src = list.OfType<Security>().Where(x => x.GetType() == assetType &&
                x.SecurityDefinition.UniqueId == ttd.InstrumentUniqueId &&
                x.Currency == ttd.Currency &&
                x.CustodyAccount == ttd.AccountSrc &&
                x.PortfolioId == ttd.PortfolioSrc);

            foreach (var s in src)
            {
                var currentCount = s.GetCount(new TimeArg(TimeArgDirection.Start, ttd.SettlementDate, ttd.Index));
                if (currentCount == 0) continue;
                decimal soldCount = Math.Min(currentCount, remainingCount);
                decimal fee = Common.Round(remainingFee * soldCount / remainingCount);
                TransferTransactionDefinition mtd = TransactionDefinition.CreateModifiedTransaction<TransferTransactionDefinition>(ttd, soldCount, ttd.Price, fee);
                var evt = new DerecognitionEvent(s, mtd);
                s.AddEvent(evt);
                output.Add(evt);
                remainingCount -= soldCount;
                remainingFee -= fee;

                if (remainingCount <= 0) break;
            }

            ReconcileDerecognitionAmounts(ttd, output);

            if (remainingCount > 0)
            {
                throw new Exception($"No possible source for sale: {ttd}.");
            }

            return output;
        }

        private static void ProcessFuturesTransaction(List<Asset> list, InstrumentDefinition definition, BuyTransactionDefinition btd)
        {
            if (String.IsNullOrEmpty(btd.PortfolioDst)) throw new Exception("Portfolio not specified for futures contract.");

            BuyTransactionDefinition? mtd = (BuyTransactionDefinition?)RegisterFuturesDerecognition(list, definition, btd);

            if (mtd != null && mtd.Count != 0)
            {
                var futures = new Futures(btd, definition);
                AddAsset(list, futures, btd.Timestamp);
                var fr = futures.Events.OfType<FuturesRecognitionEvent>().First();
                RegisterCashDeduction(list, fr); // fee
                FuturesRecognitionBooking.Process(fr);
            }
        }

        private static void ProcessFuturesTransaction(List<Asset> list, InstrumentDefinition definition, SellTransactionDefinition std)
        {
            if (String.IsNullOrEmpty(std.PortfolioSrc)) throw new Exception("Portfolio not specified for futures contract.");

            SellTransactionDefinition? mtd = (SellTransactionDefinition?)RegisterFuturesDerecognition(list, definition, std);

            if (mtd != null && mtd.Count != 0)
            {
                var futures = new Futures(std, definition);
                AddAsset(list, futures, std.Timestamp);
                var fr = futures.Events.OfType<FuturesRecognitionEvent>().First();
                RegisterCashDeduction(list, fr); // fee
                FuturesRecognitionBooking.Process(fr);
            }
        }

        private static void ProcessFuturesTransaction(List<Asset> list, InstrumentDefinition definition, TransferTransactionDefinition ttr)
        {
            throw new NotImplementedException();
        }

        private static TransactionDefinition? RegisterFuturesDerecognition(List<Asset> list, InstrumentDefinition definition, TransactionDefinition td)
        {
            var src = list.OfType<Futures>().Where(x =>
               x.SecurityDefinition.UniqueId == td.InstrumentUniqueId &&
               x.Currency == td.Currency &&
               ((td is BuyTransactionDefinition && x.CashAccount == td.AccountSrc) || (td is SellTransactionDefinition && x.CashAccount == td.AccountDst)) &&
               ((td is BuyTransactionDefinition && x.CustodyAccount == td.AccountDst) || (td is SellTransactionDefinition && x.CustodyAccount == td.AccountSrc)) &&
               ((td is BuyTransactionDefinition && x.PortfolioId == td.PortfolioDst) || (td is SellTransactionDefinition && x.PortfolioId == td.PortfolioSrc))).ToList();

            decimal sgn = td is SellTransactionDefinition ? -1 : 1;
            decimal remainingCount = td.Count * sgn;
            decimal remainingFee = td.Fee;

            foreach (var s in src)
            {
                var currentCount = s.GetCount(new TimeArg(TimeArgDirection.Start, td.SettlementDate, td.Index));
                if (currentCount == 0) continue;
                if (sgn < 0 && currentCount < 0) continue;
                if (sgn > 0 && currentCount > 0) continue;

                decimal derecognizedCount = sgn > 0 ? Math.Max(remainingCount, currentCount) : Math.Min(-remainingCount, currentCount);
                decimal fee = Math.Abs(Common.Round(remainingFee * derecognizedCount / remainingCount));
                td = TransactionDefinition.CreateModifiedTransaction<TransactionDefinition>(td, td.Count - derecognizedCount, td.Price, fee);
                var fde = new FuturesDerecognitionEvent(s, td, derecognizedCount);
                s.AddEvent(fde);
                s.RecalculateFlows();
                FuturesDerecognitionBooking.Process(fde);

                // Derecognition settlement
                if (fde.Amount - fde.Fee >= 0)
                {
                    AddAsset(list, new Cash(fde), fde.Timestamp);
                }
                else
                {
                    RegisterCashDeduction(list, fde); // fee
                }

                remainingCount -= derecognizedCount;
                remainingFee -= fee;

                if (remainingCount == 0) return null; // whole transaction covered by derecognition
            }

            // some part of transaction remained, a new futures contract will have to be created
            return td;
        }

        private static void AddAsset(List<Asset> list, Asset asset, DateTime timestamp)
        {
            var index = list.FindIndex(x => x.Events.First().Timestamp > timestamp);
            list.Insert(index == -1 ? list.Count : index, asset);
        }

        private static void ProcessPrecedingEvents(List<Asset> output, HashSet<ManualEventDefinition> manual, TransactionDefinition? tr, DateTime startDate)
        {
            DateTime endDate = tr?.Timestamp ?? Common.EndDate;

            List<Asset> newAssets = new List<Asset>();

            foreach (var asset in output)
            {
                // Coupons, dividends, redemptions happen during the day and proceeds can be used immediately.
                var flows = asset.Events.OfType<FlowEvent>().Where(x => x.Timestamp > startDate && x.Timestamp <= endDate).ToList();
                foreach (var ev in flows)
                {
                    var cash = new Cash(ev);
                    newAssets.Add(cash);
                    InflowBooking.Process(ev);
                    if (ev.FlowType == FlowType.Redemption)
                    {
                        asset.GenerateValuation(ev.Timestamp, true);
                    }
                }
                // Futures settlement happens at the very end of the day (after market closes).
                foreach (var ev in asset.Events.OfType<FuturesRevaluationEvent>().Where(x => x.Timestamp >= startDate && x.Timestamp < endDate))
                {
                    if (ev.Amount >= 0)
                    {
                        var cash = new Cash(ev);
                        newAssets.Add(cash);
                    }
                    else
                    {
                        RegisterCashDeduction(output, ev);
                    }
                    FuturesRevaluationBooking.Process(ev);
                }
            }

            newAssets.ForEach(x => AddAsset(output, x, x.Events.First().Timestamp));

            // Process manual events.
            foreach (var mn in manual.Where(x => x.Timestamp < endDate))
            {
                if (mn is AdditionalPremiumEventDefinition ape)
                {
                    ProcessAdditionalPremium(output, ape);
                    ManualEventBooking.Process(ape);
                }
                if (mn is AdditionalChargeEventDefinition ace)
                {
                    ProcessAdditionalCharge(output, ace);
                    ManualEventBooking.Process(ace);
                }
                if (mn is EquityRedemptionEventDefinition ere)
                {
                    ProcessEquityRedemption(output, ere); // Accounting impact inside
                }
                if (mn is EquitySpinOffEventDefinition eso)
                {
                    ProcessEquitySpinOff(output, eso);
                    // No immediate accounting impact of equity spin-off.
                }

                manual.Remove(mn);
            }

        }

        private static void ProcessEndOfPeriod(List<Asset> output, DateTime date)
        {
            DateTime currentDate = date;
            DateTime previousDate = Financial.Calendar.AddAndAlignToEndDate(date, -1, Financial.Calendar.TimeStep.Monthly);

            // Create valuation events
            foreach (var a in output.Where(x => x.IsActive(currentDate)))
            {
                var e = a.GenerateValuation(currentDate, false);
            }

            var assets = output.Where(x => x.IsActive(previousDate, currentDate)); //output.Where(x => x.IsActive(previousDate) || x.IsActive(currentDate));
            foreach (var portfolio in assets.Select(x => x.Portfolio).Distinct())
            {
                foreach (var currency in assets.Select(x => x.Currency).Distinct())
                {
                    foreach (var assetType in assets.Select(x => x.AssetType).Distinct())
                    {
                        if (assetType == AssetType.Cash) continue;
                        ValuationBooking.Process(assets, assetType, portfolio, currency, currentDate, previousDate);
                    }
                }
            }

            foreach (var ati in Definitions.ManualEvents.OfType<AdditionalTaxableIncomeEventDefinition>().Where(x => x.Timestamp <= currentDate && x.Timestamp > previousDate))
            {
                ManualEventBooking.Process(ati);
            }
            foreach (var ate in Definitions.ManualEvents.OfType<AdditionalTaxableExpenseEventDefinition>().Where(x => x.Timestamp <= currentDate && x.Timestamp > previousDate))
            {
                ManualEventBooking.Process(ate);
            }

            TaxIncomeBooking.Process(currentDate);

            if (currentDate.Month == 12 && currentDate.Day == 31)
            {
                previousDate = Financial.Calendar.AddAndAlignToEndDate(date, -1, Financial.Calendar.TimeStep.Yearly);

                foreach (var portfolio in Definitions.Portfolios.Append(null))
                {
                    foreach (var currency in Globals.SupportedCurrencies)
                    {
                        EndOfYearBooking.Process(portfolio, currency, currentDate.AddDays(1));
                    }
                }
            }
        }

        private static void ProcessEquitySpinOff(List<Asset> output, EquitySpinOffEventDefinition mn)
        {
            TimeArg time = new TimeArg(TimeArgDirection.End, mn.Timestamp);
            List<Asset> newAssets = new List<Asset>();
            foreach (var asset in output.OfType<Equity>().Where(x => x.InstrumentUniqueId == mn.OriginalInstrumentUniqueId))
            {
                if (!(asset.IsActive(mn.Timestamp))) continue;
                decimal count = asset.GetCount(time);
                decimal price = asset.GetPurchasePrice(time, false, false);
                decimal newPrice2 = price; //mn.Amount2 / (mn.Amount2 + mn.Amount3) * price;
                decimal newPrice3 = price - newPrice2;
                // Reduce holding
                if (mn.OriginalInstrumentCountMultiplier < 1)
                {
                    decimal newCount = mn.OriginalInstrumentCountMultiplier * count;
                    var evt = new DerecognitionEvent(asset, mn, count - newCount, price);
                    asset.AddEvent(evt);
                }
                // Add converted equity
                if (mn.ConvertedInstrumentCountMultiplier > 0)
                {
                    var definition = Definitions.Instruments.First(x => x.UniqueId == mn.ConvertedInstrumentUniqueId);
                    decimal newCount = mn.ConvertedInstrumentCountMultiplier * count;
                    var newAsset = new Equity(asset, definition, mn, newCount, newPrice2, true);
                    newAssets.Add(newAsset);
                }
                // Add spun off equity
                if (mn.SpunOffInstrumentCountMultiplier > 0)
                {
                    var definition = Definitions.Instruments.First(x => x.UniqueId == mn.SpunOffInstrumentUniqueId);
                    decimal newCount = mn.SpunOffInstrumentCountMultiplier * count;
                    var newAsset = new Equity(asset, definition, mn, newCount, newPrice3, false);
                    newAssets.Add(newAsset);
                }
            }
            newAssets.ForEach(x => AddAsset(output, x, mn.Timestamp));
        }

        private static void ProcessEquityRedemption(List<Asset> output, EquityRedemptionEventDefinition mn)
        {
            TimeArg time = new TimeArg(TimeArgDirection.End, mn.Timestamp);
            List<Asset> newAssets = new List<Asset>();
            foreach (var asset in output.OfType<Security>().Where(x => (x.AssetType == AssetType.Equity || x.AssetType == AssetType.ETF ||
                x.AssetType == AssetType.CorporateBondsFund || x.AssetType == AssetType.EquityMixedFund || x.AssetType == AssetType.MoneyMarketFund || x.AssetType == AssetType.TreasuryBondsFund) &&
                x.InstrumentUniqueId == mn.InstrumentUniqueId))
            {
                if (!(asset.IsActive(mn.Timestamp))) continue;

                // Process derecognition
                var count = asset.GetCount(new TimeArg(TimeArgDirection.Start, mn.Timestamp));
                if (count == 0) continue;
                var evt = new DerecognitionEvent(asset, mn, count, mn.Price);
                asset.AddEvent(evt);

                // Process cash payment
                newAssets.Add(new Cash(mn, asset, evt.Amount));

                ManualEventBooking.Process(mn, evt);
            }
            newAssets.ForEach(x => AddAsset(output, x, mn.Timestamp));
        }

        private static void ProcessAdditionalPremium(List<Asset> output, AdditionalPremiumEventDefinition mn)
        {
            AddAsset(output, new Cash(mn), mn.Timestamp);
        }

        private static void ProcessAdditionalCharge(List<Asset> output, AdditionalChargeEventDefinition mn)
        {
            decimal remainingAmount = mn.Amount;
            if (remainingAmount == 0) throw new Exception("Additional charge attempted with amount equal to 0.");

            string account = Definitions.Portfolios.Single(x => x.UniqueId == mn.Portfolio).CashAccount;
            if (String.IsNullOrEmpty(account)) throw new Exception("Cash account not specified for additional charge.");
            string portfolio = mn.Portfolio;
            if (String.IsNullOrEmpty(portfolio)) throw new Exception("Portfolio not specified for cash deduction.");

            var cash = output.OfType<Cash>().Where(x => x.IsActive(mn.Timestamp) && x.CashAccount == account && x.PortfolioId == portfolio);
            foreach (var c in cash)
            {
                var currentAmount = c.GetNominalAmount(new TimeArg(TimeArgDirection.End, mn.Timestamp));
                if (currentAmount == 0) continue;
                var evt = new PaymentEvent(c, mn, Math.Min(currentAmount, remainingAmount));
                c.AddEvent(evt);
                remainingAmount -= evt.Amount;
                if (remainingAmount <= 0) break;
            }

            if (remainingAmount > 0)
            {
                throw new Exception($"No possible source for additional charge: {mn}.");
            }
        }

        private static void CalculateSalesTax(TransactionDefinition tr, IEnumerable<DerecognitionEvent> sales)
        {
            decimal tax;
            var time = new TimeArg(TimeArgDirection.Start, tr.Timestamp, tr.Index);
            string id = tr.InstrumentUniqueId + "_" + tr.Index;

            var manualAdjustment = Definitions.ManualEvents.OfType<IncomeTaxAdjustmentEventDefinition>().SingleOrDefault(x => x.Timestamp == tr.Timestamp && x.AssetUniqueId == id);
            if (manualAdjustment != null)
            {
                tax = manualAdjustment.Tax;
            }
            else if (Globals.TaxFreePortfolios.Contains(tr.PortfolioSrc))
            {
                tax = 0;
            }
            else
            {
                decimal originalCount = sales.Sum(x => x.ParentAsset.GetCount(time));
                decimal purchaseAmount = sales.Sum(x => x.ParentAsset.GetPurchaseAmount(time, true, true));
                decimal taxableIncome = Common.Round(tr.Amount - purchaseAmount * tr.Count / originalCount);
                if (taxableIncome <= 0) return;
                tax = TaxCalculations.CalculateFromIncome(taxableIncome);
            }

            foreach (var evt in sales)
            {
                decimal originalCountPerEvent = evt.ParentAsset.GetCount(time);
                decimal originalPricePerEvent = evt.ParentAsset.GetPurchasePrice(time, true, true);
                decimal taxableIncomePerEvent = Common.Round((evt.DirtyPrice - originalPricePerEvent) * originalCountPerEvent);
                if (taxableIncomePerEvent <= 0) continue;
                decimal taxPerEvent = TaxCalculations.CalculateFromIncome(taxableIncomePerEvent);

                evt.Tax = Math.Min(tax, taxPerEvent);
                tax -= evt.Tax;

                if (tax <= 0) return;
            }

            if (tax > 0) throw new Exception($"CalculateSalesTax metod did not assign all of the tax to derecognition events. Leftover: {tax}.");
        }

        private static void ReconcileDerecognitionAmounts(TransactionDefinition tr, List<DerecognitionEvent> output)
        {
            if (output.Count < 2) return;

            // The method is to fix situations where sum of rounded derecognition amounts does not equal rounded sale transaction amount (due to rounding errors).
            decimal transactionAmount = tr.Amount;
            decimal derecognitionAmount = output.Sum(x => x.Amount);

            decimal feeAmount = tr.Fee;
            decimal feeSum = output.Sum(x => x.Fee);

            if (transactionAmount != derecognitionAmount)
            {
                decimal diff = Common.Round(transactionAmount - derecognitionAmount);
                var evt = output.OrderByDescending(x => x.Amount).First();
                evt.Amount += diff;
            }
            if (feeAmount != feeSum)
            {
                decimal diff = Common.Round(feeAmount - feeSum);
                var evt = output.OrderByDescending(x => x.Amount).First();
                evt.Fee += diff;
            }
        }
    }
}
