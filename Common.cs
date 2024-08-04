﻿using System;
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

        public static string LocalCurrency { get; set; } = "PLN";

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
    }
}
