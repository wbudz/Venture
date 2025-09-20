using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Venture
{
    public enum ValuationClass { Undefined, Trading, AvailableForSale, HeldToMaturity }

    public enum AssetType
    {
        Undefined,
        Cash,
        Equity,
        FixedTreasuryBonds,
        FloatingTreasuryBonds,
        FixedRetailTreasuryBonds,
        FloatingRetailTreasuryBonds,
        IndexedRetailTreasuryBonds,
        FixedCorporateBonds,
        FloatingCorporateBonds,
        ETF,
        MoneyMarketFund,
        EquityMixedFund,
        TreasuryBondsFund,
        CorporateBondsFund,
        Futures
    }

    public static class Common
    {
        public static List<Asset> Assets = new List<Asset>();

        public static Book MainBook = new Book("Main", false);

        public static Book TaxBook = new Book("Tax", true);

        public static List<Book> Books = new() { MainBook, TaxBook };

        public static DateTime CurrentDate { get { return new DateTime(FVM.CurrentYear, FVM.CurrentMonth, DateTime.DaysInMonth(FVM.CurrentYear, FVM.CurrentMonth)); } }

        public static DateTime StartDate { get; set; } = new DateTime(DateTime.Now.Year, 1, 1);

        public static DateTime EndDate { get; set; } = Financial.Calendar.GetEndDate(DateTime.Now, Financial.Calendar.TimeStep.Yearly);

        public static List<string> CashAccounts { get { return Assets.Select(x => x.CashAccount).Distinct().ToList(); } }

        static FiltersViewModel FVM { get { return (FiltersViewModel)Application.Current.Resources["FiltersVM"]; } }

        public static void RefreshCommonData()
        {
            DateTime start = CurrentDate;
            DateTime end = CurrentDate;

            foreach (var asset in Assets)
            {
                if (asset.BoundsStart < start) start = asset.BoundsStart;
                if (asset.BoundsEnd > end) end = asset.BoundsEnd;
            }

            StartDate = Financial.Calendar.GetEndDate(start.AddMonths(-1), Financial.Calendar.TimeStep.Monthly);
            EndDate = Financial.Calendar.GetEndDate(end, Financial.Calendar.TimeStep.Yearly);

            FVM.ReportingDates = new ObservableCollection<DateTime>(Financial.Calendar.GenerateReportingDates(start, end, Financial.Calendar.TimeStep.Monthly));

            FVM.ReportingYears = new ObservableCollection<int>(FVM.ReportingDates.Select(x => x.Year).Distinct().Order());

            List<string> portfolios = new List<string>() { "*" };
            portfolios.AddRange(Definitions.Portfolios.Select(x => x.UniqueId).Distinct());
            portfolios.AddRange(Definitions.Portfolios.Select(x => x.PortfolioName + "_*").Distinct());
            FVM.Portfolios = new ObservableCollection<string>(portfolios.Order());

            List<string> brokers = new List<string>() { "*" };
            brokers.AddRange(Definitions.Portfolios.Select(x => x.Broker).Distinct().Order());
            FVM.Brokers = new ObservableCollection<string>(brokers);
        }

        public static string ValuationClassToString(ValuationClass input)
        {
            switch (input)
            {
                case ValuationClass.Undefined: return "";
                case ValuationClass.Trading: return "TRD";
                case ValuationClass.AvailableForSale: return "AFS";
                case ValuationClass.HeldToMaturity: return "HTM";
                default: return "";
            }
        }

        public static decimal Round(decimal value, int digits = 2)
        {
            return Math.Round(value, digits, MidpointRounding.AwayFromZero);
        }

        public static bool Like(string str, string pattern)
        {
            // Inspired by komma8.komma1
            // https://stackoverflow.com/questions/5417070/c-sharp-version-of-sql-like
            // Treats asterisk as wildcard and question mark as any one character.

            bool isMatch = true,
                isWildCardOn = false,
                isCharWildCardOn = false,
                endOfPattern = false;
            int lastWildCard = -1;
            int patternIndex = 0;
            List<char> set = new List<char>();
            char p = '\0';

            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                endOfPattern = (patternIndex >= pattern.Length);
                if (!endOfPattern)
                {
                    p = pattern[patternIndex];

                    if (!isWildCardOn && p == '*')
                    {
                        lastWildCard = patternIndex;
                        isWildCardOn = true;
                        while (patternIndex < pattern.Length &&
                            pattern[patternIndex] == '*')
                        {
                            patternIndex++;
                        }
                        if (patternIndex >= pattern.Length) p = '\0';
                        else p = pattern[patternIndex];
                    }
                    else if (p == '?')
                    {
                        isCharWildCardOn = true;
                        patternIndex++;
                    }                    
                }

                if (isWildCardOn)
                {
                    if (char.ToUpper(c) == char.ToUpper(p))
                    {
                        isWildCardOn = false;
                        patternIndex++;
                    }
                }
                else if (isCharWildCardOn)
                {
                    isCharWildCardOn = false;
                }
                else
                {
                    if (char.ToUpper(c) == char.ToUpper(p))
                    {
                        patternIndex++;
                    }
                    else
                    {
                        if (lastWildCard >= 0) patternIndex = lastWildCard;
                        else
                        {
                            isMatch = false;
                            break;
                        }
                    }
                }
            }
            endOfPattern = (patternIndex >= pattern.Length);

            if (isMatch && !endOfPattern)
            {
                bool isOnlyWildCards = true;
                for (int i = patternIndex; i < pattern.Length; i++)
                {
                    if (pattern[i] != '*')
                    {
                        isOnlyWildCards = false;
                        break;
                    }
                }
                if (isOnlyWildCards) endOfPattern = true;
            }
            return isMatch && endOfPattern;
        }

    }
}
