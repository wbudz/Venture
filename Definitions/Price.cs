using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Venture.Data
{
    public class Price : DataPoint
    {
        public string UniqueId { get { return $"{InstrumentUniqueId}_{Timestamp:yyyyMMddTHHmmss}"; } }

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

        public override void FromCSV(string[] headers, string[] line, int index)
        {
            for (int i = 0; i < Math.Min(headers.Length, line.Length); i++)
            {
                if (headers[i] == "assettype") AssetType = ConvertToEnum<AssetType>(line[i]);
                if (headers[i] == "assetid") AssetId = line[i];
                if (headers[i] == "timestamp") Timestamp = ConvertToDateTime(line[i]);
                if (headers[i] == "volume") Volume = ConvertToLong(line[i]);
                if (headers[i] == "open") Open = ConvertToDecimal(line[i]);
                if (headers[i] == "high") High = ConvertToDecimal(line[i]);
                if (headers[i] == "low") Low = ConvertToDecimal(line[i]);
                if (headers[i] == "close") Close = ConvertToDecimal(line[i]);
                if (headers[i] == "active") Active = ConvertToBool(line[i]);
            }
        }

        public override string ToString()
        {
            return $"Price: {UniqueId}";
        }
    }
}
