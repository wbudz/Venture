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
            foreach (var ave in Common.MainBook.GetAccountsAsViewEntries(Common.CurrentDate))
            {
                string selectedPortfolio = PortfolioComboBox.SelectedItem.ToString() ?? "*";
                string selectedBroker = BrokerComboBox.SelectedItem.ToString() ?? "*";

                if (Filter(ave, selectedPortfolio, selectedBroker)) VM.AccountEntries.Add(ave);
            }

            TotalValueTextBlock.Text = $"...";
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
