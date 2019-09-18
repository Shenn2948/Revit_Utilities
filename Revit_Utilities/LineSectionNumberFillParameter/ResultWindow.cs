namespace Revit_Utilities.LineSectionNumberFillParameter
{
    using System;
    using System.Text;
    using System.Windows.Forms;

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
