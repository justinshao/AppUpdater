using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Justin.Updater.Client
{
    public partial class FrmUpdateNotify : Form
    {
        [DllImport("user32")]
        private static extern bool AnimateWindow(IntPtr hwnd, int dwTime, int dwFlags);
        private const int AW_HIDE = 0x10000;
        private const int AW_ACTIVE = 0x20000;
        private const int AW_BLEND = 0x80000; // 淡入淡出效果

        /// <summary>
        /// 是否已存在一个窗体实例
        /// </summary>
        private static bool _exists = false;

        public static void Show(Action onUpdateRequest, Action OnUpdateIgnore)
        {
            if(!_exists)
            {
                FrmUpdateNotify _frmUpdateNotify = new FrmUpdateNotify();
                _frmUpdateNotify.OnUpdateRequest += onUpdateRequest;
                _frmUpdateNotify.OnUpdateIgnore += OnUpdateIgnore;
                _frmUpdateNotify.Show();

                _exists = true;
            }
        }

        int interval = 1000;

        public event Action OnUpdateRequest;
        public event Action OnUpdateIgnore;

        protected override bool ShowWithoutActivation => true;

        public FrmUpdateNotify()
        {
            InitializeComponent();
        }

        private void ShowNotify()
        {
            AnimateWindow(this.Handle, interval, AW_ACTIVE | AW_BLEND);
        }
        private void HideNotify()
        {
            AnimateWindow(this.Handle, interval, AW_HIDE | AW_BLEND);
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            _exists = false;

            OnUpdateRequest?.Invoke();

            this.Dispose();
        }

        private void btnIgnore_Click(object sender, EventArgs e)
        {
            _exists = false;

            OnUpdateIgnore?.Invoke();

            this.Dispose();
        }
        
        private void FrmUpdateNotify_Load(object sender, EventArgs e)
        {
            int x = Screen.PrimaryScreen.WorkingArea.Right - this.Width;
            int y = Screen.PrimaryScreen.WorkingArea.Bottom - this.Height;
            this.Location = new Point(x, y);

            ShowNotify();
        }
    }
}
