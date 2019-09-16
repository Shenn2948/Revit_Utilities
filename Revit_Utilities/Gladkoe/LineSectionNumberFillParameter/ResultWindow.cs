using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Revit_Utilities.Gladkoe.LineSectionNumberFillParameter
{
    public partial class ResultWindow : Form
    {
        public ResultWindow(StringBuilder sb, int i)
        {
            this.InitializeComponent();
            this.ParameterFilledLabel.Text = $@"Заполнено параметров: {i}";
            this.textBoxResults.Text = sb.ToString();
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
