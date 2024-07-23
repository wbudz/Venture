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
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Xml;

namespace Venture.Modules
{
    /// <summary>
    /// Interaction logic for AssetsView.xaml
    /// </summary>
    public partial class AccountsView : Module
    {
        private AccountsViewModel VM = (AccountsViewModel)Application.Current.Resources["AccountsVM"];
        private FiltersViewModel FVM = (FiltersViewModel)Application.Current.Resources["FiltersVM"];

        public AccountsView()
        {
            InitializeComponent();
            DataContext = this;
        }

        public void Refresh()
        {
            if (lvAccounts == null) return;

            Stopwatch sw = new Stopwatch();
            sw.Start();

            VM.AccountEntries.Clear();
            foreach (var ave in FVM.SelectedBook.GetAccountsAsViewEntries(Common.CurrentDate, FVM.AggregateAssetTypes, FVM.AggregateCurrencies, FVM.AggregatePortfolios, FVM.AggregateBrokers))
            {
                string selectedPortfolio = PortfolioComboBox.SelectedItem.ToString() ?? "*";
                string selectedBroker = BrokerComboBox.SelectedItem.ToString() ?? "*";

                if (Filter(ave, selectedPortfolio, selectedBroker)) VM.AccountEntries.Add(ave);
            }

            TotalValueTextBlock.Text = $"Total assets: {VM.AccountEntries.Where(x => x.AccountCategory == "Assets").Sum(x => x.NetAmount):N2} PLN. Total result: {-VM.AccountEntries.Where(x => x.AccountCategory == "ProfitAndLoss").Sum(x => x.NetAmount):N2} PLN.";
            TotalValueTextBlock.Visibility = Visibility.Visible;

            sw.Stop();
            ((MainWindow)Application.Current.MainWindow).StatusText.Text = $"Module refresh took: {(sw.ElapsedMilliseconds / 1000.0):0.000} seconds.";
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            List<(string property, string header)> columns = new List<(string property, string header)>();
            columns.Add(("UniqueId", "Unique id"));
            columns.Add(("AccountType", "Account type"));
            columns.Add(("AssetType", "Asset type"));
            columns.Add(("PortfolioId", "Portfolio"));
            columns.Add(("Broker", "Broker"));
            columns.Add(("Currency", "Currency"));
            columns.Add(("DebitAmount", "Debit amount"));
            columns.Add(("CreditAmount", "Credit amount"));
            columns.Add(("NetAmount", "Net amount"));
            Clipboard.SetText(CSV.Export<AccountsViewEntry>(VM.AccountEntries, columns));
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

        private void Checkbox_Checked(object sender, RoutedEventArgs e)
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
