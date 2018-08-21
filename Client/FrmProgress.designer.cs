namespace Justin.Updater.Client
{
    partial class FrmProgress
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.lbStatus = new System.Windows.Forms.Label();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.panel1 = new System.Windows.Forms.Panel();
            this.SuspendLayout();
            // 
            // lbStatus
            // 
            this.lbStatus.AutoSize = true;
            this.lbStatus.Location = new System.Drawing.Point(52, 0);
            this.lbStatus.Name = "lbStatus";
            this.lbStatus.Padding = new System.Windows.Forms.Padding(0, 5, 0, 5);
            this.lbStatus.Size = new System.Drawing.Size(95, 22);
            this.lbStatus.TabIndex = 1;
            this.lbStatus.Text = "正在准备下载...";
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(49, 22);
            this.progressBar.Maximum = 1000;
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(554, 21);
            this.progressBar.TabIndex = 2;
            // 
            // panel1
            // 
            this.panel1.BackgroundImage = global::Justin.Updater.Client.Properties.Resources.logo;
            this.panel1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.panel1.Location = new System.Drawing.Point(1, 1);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(42, 42);
            this.panel1.TabIndex = 3;
            this.panel1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.panel1_MouseDown);
            // 
            // FrmProgress
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(605, 44);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.lbStatus);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = global::Justin.Updater.Client.Properties.Resources.logo1;
            this.Name = "FrmProgress";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "安装进度";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmProgress_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lbStatus;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Panel panel1;
    }
}

