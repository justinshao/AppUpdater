using System;
using System.Windows.Forms;

namespace Justin.Updater.Client
{
    public partial class FrmInput : Form
    {
        private bool _required;

        public FrmInput(string title, string value = "", bool required = false)
        {
            InitializeComponent();

            this._required = required;
            this.Text = title;
            this.lbTitle.Text = title;
            this.txtInput.Text = value;
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Value))
                return;

            DialogResult = DialogResult.OK;
        }
        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (this._required && string.IsNullOrEmpty(Value))
                return;

            DialogResult = DialogResult.Cancel;
        }

        public string Value
        {
            get { return txtInput.Text == null ? string.Empty : txtInput.Text.Trim(); }
        }

        private void txtInput_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Enter && !string.IsNullOrEmpty(Value))
            {
                DialogResult = DialogResult.OK;
            }
        }
    }
}
