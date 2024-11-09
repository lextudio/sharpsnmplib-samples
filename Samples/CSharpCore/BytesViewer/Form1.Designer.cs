namespace BytesViewer
{
    partial class Form1
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
            System.Windows.Forms.TreeNode treeNode1 = new System.Windows.Forms.TreeNode("This is where the packet structure is showed.");
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            txtBytes = new System.Windows.Forms.TextBox();
            tvMessage = new System.Windows.Forms.TreeView();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            tstxtUser = new System.Windows.Forms.ToolStripTextBox();
            tscbAuthentication = new System.Windows.Forms.ToolStripComboBox();
            tstxtAuthentication = new System.Windows.Forms.ToolStripTextBox();
            tscbPrivacy = new System.Windows.Forms.ToolStripComboBox();
            tstxtPrivacy = new System.Windows.Forms.ToolStripTextBox();
            tsbtnAnalyze = new System.Windows.Forms.ToolStripButton();
            splitContainer1 = new System.Windows.Forms.SplitContainer();
            toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            SuspendLayout();
            // 
            // txtBytes
            // 
            txtBytes.Dock = System.Windows.Forms.DockStyle.Fill;
            txtBytes.Location = new System.Drawing.Point(0, 0);
            txtBytes.Margin = new System.Windows.Forms.Padding(7, 8, 7, 8);
            txtBytes.Multiline = true;
            txtBytes.Name = "txtBytes";
            txtBytes.PlaceholderText = "This is where you paste the HEX string, such as \"30 3E 02 01 03 30 11 02 04 00 9A 6B 7A 02 03 00 FF E3 ...\"";
            txtBytes.Size = new System.Drawing.Size(1777, 347);
            txtBytes.TabIndex = 0;
            // 
            // tvMessage
            // 
            tvMessage.Dock = System.Windows.Forms.DockStyle.Fill;
            tvMessage.Location = new System.Drawing.Point(0, 0);
            tvMessage.Margin = new System.Windows.Forms.Padding(7, 8, 7, 8);
            tvMessage.Name = "tvMessage";
            treeNode1.Name = "Node0";
            treeNode1.Text = "This is where the packet structure is showed.";
            tvMessage.Nodes.AddRange(new System.Windows.Forms.TreeNode[] { treeNode1 });
            tvMessage.Size = new System.Drawing.Size(1777, 1007);
            tvMessage.TabIndex = 1;
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(32, 32);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { toolStripLabel1, tstxtUser, tscbAuthentication, tstxtAuthentication, tscbPrivacy, tstxtPrivacy, tsbtnAnalyze });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Padding = new System.Windows.Forms.Padding(0, 0, 5, 0);
            toolStrip1.Size = new System.Drawing.Size(1777, 47);
            toolStrip1.TabIndex = 2;
            toolStrip1.Text = "toolStrip1";
            // 
            // toolStripLabel1
            // 
            toolStripLabel1.Name = "toolStripLabel1";
            toolStripLabel1.Size = new System.Drawing.Size(70, 41);
            toolStripLabel1.Text = "User";
            // 
            // tstxtUser
            // 
            tstxtUser.Name = "tstxtUser";
            tstxtUser.Size = new System.Drawing.Size(244, 47);
            // 
            // tscbAuthentication
            // 
            tscbAuthentication.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            tscbAuthentication.Items.AddRange(new object[] { "None", "MD5", "SHA-1" });
            tscbAuthentication.Name = "tscbAuthentication";
            tscbAuthentication.Size = new System.Drawing.Size(297, 47);
            // 
            // tstxtAuthentication
            // 
            tstxtAuthentication.Name = "tstxtAuthentication";
            tstxtAuthentication.Size = new System.Drawing.Size(244, 47);
            // 
            // tscbPrivacy
            // 
            tscbPrivacy.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            tscbPrivacy.Items.AddRange(new object[] { "None", "DES", "AES" });
            tscbPrivacy.Name = "tscbPrivacy";
            tscbPrivacy.Size = new System.Drawing.Size(297, 47);
            // 
            // tstxtPrivacy
            // 
            tstxtPrivacy.Name = "tstxtPrivacy";
            tstxtPrivacy.Size = new System.Drawing.Size(244, 47);
            // 
            // tsbtnAnalyze
            // 
            tsbtnAnalyze.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            tsbtnAnalyze.Image = (System.Drawing.Image)resources.GetObject("tsbtnAnalyze.Image");
            tsbtnAnalyze.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsbtnAnalyze.Name = "tsbtnAnalyze";
            tsbtnAnalyze.Size = new System.Drawing.Size(113, 41);
            tsbtnAnalyze.Text = "Analyze";
            tsbtnAnalyze.Click += txtBytes_TextChanged;
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            splitContainer1.Location = new System.Drawing.Point(0, 47);
            splitContainer1.Name = "splitContainer1";
            splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(txtBytes);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(tvMessage);
            splitContainer1.Size = new System.Drawing.Size(1777, 1359);
            splitContainer1.SplitterDistance = 347;
            splitContainer1.SplitterWidth = 5;
            splitContainer1.TabIndex = 3;
            // 
            // Form1
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(15F, 37F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1777, 1406);
            Controls.Add(splitContainer1);
            Controls.Add(toolStrip1);
            Margin = new System.Windows.Forms.Padding(7, 8, 7, 8);
            Name = "Form1";
            Text = "Form1";
            FormClosing += Form1_FormClosing;
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel1.PerformLayout();
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.TextBox txtBytes;
        private System.Windows.Forms.TreeView tvMessage;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private System.Windows.Forms.ToolStripTextBox tstxtUser;
        private System.Windows.Forms.ToolStripComboBox tscbAuthentication;
        private System.Windows.Forms.ToolStripTextBox tstxtAuthentication;
        private System.Windows.Forms.ToolStripComboBox tscbPrivacy;
        private System.Windows.Forms.ToolStripTextBox tstxtPrivacy;
        private System.Windows.Forms.ToolStripButton tsbtnAnalyze;
        private System.Windows.Forms.SplitContainer splitContainer1;
    }
}

