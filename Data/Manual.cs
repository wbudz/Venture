using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Venture.Data
{
    public enum ManualAdjustmentType { CouponTaxAdjustment, DividendTaxAdjustment }

    public class Manual: DataPoint
    {
        public string UniqueId { get { return $"{AdjustmentType}: {Timestamp:yyyyMMddTHHmmss}_{InstrumentId}"; } }

        public ManualAdjustmentType AdjustmentType { get; private set; }

        public DateTime Timestamp { get; private set; } = DateTime.MinValue;

        public string InstrumentId { get; private set; } = "";

        public decimal Amount { get; private set; } = 0;

        public override void FromCSV(string[] headers, string[] line, int index)
        {
            for (int i = 0; i < Math.Min(headers.Length, line.Length); i++)
            {
                if (headers[i] == "adjustmenttype") AdjustmentType = ConvertToEnum<ManualAdjustmentType>(line[i]);
                if (headers[i] == "timestamp") Timestamp = ConvertToDateTime(line[i]);
                if (headers[i] == "instrumentid") InstrumentId = line[i];
                if (headers[i] == "amount") Amount = ConvertToDecimal(line[i]);
                if (headers[i] == "active") Active = ConvertToBool(line[i]);
            }
        }

        public override string ToString()
        {
            return $"Data.Manual: {AdjustmentType}: {InstrumentId} @{Timestamp}";
        }
    }
}
