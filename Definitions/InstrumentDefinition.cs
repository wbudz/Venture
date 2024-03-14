using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Venture
{
    public enum CouponType { Undefined, Fixed, Floating };

    public enum EndOfMonthConvention { Undefined, DontAlign, Align };

    public class InstrumentDefinition : Definition
    {
        public string UniqueId { get { return $"{AssetType}_{AssetId}"; } }

        public string ISIN { get; private set; } = "";

        public string Ticker { get; private set; } = "";

        public AssetType AssetType { get; private set; } = AssetType.Undefined;

        public string AssetId { get; private set; } = "";

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

        public InstrumentDefinition(Dictionary<string, string> data) : base(data)
        {
            ISIN = data["isin"];
            Ticker = data["ticker"];
            AssetId = data["assetid"];
            AssetType = ConvertToEnum<AssetType>(data["assettype"]);
            Name = data["name"];
            Issuer = data["issuer"];
            Maturity = ConvertToDateTime(data["maturity"]);
            CouponType = ConvertToEnum<CouponType>(data["coupontype"]);
            CouponRate = ConvertToDecimal(data["couponrate"]);
            CouponFreq = ConvertToInt(data["couponfreq"]);
            UnitPrice = ConvertToInt(data["unitprice"]);
            Currency = data["currency"];
            string dcc = data["daycountconvention"];
            if (dcc.ToLower().Contains("act/365")) DayCountConvention = Financial.DayCountConvention.Actual_365;
            else if (dcc.ToLower().Contains("act/360")) DayCountConvention = Financial.DayCountConvention.Actual_360;
            else if (dcc.ToLower().Contains("30/360") && dcc.ToLower().Contains("eur")) DayCountConvention = Financial.DayCountConvention.European_30_360;
            else if (dcc.ToLower().Contains("30/360") && dcc.ToLower().Contains("us")) DayCountConvention = Financial.DayCountConvention.US_30_360;
            else DayCountConvention = Financial.DayCountConvention.Actual_Actual_Excel;
            EndOfMonthConvention = ConvertToEnum<EndOfMonthConvention>(data["endofmonthconvention"]);
        }

        public override string ToString()
        {
            return $"Instrument: {UniqueId}";
        }
    }
}
