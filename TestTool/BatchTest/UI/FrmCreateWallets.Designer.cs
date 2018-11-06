namespace Bhp.UI
{
    partial class FrmCreateWallets
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
            this.btn_create = new System.Windows.Forms.Button();
            this.lbl_num = new System.Windows.Forms.Label();
            this.txt_num = new System.Windows.Forms.TextBox();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.txt_dir = new System.Windows.Forms.TextBox();
            this.btn_choose = new System.Windows.Forms.Button();
            this.lbl_dir = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // btn_create
            // 
            this.btn_create.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.btn_create.Location = new System.Drawing.Point(330, 63);
            this.btn_create.Name = "btn_create";
            this.btn_create.Size = new System.Drawing.Size(75, 23);
            this.btn_create.TabIndex = 17;
            this.btn_create.Text = "create";
            this.btn_create.UseVisualStyleBackColor = true;
            this.btn_create.Click += new System.EventHandler(this.button2_Click);
            // 
            // lbl_num
            // 
            this.lbl_num.AutoSize = true;
            this.lbl_num.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.lbl_num.Location = new System.Drawing.Point(29, 68);
            this.lbl_num.Name = "lbl_num";
            this.lbl_num.Size = new System.Drawing.Size(77, 12);
            this.lbl_num.TabIndex = 10;
            this.lbl_num.Text = "Wallet Num :";
            // 
            // txt_num
            // 
            this.txt_num.Location = new System.Drawing.Point(133, 65);
            this.txt_num.Name = "txt_num";
            this.txt_num.Size = new System.Drawing.Size(149, 21);
            this.txt_num.TabIndex = 18;
            // 
            // listBox1
            // 
            this.listBox1.FormattingEnabled = true;
            this.listBox1.ItemHeight = 12;
            this.listBox1.Location = new System.Drawing.Point(31, 114);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(374, 244);
            this.listBox1.TabIndex = 19;
            // 
            // txt_dir
            // 
            this.txt_dir.Enabled = false;
            this.txt_dir.Location = new System.Drawing.Point(133, 25);
            this.txt_dir.Name = "txt_dir";
            this.txt_dir.Size = new System.Drawing.Size(149, 21);
            this.txt_dir.TabIndex = 22;
            // 
            // btn_choose
            // 
            this.btn_choose.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.btn_choose.Location = new System.Drawing.Point(330, 23);
            this.btn_choose.Name = "btn_choose";
            this.btn_choose.Size = new System.Drawing.Size(75, 23);
            this.btn_choose.TabIndex = 21;
            this.btn_choose.Text = "choose";
            this.btn_choose.UseVisualStyleBackColor = true;
            this.btn_choose.Click += new System.EventHandler(this.button1_Click);
            // 
            // lbl_dir
            // 
            this.lbl_dir.AutoSize = true;
            this.lbl_dir.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.lbl_dir.Location = new System.Drawing.Point(29, 28);
            this.lbl_dir.Name = "lbl_dir";
            this.lbl_dir.Size = new System.Drawing.Size(83, 12);
            this.lbl_dir.TabIndex = 20;
            this.lbl_dir.Text = "Wallet Dir : ";
            // 
            // FrmCreateWallets
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(439, 372);
            this.Controls.Add(this.txt_dir);
            this.Controls.Add(this.btn_choose);
            this.Controls.Add(this.lbl_dir);
            this.Controls.Add(this.listBox1);
            this.Controls.Add(this.txt_num);
            this.Controls.Add(this.btn_create);
            this.Controls.Add(this.lbl_num);
            this.Name = "FrmCreateWallets";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Create Wallets";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmCreateWallets_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btn_create;
        private System.Windows.Forms.Label lbl_num;
        private System.Windows.Forms.TextBox txt_num;
        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.TextBox txt_dir;
        private System.Windows.Forms.Button btn_choose;
        private System.Windows.Forms.Label lbl_dir;
    }
}