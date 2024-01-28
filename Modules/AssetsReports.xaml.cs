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
    public partial class AssetsReports : UserControl
    {
        public ObservableCollection<object[]> ReportEntries { get; set; } = new();

        const int DESCRIPTION_COLUMNS_COUNT = 6;

        static FiltersViewModel FVM { get { return (FiltersViewModel)Application.Current.Resources["Filters"]; } }

        public AssetsReports()
        {
            InitializeComponent();
            DataContext = this;
        }

        DataTemplate CreateTemplate(int column, bool numeric, bool mouseover = false)
        {
            var factory = new FrameworkElementFactory(typeof(TextBlock));
            var binding = new Binding("[" + column + "]");
            if (numeric)
                binding.StringFormat = "0.00";
            factory.SetBinding(TextBlock.TextProperty, binding);
            if (numeric)
                factory.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Right);

            if (mouseover)
            {
                binding = new Binding("[0]");
                factory.SetBinding(TextBlock.ToolTipProperty, binding);
            }

            return new DataTemplate { VisualTree = factory };
        }

        object[] GenerateReportEntry(Assets.Asset asset, int optionIndex, List<DateTime> dates)
        {
            object[] result = new object[1 + DESCRIPTION_COLUMNS_COUNT + dates.Count];
            result[0] = asset.UniqueId;
            result[1] = asset.InstrumentId;
            result[2] = asset.GetPurchaseDate().ToString("yyyy-MM-dd") ?? "";
            result[3] = asset.AssetType;
            result[4] = asset.Currency;
            result[5] = asset.Portfolio;
            result[6] = asset.Broker;

            for (int i = 0; i < dates.Count; i++)
            {
                switch (optionIndex)
                {
                    case 0: result[i + 1 + DESCRIPTION_COLUMNS_COUNT] = asset.GetCount(new TimeArg(TimeArgDirection.End, dates[i])); break;
                    case 1: result[i + 1 + DESCRIPTION_COLUMNS_COUNT] = asset.GetMarketPrice(new TimeArg(TimeArgDirection.End, dates[i]), false); break;
                    case 2: result[i + 1 + DESCRIPTION_COLUMNS_COUNT] = asset.GetPurchasePrice(new TimeArg(TimeArgDirection.End, dates[i]), false); break;
                    case 3: result[i + 1 + DESCRIPTION_COLUMNS_COUNT] = asset.GetValue(new TimeArg(TimeArgDirection.End, dates[i])); break;
                    case 4: result[i + 1 + DESCRIPTION_COLUMNS_COUNT] = asset.GetUnrealizedGainsLossesFromValuation(new TimeArg(TimeArgDirection.End, dates[i])); break;
                    default: break;
                }
            }
            return result;
        }

        public void Refresh()
        {
            if (gvReport == null) return;

            Stopwatch sw = new Stopwatch();
            sw.Start();

            int startYear = Int32.Parse(StartYearComboBox.SelectedValue?.ToString() ?? DateTime.Now.Year.ToString());
            int endYear = Int32.Parse(EndYearComboBox.SelectedValue?.ToString() ?? DateTime.Now.Year.ToString());
            List<DateTime> dates = FVM.ReportingDates.Where(x => (x.Year == startYear - 1 && x.Month == 12) || (x.Year >= startYear && x.Year <= endYear)).ToList();

            // Regenerate report entries
            ReportEntries.Clear();
            foreach (var asset in Common.Assets)
            {
                if (asset is Assets.Cash) continue;
                if (!asset.IsActive(new DateTime(startYear, 1, 1), new DateTime(endYear, 12, 31))) continue;
                if (asset.BoundsStart.Year == asset.BoundsEnd.Year && asset.BoundsStart.Month == asset.BoundsEnd.Month) continue; // omit assets that were owned only during one month
                if (PortfolioComboBox.SelectedItem.ToString() != "*" && PortfolioComboBox.SelectedItem.ToString() != asset.Portfolio) continue;
                if (BrokerComboBox.SelectedItem.ToString() != "*" && BrokerComboBox.SelectedItem.ToString() != asset.Broker) continue;

                ReportEntries.Add(GenerateReportEntry(asset, OptionComboBox.SelectedIndex, dates));
            }

            gvReport.Columns.Clear();
            gvReport.Columns.Add(new GridViewColumn() { Header = "Instrument id", CellTemplate = CreateTemplate(1, false, true) });
            gvReport.Columns.Add(new GridViewColumn() { Header = "Purchase date", CellTemplate = CreateTemplate(2, false) });
            gvReport.Columns.Add(new GridViewColumn() { Header = "Asset type", CellTemplate = CreateTemplate(3, false) });
            gvReport.Columns.Add(new GridViewColumn() { Header = "Currency", CellTemplate = CreateTemplate(4, false) });
            gvReport.Columns.Add(new GridViewColumn() { Header = "Portfolio", CellTemplate = CreateTemplate(5, false) });
            gvReport.Columns.Add(new GridViewColumn() { Header = "Broker", CellTemplate = CreateTemplate(6, false) });
            for (int i = 0; i < dates.Count; i++)
            {
                gvReport.Columns.Add(new GridViewColumn() { Header = dates[i].ToString("yyyy-MM-dd"), CellTemplate = CreateTemplate(i + 1 + DESCRIPTION_COLUMNS_COUNT, true) });
            }

            sw.Stop();
            ((MainWindow)Application.Current.MainWindow).StatusText.Text = $"Module refresh took: {(sw.ElapsedMilliseconds / 1000.0):0.000} seconds.";
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
            if (StartYearComboBox == null || EndYearComboBox == null) return;

            int startYear = Int32.Parse(StartYearComboBox.SelectedValue?.ToString() ?? DateTime.Now.Year.ToString());
            int endYear = Int32.Parse(EndYearComboBox.SelectedValue?.ToString() ?? DateTime.Now.Year.ToString());

            if (startYear > endYear)
            {
                if (e.Source == StartYearComboBox) EndYearComboBox.SelectedValue = StartYearComboBox.SelectedValue;
                if (e.Source == EndYearComboBox) StartYearComboBox.SelectedValue = EndYearComboBox.SelectedValue;
                e.Handled = true;
            }
            Refresh();
        }
    }
}
