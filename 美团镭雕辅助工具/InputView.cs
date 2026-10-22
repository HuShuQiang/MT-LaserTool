using System.Drawing;
using System.Windows.Forms;

namespace 美团镭雕辅助工具
{
    public partial class InputView : Form
    {
        public string InputText = string.Empty;
        public InputView()
        {
            InitializeComponent();
            Text = "请输入内容";
            InputName.Text = Properties.Settings.Default.CheckField;
        }

        private void InputValue_KeyDown(object sender, KeyEventArgs e)
        {

            if (e.KeyCode == Keys.Enter)
            {
                if (!string.IsNullOrEmpty(InputValue.Text))
                {
                    InputText = InputValue.Text;
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    Tips.Text = "输入内容不能为空！";
                    Tips.ForeColor = Color.Red;
                }
            }



        }

        private void Stop_Btn_Click(object sender, System.EventArgs e)
        {
            DialogResult = DialogResult.Abort;
            Close();
        }
    }
}
