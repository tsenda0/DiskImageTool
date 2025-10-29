namespace DiskImageTool
{
    partial class ArchiveImageSelectForm
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
            listFiles = new ListBox();
            labelMessage = new Label();
            panelCommand = new Panel();
            buttonCancel = new Button();
            buttonOK = new Button();
            panelCommand.SuspendLayout();
            SuspendLayout();
            // 
            // listFiles
            // 
            listFiles.Dock = DockStyle.Fill;
            listFiles.FormattingEnabled = true;
            listFiles.ItemHeight = 17;
            listFiles.Location = new Point(0, 56);
            listFiles.Name = "listFiles";
            listFiles.Size = new Size(208, 245);
            listFiles.TabIndex = 1;
            // 
            // labelMessage
            // 
            labelMessage.Dock = DockStyle.Top;
            labelMessage.Location = new Point(0, 0);
            labelMessage.Name = "labelMessage";
            labelMessage.Padding = new Padding(2);
            labelMessage.Size = new Size(324, 56);
            labelMessage.TabIndex = 0;
            labelMessage.Text = "複数のイメージが含まれています。\r\n開くイメージを選択してください。";
            labelMessage.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // panelCommand
            // 
            panelCommand.Controls.Add(buttonCancel);
            panelCommand.Controls.Add(buttonOK);
            panelCommand.Dock = DockStyle.Right;
            panelCommand.Location = new Point(208, 56);
            panelCommand.Name = "panelCommand";
            panelCommand.Size = new Size(116, 245);
            panelCommand.TabIndex = 2;
            // 
            // buttonCancel
            // 
            buttonCancel.DialogResult = DialogResult.Cancel;
            buttonCancel.Location = new Point(4, 64);
            buttonCancel.Name = "buttonCancel";
            buttonCancel.Size = new Size(104, 52);
            buttonCancel.TabIndex = 1;
            buttonCancel.Text = "キャンセル";
            buttonCancel.UseVisualStyleBackColor = true;
            // 
            // buttonOK
            // 
            buttonOK.DialogResult = DialogResult.OK;
            buttonOK.Location = new Point(4, 8);
            buttonOK.Name = "buttonOK";
            buttonOK.Size = new Size(104, 52);
            buttonOK.TabIndex = 0;
            buttonOK.Text = "OK";
            buttonOK.UseVisualStyleBackColor = true;
            // 
            // ArchiveImageSelectForm
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(324, 301);
            Controls.Add(listFiles);
            Controls.Add(panelCommand);
            Controls.Add(labelMessage);
            Font = new Font("Yu Gothic UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 128);
            Name = "ArchiveImageSelectForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "イメージの選択";
            panelCommand.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private ListBox listFiles;
        private Label labelMessage;
        private Panel panelCommand;
        private Button buttonOK;
        private Button buttonCancel;
    }
}