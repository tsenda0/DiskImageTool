namespace DiskImageTool
{
    partial class FormFileSystemInfo
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
            propertyGrid1.Size = new Size(304, 481);
            propertyGrid1.TabIndex = 0;
            propertyGrid1.ToolbarVisible = false;
            // 
            // panel1
            // 
            panel1.Controls.Add(buttonClose);
            panel1.Dock = DockStyle.Right;
            panel1.Location = new Point(304, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(120, 481);
            panel1.TabIndex = 1;
            // 
            // buttonClose
            // 
            buttonClose.DialogResult = DialogResult.OK;
            buttonClose.Location = new Point(12, 12);
            buttonClose.Name = "buttonClose";
            buttonClose.Size = new Size(96, 32);
            buttonClose.TabIndex = 0;
            buttonClose.Text = "閉じる";
            buttonClose.UseVisualStyleBackColor = true;
            // 
            // FormFileSystemInfo
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(424, 481);
            Controls.Add(propertyGrid1);
            Controls.Add(panel1);
            Name = "FormFileSystemInfo";
            StartPosition = FormStartPosition.CenterParent;
            Text = "ファイルシステム詳細";
            panel1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private PropertyGrid propertyGrid1;
        private Panel panel1;
        private Button buttonClose;
    }
}