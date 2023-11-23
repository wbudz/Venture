using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace Budziszewski.Venture.Modules
{
    /// <summary>
    /// Interaction logic for DefinitionsView.xaml
    /// </summary>
    public partial class AssetsView : UserControl
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
            AssetEntries.Clear();
            foreach (var asset in Common.Assets)
            {
                if (!asset.IsActive(new TimeArg(TimeArgDirection.End, Common.CurrentDate))) continue;
                if (PortfolioComboBox.SelectedItem.ToString() != "*" && PortfolioComboBox.SelectedItem.ToString() != asset.Portfolio) continue;
                if (BrokerComboBox.SelectedItem.ToString() != "*" && BrokerComboBox.SelectedItem.ToString() != asset.Broker) continue;
                AssetEntries.Add(asset.GenerateAssetViewEntry(Common.CurrentDate));
            }
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
