namespace Revit_Utilities.Gladkoe.LineSectionNumberFillParameter
{
    partial class ResultWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.textBoxResults = new System.Windows.Forms.TextBox();
            this.OKButton = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.ParameterFilledLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // textBoxResults
            // 
            this.textBoxResults.Location = new System.Drawing.Point(12, 79);
            this.textBoxResults.Multiline = true;
            this.textBoxResults.Name = "textBoxResults";
            this.textBoxResults.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBoxResults.Size = new System.Drawing.Size(615, 347);
            this.textBoxResults.TabIndex = 0;
            // 
            // OKButton
            // 
            this.OKButton.Location = new System.Drawing.Point(260, 432);
            this.OKButton.Name = "OKButton";
            this.OKButton.Size = new System.Drawing.Size(100, 30);
            this.OKButton.TabIndex = 1;
            this.OKButton.Text = "OK";
            this.OKButton.UseVisualStyleBackColor = true;
            this.OKButton.Click += new System.EventHandler(this.OKButton_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 63);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(219, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Элементы, где не заполнены параметры:";
            // 
            // ParameterFilledLabel
            // 
            this.ParameterFilledLabel.AutoSize = true;
            this.ParameterFilledLabel.Location = new System.Drawing.Point(12, 9);
            this.ParameterFilledLabel.Name = "ParameterFilledLabel";
            this.ParameterFilledLabel.Size = new System.Drawing.Size(138, 13);
            this.ParameterFilledLabel.TabIndex = 2;
            this.ParameterFilledLabel.Text = "Заполнено параметров: 0";
            // 
            // ResultWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(639, 469);
            this.Controls.Add(this.ParameterFilledLabel);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.OKButton);
            this.Controls.Add(this.textBoxResults);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "ResultWindow";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "ResultWindow";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBoxResults;
        private System.Windows.Forms.Button OKButton;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label ParameterFilledLabel;
    }
}