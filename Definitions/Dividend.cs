using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Venture.Assets;
using static Financial.Calendar;

namespace Venture.Data
{
    // InstrumentId	RecordDate	ExDate	PaymentDate	PaymentPerShare

    public class Dividend : DataPoint
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

        public override void FromCSV(string[] headers, string[] line, int index)
        {
            for (int i = 0; i < Math.Min(headers.Length, line.Length); i++)
            {
                if (headers[i] == "assettype") AssetType = ConvertToEnum<AssetType>(line[i]);
                if (headers[i] == "assetid") AssetId = line[i];
                if (headers[i] == "recorddate") RecordDate = ConvertToDateTime(line[i]);
                if (headers[i] == "exdate") ExDate = ConvertToDateTime(line[i]);
                if (headers[i] == "paymentdate") PaymentDate = ConvertToDateTime(line[i]);
                if (headers[i] == "paymentpershare") PaymentPerShare = ConvertToDecimal(line[i]);
                if (headers[i] == "currency") Currency = line[i];
                if (headers[i] == "fxrate") FXRate = ConvertToDecimal(line[i]);
                if (headers[i] == "active") Active = ConvertToBool(line[i]);
            }
        }

        public override string ToString()
        {
            return $"Data.Dividend: {AssetId} @{ExDate:yyyy-MM-dd}/{PaymentDate:yyyy-MM-dd}: {PaymentPerShare:N2}";
        }
    }
}
