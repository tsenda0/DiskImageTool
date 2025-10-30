
namespace DiskImageTool
{
    partial class FormDiskImageTool
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /*
        /// <summary>
        ///  Clean up any resources being used.
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
        */

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            buttonExtractAll = new Button();
            buttonSelectFile = new Button();
            labelFileName = new Label();
            panelTop = new Panel();
            panelFileName = new Panel();
            panelSelectFile = new Panel();
            panelRight = new Panel();
            buttonVersionInfo = new Button();
            checkIsUTC = new CheckBox();
            buttonFATInfo = new Button();
            buttonExtract = new Button();
            openFileDialog1 = new OpenFileDialog();
            labelStatus = new Label();
            panelFileList = new Panel();
            listViewFiles = new ListView();
            columnFileName = new ColumnHeader();
            columnSize = new ColumnHeader();
            columnDate = new ColumnHeader();
            folderBrowserDialog1 = new FolderBrowserDialog();
            panelTop.SuspendLayout();
            panelFileName.SuspendLayout();
            panelSelectFile.SuspendLayout();
            panelRight.SuspendLayout();
            panelFileList.SuspendLayout();
            SuspendLayout();
            // 
            // buttonExtractAll
            // 
            buttonExtractAll.Location = new Point(4, 8);
            buttonExtractAll.Name = "buttonExtractAll";
            buttonExtractAll.Size = new Size(104, 52);
            buttonExtractAll.TabIndex = 0;
            buttonExtractAll.Text = "全抽出";
            buttonExtractAll.UseVisualStyleBackColor = true;
            buttonExtractAll.Click += extractAll_Click;
            // 
            // buttonSelectFile
            // 
            buttonSelectFile.Dock = DockStyle.Fill;
            buttonSelectFile.Location = new Point(4, 4);
            buttonSelectFile.Name = "buttonSelectFile";
            buttonSelectFile.Size = new Size(104, 52);
            buttonSelectFile.TabIndex = 0;
            buttonSelectFile.Text = "イメージファイル\r\n選択...";
            buttonSelectFile.UseVisualStyleBackColor = true;
            buttonSelectFile.Click += openImageFile_Click;
            // 
            // labelFileName
            // 
            labelFileName.BorderStyle = BorderStyle.FixedSingle;
            labelFileName.Dock = DockStyle.Fill;
            labelFileName.Location = new Point(4, 8);
            labelFileName.Name = "labelFileName";
            labelFileName.Size = new Size(420, 44);
            labelFileName.TabIndex = 0;
            labelFileName.Text = "ファイルを選択してください";
            labelFileName.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // panelTop
            // 
            panelTop.Controls.Add(panelFileName);
            panelTop.Controls.Add(panelSelectFile);
            panelTop.Dock = DockStyle.Top;
            panelTop.Location = new Point(0, 0);
            panelTop.Name = "panelTop";
            panelTop.Size = new Size(544, 60);
            panelTop.TabIndex = 0;
            // 
            // panelFileName
            // 
            panelFileName.Controls.Add(labelFileName);
            panelFileName.Dock = DockStyle.Fill;
            panelFileName.Location = new Point(0, 0);
            panelFileName.Name = "panelFileName";
            panelFileName.Padding = new Padding(4, 8, 4, 8);
            panelFileName.Size = new Size(428, 60);
            panelFileName.TabIndex = 0;
            // 
            // panelSelectFile
            // 
            panelSelectFile.Controls.Add(buttonSelectFile);
            panelSelectFile.Dock = DockStyle.Right;
            panelSelectFile.Location = new Point(428, 0);
            panelSelectFile.Name = "panelSelectFile";
            panelSelectFile.Padding = new Padding(4, 4, 8, 4);
            panelSelectFile.Size = new Size(116, 60);
            panelSelectFile.TabIndex = 1;
            // 
            // panelRight
            // 
            panelRight.Controls.Add(buttonVersionInfo);
            panelRight.Controls.Add(checkIsUTC);
            panelRight.Controls.Add(buttonFATInfo);
            panelRight.Controls.Add(buttonExtract);
            panelRight.Controls.Add(buttonExtractAll);
            panelRight.Dock = DockStyle.Right;
            panelRight.Location = new Point(428, 80);
            panelRight.Name = "panelRight";
            panelRight.Size = new Size(116, 421);
            panelRight.TabIndex = 3;
            // 
            // buttonVersionInfo
            // 
            buttonVersionInfo.Location = new Point(4, 276);
            buttonVersionInfo.Name = "buttonVersionInfo";
            buttonVersionInfo.Size = new Size(104, 52);
            buttonVersionInfo.TabIndex = 4;
            buttonVersionInfo.Text = "バージョン情報...";
            buttonVersionInfo.UseVisualStyleBackColor = true;
            buttonVersionInfo.Click += buttonVersionInfo_Click;
            // 
            // checkIsUTC
            // 
            checkIsUTC.AutoSize = true;
            checkIsUTC.Location = new Point(12, 128);
            checkIsUTC.Name = "checkIsUTC";
            checkIsUTC.Size = new Size(87, 38);
            checkIsUTC.TabIndex = 2;
            checkIsUTC.Text = "日付をUTC\r\nとして扱う";
            checkIsUTC.UseVisualStyleBackColor = true;
            checkIsUTC.Click += checkIsUTC_Click;
            // 
            // buttonFATInfo
            // 
            buttonFATInfo.Location = new Point(4, 208);
            buttonFATInfo.Name = "buttonFATInfo";
            buttonFATInfo.Size = new Size(104, 52);
            buttonFATInfo.TabIndex = 3;
            buttonFATInfo.Text = "FAT詳細...";
            buttonFATInfo.UseVisualStyleBackColor = true;
            buttonFATInfo.Click += buttonFATInfo_Click;
            // 
            // buttonExtract
            // 
            buttonExtract.Location = new Point(4, 64);
            buttonExtract.Name = "buttonExtract";
            buttonExtract.Size = new Size(104, 52);
            buttonExtract.TabIndex = 1;
            buttonExtract.Text = "選択ファイルを\r\n抽出";
            buttonExtract.UseVisualStyleBackColor = true;
            buttonExtract.Click += extract_Click;
            // 
            // openFileDialog1
            // 
            openFileDialog1.FileName = "openFileDialog1";
            // 
            // labelStatus
            // 
            labelStatus.Dock = DockStyle.Top;
            labelStatus.Location = new Point(0, 60);
            labelStatus.Name = "labelStatus";
            labelStatus.Size = new Size(544, 20);
            labelStatus.TabIndex = 1;
            labelStatus.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // panelFileList
            // 
            panelFileList.Controls.Add(listViewFiles);
            panelFileList.Dock = DockStyle.Fill;
            panelFileList.Location = new Point(0, 80);
            panelFileList.Name = "panelFileList";
            panelFileList.Padding = new Padding(4);
            panelFileList.Size = new Size(428, 421);
            panelFileList.TabIndex = 2;
            // 
            // listViewFiles
            // 
            listViewFiles.CheckBoxes = true;
            listViewFiles.Columns.AddRange(new ColumnHeader[] { columnFileName, columnSize, columnDate });
            listViewFiles.Dock = DockStyle.Fill;
            listViewFiles.FullRowSelect = true;
            listViewFiles.GridLines = true;
            listViewFiles.Location = new Point(4, 4);
            listViewFiles.Name = "listViewFiles";
            listViewFiles.OwnerDraw = true;
            listViewFiles.ShowItemToolTips = true;
            listViewFiles.Size = new Size(420, 413);
            listViewFiles.TabIndex = 0;
            listViewFiles.UseCompatibleStateImageBehavior = false;
            listViewFiles.View = View.Details;
            listViewFiles.ColumnClick += listViewFiles_ColumnClick;
            listViewFiles.DrawColumnHeader += listViewFiles_DrawColumnHeader;
            listViewFiles.DrawItem += listViewFiles_DrawItem;
            // 
            // columnFileName
            // 
            columnFileName.Text = "ファイル名";
            columnFileName.Width = 140;
            // 
            // columnSize
            // 
            columnSize.Text = "サイズ";
            columnSize.TextAlign = HorizontalAlignment.Right;
            columnSize.Width = 90;
            // 
            // columnDate
            // 
            columnDate.Text = "日付";
            columnDate.Width = 160;
            // 
            // FormDiskImageTool
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(544, 501);
            Controls.Add(panelFileList);
            Controls.Add(panelRight);
            Controls.Add(labelStatus);
            Controls.Add(panelTop);
            Font = new Font("Yu Gothic UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 128);
            Name = "FormDiskImageTool";
            Text = "ディスクイメージ ファイル抽出";
            FormClosing += formDiskImageTool_FormClosing;
            panelTop.ResumeLayout(false);
            panelFileName.ResumeLayout(false);
            panelSelectFile.ResumeLayout(false);
            panelRight.ResumeLayout(false);
            panelRight.PerformLayout();
            panelFileList.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Button buttonExtractAll;
        private Button buttonSelectFile;
        private Label labelFileName;
        private Panel panelTop;
        private Button buttonExtract;
        private Panel panelRight;
        private OpenFileDialog openFileDialog1;
        private Label labelStatus;
        private Panel panelSelectFile;
        private Panel panelFileList;
        private FolderBrowserDialog folderBrowserDialog1;
        private ListView listViewFiles;
        private ColumnHeader columnFileName;
        private ColumnHeader columnSize;
        private ColumnHeader columnDate;
        private Button buttonFATInfo;
        private CheckBox checkIsUTC;
        private Panel panelFileName;
        private Button buttonVersionInfo;
    }
}
