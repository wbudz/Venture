using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Venture
{
    public class PriceDefinition : Definition
    {
        public string UniqueId { get { return $"{InstrumentUniqueId}_{Timestamp:yyyyMMdd}"; } }

        public AssetType AssetType { get; private set; } = AssetType.Undefined;

        public string AssetId { get; private set; } = "";

        public string InstrumentUniqueId
        {
            get
            {
                return AssetType + "_" + AssetId;
            }
        }

        public DateTime Timestamp { get; private set; } = DateTime.MinValue;

        public long Volume { get; private set; } = 0;

        public decimal Open { get; private set; }

        public decimal High { get; private set; }

        public decimal Low { get; private set; }

        public decimal Close { get; private set; }

        public decimal Value { get { return Close; } }

        public PriceDefinition(Dictionary<string, string> data) : base(data)
        {
            AssetType = ConvertToEnum<AssetType>(data["assettype"]);
            AssetId = data["assetid"];
            Timestamp = ConvertToDateTime(data["timestamp"]);
            if (data.ContainsKey("volume"))
                Volume = ConvertToLong(data["volume"]);
            if (data.ContainsKey("open"))
                Open = ConvertToDecimal(data["open"]);
            if (data.ContainsKey("high"))
                High = ConvertToDecimal(data["high"]);
            if (data.ContainsKey("low"))
                Low = ConvertToDecimal(data["low"]);
            Close = ConvertToDecimal(data["close"]);
        }

        public override string ToString()
        {
            return $"Price: {UniqueId}";
        }
    }
}
