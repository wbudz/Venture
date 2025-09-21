using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Venture
{
    public abstract class Definition
    {
        protected static readonly CultureInfo cultureIntegerWithDotSeparator = new CultureInfo("") { NumberFormat = new NumberFormatInfo() { NumberDecimalSeparator = "." } };
        protected static readonly CultureInfo cultureIntegerWithCommaSeparator = new CultureInfo("") { NumberFormat = new NumberFormatInfo() { NumberDecimalSeparator = "," } };

        public bool Active { get; protected set; } = true;

        public Definition(Dictionary<string, string> data)
        {
            if (data.ContainsKey("active"))
                Active = ConvertToBool(data["active"]);
        }

        protected static decimal GetFXRateFromData(string data)
        {
            if (String.IsNullOrEmpty(data))
                return 1;
            else
                return ConvertToDecimal(data);
        }

        protected static ValuationClass GetValuationClassFromData(string data)
        {
            if (data.ToLower().Trim() == "afs" || data.Replace(" ", "").ToLower().Trim() == "availableforsale") return ValuationClass.AvailableForSale;
            else if (data.ToLower().Trim() == "trd" || data.Replace(" ", "").ToLower().Trim() == "trading") return ValuationClass.Trading;
            else if (data.ToLower().Trim() == "htm" || data.Replace(" ", "").ToLower().Trim() == "heldtomaturity") return ValuationClass.HeldToMaturity;
            else return ValuationClass.Undefined;
        }

        protected static DateTime ConvertToDateTime(string text)
        {
            DateTime result;
            if (DateTime.TryParseExact(text, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces, out result)) return result;
            if (DateTime.TryParseExact(text, "yyyyMMdd HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces, out result)) return result;
            if (DateTime.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out result)) return result;
            if (DateTime.TryParseExact(text, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out result)) return result;
            else return DateTime.MinValue;
        }

        protected static long ConvertToLong(string text)
        {
            long result;
            if (Int64.TryParse(text, NumberStyles.Integer, cultureIntegerWithDotSeparator, out result)) return result;
            if (Int64.TryParse(text, NumberStyles.Integer, cultureIntegerWithCommaSeparator, out result)) return result;
            if (Int64.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out result)) return result;
            else return 0;
        }

        protected static int ConvertToInt(string text) => (int)ConvertToLong(text);

        protected static decimal ConvertToDecimal(string text)
        {
            decimal result;
            decimal factor = 1;
            if (text.Contains("%"))
            {
                text = text.Replace("%", "");
                factor = 100;
            }
            text = text.Replace(" ", "");
            if (Decimal.TryParse(text, NumberStyles.Float, cultureIntegerWithDotSeparator, out result)) return result / factor;
            if (Decimal.TryParse(text, NumberStyles.Float, cultureIntegerWithCommaSeparator, out result)) return result / factor;
            if (Decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out result)) return result / factor;
            else return 0;
        }

        protected static bool ConvertToBool(string text)
        {
            bool result;
            if (text.Trim() == "1") return true;
            if (text.Trim() == "0") return false;
            if (Boolean.TryParse(text, out result)) return result;
            else return false;
        }

        protected static T ConvertToEnum<T>(string text) where T : Enum
        {
            object? result;
            if (Enum.TryParse(typeof(T), text, true, out result))
            {
                return (T)result;
            }
            else if (Enum.TryParse(typeof(T), Enum.GetNames(typeof(T))[0], true, out result))
            {
                return (T)result;
            }
            else throw new Exception($"Cannot convert \"{text}\" to enum of type:{typeof(T)}.");
        }

    }
}
