﻿using System;
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

            Stopwatch sw = new Stopwatch();
            sw.Start();

            AssetEntries.Clear();
            foreach (var asset in Common.Assets)
            {
                if (!asset.IsActive(new TimeArg(TimeArgDirection.End, Common.CurrentDate))) continue;
                if (PortfolioComboBox.SelectedItem.ToString() != "*" && PortfolioComboBox.SelectedItem.ToString() != asset.Portfolio) continue;
                if (BrokerComboBox.SelectedItem.ToString() != "*" && BrokerComboBox.SelectedItem.ToString() != asset.FinancialInstitution) continue;
                AssetEntries.Add(new AssetsViewEntry(asset, Common.CurrentDate));
            }

            TotalValueTextBlock.Text = $"Total value: {AssetEntries.Sum(x => x.BookValue):N2} PLN, therein cash: {AssetEntries.Where(x => x.AssetType == "Cash").Sum(x => x.BookValue):N2} PLN";
            TotalValueTextBlock.Visibility = Visibility.Visible;

            sw.Stop();
            ((MainWindow)Application.Current.MainWindow).StatusText.Text = $"Module refresh took: {(sw.ElapsedMilliseconds / 1000.0):0.000} seconds.";
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(CSV.Export<AssetsViewEntry>(AssetEntries));
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
