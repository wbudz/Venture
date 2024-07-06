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
    /// Interaction logic for AssetsView.xaml
    /// </summary>
    public partial class AssetsView : Module
    {
        public ObservableCollection<AssetsViewEntry> AssetEntries { get; set; } = new ObservableCollection<AssetsViewEntry>();

        public AssetsView()
        {
            InitializeComponent();
            DataContext = this;
        }

        public void Refresh()
        {
            if (lvAssets == null) return;

            Stopwatch sw = new Stopwatch();
            sw.Start();

            AssetEntries.Clear();
            foreach (var asset in Common.Assets)
            {
                if (!asset.IsActive(new TimeArg(TimeArgDirection.End, Common.CurrentDate))) continue;
                var ave = new AssetsViewEntry(asset, Common.CurrentDate);

                string selectedPortfolio = PortfolioComboBox.SelectedItem.ToString() ?? "*";
                string selectedBroker = BrokerComboBox.SelectedItem.ToString() ?? "*";

                if (Filter(ave, selectedPortfolio, selectedBroker)) AssetEntries.Add(ave);
            }

            TotalValueTextBlock.Text = $"Total value: {AssetEntries.Sum(x => x.BookValue):N2} PLN, therein cash: {AssetEntries.Where(x => x.AssetType == "Cash").Sum(x => x.BookValue):N2} PLN";
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
            columns.Add(("CustodyAccount", "Custody account"));
            columns.Add(("CashAccount", "Cash account"));
            columns.Add(("Currency", "Currency"));
            columns.Add(("ValuationClass", "Valuation class"));
            columns.Add(("InstrumentId", "Instrument"));
            columns.Add(("RecognitionDate", "Recognition date"));
            columns.Add(("Count", "Count"));
            columns.Add(("NominalAmount", "Nominal amount"));
            columns.Add(("PurchaseAmount", "Purchase amount"));
            columns.Add(("AmortizedCostValue", "Amortized cost value"));
            columns.Add(("MarketValue", "Market value"));
            columns.Add(("AccruedInterest", "Accrued interest"));
            columns.Add(("BookValue", "Value"));
            Clipboard.SetText(CSV.Export<AssetsViewEntry>(AssetEntries, columns));
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
