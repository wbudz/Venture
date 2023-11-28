using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Venture.Data
{
    // InstrumentId	RecordDate	ExDate	PaymentDate	PaymentPerShare

    public class Dividend : DataPoint
    {
        public string UniqueId { get { return $"{PaymentDate:yyyyMMddTHHmmss}_{InstrumentId}"; } }

        public string InstrumentId { get; private set; } = "";

        public DateTime RecordDate { get; private set; } = DateTime.MinValue;

        public DateTime ExDate { get; private set; } = DateTime.MinValue;

        public DateTime PaymentDate { get; private set; } = DateTime.MinValue;

        public decimal PaymentPerShare { get; private set; } = 0;

        public override void FromCSV(string[] headers, string[] line, int index)
        {
            for (int i = 0; i < Math.Min(headers.Length, line.Length); i++)
            {
                if (headers[i] == "instrumentid") InstrumentId = line[i];
                if (headers[i] == "recorddate") RecordDate = ConvertToDateTime(line[i]);
                if (headers[i] == "exdate") ExDate = ConvertToDateTime(line[i]);
                if (headers[i] == "paymentdate") PaymentDate = ConvertToDateTime(line[i]);
                if (headers[i] == "paymentpershare") PaymentPerShare = ConvertToDecimal(line[i]);
                if (headers[i] == "active") Active = ConvertToBool(line[i]);
            }
        }
    }
}
