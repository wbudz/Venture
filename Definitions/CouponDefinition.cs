using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Venture
{
    public class CouponDefinition : Definition
    {
        public string UniqueId { get { return $"{InstrumentUniqueId}_{Timestamp:yyyyMMdd}"; } }

        public DateTime Timestamp { get; private set; } = DateTime.MinValue;

        public AssetType AssetType { get; private set; } = AssetType.Undefined;

        public string AssetId { get; private set; } = "";

        public string InstrumentUniqueId
        {
            get
            {
                return AssetType + "_" + AssetId;
            }
        }

        public decimal CouponRate { get; private set; } = 0;

        public CouponDefinition(Dictionary<string, string> data) : base(data)
        {
            Timestamp = ConvertToDateTime(data["timestamp"]);
            AssetType = ConvertToEnum<AssetType>(data["assettype"]);
            AssetId = data["assetid"];
            CouponRate = ConvertToDecimal(data["rate"]);
        }

        public override string ToString()
        {
            return $"Coupon: {UniqueId}";
        }
    }
}
