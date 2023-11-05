using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Budziszewski.Venture.Data
{
    public enum PaymentType { Undefined, ShareCapital, OtherCapital, Tax }

    public enum TransactionType { Undefined, Buy, Sell, Cash }

    public class Transaction : DataPoint
    {
        public string UniqueId { get { return $"{TradeDate:yyyyMMddTHHmmss}_{InstrumentId}_{TransactionType}_{Index}"; } }

        public int Index { get; private set; } = 0;

        public TransactionType TransactionType { get; private set; } = TransactionType.Undefined;

        public string InstrumentId { get; private set; } = "";

        public DateTime TradeDate { get; private set; } = DateTime.MinValue;

        public DateTime SettlementDate { get; private set; } = DateTime.MinValue;

        public decimal Count { get; private set; } = 0.0M;

        public string Currency { get; private set; } = "PLN";

        public decimal NominalAmount { get; private set; } = 0.0M;

        public decimal Price { get; private set; } = 0.0M;

        public decimal Fee { get; private set; } = 0.0M;

        public string AccountSrc { get; private set; } = "";

        public string AccountDst { get; private set; } = "";

        public string PortfolioSrc { get; private set; } = "";

        public string PortfolioDst { get; private set; } = "";

        public ValuationClass ValuationClass { get; private set; } = ValuationClass.Undefined;

        public decimal FXRate { get; private set; } = 1;

        public PaymentType PaymentType { get; private set; } = PaymentType.Undefined;

        public override void FromCSV(string[] headers, string[] line, int index)
        {
            Index = index + 1;
            for (int i = 0; i < Math.Min(headers.Length, line.Length); i++)
            {
                if (headers[i] == "transactiontype") TransactionType = ConvertToEnum<TransactionType>(line[i]);
                if (headers[i] == "instrumentid") InstrumentId = line[i];
                if (headers[i] == "tradedate") TradeDate = ConvertToDateTime(line[i]);
                if (headers[i] == "settlementdate") SettlementDate = ConvertToDateTime(line[i]);
                if (headers[i] == "count") Count = ConvertToDecimal(line[i]);
                if (headers[i] == "currency") Currency = line[i];
                if (headers[i] == "nominalamount") NominalAmount = ConvertToDecimal(line[i]);
                if (headers[i] == "price") Price = ConvertToDecimal(line[i]);
                if (headers[i] == "fee") Fee = ConvertToDecimal(line[i]);
                if (headers[i] == "accountsrc") AccountSrc = line[i];
                if (headers[i] == "accountdst") AccountDst = line[i];
                if (headers[i] == "portfoliosrc") PortfolioSrc = line[i];
                if (headers[i] == "portfoliodst") PortfolioDst = line[i];
                if (headers[i] == "valuationclass")
                {
                    if (line[i].ToLower() == "afs" || line[i].ToLower() == "availableforsale") ValuationClass = ValuationClass.AvailableForSale;
                    if (line[i].ToLower() == "trd" || line[i].ToLower() == "trading") ValuationClass = ValuationClass.Trading;
                    if (line[i].ToLower() == "htm" || line[i].ToLower() == "held to maturity") ValuationClass = ValuationClass.HeldToMaturity;
                };
                if (headers[i] == "fxrate")
                {
                    if (line[i] == "") FXRate = 1;
                    else FXRate = ConvertToDecimal(line[i]);
                }
                if (headers[i] == "paymenttype") PaymentType = ConvertToEnum<PaymentType>(line[i]);
                if (headers[i] == "active") Active = ConvertToBool(line[i]);
            }
        }
    }
}
