using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Venture.Data
{
    public enum CouponType { Undefined, Fixed, Floating };

    public enum EndOfMonthConvention { Undefined, DontAlign, Align };

    public class Instrument : DataPoint
    {
        public string UniqueId { get { return $"{InstrumentType}_{InstrumentId}"; } }

        public string ISIN { get; private set; } = "";

        public string Ticker { get; private set; } = "";

        public string InstrumentId { get; private set; } = "";

        public AssetType InstrumentType { get; private set; } = AssetType.Undefined;

        public string Name { get; private set; } = "";

        public string Issuer { get; private set; } = "";

        public DateTime Maturity { get; private set; } = DateTime.MinValue;

        public CouponType CouponType { get; private set; } = CouponType.Undefined;

        public decimal CouponRate { get; private set; } = 0;

        public int CouponFreq { get; private set; } = 0;

        public int UnitPrice { get; private set; } = 0;

        public string Currency { get; private set; } = "";

        public Financial.DayCountConvention DayCountConvention { get; private set; } = Financial.DayCountConvention.Actual_Actual_Excel;

        public EndOfMonthConvention EndOfMonthConvention { get; private set; } = EndOfMonthConvention.Undefined;

        public override void FromCSV(string[] headers, string[] line, int index)
        {
            for (int i = 0; i < Math.Min(headers.Length, line.Length); i++)
            {
                if (headers[i] == "isin") ISIN = line[i];
                if (headers[i] == "ticker") Ticker = line[i];
                if (headers[i] == "instrumentid") InstrumentId = line[i];
                if (headers[i] == "instrumenttype") InstrumentType = ConvertToEnum<AssetType>(line[i]);
                if (headers[i] == "name") Name = line[i];
                if (headers[i] == "issuer") Issuer = line[i];
                if (headers[i] == "maturity") Maturity = ConvertToDateTime(line[i]);
                if (headers[i] == "coupontype") CouponType = ConvertToEnum<CouponType>(line[i]);
                if (headers[i] == "couponrate") CouponRate = ConvertToDecimal(line[i]);
                if (headers[i] == "couponfreq") CouponFreq = ConvertToInt(line[i]);
                if (headers[i] == "unitprice") UnitPrice = ConvertToInt(line[i]);
                if (headers[i] == "currency") Currency = line[i];
                if (headers[i] == "daycountconvention")
                {
                    if (line[i].ToLower().Contains("act/365")) DayCountConvention = Financial.DayCountConvention.Actual_365;
                    else if (line[i].ToLower().Contains("act/360")) DayCountConvention = Financial.DayCountConvention.Actual_360;
                    else if (line[i].ToLower().Contains("30/360") && line[i].ToLower().Contains("eur")) DayCountConvention = Financial.DayCountConvention.European_30_360;
                    else if (line[i].ToLower().Contains("30/360") && line[i].ToLower().Contains("us")) DayCountConvention = Financial.DayCountConvention.US_30_360;
                    else DayCountConvention = Financial.DayCountConvention.Actual_Actual_Excel;
                }
                if (headers[i] == "endofmonthconvention") EndOfMonthConvention = ConvertToEnum<EndOfMonthConvention>(line[i]);
                if (headers[i] == "active") Active = ConvertToBool(line[i]);
            }
        }

        public override string ToString()
        {
            return $"Data.Instrument: {InstrumentId} ({InstrumentType})";
        }
    }
}
