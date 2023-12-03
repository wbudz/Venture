using System;
using System.Collections.Generic;
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
    public partial class DefinitionsView : UserControl
    {
        public Type DataPointType { get; private set; }

        public DefinitionsView(Type type)
        {
            InitializeComponent();

            if (type.IsAbstract) throw new ArgumentException("DefinitionsView module cannot be created for abstract type.");
            if (!type.IsSubclassOf(typeof(Data.DataPoint))) throw new ArgumentException("DefinitionsView module can only be created for types descended from Data.DataPoint.");
            DataPointType = type;
        }

        public void Refresh()
        {
            lv.ItemsSource = null;
            if (DataPointType == typeof(Data.Price))
            {
                lv.View = (GridView)Resources["PricesGridView"];
                lv.ItemsSource = Data.Definitions.Prices;
            }
            if (DataPointType == typeof(Data.Instrument))
            {
                lv.View = (GridView)Resources["InstrumentsGridView"];
                lv.ItemsSource = Data.Definitions.Instruments;
            }
            if (DataPointType == typeof(Data.Transaction))
            {
                lv.View = (GridView)Resources["TransactionsGridView"];
                lv.ItemsSource = Data.Definitions.Transactions;
            }
            if (DataPointType == typeof(Data.Dividend))
            {
                lv.View = (GridView)Resources["DividendsGridView"];
                lv.ItemsSource = Data.Definitions.Dividends;
            }
            if (DataPointType == typeof(Data.Coupon))
            {
                lv.View = (GridView)Resources["CouponsGridView"];
                lv.ItemsSource = Data.Definitions.Coupons;
            }
            if (DataPointType == typeof(Data.Manual))
            {
                lv.View = (GridView)Resources["ManualGridView"];
                lv.ItemsSource = Data.Definitions.Manual;
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        //private DataTemplate GetCellTemplate(Type type, string bindingPath, bool bold, int precision = 2)
        //{
        //    DataTemplate dt = new DataTemplate();
        //    dt.DataType = type;

        //    FrameworkElementFactory tbFactory = new FrameworkElementFactory(typeof(TextBlock));
        //    Binding b = new Binding(bindingPath);
        //    if (type == typeof(DateTime)) { b.StringFormat = "yyyy-MM-dd"; }
        //    if (type == typeof(double) || type == typeof(decimal)) { b.StringFormat = precision < 1 ? "0" : "0.".PadRight(precision + 2, '0'); }
        //    tbFactory.SetBinding(TextBlock.TextProperty, b);
        //    if (bold) tbFactory.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);

        //    dt.VisualTree = tbFactory;
        //    return dt;
        //}
    }
}
