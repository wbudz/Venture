using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Budziszewski.Venture
{
    public enum ValuationClass { Undefined, Trading, AvailableForSale, HeldToMaturity }

    public enum InstrumentType { Undefined, Cash, Equity, Bond, ETF, Fund, Futures }

    public static class Common
    {
        public static List<Assets.Asset> Assets = new List<Assets.Asset>();

        public static DateTime CurrentDate = Financial.Calendar.GetEndDate(DateTime.Now.AddMonths(-1), Financial.Calendar.TimeStep.Monthly);

        public static ObservableCollection<DateTime> ReportingDates = new();

        public static ObservableCollection<int> ReportingYears = new();

        //public List<string> CustodyAccounts { get { return Assets.Select(x => x.CustodyAccount).Distinct().ToList(); } }

        public static List<string> CashAccounts { get { return Assets.Select(x => x.CashAccount).Distinct().ToList(); } }

        public static void RefreshReportingYears()
        {
            ReportingDates.Clear();
            ReportingDates.Add(CurrentDate);
            ReportingYears.Clear();
            foreach (var year in ReportingDates.Select(x => x.Year).Distinct().Order())
            {
                ReportingYears.Add(year);
            }
        }
    }
}
