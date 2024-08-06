using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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

namespace Venture.Modules
{
    public partial class ReportModule : Module
    {
        public ReportModule()
        {
            DataContext = this;
        }

        protected DataTemplate CreateTemplate(int column, bool numeric)
        {
            var factory = new FrameworkElementFactory(typeof(TextBlock));
            var binding = new Binding("[" + column + "]");
            if (numeric)
                binding.StringFormat = "0.00";
            factory.SetBinding(TextBlock.TextProperty, binding);
            if (numeric)
                factory.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Right);

            return new DataTemplate { VisualTree = factory };
        }
    }
}
