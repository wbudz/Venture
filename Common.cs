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

        public static DateTime FinalDate = Financial.Calendar.GetEndDate(DateTime.Now, Financial.Calendar.TimeStep.Yearly).AddDays(1);

        public static ObservableCollection<DateTime> ReportingDates = new();

        public static ObservableCollection<int> ReportingYears = new();

        //public List<string> CustodyAccounts { get { return Assets.Select(x => x.CustodyAccount).Distinct().ToList(); } }

        public static List<string> CashAccounts { get { return Assets.Select(x => x.CashAccount).Distinct().ToList(); } }

        public static void RefreshReportingYears()
        {
            DateTime start = CurrentDate;
            DateTime end = CurrentDate;

            foreach (var asset in Assets)
            {
                if (asset.BoundsStart < start) start = asset.BoundsStart;
                if (asset.BoundsEnd > end) end = asset.BoundsEnd;
            }

            start = Financial.Calendar.GetEndDate(start.AddMonths(-1), Financial.Calendar.TimeStep.Monthly);
            end = Financial.Calendar.GetEndDate(end, Financial.Calendar.TimeStep.Yearly);

            FinalDate = end.AddDays(1);

            ReportingDates.Clear();
            foreach (var date in Financial.Calendar.GenerateReportingDates(start, end, Financial.Calendar.TimeStep.Monthly))
            {
                ReportingDates.Add(date);
            }

            ReportingYears.Clear();
            foreach (var year in ReportingDates.Select(x => x.Year).Distinct().Order())
            {
                ReportingYears.Add(year);
            }
        }
    }
}
