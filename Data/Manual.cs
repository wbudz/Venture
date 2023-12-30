using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Venture.Data
{
    public enum ManualAdjustmentType { CouponTaxAdjustment, DividendTaxAdjustment, EquitySpinOff }

    public class Manual: DataPoint
    {
        public string UniqueId { get { return $"{AdjustmentType}: {Timestamp:yyyyMMddTHHmmss}_{InstrumentId1}"; } }

        public ManualAdjustmentType AdjustmentType { get; private set; }

        public DateTime Timestamp { get; private set; } = DateTime.MinValue;

        public string InstrumentId1 { get; private set; } = "";

        public string InstrumentId2 { get; private set; } = "";

        public string InstrumentId3 { get; private set; } = "";

        public decimal Amount1 { get; private set; } = 0;

        public decimal Amount2 { get; private set; } = 0;

        public decimal Amount3 { get; private set; } = 0;

        public override void FromCSV(string[] headers, string[] line, int index)
        {
            for (int i = 0; i < Math.Min(headers.Length, line.Length); i++)
            {
                if (headers[i] == "adjustmenttype") AdjustmentType = ConvertToEnum<ManualAdjustmentType>(line[i]);
                if (headers[i] == "timestamp") Timestamp = ConvertToDateTime(line[i]);
                if (headers[i] == "instrumentid1") InstrumentId1 = line[i];
                if (headers[i] == "instrumentid2") InstrumentId2 = line[i];
                if (headers[i] == "instrumentid3") InstrumentId3 = line[i];
                if (headers[i] == "amount1") Amount1 = ConvertToDecimal(line[i]);
                if (headers[i] == "amount2") Amount2 = ConvertToDecimal(line[i]);
                if (headers[i] == "amount3") Amount3 = ConvertToDecimal(line[i]);
                if (headers[i] == "active") Active = ConvertToBool(line[i]);
            }
        }

        public override string ToString()
        {
            return $"Data.Manual: {AdjustmentType}: {InstrumentId1} @{Timestamp}";
        }
    }
}
