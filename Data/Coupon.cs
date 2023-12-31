﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Venture.Data
{
    public class Coupon : DataPoint
    {
        public string UniqueId { get { return $"{Timestamp:yyyyMMddTHHmmss}_{InstrumentId}"; } }

        public DateTime Timestamp { get; private set; } = DateTime.MinValue;

        public string InstrumentId { get; private set; } = "";

        public decimal CouponRate { get; private set; } = 0;

        public override void FromCSV(string[] headers, string[] line, int index)
        {
            for (int i = 0; i < Math.Min(headers.Length, line.Length); i++)
            {
                if (headers[i] == "timestamp") Timestamp = ConvertToDateTime(line[i]);
                if (headers[i] == "instrumentid") InstrumentId = line[i];
                if (headers[i] == "rate") CouponRate = ConvertToDecimal(line[i]);
                if (headers[i] == "active") Active = ConvertToBool(line[i]);
            }
        }

        public override string ToString()
        {
            return $"Data.Coupon: {InstrumentId} @{Timestamp}";
        }
    }
}
