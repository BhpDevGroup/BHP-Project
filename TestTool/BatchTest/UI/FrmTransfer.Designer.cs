namespace Bhp.UI
{
    partial class FrmTransfer
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
            this.button1 = new System.Windows.Forms.Button();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.listBox2 = new System.Windows.Forms.ListBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.button3 = new System.Windows.Forms.Button();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripProgressBar1 = new System.Windows.Forms.ToolStripProgressBar();
            this.lbl_from = new System.Windows.Forms.Label();
            this.lbl_to = new System.Windows.Forms.Label();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.btn_to = new System.Windows.Forms.Button();
            this.btn_timer = new System.Windows.Forms.Button();
            this.lbl_one = new System.Windows.Forms.Label();
            this.txt_one = new System.Windows.Forms.TextBox();
            this.btn_one = new System.Windows.Forms.Button();
            this.btn_many = new System.Windows.Forms.Button();
            this.txt_value = new System.Windows.Forms.TextBox();
            this.lbl_value = new System.Windows.Forms.Label();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.lbl_password = new System.Windows.Forms.Label();
            this.txt_password = new System.Windows.Forms.TextBox();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(382, 23);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 0;
            this.button1.Text = "From";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // listBox1
            // 
            this.listBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listBox1.FormattingEnabled = true;
            this.listBox1.ItemHeight = 12;
            this.listBox1.Location = new System.Drawing.Point(3, 17);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(464, 350);
            this.listBox1.TabIndex = 1;
            // 
            // textBox1
            // 
            this.textBox1.Enabled = false;
            this.textBox1.Location = new System.Drawing.Point(82, 25);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(280, 21);
            this.textBox1.TabIndex = 2;
            // 
            // listBox2
            // 
            this.listBox2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listBox2.FormattingEnabled = true;
            this.listBox2.ItemHeight = 12;
            this.listBox2.Location = new System.Drawing.Point(3, 17);
            this.listBox2.Name = "listBox2";
            this.listBox2.Size = new System.Drawing.Size(464, 350);
            this.listBox2.TabIndex = 1;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.listBox1);
            this.groupBox1.Location = new System.Drawing.Point(12, 198);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(470, 370);
            this.groupBox1.TabIndex = 3;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "From";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.listBox2);
            this.groupBox2.Location = new System.Drawing.Point(507, 198);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(470, 370);
            this.groupBox2.TabIndex = 4;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "To";
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(82, 65);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(75, 23);
            this.button3.TabIndex = 5;
            this.button3.Text = "Transfer";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Location = new System.Drawing.Point(0, 572);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(1016, 22);
            this.statusStrip1.TabIndex = 6;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripProgressBar1
            // 
            this.toolStripProgressBar1.Name = "toolStripProgressBar1";
            this.toolStripProgressBar1.Size = new System.Drawing.Size(100, 16);
            // 
            // lbl_from
            // 
            this.lbl_from.AutoSize = true;
            this.lbl_from.Location = new System.Drawing.Point(19, 28);
            this.lbl_from.Name = "lbl_from";
            this.lbl_from.Size = new System.Drawing.Size(47, 12);
            this.lbl_from.TabIndex = 7;
            this.lbl_from.Text = "from : ";
            // 
            // lbl_to
            // 
            this.lbl_to.AutoSize = true;
            this.lbl_to.Location = new System.Drawing.Point(18, 27);
            this.lbl_to.Name = "lbl_to";
            this.lbl_to.Size = new System.Drawing.Size(35, 12);
            this.lbl_to.TabIndex = 8;
            this.lbl_to.Text = "to : ";
            // 
            // textBox2
            // 
            this.textBox2.Enabled = false;
            this.textBox2.Location = new System.Drawing.Point(77, 24);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(280, 21);
            this.textBox2.TabIndex = 9;
            // 
            // btn_to
            // 
            this.btn_to.Location = new System.Drawing.Point(378, 22);
            this.btn_to.Name = "btn_to";
            this.btn_to.Size = new System.Drawing.Size(75, 23);
            this.btn_to.TabIndex = 10;
            this.btn_to.Text = "To";
            this.btn_to.UseVisualStyleBackColor = true;
            this.btn_to.Click += new System.EventHandler(this.btn_to_Click);
            // 
            // btn_timer
            // 
            this.btn_timer.Location = new System.Drawing.Point(245, 65);
            this.btn_timer.Name = "btn_timer";
            this.btn_timer.Size = new System.Drawing.Size(117, 23);
            this.btn_timer.TabIndex = 11;
            this.btn_timer.Text = "Timer Start";
            this.btn_timer.UseVisualStyleBackColor = true;
            this.btn_timer.Click += new System.EventHandler(this.btn_timer_Click);
            // 
            // lbl_one
            // 
            this.lbl_one.AutoSize = true;
            this.lbl_one.Location = new System.Drawing.Point(18, 28);
            this.lbl_one.Name = "lbl_one";
            this.lbl_one.Size = new System.Drawing.Size(41, 12);
            this.lbl_one.TabIndex = 12;
            this.lbl_one.Text = "one : ";
            // 
            // txt_one
            // 
            this.txt_one.Enabled = false;
            this.txt_one.Location = new System.Drawing.Point(78, 25);
            this.txt_one.Name = "txt_one";
            this.txt_one.Size = new System.Drawing.Size(280, 21);
            this.txt_one.TabIndex = 13;
            // 
            // btn_one
            // 
            this.btn_one.Location = new System.Drawing.Point(378, 23);
            this.btn_one.Name = "btn_one";
            this.btn_one.Size = new System.Drawing.Size(75, 23);
            this.btn_one.TabIndex = 14;
            this.btn_one.Text = "one";
            this.btn_one.UseVisualStyleBackColor = true;
            this.btn_one.Click += new System.EventHandler(this.btn_one_Click);
            // 
            // btn_many
            // 
            this.btn_many.Location = new System.Drawing.Point(378, 59);
            this.btn_many.Name = "btn_many";
            this.btn_many.Size = new System.Drawing.Size(75, 23);
            this.btn_many.TabIndex = 15;
            this.btn_many.Text = "send many";
            this.btn_many.UseVisualStyleBackColor = true;
            this.btn_many.Click += new System.EventHandler(this.btn_many_Click);
            // 
            // txt_value
            // 
            this.txt_value.Location = new System.Drawing.Point(77, 61);
            this.txt_value.Name = "txt_value";
            this.txt_value.Size = new System.Drawing.Size(280, 21);
            this.txt_value.TabIndex = 16;
            this.txt_value.Text = "100";
            // 
            // lbl_value
            // 
            this.lbl_value.AutoSize = true;
            this.lbl_value.Location = new System.Drawing.Point(18, 70);
            this.lbl_value.Name = "lbl_value";
            this.lbl_value.Size = new System.Drawing.Size(53, 12);
            this.lbl_value.TabIndex = 17;
            this.lbl_value.Text = "value : ";
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.txt_one);
            this.groupBox3.Controls.Add(this.lbl_value);
            this.groupBox3.Controls.Add(this.lbl_one);
            this.groupBox3.Controls.Add(this.btn_many);
            this.groupBox3.Controls.Add(this.txt_value);
            this.groupBox3.Controls.Add(this.btn_one);
            this.groupBox3.Location = new System.Drawing.Point(15, 91);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(469, 100);
            this.groupBox3.TabIndex = 18;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "groupBox_one";
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.textBox1);
            this.groupBox4.Controls.Add(this.lbl_from);
            this.groupBox4.Controls.Add(this.button1);
            this.groupBox4.Controls.Add(this.btn_timer);
            this.groupBox4.Controls.Add(this.button3);
            this.groupBox4.Location = new System.Drawing.Point(507, 91);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(469, 100);
            this.groupBox4.TabIndex = 19;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "groupBox_many";
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.lbl_password);
            this.groupBox5.Controls.Add(this.txt_password);
            this.groupBox5.Controls.Add(this.lbl_to);
            this.groupBox5.Controls.Add(this.textBox2);
            this.groupBox5.Controls.Add(this.btn_to);
            this.groupBox5.Location = new System.Drawing.Point(15, 11);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Size = new System.Drawing.Size(961, 74);
            this.groupBox5.TabIndex = 20;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "groupBox_common";
            // 
            // lbl_password
            // 
            this.lbl_password.AutoSize = true;
            this.lbl_password.Location = new System.Drawing.Point(487, 27);
            this.lbl_password.Name = "lbl_password";
            this.lbl_password.Size = new System.Drawing.Size(71, 12);
            this.lbl_password.TabIndex = 11;
            this.lbl_password.Text = "password : ";
            // 
            // txt_password
            // 
            this.txt_password.Location = new System.Drawing.Point(574, 24);
            this.txt_password.Name = "txt_password";
            this.txt_password.Size = new System.Drawing.Size(280, 21);
            this.txt_password.TabIndex = 12;
            this.txt_password.Text = "123456";
            // 
            // FrmTransfer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1016, 594);
            this.Controls.Add(this.groupBox5);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Name = "FrmTransfer";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "FrmTransfer";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmTransfer_FormClosing);
            this.Shown += new System.EventHandler(this.FrmTransfer_Shown);
            this.groupBox1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.groupBox5.ResumeLayout(false);
            this.groupBox5.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.ListBox listBox2;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripProgressBar toolStripProgressBar1;
        private System.Windows.Forms.Label lbl_from;
        private System.Windows.Forms.Label lbl_to;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.Button btn_to;
        private System.Windows.Forms.Button btn_timer;
        private System.Windows.Forms.Label lbl_one;
        private System.Windows.Forms.TextBox txt_one;
        private System.Windows.Forms.Button btn_one;
        private System.Windows.Forms.Button btn_many;
        private System.Windows.Forms.TextBox txt_value;
        private System.Windows.Forms.Label lbl_value;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.Label lbl_password;
        private System.Windows.Forms.TextBox txt_password;
    }
}