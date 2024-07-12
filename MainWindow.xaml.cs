using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
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
using static Financial.Calendar;

namespace Venture
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        }

        private void TreeViewItem_Selected(object sender, RoutedEventArgs e)
        {
            CurrentModule.Children.Clear();

            if ((sender as TreeViewItem)?.Header.ToString() == "Portfolios") CurrentModule.Children.Add(new Modules.DefinitionsView(typeof(PortfolioDefinition)));
            if ((sender as TreeViewItem)?.Header.ToString() == "Prices") CurrentModule.Children.Add(new Modules.DefinitionsView(typeof(PriceDefinition)));
            if ((sender as TreeViewItem)?.Header.ToString() == "Instruments") CurrentModule.Children.Add(new Modules.DefinitionsView(typeof(InstrumentDefinition)));
            if ((sender as TreeViewItem)?.Header.ToString() == "Transactions") CurrentModule.Children.Add(new Modules.DefinitionsView(typeof(TransactionDefinition)));
            if ((sender as TreeViewItem)?.Header.ToString() == "Dividends") CurrentModule.Children.Add(new Modules.DefinitionsView(typeof(DividendDefinition)));
            if ((sender as TreeViewItem)?.Header.ToString() == "Coupons") CurrentModule.Children.Add(new Modules.DefinitionsView(typeof(CouponDefinition)));
            if ((sender as TreeViewItem)?.Header.ToString() == "Manual adjustments") CurrentModule.Children.Add(new Modules.DefinitionsView(typeof(ManualEventDefinition)));

            if ((sender as TreeViewItem)?.Header.ToString() == "Assets") CurrentModule.Children.Add(new Modules.AssetsView());
            if ((sender as TreeViewItem)?.Header.ToString() == "Asset classes") CurrentModule.Children.Add(new Modules.ClassesView());
            if ((sender as TreeViewItem)?.Header.ToString() == "Reports") CurrentModule.Children.Add(new Modules.AssetsReports());
            if ((sender as TreeViewItem)?.Header.ToString() == "Cashflow") CurrentModule.Children.Add(new Modules.CashflowView());
            if ((sender as TreeViewItem)?.Header.ToString() == "Futures") CurrentModule.Children.Add(new Modules.FuturesView());
        }

        #region Commands

        public static RoutedCommand ExitCommand = new RoutedCommand("ExitCommand", typeof(RoutedCommand));

        private void ExitCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void ExitCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
        }

        public static RoutedCommand LoadDataCommand = new RoutedCommand("ExitCommand", typeof(RoutedCommand));
        //void CustomRoutedCommand_Loaded(object sender, RoutedEventArgs e)
        //{
        //    MyCommand.InputGestures.Add(new KeyGesture(Key.C, ModifierKeys.Alt));
        //}

        private void LoadDataCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void LoadDataCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Definitions.Load();
            sw.Stop();
            StatusText.Text = $"Data loading took: {(sw.ElapsedMilliseconds / 1000.0):0.000} seconds.";
        }

        public static RoutedCommand GenerateAssetsCommand = new RoutedCommand("GenerateAssets", typeof(RoutedCommand));
        //void CustomRoutedCommand_Loaded(object sender, RoutedEventArgs e)
        //{
        //    MyCommand.InputGestures.Add(new KeyGesture(Key.C, ModifierKeys.Alt));
        //}

        private void GenerateAssetsCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void GenerateAssetsCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Common.Assets = AssetsGenerator.GenerateAssets();
            Common.RefreshCommonData();
            sw.Stop();
            StatusText.Text = $"Assets generation took: {(sw.ElapsedMilliseconds / 1000.0):0.000} seconds.";
        }

        #endregion
    }
}
