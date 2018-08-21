using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Justin.Updater.Client
{
    public partial class FrmProgress : Form
    {
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        public static extern bool SendMessage(IntPtr hwnd, int wMsg, int wParam, int lParam);
        
        public FrmProgress()
        {
            InitializeComponent();

            BindEvents();
        }
        
        private void FrmProgress_FormClosing(object sender, FormClosingEventArgs e)
        {
            UnBindEvents();
        }

        private void BindEvents()
        {
            UpdateHelper.OnProgress += OnProgress;
            UpdateHelper.OnProcessFile += OnProcessFile; ;
        }
        private void UnBindEvents()
        {
            UpdateHelper.OnProgress -= OnProgress;
            UpdateHelper.OnProcessFile -= OnProcessFile; ;
        }

        private void OnProcessFile(string msg)
        {
            this.Invoke((Action)(() =>
            {
                lbStatus.Text = msg;
            }));
        }
        private void OnProgress(double progress)
        {
            this.Invoke((Action)(() => 
            {
                progressBar.Value = (int)(progressBar.Maximum * progress);
            }));
        }

        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            ReleaseCapture();
            SendMessage(this.Handle, 0x0112, 0xF012, 0);
        }
    }
}
