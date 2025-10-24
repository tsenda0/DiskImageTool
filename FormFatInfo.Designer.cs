namespace DiskImageTool
{
    partial class FormFatInfo
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
            propertyGrid1 = new PropertyGrid();
            panel1 = new Panel();
            buttonClose = new Button();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // propertyGrid1
            // 
            propertyGrid1.DisabledItemForeColor = SystemColors.ControlText;
            propertyGrid1.Dock = DockStyle.Fill;
            propertyGrid1.Location = new Point(0, 0);
            propertyGrid1.Name = "propertyGrid1";
            propertyGrid1.PropertySort = PropertySort.Alphabetical;
            propertyGrid1.Size = new Size(304, 353);
            propertyGrid1.TabIndex = 0;
            propertyGrid1.ToolbarVisible = false;
            // 
            // panel1
            // 
            panel1.Controls.Add(buttonClose);
            panel1.Dock = DockStyle.Bottom;
            panel1.Location = new Point(0, 353);
            panel1.Name = "panel1";
            panel1.Size = new Size(304, 48);
            panel1.TabIndex = 1;
            // 
            // buttonClose
            // 
            buttonClose.DialogResult = DialogResult.OK;
            buttonClose.Location = new Point(92, 8);
            buttonClose.Name = "buttonClose";
            buttonClose.Size = new Size(120, 32);
            buttonClose.TabIndex = 0;
            buttonClose.Text = "閉じる";
            buttonClose.UseVisualStyleBackColor = true;
            // 
            // FormFatInfo
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(304, 401);
            Controls.Add(propertyGrid1);
            Controls.Add(panel1);
            Name = "FormFatInfo";
            StartPosition = FormStartPosition.CenterParent;
            Text = "FAT詳細";
            panel1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private PropertyGrid propertyGrid1;
        private Panel panel1;
        private Button buttonClose;
    }
}