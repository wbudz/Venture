using Microsoft.SolverFoundation.Services;
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
    public partial class CashflowReport : ReportModule
    {
        static FiltersViewModel FVM { get { return (FiltersViewModel)Application.Current.Resources["FiltersVM"]; } }

        public ObservableCollection<object[]> ReportEntries { get; set; } = new();

        public CashflowReport()
        {
            InitializeComponent();
            DataContext = this;
        }

        public void Refresh()
        {
            if (lvReport == null) return;

            Stopwatch sw = new Stopwatch();
            sw.Start(); 
            
            int startYear = Int32.Parse(StartYearComboBox.SelectedValue?.ToString() ?? DateTime.Now.Year.ToString());
            int endYear = Int32.Parse(EndYearComboBox.SelectedValue?.ToString() ?? DateTime.Now.Year.ToString());
            List<DateTime> dates = FVM.ReportingDates.Where(x => (x.Year == startYear - 1 && x.Month == 12) || (x.Year >= startYear && x.Year <= endYear)).ToList();

            ReportEntries.Clear();
            ReportEntries.Add(new object[dates.Count]);

            ReportEntries[0][0] = "Inflow: capital increase";

            for (int i = 1; i < dates.Count; i++)
            {
                List<CashflowViewEntry> source = new List<CashflowViewEntry>();
                foreach (var asset in Common.Assets.OfType<Cash>())
                {
                    string selectedPortfolio = PortfolioComboBox.SelectedItem.ToString() ?? "*";
                    string selectedBroker = BrokerComboBox.SelectedItem.ToString() ?? "*";
                    if (((IFilterable)asset).Filter(selectedPortfolio, selectedBroker))
                    {
                        source.AddRange(asset.Events.OfType<PaymentEvent>().Where(x => x.Timestamp >= dates[i - 1] && x.Timestamp <= dates[i]).Select(x => new CashflowViewEntry(x)));
                    }
                }
                decimal capitalIncrease = source.Where(x => x.CashflowType == CashflowType.Inflow_CapitalIncrease).Sum(x => x.Amount);
                ReportEntries[0][i] = capitalIncrease;
            }

            gvReport.Columns.Clear();
            gvReport.Columns.Add(new GridViewColumn() { Header = "Header", CellTemplate = CreateTemplate(0, false) });
            for (int i = 0; i < dates.Count; i++)
            {
                gvReport.Columns.Add(new GridViewColumn() { Header = dates[i].ToString("yyyy-MM-dd"), CellTemplate = CreateTemplate(i + 1, true) });
            }

            sw.Stop();
            ((MainWindow)Application.Current.MainWindow).StatusText.Text = $"Module refresh took: {(sw.ElapsedMilliseconds / 1000.0):0.000} seconds.";
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            //List<(string property, string header)> columns = new List<(string property, string header)>();
            //columns.Add(("UniqueId", "Unique id"));
            //columns.Add(("ParentAssetUniqueId", "Source"));
            //columns.Add(("CashflowType", "Cashflow type"));
            //columns.Add(("AssociatedEvent", "Associated event"));
            //columns.Add(("Timestamp", "Timestamp"));
            //columns.Add(("Portfolio", "Portfolio"));
            //columns.Add(("CashAccount", "Cash account"));
            //columns.Add(("Broker", "Broker"));
            //columns.Add(("Direction", "Direction"));
            //columns.Add(("Currency", "Currency"));
            //columns.Add(("TransactionIndex", "Transaction index"));
            //columns.Add(("GrossAmount", "Gross amount"));
            //columns.Add(("Tax", "Tax"));
            //columns.Add(("Amount", "Amount"));
            //columns.Add(("FXRate", "FX Rate"));
            //Clipboard.SetText(CSV.Export<CashflowViewEntry>(CashflowViewEntries, columns));
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
            //if (StartYearComboBox == null || EndYearComboBox == null || StartMonthComboBox == null || EndMonthComboBox == null) return;

            //int startYear = Int32.Parse(StartYearComboBox.SelectedValue?.ToString() ?? DateTime.Now.Year.ToString());
            //int endYear = Int32.Parse(EndYearComboBox.SelectedValue?.ToString() ?? DateTime.Now.Year.ToString());
            //int startMonth = Int32.Parse(StartMonthComboBox.SelectedValue?.ToString() ?? DateTime.Now.Month.ToString());
            //int endMonth = Int32.Parse(EndMonthComboBox.SelectedValue?.ToString() ?? DateTime.Now.Month.ToString());

            //if (startYear > endYear || (startYear == endYear && startMonth > endMonth))
            //{
            //    if (e.Source == StartYearComboBox || e.Source == StartMonthComboBox)
            //    {
            //        EndYearComboBox.SelectedValue = StartYearComboBox.SelectedValue;
            //        EndMonthComboBox.SelectedValue = StartMonthComboBox.SelectedValue;
            //    }
            //    if (e.Source == EndYearComboBox || e.Source == EndMonthComboBox)
            //    {
            //        StartYearComboBox.SelectedValue = EndYearComboBox.SelectedValue;
            //        StartMonthComboBox.SelectedValue = EndMonthComboBox.SelectedValue;
            //    }
            //    e.Handled = true;
            //}
            //Refresh();
        }
    }
}
