using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace 美团镭雕辅助工具
{
    public partial class SetupView : Form
    {
        public SetupView()
        {
            InitializeComponent();
            InitConfig();
        }

        private void InitConfig()
        {
            IP.Text = Properties.Settings.Default.IP;
            Port.Text = Properties.Settings.Default.Port;
            Field.Text = Properties.Settings.Default.Field;
            Prefix.Text = Properties.Settings.Default.Prefix;
            SNRegex.Text = Properties.Settings.Default.SNRegex;
            CheckField.Text = Properties.Settings.Default.CheckField;
            ReadDelay.Text = Properties.Settings.Default.ReadDelay;
            Save_Btn.Text = "保存设置";
            Save_Btn.BackColor = Color.OrangeRed;
        }

        private void Save_Btn_Click(object sender, EventArgs e)
        {
            try
            {
                Properties.Settings.Default.IP = IP.Text;
                Properties.Settings.Default.Port = Port.Text;
                Properties.Settings.Default.Field = Field.Text;
                Properties.Settings.Default.Prefix = Prefix.Text;
                Properties.Settings.Default.SNRegex = SNRegex.Text;
                Properties.Settings.Default.CheckField = CheckField.Text;
                Properties.Settings.Default.ReadDelay = ReadDelay.Text;
                Properties.Settings.Default.Save();
                Save_Btn.Text = "保存成功";
                Save_Btn.BackColor = Color.LightGreen;
                Close();
            }
            catch(Exception ex)
            {
                MessageBox.Show($"保存配置时发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        private void SetupView_TextChanged(object sender, EventArgs e)
        {
            if(Prefix.Text != Properties.Settings.Default.Prefix || SNRegex.Text != Properties.Settings.Default.SNRegex || IP.Text != Properties.Settings.Default.IP || Port.Text != Properties.Settings.Default.Port || Field.Text != Properties.Settings.Default.Field)
            {
                Save_Btn.Text = "保存设置";
                Save_Btn.BackColor = Color.OrangeRed;
            }
            else
            {
                Save_Btn.Text = "保存成功";
                Save_Btn.BackColor = Color.LightGreen;
            }

        }
    }
}
