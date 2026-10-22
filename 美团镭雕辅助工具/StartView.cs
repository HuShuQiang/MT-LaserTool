using System;
using System.Drawing;
using System.Windows.Forms;

namespace 美团镭雕辅助工具
{
    public partial class StartView : Form
    {
        public StartView()
        {
            InitializeComponent();
            this.KeyPreview = true;
            Focus();
            
        }

        public void SetColor(Color color)
        {
            tableLayoutPanel1.BackColor = color;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
            {
                DialogResult = DialogResult.Yes;
                e.Handled = true; 
            }
            else
            {
                DialogResult = DialogResult.No;
                e.Handled = true; 
            }
            base.OnKeyDown(e);

            Close();
        }

        private void StartView_Load(object sender, EventArgs e)
        {
            TopLevel = true;
            Select();
            Focus();
        }
    }
}
