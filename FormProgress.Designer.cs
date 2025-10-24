namespace DiskImageTool
{
    partial class FormProgress
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
            labelMessage = new Label();
            labelCurrent = new Label();
            progressBar1 = new ProgressBar();
            buttonCancel = new Button();
            labelTitle = new Label();
            SuspendLayout();
            // 
            // labelMessage
            // 
            labelMessage.Location = new Point(16, 44);
            labelMessage.Name = "labelMessage";
            labelMessage.Size = new Size(372, 24);
            labelMessage.TabIndex = 1;
            labelMessage.Text = "抽出しています";
            labelMessage.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // labelCurrent
            // 
            labelCurrent.Location = new Point(16, 72);
            labelCurrent.Name = "labelCurrent";
            labelCurrent.Size = new Size(372, 24);
            labelCurrent.TabIndex = 2;
            labelCurrent.Text = "抽出しています";
            labelCurrent.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // progressBar1
            // 
            progressBar1.Location = new Point(16, 100);
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new Size(372, 24);
            progressBar1.TabIndex = 3;
            // 
            // buttonCancel
            // 
            buttonCancel.Location = new Point(152, 132);
            buttonCancel.Name = "buttonCancel";
            buttonCancel.Size = new Size(100, 32);
            buttonCancel.TabIndex = 4;
            buttonCancel.Text = "キャンセル";
            buttonCancel.UseVisualStyleBackColor = true;
            buttonCancel.Click += buttonCancel_Click;
            // 
            // labelTitle
            // 
            labelTitle.Location = new Point(16, 16);
            labelTitle.Name = "labelTitle";
            labelTitle.Size = new Size(372, 24);
            labelTitle.TabIndex = 0;
            labelTitle.Text = "抽出しています";
            labelTitle.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // FormProgress
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(404, 177);
            ControlBox = false;
            Controls.Add(labelTitle);
            Controls.Add(buttonCancel);
            Controls.Add(progressBar1);
            Controls.Add(labelCurrent);
            Controls.Add(labelMessage);
            Font = new Font("Yu Gothic UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 128);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Name = "FormProgress";
            StartPosition = FormStartPosition.Manual;
            Text = "ファイル抽出中";
            ResumeLayout(false);
        }

        #endregion

        private Label labelMessage;
        private Label labelCurrent;
        private ProgressBar progressBar1;
        private Button buttonCancel;
        private Label labelTitle;
    }
}