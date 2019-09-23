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

namespace Gladkoe.ParameterDataManipulations.Views
{
    public partial class DuplicatesWindow : Window
    {
        public DuplicatesWindow(StringBuilder sb)
        {
            this.InitializeComponent();
            this.DuplicatesResultBox.IsReadOnly = true;
            this.DuplicatesResultBox.Text = sb.ToString();
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
