using System;
using System.Collections.Generic;
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
using static Budziszewski.Financial.Calendar;

namespace Budziszewski.Venture
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
            StringBuilder sb = new StringBuilder();
            //for (int i = 0; i < 365; i++)
            //{
            //    sb.Append(Financial.Calendar.GetEndDate(new DateTime(2023, 1, 1).AddDays(i), TimeStep.Daily).ToShortDateString());
            //    sb.Append('\t');
            //    sb.Append(Financial.Calendar.GetEndDate(new DateTime(2023, 1, 1).AddDays(i), TimeStep.Weekly).ToShortDateString());
            //    sb.Append('\t');
            //    sb.Append(Financial.Calendar.GetEndDate(new DateTime(2023, 1, 1).AddDays(i), TimeStep.Monthly).ToShortDateString());
            //    sb.Append('\t');
            //    sb.Append(Financial.Calendar.GetEndDate(new DateTime(2023, 1, 1).AddDays(i), TimeStep.Quarterly).ToShortDateString());
            //    sb.Append('\t');
            //    sb.Append(Financial.Calendar.GetEndDate(new DateTime(2023, 1, 1).AddDays(i), TimeStep.Semiannually).ToShortDateString());
            //    sb.Append('\t');
            //    sb.Append(Financial.Calendar.GetEndDate(new DateTime(2023, 1, 1).AddDays(i), TimeStep.Yearly).ToShortDateString());
            //    sb.Append('\r');
            //    sb.Append('\n');
            //}
            var dates = Financial.Calendar.GenerateReportingDates(new DateTime(2022, 12, 31), new DateTime(2023, 12, 31), TimeStep.Weekly);
            Clipboard.SetText(sb.ToString());
        }

        private void TreeViewItem_Selected(object sender, RoutedEventArgs e)
        {
            CurrentModule.Children.Clear();
            if ((sender as TreeViewItem)?.Header.ToString() == "Prices") CurrentModule.Children.Add(new Modules.DefinitionsView(typeof(Data.Price)));
            if ((sender as TreeViewItem)?.Header.ToString() == "Instruments") CurrentModule.Children.Add(new Modules.DefinitionsView(typeof(Data.Instrument)));
            if ((sender as TreeViewItem)?.Header.ToString() == "Transactions") CurrentModule.Children.Add(new Modules.DefinitionsView(typeof(Data.Transaction)));
        }

        #region Commands

        public static RoutedCommand ExitCommand = new RoutedCommand("ExitCommand", typeof(RoutedCommand));
        //void CustomRoutedCommand_Loaded(object sender, RoutedEventArgs e)
        //{
        //    MyCommand.InputGestures.Add(new KeyGesture(Key.C, ModifierKeys.Alt));
        //}

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
            Data.Definitions.Load();
            sw.Stop();
            StatusText.Text = $"Data loading took: {(sw.ElapsedMilliseconds / 1000.0):0.000} seconds.";
        }

        #endregion
    }
}
