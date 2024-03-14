using Financial;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Xml.Linq;
using static Financial.Calendar;

namespace Venture
{
    // InstrumentId	RecordDate	ExDate	PaymentDate	PaymentPerShare

    public class DividendDefinition : Definition
    {
        public string UniqueId { get { return $"{InstrumentUniqueId}_{PaymentDate:yyyyMMdd}"; } }

        public AssetType AssetType { get; private set; } = AssetType.Undefined;

        public string AssetId { get; private set; } = "";

        public string InstrumentUniqueId
        {
            get
            {
                return AssetType + "_" + AssetId;
            }
        }

        public DateTime RecordDate { get; private set; } = DateTime.MinValue;

        public DateTime ExDate { get; private set; } = DateTime.MinValue;

        public DateTime PaymentDate { get; private set; } = DateTime.MinValue;

        public decimal PaymentPerShare { get; private set; } = 0;

        public string Currency { get; private set; } = "PLN";

        public decimal FXRate { get; private set; } = 1;

        public DividendDefinition(Dictionary<string, string> data) : base(data)
        {
            AssetType = ConvertToEnum<AssetType>(data["assettype"]);
            AssetId = data["assetid"];
            RecordDate = ConvertToDateTime(data["recorddate"]);
            ExDate = ConvertToDateTime(data["exdate"]);
            PaymentDate = ConvertToDateTime(data["paymentdate"]);
            PaymentPerShare = ConvertToDecimal(data["paymentpershare"]);
            Currency = data["currency"];
            FXRate = GetFXRateFromData(data["fxrate"]);
        }            

        public override string ToString()
        {
            return $"Data.Dividend: {AssetId} @{ExDate:yyyy-MM-dd}/{PaymentDate:yyyy-MM-dd}: {PaymentPerShare:N2}";
        }
    }
}
