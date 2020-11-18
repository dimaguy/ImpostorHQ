
namespace ImpostorHQ.Plugin.Fashionable.Designer
{
    partial class Form1
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.pnlBar = new System.Windows.Forms.Panel();
            this.btnClose = new System.Windows.Forms.Button();
            this.lblTitle = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.cmbHat = new System.Windows.Forms.ComboBox();
            this.gbControls = new System.Windows.Forms.GroupBox();
            this.label3 = new System.Windows.Forms.Label();
            this.cmbPets = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.cmbSkin = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.pcPet = new System.Windows.Forms.PictureBox();
            this.pcChl = new System.Windows.Forms.PictureBox();
            this.pcHat = new System.Windows.Forms.PictureBox();
            this.pcCharacter = new System.Windows.Forms.PictureBox();
            this.btnExport = new System.Windows.Forms.Button();
            this.txtMessage = new System.Windows.Forms.TextBox();
            this.pnlBar.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.gbControls.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pcPet)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pcChl)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pcHat)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pcCharacter)).BeginInit();
            this.SuspendLayout();
            // 
            // pnlBar
            // 
            this.pnlBar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.pnlBar.Controls.Add(this.btnClose);
            this.pnlBar.Controls.Add(this.lblTitle);
            this.pnlBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlBar.Location = new System.Drawing.Point(0, 0);
            this.pnlBar.Name = "pnlBar";
            this.pnlBar.Size = new System.Drawing.Size(578, 38);
            this.pnlBar.TabIndex = 0;
            this.pnlBar.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pnlBar_MouseDown);
            // 
            // btnClose
            // 
            this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClose.FlatAppearance.BorderColor = System.Drawing.Color.Red;
            this.btnClose.FlatAppearance.BorderSize = 2;
            this.btnClose.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Red;
            this.btnClose.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClose.Location = new System.Drawing.Point(548, 7);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(24, 24);
            this.btnClose.TabIndex = 1;
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 15F);
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Location = new System.Drawing.Point(5, 5);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(353, 28);
            this.lblTitle.TabIndex = 1;
            this.lblTitle.Text = "ImpostorHQ Fashionable Skin Designer";
            this.lblTitle.MouseDown += new System.Windows.Forms.MouseEventHandler(this.lblTitle_MouseDown);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.pcChl);
            this.groupBox1.Controls.Add(this.pcHat);
            this.groupBox1.Controls.Add(this.pcCharacter);
            this.groupBox1.ForeColor = System.Drawing.Color.Orange;
            this.groupBox1.Location = new System.Drawing.Point(16, 55);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(87, 260);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Preview";
            // 
            // cmbHat
            // 
            this.cmbHat.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.cmbHat.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbHat.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbHat.ForeColor = System.Drawing.Color.White;
            this.cmbHat.FormattingEnabled = true;
            this.cmbHat.Location = new System.Drawing.Point(16, 40);
            this.cmbHat.Name = "cmbHat";
            this.cmbHat.Size = new System.Drawing.Size(121, 21);
            this.cmbHat.TabIndex = 2;
            this.cmbHat.SelectedIndexChanged += new System.EventHandler(this.cmbHat_SelectedIndexChanged);
            // 
            // gbControls
            // 
            this.gbControls.Controls.Add(this.txtMessage);
            this.gbControls.Controls.Add(this.btnExport);
            this.gbControls.Controls.Add(this.pictureBox1);
            this.gbControls.Controls.Add(this.label3);
            this.gbControls.Controls.Add(this.cmbPets);
            this.gbControls.Controls.Add(this.pcPet);
            this.gbControls.Controls.Add(this.label2);
            this.gbControls.Controls.Add(this.cmbSkin);
            this.gbControls.Controls.Add(this.label1);
            this.gbControls.Controls.Add(this.cmbHat);
            this.gbControls.ForeColor = System.Drawing.Color.White;
            this.gbControls.Location = new System.Drawing.Point(109, 65);
            this.gbControls.Name = "gbControls";
            this.gbControls.Size = new System.Drawing.Size(457, 250);
            this.gbControls.TabIndex = 3;
            this.gbControls.TabStop = false;
            this.gbControls.Text = "Creator";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.label3.ForeColor = System.Drawing.Color.White;
            this.label3.Location = new System.Drawing.Point(16, 120);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(26, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "Pet:";
            // 
            // cmbPets
            // 
            this.cmbPets.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.cmbPets.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPets.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbPets.ForeColor = System.Drawing.Color.White;
            this.cmbPets.FormattingEnabled = true;
            this.cmbPets.Location = new System.Drawing.Point(16, 133);
            this.cmbPets.Name = "cmbPets";
            this.cmbPets.Size = new System.Drawing.Size(121, 21);
            this.cmbPets.TabIndex = 7;
            this.cmbPets.SelectedIndexChanged += new System.EventHandler(this.cmbPets_SelectedIndexChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.label2.ForeColor = System.Drawing.Color.White;
            this.label2.Location = new System.Drawing.Point(16, 71);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(32, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Skin:";
            // 
            // cmbSkin
            // 
            this.cmbSkin.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.cmbSkin.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSkin.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbSkin.ForeColor = System.Drawing.Color.White;
            this.cmbSkin.FormattingEnabled = true;
            this.cmbSkin.Location = new System.Drawing.Point(16, 84);
            this.cmbSkin.Name = "cmbSkin";
            this.cmbSkin.Size = new System.Drawing.Size(121, 21);
            this.cmbSkin.TabIndex = 4;
            this.cmbSkin.SelectedIndexChanged += new System.EventHandler(this.cmbSkin_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Location = new System.Drawing.Point(16, 27);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(28, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Hat:";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::ImpostorHQ.Plugin.Fashionable.Designer.Properties.Resources.hq_header;
            this.pictureBox1.Location = new System.Drawing.Point(202, 21);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(240, 120);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 8;
            this.pictureBox1.TabStop = false;
            // 
            // pcPet
            // 
            this.pcPet.Location = new System.Drawing.Point(143, 104);
            this.pcPet.Name = "pcPet";
            this.pcPet.Size = new System.Drawing.Size(60, 54);
            this.pcPet.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pcPet.TabIndex = 5;
            this.pcPet.TabStop = false;
            // 
            // pcChl
            // 
            this.pcChl.Location = new System.Drawing.Point(11, 173);
            this.pcChl.Name = "pcChl";
            this.pcChl.Size = new System.Drawing.Size(68, 66);
            this.pcChl.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pcChl.TabIndex = 4;
            this.pcChl.TabStop = false;
            // 
            // pcHat
            // 
            this.pcHat.Location = new System.Drawing.Point(16, 20);
            this.pcHat.Name = "pcHat";
            this.pcHat.Size = new System.Drawing.Size(61, 50);
            this.pcHat.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pcHat.TabIndex = 3;
            this.pcHat.TabStop = false;
            // 
            // pcCharacter
            // 
            this.pcCharacter.Image = global::ImpostorHQ.Plugin.Fashionable.Designer.Properties.Resources._base;
            this.pcCharacter.Location = new System.Drawing.Point(6, 47);
            this.pcCharacter.Name = "pcCharacter";
            this.pcCharacter.Size = new System.Drawing.Size(75, 125);
            this.pcCharacter.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pcCharacter.TabIndex = 2;
            this.pcCharacter.TabStop = false;
            // 
            // btnExport
            // 
            this.btnExport.FlatAppearance.BorderColor = System.Drawing.Color.White;
            this.btnExport.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnExport.ForeColor = System.Drawing.Color.White;
            this.btnExport.Location = new System.Drawing.Point(367, 217);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(75, 23);
            this.btnExport.TabIndex = 9;
            this.btnExport.Text = "Export...";
            this.btnExport.UseVisualStyleBackColor = true;
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click);
            // 
            // txtMessage
            // 
            this.txtMessage.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.txtMessage.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtMessage.ForeColor = System.Drawing.Color.White;
            this.txtMessage.Location = new System.Drawing.Point(19, 174);
            this.txtMessage.Multiline = true;
            this.txtMessage.Name = "txtMessage";
            this.txtMessage.Size = new System.Drawing.Size(330, 66);
            this.txtMessage.TabIndex = 10;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.ClientSize = new System.Drawing.Size(578, 327);
            this.Controls.Add(this.gbControls);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.pnlBar);
            this.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form1";
            this.Text = "ImpostorHQ.Fashionable Designer";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.pnlBar.ResumeLayout(false);
            this.pnlBar.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.gbControls.ResumeLayout(false);
            this.gbControls.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pcPet)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pcChl)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pcHat)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pcCharacter)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pnlBar;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.PictureBox pcCharacter;
        private System.Windows.Forms.ComboBox cmbHat;
        private System.Windows.Forms.GroupBox gbControls;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.PictureBox pcHat;
        private System.Windows.Forms.PictureBox pcChl;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cmbSkin;
        private System.Windows.Forms.PictureBox pcPet;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox cmbPets;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Button btnExport;
        private System.Windows.Forms.TextBox txtMessage;
    }
}

