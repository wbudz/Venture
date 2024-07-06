using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Venture
{

    public static class Definitions
    {
        public static ObservableCollection<PortfolioDefinition> Portfolios = new ObservableCollection<PortfolioDefinition>();
        public static ObservableCollection<PriceDefinition> Prices = new ObservableCollection<PriceDefinition>();
        public static ObservableCollection<InstrumentDefinition> Instruments = new ObservableCollection<InstrumentDefinition>();
        public static ObservableCollection<TransactionDefinition> Transactions = new ObservableCollection<TransactionDefinition>();
        public static ObservableCollection<DividendDefinition> Dividends = new ObservableCollection<DividendDefinition>();
        public static ObservableCollection<CouponDefinition> Coupons = new ObservableCollection<CouponDefinition>();
        public static ObservableCollection<ManualEventDefinition> ManualEvents = new ObservableCollection<ManualEventDefinition>();

        public static void Load()
        {
            Portfolios.Clear();
            Prices.Clear();
            Instruments.Clear();
            Transactions.Clear();
            Dividends.Clear();
            Coupons.Clear();
            ManualEvents.Clear();

            // Portfolios
            var portfolios = new List<PortfolioDefinition>();
            CSV csv = new CSV(Properties.Settings.Default.PortfoliosSource);
            csv.Read();
            foreach (var item in csv.Interpret())
            {
                var newItem = new PortfolioDefinition(item);
                if (newItem.Active)
                {
                    portfolios.Add(newItem);
                }
            }
            portfolios = portfolios.OrderBy(x => x.UniqueId).ToList();
            portfolios.ForEach(x => Portfolios.Add(x));

            // Prices
            var prices = new List<PriceDefinition>();
            csv = new CSV(Properties.Settings.Default.PricesSource);
            csv.Read();
            foreach (var item in csv.Interpret())
            {
                var newItem = new PriceDefinition(item);
                if (newItem.Active)
                {
                    prices.Add(newItem);
                }
            }
            prices = prices.OrderBy(x => x.InstrumentUniqueId).ThenBy(x => x.Timestamp).ToList();
            prices.ForEach(x => Prices.Add(x));

            // InstrumentDefinitions
            var instruments = new List<InstrumentDefinition>();
            csv = new CSV(Properties.Settings.Default.InstrumentsSource);
            csv.Read();
            foreach (var item in csv.Interpret())
            {
                var newItem = new InstrumentDefinition(item);
                if (newItem.Active)
                {
                    instruments.Add(newItem);
                }
            }
            instruments = instruments.OrderBy(x => x.UniqueId).ToList();
            instruments.ForEach(x => Instruments.Add(x));

            // TransactionDefinitions
            var transactions = new List<TransactionDefinition>();
            csv = new CSV(Properties.Settings.Default.TransactionsSource);
            csv.Read();
            foreach (var item in csv.Interpret())
            {
                TransactionDefinition newItem;
                switch (item["transactiontype"].ToLower().Trim())
                {
                    case "buy": newItem = new BuyTransactionDefinition(item); break;
                    case "sell": newItem = new SellTransactionDefinition(item); break;
                    case "pay": newItem = new PayTransactionDefinition(item); break;
                    case "transfer": newItem = new TransferTransactionDefinition(item); break;
                    default:
                        continue;
                }
                if (newItem.Active)
                {
                    transactions.Add(newItem);
                }
            }
            transactions = transactions.OrderBy(x => x.Timestamp).ThenBy(x => x.Index).ToList();
            CheckTransactionsPortfolios(transactions);
            CheckTransactionsOrder(transactions);
            transactions.ForEach(x => Transactions.Add(x));

            // DividendDefinitions
            var dividends = new List<DividendDefinition>();
            csv = new CSV(Properties.Settings.Default.DividendsSource);
            csv.Read();
            foreach (var item in csv.Interpret())
            {
                var newItem = new DividendDefinition(item);
                if (newItem.Active)
                {
                    dividends.Add(newItem);
                }
            }
            dividends = dividends.OrderBy(x => x.InstrumentUniqueId).ThenBy(x => x.PaymentDate).ToList();
            dividends.ForEach(x => Dividends.Add(x));

            // CouponDefinitions
            var coupons = new List<CouponDefinition>();
            csv = new CSV(Properties.Settings.Default.CouponsSource);
            csv.Read();
            foreach (var item in csv.Interpret())
            {
                var newItem = new CouponDefinition(item);
                if (newItem.Active)
                {
                    coupons.Add(newItem);
                }
            }
            coupons = coupons.OrderBy(x => x.InstrumentUniqueId).ThenBy(x => x.Timestamp).ToList();
            coupons.ForEach(x => Coupons.Add(x));

            // ManualEventsDefinitions
            var manuals = new List<ManualEventDefinition>();
            csv = new CSV(Properties.Settings.Default.ManualSource);
            csv.Read();
            foreach (var item in csv.Interpret())
            {
                ManualEventDefinition newItem;
                switch (item["adjustmenttype"].ToLower().Trim())
                {
                    case "couponamountadjustment": newItem = new CouponAmountAdjustmentEventDefinition(item); break;
                    case "dividendamountadjustment": newItem = new DividendAmountAdjustmentEventDefinition(item); break;
                    case "redemptiontaxadjustment": newItem = new RedemptionTaxAdjustmentEventDefinition(item); break;
                    case "coupontaxadjustment": newItem = new CouponTaxAdjustmentEventDefinition(item); break;
                    case "dividendtaxadjustment": newItem = new DividendTaxAdjustmentEventDefinition(item); break;
                    case "incometaxadjustment": newItem = new IncomeTaxAdjustmentEventDefinition(item); break;
                    case "equityspinoff": newItem = new EquitySpinOffEventDefinition(item); break;
                    case "prematureredemption": newItem = new PrematureRedemptionEventDefinition(item); break;
                    case "equityredemption": newItem = new EquityRedemptionEventDefinition(item); break;
                    case "additionalpremium": newItem = new AdditionalPremiumEventDefinition(item); break;
                    case "additionalcharge": newItem = new AdditionalChargeEventDefinition(item); break;
                    default:
                        throw new Exception($"Incorrect manual event definition: {item}.");
                }
                if (newItem.Active)
                {
                    manuals.Add(newItem);
                }
            }
            manuals = manuals.OrderBy(x => x.Timestamp).ToList();
            CheckManualReferences(manuals, Transactions, Instruments);
            manuals.ForEach(x => ManualEvents.Add(x));
        }

        private static void CheckTransactionsOrder(IEnumerable<TransactionDefinition> transactions)
        {
            var array = transactions.ToArray();
            for (int i = 1; i < array.Length; i++)
            {
                if (array[i].Index <= array[i - 1].Index) throw new Exception($"Error ordering transactions: {array[i - 1]}, {array[i]}.");
                if (array[i].Timestamp < array[i - 1].Timestamp) throw new Exception($"Error ordering transactions: {array[i - 1]}, {array[i]}.");
            }
        }

        private static void CheckTransactionsPortfolios(IEnumerable<TransactionDefinition> transactions)
        {
            foreach (var t in transactions)
            {
                if (t.PortfolioSrc != "" && !Definitions.Portfolios.Any(x => x.UniqueId == t.PortfolioSrc)) throw new Exception($"Incorrect source portfolio specification: {t}.");
                if (t.PortfolioDst != "" && !Definitions.Portfolios.Any(x => x.UniqueId == t.PortfolioDst)) throw new Exception($"Incorrect destination portfolio specification: {t}.");
            }
        }

        private static void CheckManualReferences(IEnumerable<ManualEventDefinition> manuals, IEnumerable<TransactionDefinition> transactions, IEnumerable<InstrumentDefinition> instruments)
        {
            foreach (var mn in manuals)
            {
                if (mn is FlowAmountAdjustmentEventDefinition faa)
                {
                    string instrumentType;
                    string instrumentId;
                    int transactionIndex;
                    try
                    {
                        instrumentType = faa.AssetUniqueId.Split('_')[0];
                        instrumentId = faa.AssetUniqueId.Split('_')[1];
                        transactionIndex = Int32.Parse(faa.AssetUniqueId.Split('_')[2]);
                    }
                    catch
                    {
                        throw new Exception($"Error in manual event definition: {faa}.");
                    }
                    var transaction = transactions.SingleOrDefault(x => x.AssetType.ToString() == instrumentType && x.AssetId == instrumentId && x.Index == transactionIndex);
                    if (transaction == null)
                    {
                        throw new Exception($"Transaction not found for manual event definition: {faa}.");
                    }
                }
                if (mn is TaxAmountAdjustmentEventDefinition taa)
                {
                    string instrumentType;
                    string instrumentId;
                    int transactionIndex;
                    try
                    {
                        instrumentType = taa.AssetUniqueId.Split('_')[0];
                        instrumentId = taa.AssetUniqueId.Split('_')[1];
                        transactionIndex = Int32.Parse(taa.AssetUniqueId.Split('_')[2]);
                    }
                    catch
                    {
                        throw new Exception($"Error in manual event definition: {taa}.");
                    }
                    var transaction = transactions.SingleOrDefault(x => x.AssetType.ToString() == instrumentType && x.AssetId == instrumentId && x.Index == transactionIndex);
                    if (transaction == null)
                    {
                        throw new Exception($"Transaction not found for manual event definition: {taa}.");
                    }
                }
                if (mn is EquitySpinOffEventDefinition eso)
                {
                    string instrumentType1;
                    string instrumentId1;
                    string instrumentType2;
                    string instrumentId2;
                    string instrumentType3;
                    string instrumentId3;
                    try
                    {
                        instrumentType1 = eso.OriginalInstrumentUniqueId.Split('_')[0];
                        instrumentId1 = eso.OriginalInstrumentUniqueId.Split('_')[1];
                        if (eso.OriginalInstrumentUniqueId.Split('_').Length > 2) throw new Exception("Manual event definition too long.");
                        instrumentType2 = eso.ConvertedInstrumentUniqueId.Split('_')[0];
                        instrumentId2 = eso.ConvertedInstrumentUniqueId.Split('_')[1];
                        if (eso.ConvertedInstrumentUniqueId.Split('_').Length > 2) throw new Exception("Manual event definition too long.");
                        instrumentType3 = eso.SpunOffInstrumentUniqueId.Split('_')[0];
                        instrumentId3 = eso.SpunOffInstrumentUniqueId.Split('_')[1];
                        if (eso.SpunOffInstrumentUniqueId.Split('_').Length > 2) throw new Exception("Manual event definition too long.");
                    }
                    catch
                    {
                        throw new Exception($"Error in manual event definition: {eso}.");
                    }
                    var instrument1 = instruments.SingleOrDefault(x => x.AssetType.ToString() == instrumentType1 && x.AssetId == instrumentId1);
                    if (instrument1 == null)
                    {
                        throw new Exception($"Instrument not found for manual event definition: {eso}.");
                    }
                    var instrument2 = instruments.SingleOrDefault(x => x.AssetType.ToString() == instrumentType2 && x.AssetId == instrumentId2);
                    if (instrument2 == null)
                    {
                        throw new Exception($"Instrument not found for manual event definition: {eso}.");
                    }
                    var instrument3 = instruments.SingleOrDefault(x => x.AssetType.ToString() == instrumentType3 && x.AssetId == instrumentId3);
                    if (instrument3 == null)
                    {
                        throw new Exception($"Instrument not found for manual event definition: {eso}.");
                    }
                }
                if (mn is PrematureRedemptionEventDefinition pr)
                {
                    string instrumentType;
                    string instrumentId;
                    try
                    {
                        instrumentType = pr.InstrumentUniqueId.Split('_')[0];
                        instrumentId = pr.InstrumentUniqueId.Split('_')[1];
                        if (pr.InstrumentUniqueId.Split('_').Length > 2) throw new Exception("Manual event definition too long.");
                    }
                    catch
                    {
                        throw new Exception($"Error in manual event definition: {pr}.");
                    }
                    var instrument = instruments.SingleOrDefault(x => x.AssetType.ToString() == instrumentType && x.AssetId == instrumentId);
                    if (instrument == null)
                    {
                        throw new Exception($"Instrument not found for manual event definition: {pr}.");
                    }
                }
                if (mn is AdditionalPremiumEventDefinition ape)
                {
                    if (String.IsNullOrEmpty(ape.Portfolio))
                    {
                        throw new Exception($"Incorrect additional premium/charge portfolio definition in Text1 field: {ape}.");
                    }
                    if (!Definitions.Portfolios.Any(x => x.UniqueId == ape.Portfolio))
                    {
                        throw new Exception($"Incorrect source portfolio specification in Text1 field: {ape}.");
                    }
                    if (String.IsNullOrEmpty(ape.Currency))
                    {
                        throw new Exception($"Incorrect additional premium/charge currency in Text2 field: {ape}.");
                    }
                    if (String.IsNullOrEmpty(ape.Description))
                    {
                        throw new Exception($"Incorrect additional premium/charge description in Text3 field: {ape}.");
                    }
                }
                if (mn is AdditionalChargeEventDefinition ace)
                {
                    if (String.IsNullOrEmpty(ace.Portfolio))
                    {
                        throw new Exception($"Incorrect additional premium/charge portfolio definition in Text1 field: {ace}.");
                    }
                    if (!Definitions.Portfolios.Any(x => x.UniqueId == ace.Portfolio))
                    {
                        throw new Exception($"Incorrect source portfolio specification in Text1 field: {ace}.");
                    }
                    if (String.IsNullOrEmpty(ace.Currency))
                    {
                        throw new Exception($"Incorrect additional premium/charge currency in Text2 field: {ace}.");
                    }
                    if (String.IsNullOrEmpty(ace.Description))
                    {
                        throw new Exception($"Incorrect additional premium/charge description in Text3 field: {ace}.");
                    }
                }
            }
        }
    }
}
