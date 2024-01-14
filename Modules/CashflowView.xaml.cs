using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
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
    public partial class CashflowView : UserControl
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

            foreach (var asset in Common.Assets)
            {
                source.AddRange(asset.Events.OfType<Events.Payment>().Where(x => x.TransactionIndex!= -1 && x.Timestamp <= Common.CurrentDate).Select(x => new CashflowViewEntry(x)));
                source.AddRange(asset.Events.OfType<Events.Flow>().Where(x => x.Timestamp <= Common.CurrentDate).Select(x => new CashflowViewEntry(x)));
            }

            foreach (var item in source.OrderBy(x=>x.Timestamp).ThenBy(x => x.TransactionIndex))
            {
                if (PortfolioComboBox.SelectedItem.ToString() != "*" && PortfolioComboBox.SelectedItem.ToString() != item.Portfolio) continue;
                if (BrokerComboBox.SelectedItem.ToString() != "*" && BrokerComboBox.SelectedItem.ToString() != item.Broker) continue;
                CashflowViewEntries.Add(item);
            }

            //TotalValueTextBlock.Text = $"Total value: {AssetEntries.Sum(x => x.BookValue):N2} PLN, therein cash: {AssetEntries.Where(x => x.AssetType == "Cash").Sum(x => x.BookValue):N2} PLN";
            //TotalValueTextBlock.Visibility = Visibility.Visible;

            sw.Stop();
            ((MainWindow)Application.Current.MainWindow).StatusText.Text = $"Module refresh took: {(sw.ElapsedMilliseconds / 1000.0):0.000} seconds.";
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(Data.CSV.Export<CashflowViewEntry>(CashflowViewEntries));
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
    }
}
