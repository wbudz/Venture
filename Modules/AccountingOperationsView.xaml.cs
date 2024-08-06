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
using System.Xml;
using Windows.Gaming.Input;

namespace Venture.Modules
{
    /// <summary>
    /// Interaction logic for AssetsView.xaml
    /// </summary>
    public partial class AccountingOperationsView : Module
    {
        private OperationsViewModel VM = (OperationsViewModel)Application.Current.Resources["OperationsVM"];
        private FiltersViewModel FVM = (FiltersViewModel)Application.Current.Resources["FiltersVM"];

        public AccountingOperationsView()
        {
            InitializeComponent();
            DataContext = this;
        }

        public void Refresh()
        {
            if (lvOperations == null) return;

            Stopwatch sw = new Stopwatch();
            sw.Start();

            VM.OperationsEntries.Clear();

            long operationsIndex;
            long transactionIndex;

            if (!Int64.TryParse(OperationTextBox.Text, out operationsIndex)) operationsIndex = -1;
            if (!Int64.TryParse(TransactionTextBox.Text, out transactionIndex)) transactionIndex = -1;

            if (operationsIndex < 0 && transactionIndex < 0) return;

            foreach (var oe in FVM.SelectedBook.GetAccountsAsViewEntries(operationsIndex, transactionIndex))
            {
                VM.OperationsEntries.Add(oe);
            }

            TotalValueTextBlock.Text = $"Total amount: {VM.OperationsEntries.Sum(x => x.Amount):N2} PLN.";
            TotalValueTextBlock.Visibility = Visibility.Visible;

            sw.Stop();
            ((MainWindow)Application.Current.MainWindow).StatusText.Text = $"Module refresh took: {(sw.ElapsedMilliseconds / 1000.0):0.000} seconds.";
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            List<(string property, string header)> columns = new List<(string property, string header)>();
            columns.Add(("UniqueId", "Unique id"));
            columns.Add(("OperationIndex", "Operation index"));
            columns.Add(("TransactionIndex", "Transaction index"));
            columns.Add(("PortfolioId", "Portfolio"));
            columns.Add(("Broker", "Broker"));
            columns.Add(("Description", "Description"));
            columns.Add(("Currency", "Currency"));
            columns.Add(("NetAmount", "Net amount"));
            Clipboard.SetText(CSV.Export<AccountEntriesViewEntry>(VM.OperationsEntries, columns));
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

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) Refresh();
        }
    }
}
