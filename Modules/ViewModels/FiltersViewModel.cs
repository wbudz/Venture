﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Venture.Modules;

namespace Venture
{
    public class FiltersViewModel : INotifyPropertyChanged
    {
        public FiltersViewModel()
        {
            Common.Books.ForEach(x => books.Add(x));
            selectedBook = Books.First();
        }

        private Book selectedBook;
        public Book SelectedBook
        {
            get { return selectedBook; }
            set { selectedBook = value; OnPropertyChanged(); }
        }

        private ObservableCollection<Book> books = new();
        public ObservableCollection<Book> Books
        {
            get { return books; }
            set { books = value; OnPropertyChanged(); }
        }

        private string currentAccountsViewMode = "No aggregation";
        public string CurrentAccountsViewMode
        {
            get { return currentAccountsViewMode; }
            set { currentAccountsViewMode = value; OnPropertyChanged(); }
        }

        private bool aggregateAssetTypes = false;
        public bool AggregateAssetTypes
        {
            get { return aggregateAssetTypes; }
            set { aggregateAssetTypes = value; OnPropertyChanged(); }
        }

        private bool aggregateCurrencies = false;
        public bool AggregateCurrencies
        {
            get { return aggregateCurrencies; }
            set { aggregateCurrencies = value; OnPropertyChanged(); }
        }

        private bool aggregatePortfolios = false;
        public bool AggregatePortfolios
        {
            get { return aggregatePortfolios; }
            set { aggregatePortfolios = value; OnPropertyChanged(); }
        }

        private bool aggregateBrokers = false;
        public bool AggregateBrokers
        {
            get { return aggregateBrokers; }
            set { aggregateBrokers = value; OnPropertyChanged(); }
        }

        private int currentYear = DateTime.Now.Year;

        public int CurrentYear
        {
            get { return currentYear; }
            set { currentYear = value; OnPropertyChanged(); }
        }

        private int currentMonth = DateTime.Now.Month;
        public int CurrentMonth
        {
            get { return currentMonth; }
            set { currentMonth = value; OnPropertyChanged(); }
        }

        private ObservableCollection<DateTime> reportingDates = new() { new DateTime(DateTime.Now.Year, 12, 31) };
        public ObservableCollection<DateTime> ReportingDates
        {
            get { return reportingDates; }
            set { reportingDates = value; OnPropertyChanged(); }
        }

        private ObservableCollection<int> reportingYears = new() { DateTime.Now.Year };
        public ObservableCollection<int> ReportingYears
        {
            get { return reportingYears; }
            set { reportingYears = value; OnPropertyChanged(); }
        }

        private ObservableCollection<int> reportingMonths = new() { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
        public ObservableCollection<int> ReportingMonths
        {
            get { return reportingMonths; }
            set { reportingMonths = value; OnPropertyChanged(); }
        }

        private string currentPortfolio = "*";
        public string CurrentPortfolio
        {
            get { return currentPortfolio; }
            set { currentPortfolio = value; OnPropertyChanged(); }
        }


        public ObservableCollection<string> portfolios = new() { "*" };
        public ObservableCollection<string> Portfolios
        {
            get { return portfolios; }
            set { portfolios = value; OnPropertyChanged(); }
        }

        private string currentBroker = "*";
        public string CurrentBroker
        {
            get { return currentBroker; }
            set { currentBroker = value; OnPropertyChanged(); }
        }


        public ObservableCollection<string> brokers = new() { "*" };
        public ObservableCollection<string> Brokers
        {
            get { return brokers; }
            set { brokers = value; OnPropertyChanged(); }
        }


        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
