namespace Justin.Updater.Client
{
    partial class FrmUpdateNotify
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
            this.btnUpdate = new System.Windows.Forms.Button();
            this.btnIgnore = new System.Windows.Forms.Button();
            this.labelInfo = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // btnUpdate
            // 
            this.btnUpdate.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.btnUpdate.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnUpdate.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnUpdate.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.btnUpdate.Location = new System.Drawing.Point(38, 81);
            this.btnUpdate.Name = "btnUpdate";
            this.btnUpdate.Size = new System.Drawing.Size(81, 35);
            this.btnUpdate.TabIndex = 0;
            this.btnUpdate.Text = "更 新";
            this.btnUpdate.UseVisualStyleBackColor = false;
            this.btnUpdate.Click += new System.EventHandler(this.btnUpdate_Click);
            // 
            // btnIgnore
            // 
            this.btnIgnore.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.btnIgnore.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnIgnore.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnIgnore.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.btnIgnore.Location = new System.Drawing.Point(155, 81);
            this.btnIgnore.Name = "btnIgnore";
            this.btnIgnore.Size = new System.Drawing.Size(81, 35);
            this.btnIgnore.TabIndex = 1;
            this.btnIgnore.Text = "忽 略";
            this.btnIgnore.UseVisualStyleBackColor = false;
            this.btnIgnore.Click += new System.EventHandler(this.btnIgnore_Click);
            // 
            // labelInfo
            // 
            this.labelInfo.AutoSize = true;
            this.labelInfo.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.labelInfo.ForeColor = System.Drawing.SystemColors.ControlLight;
            this.labelInfo.Location = new System.Drawing.Point(57, 25);
            this.labelInfo.Name = "labelInfo";
            this.labelInfo.Size = new System.Drawing.Size(184, 16);
            this.labelInfo.TabIndex = 2;
            this.labelInfo.Text = "系统有更新，是否更新？";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::Justin.Updater.Client.Properties.Resources.logo;
            this.pictureBox1.Location = new System.Drawing.Point(5, 5);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(42, 42);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 3;
            this.pictureBox1.TabStop = false;
            // 
            // FrmUpdateNotify
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.SteelBlue;
            this.ClientSize = new System.Drawing.Size(273, 135);
            this.ControlBox = false;
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.labelInfo);
            this.Controls.Add(this.btnIgnore);
            this.Controls.Add(this.btnUpdate);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmUpdateNotify";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "FrmUpdateNotify";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.FrmUpdateNotify_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnUpdate;
        private System.Windows.Forms.Button btnIgnore;
        private System.Windows.Forms.Label labelInfo;
        private System.Windows.Forms.PictureBox pictureBox1;
    }
}