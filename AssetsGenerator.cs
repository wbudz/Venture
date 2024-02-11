using Venture.Assets;
using Venture.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Windows.Documents;
using System.Security.Policy;
using static Financial.Calendar;
using Venture.Events;

namespace Venture
{
    public static class AssetsGenerator
    {
        public static List<Assets.Asset> GenerateAssets()
        {
            List<Assets.Asset> output = new List<Assets.Asset>();
            Queue<Data.Transaction> transactions = new Queue<Data.Transaction>(Data.Definitions.Transactions);
            HashSet<Manual> manual = new HashSet<Manual>(Data.Definitions.GetManualEventSources());

            if (transactions.Count() == 0) return output;

            DateTime lastDate = new DateTime(transactions.Peek().Timestamp.Year - 1, 12, 31);

            while (transactions.Count > 0)
            {
                Data.Transaction tr = transactions.Dequeue();
                if (tr.Amount == 0) throw new Exception($"Transaction with amount equal to 0 encountered: {tr}.");

                // Go through pending events that come before (influence) current transaction - e.g. dividends, coupons that may add new cash    
                // Also, Process manual entries that influence assets
                ProcessEvents(output, manual, tr, lastDate);

                // Process the transaction
                if (tr.TransactionType == Data.TransactionType.Buy)
                {
                    var definition = Data.Definitions.Instruments.FirstOrDefault(x => x.InstrumentId == tr.InstrumentId);
                    if (definition == null) throw new Exception($"Purchase transaction definition pointed to unknown instrument id: {tr.InstrumentId}.");

                    Asset asset;
                    switch (definition.InstrumentType)
                    {
                        case AssetType.Undefined:
                            throw new Exception("Tried creating asset with undefined instrument type.");
                        case AssetType.Cash:
                            throw new Exception("Tried creating asset with purchase transaction and cash instrument type.");
                        case AssetType.Equity:
                        case AssetType.ETF:
                            asset = new Assets.Equity(tr, definition); break;
                        case AssetType.FixedTreasuryBonds:
                        case AssetType.FloatingTreasuryBonds:
                        case AssetType.FixedRetailTreasuryBonds:
                        case AssetType.FloatingRetailTreasuryBonds:
                        case AssetType.IndexedRetailTreasuryBonds:
                        case AssetType.FixedCorporateBonds:
                        case AssetType.FloatingCorporateBonds:
                            asset = new Assets.Bond(tr, definition); break;
                        case AssetType.MoneyMarketFund:
                        case AssetType.EquityMixedFund:
                        case AssetType.TreasuryBondsFund:
                        case AssetType.CorporateBondsFund:
                            asset = new Assets.Fund(tr, definition); break;
                        case AssetType.Futures: throw new NotImplementedException();
                        default: throw new Exception("Tried creating asset with unknown instrument type.");
                    }

                    AddAsset(output, asset, tr.Timestamp);

                    // Subtract cash used for purchase
                    RegisterCashDeduction(output, tr);
                }
                if (tr.TransactionType == Data.TransactionType.Sell)
                {
                    var definition = Data.Definitions.Instruments.FirstOrDefault(x => x.InstrumentId == tr.InstrumentId);
                    if (definition == null) throw new Exception("Sale transaction definition pointed to unknown instrument id.");

                    var sales = RegisterSale(output, definition, tr);
                    if (sales.All(x => x.ParentAsset.IsFund) && tr.Timestamp <= Globals.TaxableFundSaleEndDate)
                    {
                        CalculateSalesTax(tr, sales);
                    }

                    // Add cash gained from sale(s)
                    AddAsset(output, new Cash(tr, sales), tr.Timestamp);

                }
                if (tr.TransactionType == Data.TransactionType.Cash)
                {
                    // Register cash addition
                    if (!String.IsNullOrEmpty(tr.AccountDst))
                    {
                        AddAsset(output, new Cash(tr), tr.Timestamp);
                    }
                    // Register cash deduction
                    if (!String.IsNullOrEmpty(tr.AccountSrc))
                    {
                        RegisterCashDeduction(output, tr);
                    }
                }

                lastDate = tr.Timestamp;
            }

            ProcessEvents(output, manual, null, lastDate);

            return output;
        }

        private static void RegisterCashDeduction(List<Assets.Asset> list, Data.Transaction tr)
        {
            decimal remainingAmount = tr.Amount + tr.Fee;
            if (remainingAmount == 0) throw new Exception("Cash deduction attempted with amount equal to 0.");

            string portfolio = "";
            switch (tr.TransactionType)
            {
                case TransactionType.Undefined: throw new Exception("Tried deducting cash with undefined transaction type.");
                case TransactionType.Buy: portfolio = tr.PortfolioDst; break;
                case TransactionType.Sell: throw new Exception("Tried deducting cash with sale transaction type.");
                case TransactionType.Cash: portfolio = tr.PortfolioSrc; break;
                default: throw new Exception("Tried deducting cash with unknown transaction type.");
            }
            if (String.IsNullOrEmpty(portfolio)) throw new Exception("Portfolio not specified for cash deduction.");

            var cash = list.OfType<Cash>().Where(x => x.IsActive(tr.Timestamp) && x.Currency == tr.Currency && x.CashAccount == tr.AccountSrc && x.Portfolio == portfolio);
            foreach (var c in cash)
            {
                var currentAmount = c.GetNominalAmount(new TimeArg(TimeArgDirection.Start, tr.SettlementDate, tr.Index));
                if (currentAmount == 0) continue;
                var evt = new Events.Payment(c, tr, Math.Min(currentAmount, remainingAmount), Events.PaymentDirection.Outflow);
                c.AddEvent(evt);
                remainingAmount -= evt.Amount;
                if (remainingAmount <= 0) break;
            }

            if (remainingAmount > 0)
            {
                //Log.Report(Severity.Error, "No possible source for cash deduction.", e);
                throw new Exception($"No possible source for cash deduction: {tr}.");
            }
        }

        private static List<Events.Derecognition> RegisterSale(List<Assets.Asset> list, Data.Instrument definition, Data.Transaction tr)
        {
            List<Events.Derecognition> output = new List<Derecognition>();
            decimal remainingCount = tr.Count;

            Type assetType;
            switch (definition.InstrumentType)
            {
                case AssetType.Undefined:
                    throw new Exception("Tried selling an asset with undefined instrument type.");
                case AssetType.Cash:
                    throw new Exception("Tried selling an asset with cash type; cash transaction should be used instead.");
                case AssetType.Equity:
                case AssetType.ETF:
                    assetType = typeof(Assets.Equity); break;
                case AssetType.FixedTreasuryBonds:
                case AssetType.FloatingTreasuryBonds:
                case AssetType.FixedRetailTreasuryBonds:
                case AssetType.FloatingRetailTreasuryBonds:
                case AssetType.IndexedRetailTreasuryBonds:
                case AssetType.FixedCorporateBonds:
                case AssetType.FloatingCorporateBonds:
                    assetType = typeof(Assets.Bond); break;
                case AssetType.MoneyMarketFund:
                case AssetType.EquityMixedFund:
                case AssetType.TreasuryBondsFund:
                case AssetType.CorporateBondsFund:
                    assetType = typeof(Assets.Fund); break;
                case AssetType.Futures: throw new NotImplementedException();
                default: throw new Exception("Tried selling an asset with unknown instrument type.");
            }

            if (String.IsNullOrEmpty(tr.PortfolioSrc)) throw new Exception("Portfolio not specified for sale.");

            var src = list.OfType<Security>().Where(x => x.GetType() == assetType &&
                x.SecurityDefinition.InstrumentId == tr.InstrumentId &&
                x.Currency == tr.Currency &&
                x.CustodyAccount == tr.AccountSrc &&
                x.Portfolio == tr.PortfolioSrc);

            foreach (var s in src)
            {
                var currentCount = s.GetCount(new TimeArg(TimeArgDirection.Start, tr.SettlementDate, tr.Index));
                if (currentCount == 0) continue;
                var evt = new Events.Derecognition(s, tr, Math.Min(currentCount, remainingCount), tr.Timestamp);
                s.AddEvent(evt);
                output.Add(evt);
                remainingCount -= evt.Count;
                if (remainingCount <= 0) break;
            }

            ReconcileDerecognitionAmounts(tr, output);

            if (remainingCount > 0)
            {
                throw new Exception($"No possible source for sale: {tr}.");
            }

            return output;
        }

        private static void AddAsset(List<Assets.Asset> list, Assets.Asset asset, DateTime timestamp)
        {
            var index = list.FindIndex(x => x.Events.First().Timestamp > timestamp);
            list.Insert(index == -1 ? list.Count : index, asset);
        }

        private static void ProcessEvents(List<Asset> output, HashSet<Manual> manual, Data.Transaction? tr, DateTime startDate)
        {
            DateTime endDate = tr?.Timestamp ?? Common.FinalDate;

            List<Asset> newAssets = new List<Asset>();

            foreach (var asset in output)
            {
                foreach (var ev in asset.Events.OfType<Events.Flow>().Where(x => x.Timestamp > startDate && x.Timestamp <= endDate))
                {
                    var cash = new Cash(ev);
                    if (cash.Events.Count() == 0) throw new Exception("Cash with now events was created.");
                    newAssets.Add(cash);
                }
            }

            newAssets.ForEach(x => AddAsset(output, x, x.Events.First().Timestamp));

            ProcessEquitySpinOffs(output, manual, endDate);
        }

        private static void ProcessEquitySpinOffs(List<Asset> output, HashSet<Manual> manual, DateTime timestamp)
        {
            foreach (var mn in manual.Where(x => x.AdjustmentType == ManualAdjustmentType.EquitySpinOff).Where(x => x.Timestamp < timestamp))
            {
                TimeArg time = new TimeArg(TimeArgDirection.End, mn.Timestamp);
                List<Asset> newAssets = new List<Asset>();
                foreach (var asset in output.OfType<Equity>().Where(x => x.SecurityDefinition.UniqueId == mn.Instrument1))
                {
                    if (!(asset.IsActive(mn.Timestamp))) continue;
                    decimal count = asset.GetCount(time);
                    decimal price = asset.GetPurchasePrice(time, false);
                    decimal newPrice2 = price; //mn.Amount2 / (mn.Amount2 + mn.Amount3) * price;
                    decimal newPrice3 = price - newPrice2;
                    // Reduce holding
                    if (mn.Amount1 < 1)
                    {
                        decimal newCount = mn.Amount1 * count;
                        var evt = new Events.Derecognition(asset, mn, count - newCount, price);
                        asset.AddEvent(evt);
                    }
                    // Add converted equity
                    if (mn.Amount2 > 0)
                    {
                        var definition = Data.Definitions.Instruments.First(x => x.UniqueId == mn.Instrument2);
                        decimal newCount = mn.Amount2 * count;
                        var newAsset = new Equity(asset, definition, mn, newCount, newPrice2);
                        newAssets.Add(newAsset);
                    }
                    // Add spun off equity
                    if (mn.Amount3 > 0)
                    {
                        var definition = Data.Definitions.Instruments.First(x => x.UniqueId == mn.Instrument3);
                        decimal newCount = mn.Amount3 * count;
                        var newAsset = new Equity(asset, definition, mn, newCount, newPrice3);
                        newAssets.Add(newAsset);
                    }
                }
                newAssets.ForEach(x => AddAsset(output, x, mn.Timestamp));
                manual.Remove(mn);
            }
        }

        private static void CalculateSalesTax(Data.Transaction tr, IEnumerable<Derecognition> sales)
        {
            decimal tax;
            var time = new TimeArg(TimeArgDirection.Start, tr.Timestamp, tr.Index);

            var manualAdjustment = Data.Definitions.GetManualAdjustment(Data.ManualAdjustmentType.IncomeTaxAdjustment, tr.Timestamp, tr);
            if (manualAdjustment != null)
            {
                tax = manualAdjustment.Amount1;
            }
            else if (Globals.TaxFreePortfolios.Contains(tr.PortfolioSrc))
            {
                tax = 0;
            }
            else
            {
                decimal originalCount = sales.Sum(x => x.ParentAsset.GetCount(time));
                decimal purchaseAmount = sales.Sum(x => x.ParentAsset.GetPurchaseAmount(time, true));
                decimal taxableIncome = Common.Round(tr.Amount - purchaseAmount * tr.Count / originalCount);
                if (taxableIncome <= 0) return;
                tax = TaxCalculations.CalculateFromIncome(taxableIncome);
            }

            foreach (var evt in sales)
            {
                decimal originalCountPerEvent = evt.ParentAsset.GetCount(time);
                decimal originalPricePerEvent = evt.ParentAsset.GetPurchasePrice(time, true);
                decimal taxableIncomePerEvent = Common.Round((evt.DirtyPrice - originalPricePerEvent) * originalCountPerEvent);
                if (taxableIncomePerEvent <= 0) continue;
                decimal taxPerEvent = TaxCalculations.CalculateFromIncome(taxableIncomePerEvent);

                evt.Tax = Math.Min(tax, taxPerEvent);
                tax -= evt.Tax;

                if (tax <= 0) return;
            }

            if (tax > 0) throw new Exception($"CalculateSalesTax metod did not assign all of the tax to derecognition events. Leftover: {tax}.");
        }

        private static void ReconcileDerecognitionAmounts(Data.Transaction tr, List<Derecognition> output)
        {
            if (output.Count < 2) return;

            // The method is to fix situations where sum of rounded derecognition amounts does not equal rounded sale transaction amount (due to rounding errors).
            decimal transactionAmount = tr.Amount;
            decimal derecognitionAmount = output.Sum(x => x.Amount);

            if (transactionAmount != derecognitionAmount)
            {
                decimal diff = Common.Round(transactionAmount - derecognitionAmount);
                var evt = output.OrderByDescending(x => x.Amount).First();
                evt.Amount += diff;
                //throw new Exception("ReconcileDerecognitionAmounts activated."); //TODO: remove
            }
        }
    }
}
