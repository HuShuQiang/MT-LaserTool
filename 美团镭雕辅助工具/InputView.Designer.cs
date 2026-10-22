namespace 美团镭雕辅助工具
{
    partial class InputView
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
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.InputName = new System.Windows.Forms.Label();
            this.InputValue = new System.Windows.Forms.TextBox();
            this.Tips = new System.Windows.Forms.Label();
            this.Stop_Btn = new System.Windows.Forms.Button();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.BackColor = System.Drawing.Color.MediumTurquoise;
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.InputName, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.InputValue, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.Tips, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.Stop_Btn, 0, 4);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(8, 9, 8, 9);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 5;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 65F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 65F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(778, 444);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // InputName
            // 
            this.InputName.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.InputName.AutoSize = true;
            this.InputName.Font = new System.Drawing.Font("微软雅黑", 20F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.InputName.Location = new System.Drawing.Point(264, 56);
            this.InputName.Name = "InputName";
            this.InputName.Size = new System.Drawing.Size(250, 52);
            this.InputName.TabIndex = 0;
            this.InputName.Text = "InputName";
            // 
            // InputValue
            // 
            this.InputValue.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.InputValue.ImeMode = System.Windows.Forms.ImeMode.Disable;
            this.InputValue.Location = new System.Drawing.Point(3, 118);
            this.InputValue.Name = "InputValue";
            this.InputValue.Size = new System.Drawing.Size(772, 60);
            this.InputValue.TabIndex = 1;
            this.InputValue.KeyDown += new System.Windows.Forms.KeyEventHandler(this.InputValue_KeyDown);
            // 
            // Tips
            // 
            this.Tips.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.Tips.AutoSize = true;
            this.Tips.Font = new System.Drawing.Font("微软雅黑", 20F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.Tips.Location = new System.Drawing.Point(336, 204);
            this.Tips.Name = "Tips";
            this.Tips.Size = new System.Drawing.Size(106, 52);
            this.Tips.TabIndex = 2;
            this.Tips.Text = "Tips";
            // 
            // Stop_Btn
            // 
            this.Stop_Btn.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.Stop_Btn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.Stop_Btn.Enabled = false;
            this.Stop_Btn.Location = new System.Drawing.Point(289, 324);
            this.Stop_Btn.Name = "Stop_Btn";
            this.Stop_Btn.Size = new System.Drawing.Size(200, 75);
            this.Stop_Btn.TabIndex = 3;
            this.Stop_Btn.Text = "停止运行";
            this.Stop_Btn.UseVisualStyleBackColor = false;
            this.Stop_Btn.Visible = false;
            this.Stop_Btn.Click += new System.EventHandler(this.Stop_Btn_Click);
            // 
            // InputView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(144F, 144F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(778, 444);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Font = new System.Drawing.Font("微软雅黑", 20F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.Margin = new System.Windows.Forms.Padding(8, 9, 8, 9);
            this.Name = "InputView";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "InputView";
            this.TopMost = true;
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label InputName;
        private System.Windows.Forms.TextBox InputValue;
        private System.Windows.Forms.Label Tips;
        private System.Windows.Forms.Button Stop_Btn;
    }
}