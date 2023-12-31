﻿using Venture.Assets;
using Venture.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Windows.Documents;

namespace Venture
{
    public static class AssetsGenerator
    {
        public static List<Assets.Asset> GenerateAssets()
        {
            List<Assets.Asset> output = new List<Assets.Asset>();
            Queue<Data.Transaction> transactions = new Queue<Data.Transaction>(Data.Definitions.Transactions);
            HashSet<Events.Flow> flowEvents = new HashSet<Events.Flow>();
            HashSet<Manual> manual = new HashSet<Manual>(Data.Definitions.GetManualEventSources());

            while (transactions.Count > 0)
            {
                Data.Transaction tr = transactions.Dequeue();

                // Go through pending events that come before (influence) current transaction - e.g. dividends, coupons that may add new cash
                foreach (var ev in flowEvents.Where(x => x.Timestamp <= tr.SettlementDate))
                {
                    var cash = new Cash(ev);
                    if (cash.Events.Count() > 0) AddAsset(output, cash, ev.Timestamp);
                    flowEvents.Remove(ev);
                }

                // Process manual entries that influence assets
                ProcessEquitySpinOffs(manual, output, tr.SettlementDate);

                // Process the transaction
                if (tr.TransactionType == Data.TransactionType.Buy)
                {
                    var definition = Data.Definitions.Instruments.FirstOrDefault(x => x.InstrumentId == tr.InstrumentId);
                    if (definition == null) throw new Exception("Purchase transaction definition pointed to unknown instrument id.");

                    Asset asset;
                    DateTime date = definition.RecognitionOnTradeDate ? tr.TradeDate : tr.SettlementDate;
                    switch (definition.InstrumentType)
                    {
                        case AssetType.Undefined: throw new Exception("Tried creating asset with undefined instrument type.");
                        case AssetType.Cash: throw new Exception("Tried creating asset with purchase transaction and cash instrument type.");
                        case AssetType.Equity: asset = new Assets.Equity(tr, definition); break;
                        case AssetType.FixedTreasuryBonds:
                        case AssetType.FloatingTreasuryBonds:
                        case AssetType.FixedRetailTreasuryBonds:
                        case AssetType.FloatingRetailTreasuryBonds:
                        case AssetType.IndexedRetailTreasuryBonds:
                        case AssetType.FixedCorporateBonds:
                        case AssetType.FloatingCorporateBonds:
                            asset = new Assets.Bond(tr, definition); break;
                        case AssetType.ETF:
                            asset = new Assets.ETF(tr, definition); break;
                        case AssetType.MoneyMarketFund:
                        case AssetType.EquityMixedFund:
                        case AssetType.TreasuryBondsFund:
                        case AssetType.CorporateBondsFund:
                            asset = new Assets.Fund(tr, definition); break;
                        case AssetType.Futures: throw new NotImplementedException();
                        default: throw new Exception("Tried creating asset with unknown instrument type.");
                    }

                    AddAsset(output, asset, date);
                    // Add pending events
                    foreach (var evt in asset.Events.OfType<Events.Flow>())
                    {
                        flowEvents.Add(evt);
                    }
                    // Subtract cash used for purchase
                    RegisterCashDeduction(output, tr);
                }
                if (tr.TransactionType == Data.TransactionType.Sell)
                {
                    var definition = Data.Definitions.Instruments.FirstOrDefault(x => x.InstrumentId == tr.InstrumentId);
                    if (definition == null) throw new Exception("Sale transaction definition pointed to unknown instrument id.");

                    RegisterSale(output, definition, tr);

                    // Add cash gained from sale
                    AddAsset(output, new Cash(tr), definition.RecognitionOnTradeDate ? tr.TradeDate : tr.SettlementDate);

                }
                if (tr.TransactionType == Data.TransactionType.Cash)
                {
                    // Register cash addition
                    if (!String.IsNullOrEmpty(tr.AccountDst))
                    {
                        AddAsset(output, new Cash(tr), tr.SettlementDate);
                    }
                    // Register cash deduction
                    if (!String.IsNullOrEmpty(tr.AccountSrc))
                    {
                        RegisterCashDeduction(output, tr);
                    }
                }
            }

            // Process remaining manual entries that influence assets
            ProcessEquitySpinOffs(manual, output, null);

            return output;
        }

        private static void RegisterCashDeduction(List<Assets.Asset> list, Data.Transaction tr)
        {
            decimal remainingAmount = tr.Amount + tr.Fee;

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

            var cash = list.OfType<Cash>().Where(x => x.Currency == tr.Currency && x.CashAccount == tr.AccountSrc && x.Portfolio == portfolio);
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

        private static void RegisterSale(List<Assets.Asset> list, Data.Instrument definition, Data.Transaction tr)
        {
            decimal remainingCount = tr.Count;

            Type assetType;
            switch (definition.InstrumentType)
            {
                case AssetType.Undefined: throw new Exception("Tried selling an asset with undefined instrument type.");
                case AssetType.Cash: throw new Exception("Tried selling an asset with cash type; cash transaction should be used instead.");
                case AssetType.Equity: assetType = typeof(Assets.Equity); break;
                case AssetType.FixedTreasuryBonds:
                case AssetType.FloatingTreasuryBonds:
                case AssetType.FixedRetailTreasuryBonds:
                case AssetType.FloatingRetailTreasuryBonds:
                case AssetType.IndexedRetailTreasuryBonds:
                case AssetType.FixedCorporateBonds:
                case AssetType.FloatingCorporateBonds:
                    assetType = typeof(Assets.Bond); break;
                case AssetType.ETF:
                    assetType = typeof(Assets.ETF); break;
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
                var evt = new Events.Derecognition(s, tr, Math.Min(currentCount, remainingCount), definition.RecognitionOnTradeDate ? tr.TradeDate : tr.SettlementDate);
                s.AddEvent(evt);
                remainingCount -= evt.Count;
                if (remainingCount <= 0) break;
            }

            if (remainingCount > 0)
            {
                throw new Exception($"No possible source for sale: {tr}.");
            }
        }

        private static void AddAsset(List<Assets.Asset> list, Assets.Asset asset, DateTime timestamp)
        {
            var index = list.FindIndex(x => x.Events.First().Timestamp > timestamp);
            list.Insert(index == -1 ? list.Count : index, asset);
        }

        private static void ProcessEquitySpinOffs(HashSet<Manual> manual, List<Asset> output, DateTime? timestamp)
        {
            foreach (var mn in manual.Where(x => x.AdjustmentType == ManualAdjustmentType.EquitySpinOff).Where(x => timestamp == null || x.Timestamp < timestamp))
            {
                TimeArg time = new TimeArg(TimeArgDirection.End, mn.Timestamp);
                List<Asset> newAssets = new List<Asset>();
                foreach (var asset in output.OfType<Equity>().Where(x => x.SecurityDefinition.InstrumentId == mn.InstrumentId1))
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
                        var evt = new Events.Derecognition(asset, mn, mn.Timestamp, count - newCount, price);
                        asset.AddEvent(evt);
                    }
                    // Add converted equity
                    if (mn.Amount2 > 0)
                    {
                        var definition = Data.Definitions.Instruments.First(x => x.InstrumentId == mn.InstrumentId2);
                        decimal newCount = mn.Amount2 * count;
                        var newAsset = new Equity(asset, definition, mn, newCount, newPrice2);
                        newAssets.Add(newAsset);
                    }
                    // Add spun off equity
                    if (mn.Amount3 > 0)
                    {
                        var definition = Data.Definitions.Instruments.First(x => x.InstrumentId == mn.InstrumentId3);
                        decimal newCount = mn.Amount3 * count;
                        var newAsset = new Equity(asset, definition, mn, newCount, newPrice3);
                        newAssets.Add(newAsset);
                    }
                }
                newAssets.ForEach(x => output.Add(x));
                manual.Remove(mn);
            }
        }
    }
}
