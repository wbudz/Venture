using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.Intrinsics.X86;
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
    /// Interaction logic for DefinitionsView.xaml
    /// </summary>
    public partial class CashflowView : Module
    {
        public ObservableCollection<CashflowViewEntry> CashflowViewEntries { get; set; } = new ObservableCollection<CashflowViewEntry>();

        public CashflowView()
        {
            InitializeComponent();
            DataContext = this;
        }

        public void Refresh()
        {
            if (lvFlows == null) return;

            Stopwatch sw = new Stopwatch();
            sw.Start();

            CashflowViewEntries.Clear();
            List<CashflowViewEntry> source = new List<CashflowViewEntry>();

            DateTime startDate = new DateTime(
                Int32.Parse(StartYearComboBox.SelectedItem.ToString() ?? Common.CurrentDate.Year.ToString()),
                Int32.Parse(StartMonthComboBox.SelectedItem.ToString() ?? Common.CurrentDate.Month.ToString()),
                1);
            DateTime endDate = new DateTime(
                Int32.Parse(EndYearComboBox.SelectedItem.ToString() ?? Common.CurrentDate.Year.ToString()),
                Int32.Parse(EndMonthComboBox.SelectedItem.ToString() ?? Common.CurrentDate.Month.ToString()),
                1);
            endDate = new DateTime(endDate.Year, endDate.Month, DateTime.DaysInMonth(endDate.Year, endDate.Month));

            foreach (var asset in Common.Assets.OfType<Cash>())
            {
                source.AddRange(asset.Events.OfType<PaymentEvent>().Where(x => x.Timestamp >= startDate && x.Timestamp <= endDate).Select(x => new CashflowViewEntry(x)));
            }

            string selectedPortfolio = PortfolioComboBox.SelectedItem.ToString() ?? "*";
            string selectedBroker = BrokerComboBox.SelectedItem.ToString() ?? "*";
            foreach (var item in source.OrderBy(x => x.Timestamp).ThenBy(x => x.TransactionIndex))
            { 
                if (((IFilterable)item).Filter(selectedPortfolio, selectedBroker)) CashflowViewEntries.Add(item);
            }

            TotalValueTextBlock.Text = $"Total value: {CashflowViewEntries.Sum(x => x.Amount):N2} PLN";
            TotalValueTextBlock.Visibility = Visibility.Visible;

            sw.Stop();
            ((MainWindow)Application.Current.MainWindow).StatusText.Text = $"Module refresh took: {(sw.ElapsedMilliseconds / 1000.0):0.000} seconds.";
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            List<(string property, string header)> columns = new List<(string property, string header)>();
            columns.Add(("UniqueId", "Unique id"));
            columns.Add(("ParentAssetUniqueId", "Source"));
            columns.Add(("CashflowType", "Cashflow type"));
            columns.Add(("AssociatedEvent", "Associated event"));
            columns.Add(("Timestamp", "Timestamp"));
            columns.Add(("Portfolio", "Portfolio"));
            columns.Add(("CashAccount", "Cash account"));
            columns.Add(("Broker", "Broker"));
            columns.Add(("Direction", "Direction"));
            columns.Add(("Currency", "Currency"));
            columns.Add(("TransactionIndex", "Transaction index"));
            columns.Add(("GrossAmount", "Gross amount"));
            columns.Add(("Tax", "Tax"));
            columns.Add(("Amount", "Amount"));
            columns.Add(("FXRate", "FX Rate"));
            Clipboard.SetText(CSV.Export<CashflowViewEntry>(CashflowViewEntries, columns));
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
            if (StartYearComboBox == null || EndYearComboBox == null || StartMonthComboBox == null || EndMonthComboBox == null) return;

            int startYear = Int32.Parse(StartYearComboBox.SelectedValue?.ToString() ?? DateTime.Now.Year.ToString());
            int endYear = Int32.Parse(EndYearComboBox.SelectedValue?.ToString() ?? DateTime.Now.Year.ToString());
            int startMonth = Int32.Parse(StartMonthComboBox.SelectedValue?.ToString() ?? DateTime.Now.Month.ToString());
            int endMonth = Int32.Parse(EndMonthComboBox.SelectedValue?.ToString() ?? DateTime.Now.Month.ToString());

            if (startYear > endYear || (startYear == endYear && startMonth > endMonth))
            {
                if (e.Source == StartYearComboBox || e.Source == StartMonthComboBox)
                {
                    EndYearComboBox.SelectedValue = StartYearComboBox.SelectedValue;
                    EndMonthComboBox.SelectedValue = StartMonthComboBox.SelectedValue;
                }
                if (e.Source == EndYearComboBox || e.Source == EndMonthComboBox)
                {
                    StartYearComboBox.SelectedValue = EndYearComboBox.SelectedValue;
                    StartMonthComboBox.SelectedValue = EndMonthComboBox.SelectedValue;
                }
                e.Handled = true;
            }
            Refresh();
        }
    }
}
