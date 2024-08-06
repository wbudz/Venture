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
    /// Interaction logic for FuturesView.xaml
    /// </summary>
    public partial class FuturesView : Module
    {
        private FuturesViewModel VM = (FuturesViewModel)Application.Current.Resources["FuturesVM"];

        public FuturesView()
        {
            InitializeComponent();
        }

        public void Refresh()
        {
            if (lvFuturesEvents == null) return;

            Stopwatch sw = new Stopwatch();
            sw.Start();

            VM.FuturesEntries.Clear();

            DateTime startDate = new DateTime(
                Int32.Parse(StartYearComboBox.SelectedItem.ToString() ?? Common.CurrentDate.Year.ToString()),
                Int32.Parse(StartMonthComboBox.SelectedItem.ToString() ?? Common.CurrentDate.Month.ToString()),
                1);
            DateTime endDate = new DateTime(
                Int32.Parse(EndYearComboBox.SelectedItem.ToString() ?? Common.CurrentDate.Year.ToString()),
                Int32.Parse(EndMonthComboBox.SelectedItem.ToString() ?? Common.CurrentDate.Month.ToString()),
                1);
            endDate = new DateTime(endDate.Year, endDate.Month, DateTime.DaysInMonth(endDate.Year, endDate.Month));

            foreach (var asset in Common.Assets.OfType<Futures>())
            {
                if (!asset.IsActive(startDate, endDate)) continue;
                var ave = new FuturesViewEntry(asset, startDate, endDate);

                string selectedPortfolio = PortfolioComboBox.SelectedItem.ToString() ?? "*";
                string selectedBroker = BrokerComboBox.SelectedItem.ToString() ?? "*";

                if (((IFilterable)ave).Filter(selectedPortfolio, selectedBroker)) VM.FuturesEntries.Add(ave);
            }

            TotalValueTextBlock.Text = $"Total result: {VM.FuturesEntries.Sum(x => x.Result):N2} PLN. Total fees: {VM.FuturesEntries.Sum(x => x.Fees):N2} PLN.";
            TotalValueTextBlock.Visibility = Visibility.Visible;

            sw.Stop();
            ((MainWindow)Application.Current.MainWindow).StatusText.Text = $"Module refresh took: {(sw.ElapsedMilliseconds / 1000.0):0.000} seconds.";
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            List<(string property, string header)> columns = new List<(string property, string header)>();
            columns.Add(("UniqueId", "Unique id"));
            columns.Add(("Portfolio", "Portfolio"));
            columns.Add(("CustodyAccount", "Custody account"));
            columns.Add(("CashAccount", "Cash account"));
            columns.Add(("Currency", "Currency"));            
            columns.Add(("InstrumentId", "Instrument"));
            columns.Add(("RecognitionDate", "Recognition date"));
            columns.Add(("DerecognitionDate", "Derecognition date"));
            columns.Add(("Result", "Result"));            
            Clipboard.SetText(CSV.Export<FuturesViewEntry>(VM.FuturesEntries, columns));
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
