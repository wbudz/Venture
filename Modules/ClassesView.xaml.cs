using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Venture.Modules
{
    /// <summary>
    /// Interaction logic for ClassesView.xaml
    /// </summary>
    public partial class ClassesView : Module
    {
        public ObservableCollection<ClassesViewEntry> ClassEntries { get; set; } = new ObservableCollection<ClassesViewEntry>();

        public ClassesView()
        {
            InitializeComponent();
            DataContext = this;
        }

        public void Refresh()
        {
            if (lvClasses == null) return;

            Stopwatch sw = new Stopwatch();
            sw.Start();

            ClassEntries.Clear();
            List<string> subportfolios = Definitions.Portfolios.Select(x => x.UniqueId).Distinct().Order().ToList();
            List<string> portfolios = subportfolios.Select(x => x.Substring(0, x.IndexOf('_') + 1)).Distinct().Order().ToList();
            List<string> assetTypes = Common.Assets.Select(x => x.AssetType.ToString()).Distinct().Order().ToList();
            List<string> currencies = Common.Assets.Select(x => x.Currency).Distinct().Order().ToList();

            string selectedPortfolio = PortfolioComboBox.SelectedItem.ToString() ?? "*";
            string selectedBroker = BrokerComboBox.SelectedItem.ToString() ?? "*";

            foreach (var p in subportfolios)
            {
                foreach (var t in assetTypes)
                {
                    foreach (var c in currencies)
                    {
                        var ave = new ClassesViewEntry(Common.Assets, p, t, c, Common.CurrentDate);
                        if (((IFilterable)ave).Filter(selectedPortfolio, selectedBroker) && ave.BookValue != 0) ClassEntries.Add(ave);
                    }
                }
            }

            decimal assetsTotal = ClassEntries.Sum(x => x.BookValue);
            Dictionary<string, decimal> portfolioTotal = portfolios.ToDictionary(x => x, x => ClassEntries.Where(y => y.PortfolioId.StartsWith(x)).Sum(z => z.BookValue));
            Dictionary<string, decimal> subportfolioTotal = subportfolios.ToDictionary(x => x, x => ClassEntries.Where(y => y.PortfolioId == x).Sum(z => z.BookValue));
            foreach (var entry in ClassEntries)
            {
                entry.PercentOfSubportfolio = Math.Round(entry.BookValue / subportfolioTotal[entry.PortfolioId] * 100, 2);
                entry.PercentOfPortfolio = Math.Round(entry.BookValue / portfolioTotal[entry.PortfolioId.Substring(0, entry.PortfolioId.IndexOf('_') + 1)] * 100, 2);
                entry.PercentOfTotal = Math.Round(entry.BookValue / assetsTotal * 100, 2);
            }

            TotalValueTextBlock.Text = $"Total value: {ClassEntries.Sum(x => x.BookValue):N2} PLN, therein cash: {ClassEntries.Where(x => x.AssetType == "Cash").Sum(x => x.BookValue):N2} PLN";
            TotalValueTextBlock.Visibility = Visibility.Visible;

            sw.Stop();
            ((MainWindow)Application.Current.MainWindow).StatusText.Text = $"Module refresh took: {(sw.ElapsedMilliseconds / 1000.0):0.000} seconds.";
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            List<(string property, string header)> columns = new List<(string property, string header)>();
            columns.Add(("UniqueId", "Unique id"));
            columns.Add(("Type", "Type"));
            columns.Add(("Portfolio", "Portfolio"));
            columns.Add(("Currency", "Currency"));
            columns.Add(("AmortizedCostValue", "Amortized cost value"));
            columns.Add(("MarketValue", "Market value"));
            columns.Add(("AccruedInterest", "Accrued interest"));
            columns.Add(("BookValue", "Value"));
            columns.Add(("PercentOfSubportfolio", "% of subportfolio"));
            columns.Add(("PercentOfPortfolio", "% of portfolio"));
            columns.Add(("PercentOfTotal", "% of total assets"));
            Clipboard.SetText(CSV.Export<ClassesViewEntry>(ClassEntries, columns));
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Refresh();
        }

        private void ListView_AutoSizeColumns(object sender, SelectionChangedEventArgs e)
        {
            if (sender == null) return;
            foreach (var column in ((GridView)((ListView)sender).View).Columns)
            {
                if (double.IsNaN(column.Width))
                {
                    column.Width = column.ActualWidth;
                }

                column.Width = double.NaN;
            }
        }
    }
}
