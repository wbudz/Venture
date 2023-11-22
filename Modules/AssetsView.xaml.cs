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

        private void DateComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                int year = YearComboBox?.SelectedItem != null ? Int32.Parse(YearComboBox.SelectedItem?.ToString() ?? "") : DateTime.Now.Year;
                int month = MonthComboBox?.SelectedItem != null ? Int32.Parse(MonthComboBox.SelectedItem?.ToString() ?? "") : 12;
                DateTime date = new DateTime(year, month, 1);
                date = Financial.Calendar.GetEndDate(date, Financial.Calendar.TimeStep.Monthly);
                Common.CurrentDate = date;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error setting current date: {ex}.");
            }
            finally
            {
                Refresh();
            }
        }
    }
}
