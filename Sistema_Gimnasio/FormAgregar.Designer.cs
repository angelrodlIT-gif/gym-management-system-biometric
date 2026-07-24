using System;
using System.Drawing;
using System.Windows.Forms;


namespace Sistema_Gimnasio
{
    partial class FormAgregar
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormAgregar));
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.panel3 = new System.Windows.Forms.Panel();
            this.NombreText = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.panelDatosper = new System.Windows.Forms.Panel();
            this.ContinuarBut = new System.Windows.Forms.Button();
            this.panel5 = new System.Windows.Forms.Panel();
            this.EdadText = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.panel4 = new System.Windows.Forms.Panel();
            this.DireccionText = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.panel2 = new System.Windows.Forms.Panel();
            this.TelefonoText = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.closeButton = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.RegresarBut = new System.Windows.Forms.Button();
            this.AceptarBut = new System.Windows.Forms.Button();
            this.panelBiometrico = new System.Windows.Forms.Panel();
            this.label11 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.panelContenedor = new System.Windows.Forms.Panel();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.panel3.SuspendLayout();
            this.panelDatosper.SuspendLayout();
            this.panel5.SuspendLayout();
            this.panel4.SuspendLayout();
            this.panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.panel1.SuspendLayout();
            this.panelBiometrico.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            this.SuspendLayout();
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.Black;
            this.label2.Location = new System.Drawing.Point(53, 28);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(277, 19);
            this.label2.TabIndex = 21;
            this.label2.Text = "Introduce los datos personales del miembro";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.ForeColor = System.Drawing.Color.Black;
            this.label3.Location = new System.Drawing.Point(52, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(173, 28);
            this.label3.TabIndex = 22;
            this.label3.Text = "Datos Personales";
            // 
            // panel3
            // 
            this.panel3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel3.Controls.Add(this.NombreText);
            this.panel3.Controls.Add(this.label4);
            this.panel3.Location = new System.Drawing.Point(7, 63);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(1001, 67);
            this.panel3.TabIndex = 21;
            // 
            // NombreText
            // 
            this.NombreText.Font = new System.Drawing.Font("Segoe UI Semibold", 10.2F, System.Drawing.FontStyle.Bold);
            this.NombreText.Location = new System.Drawing.Point(8, 38);
            this.NombreText.Name = "NombreText";
            this.NombreText.Size = new System.Drawing.Size(983, 30);
            this.NombreText.TabIndex = 20;
            this.NombreText.TextChanged += new System.EventHandler(this.NombreText_TextChanged_1);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.ForeColor = System.Drawing.Color.Black;
            this.label4.Location = new System.Drawing.Point(3, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(191, 28);
            this.label4.TabIndex = 19;
            this.label4.Text = "Nombre Completo:";
            // 
            // panelDatosper
            // 
            this.panelDatosper.BackColor = System.Drawing.Color.Transparent;
            this.panelDatosper.Controls.Add(this.ContinuarBut);
            this.panelDatosper.Controls.Add(this.panel5);
            this.panelDatosper.Controls.Add(this.panel4);
            this.panelDatosper.Controls.Add(this.panel2);
            this.panelDatosper.Controls.Add(this.label3);
            this.panelDatosper.Controls.Add(this.pictureBox1);
            this.panelDatosper.Controls.Add(this.panel3);
            this.panelDatosper.Controls.Add(this.label2);
            this.panelDatosper.Location = new System.Drawing.Point(16, 63);
            this.panelDatosper.MinimumSize = new System.Drawing.Size(1214, 56);
            this.panelDatosper.Name = "panelDatosper";
            this.panelDatosper.Size = new System.Drawing.Size(1214, 412);
            this.panelDatosper.TabIndex = 3;
            this.panelDatosper.Paint += new System.Windows.Forms.PaintEventHandler(this.panelDatosper_Paint);
            // 
            // ContinuarBut
            // 
            this.ContinuarBut.BackColor = System.Drawing.Color.Transparent;
            this.ContinuarBut.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ContinuarBut.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.ContinuarBut.Location = new System.Drawing.Point(7, 354);
            this.ContinuarBut.Name = "ContinuarBut";
            this.ContinuarBut.Size = new System.Drawing.Size(127, 34);
            this.ContinuarBut.TabIndex = 24;
            this.ContinuarBut.Text = "CONTINUAR";
            this.ContinuarBut.UseVisualStyleBackColor = false;
            this.ContinuarBut.Click += new System.EventHandler(this.ContinuarBut_Click);
            // 
            // panel5
            // 
            this.panel5.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel5.Controls.Add(this.EdadText);
            this.panel5.Controls.Add(this.label9);
            this.panel5.Location = new System.Drawing.Point(7, 265);
            this.panel5.Name = "panel5";
            this.panel5.Size = new System.Drawing.Size(1001, 65);
            this.panel5.TabIndex = 23;
            // 
            // EdadText
            // 
            this.EdadText.BackColor = System.Drawing.Color.White;
            this.EdadText.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.EdadText.Font = new System.Drawing.Font("Segoe UI Semibold", 10.2F, System.Drawing.FontStyle.Bold);
            this.EdadText.Location = new System.Drawing.Point(8, 38);
            this.EdadText.Name = "EdadText";
            this.EdadText.Size = new System.Drawing.Size(983, 30);
            this.EdadText.TabIndex = 20;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label9.ForeColor = System.Drawing.Color.Black;
            this.label9.Location = new System.Drawing.Point(3, 0);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(63, 28);
            this.label9.TabIndex = 19;
            this.label9.Text = "Edad:";
            // 
            // panel4
            // 
            this.panel4.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel4.Controls.Add(this.DireccionText);
            this.panel4.Controls.Add(this.label8);
            this.panel4.Location = new System.Drawing.Point(7, 199);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(1001, 67);
            this.panel4.TabIndex = 23;
            // 
            // DireccionText
            // 
            this.DireccionText.Font = new System.Drawing.Font("Segoe UI Semibold", 10.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.DireccionText.Location = new System.Drawing.Point(8, 38);
            this.DireccionText.Name = "DireccionText";
            this.DireccionText.Size = new System.Drawing.Size(983, 30);
            this.DireccionText.TabIndex = 20;
            this.DireccionText.Text = "Genova entre Camelias y San Salvador.";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.BackColor = System.Drawing.Color.Transparent;
            this.label8.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label8.ForeColor = System.Drawing.Color.Black;
            this.label8.Location = new System.Drawing.Point(3, 0);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(107, 28);
            this.label8.TabIndex = 19;
            this.label8.Text = "Dirección:";
            // 
            // panel2
            // 
            this.panel2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel2.Controls.Add(this.TelefonoText);
            this.panel2.Controls.Add(this.label7);
            this.panel2.Location = new System.Drawing.Point(7, 133);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(1001, 67);
            this.panel2.TabIndex = 22;
            // 
            // TelefonoText
            // 
            this.TelefonoText.Font = new System.Drawing.Font("Segoe UI Semibold", 10.2F, System.Drawing.FontStyle.Bold);
            this.TelefonoText.Location = new System.Drawing.Point(8, 38);
            this.TelefonoText.Name = "TelefonoText";
            this.TelefonoText.Size = new System.Drawing.Size(983, 30);
            this.TelefonoText.TabIndex = 20;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.ForeColor = System.Drawing.Color.Black;
            this.label7.Location = new System.Drawing.Point(3, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(99, 28);
            this.label7.TabIndex = 19;
            this.label7.Text = "Teléfono:";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(13, 10);
            this.pictureBox1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(32, 32);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBox1.TabIndex = 21;
            this.pictureBox1.TabStop = false;
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(92)))), ((int)(((byte)(46)))));
            this.panel1.Controls.Add(this.closeButton);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Location = new System.Drawing.Point(-14, -2);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1282, 59);
            this.panel1.TabIndex = 25;
            this.panel1.Paint += new System.Windows.Forms.PaintEventHandler(this.panel1_Paint);
            // 
            // closeButton
            // 
            this.closeButton.BackColor = System.Drawing.Color.Transparent;
            this.closeButton.Image = global::Sistema_Gimnasio.Properties.Resources.icons8_close_25__1_;
            this.closeButton.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.closeButton.Location = new System.Drawing.Point(1223, 11);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(42, 33);
            this.closeButton.TabIndex = 14;
            this.closeButton.UseVisualStyleBackColor = false;
            this.closeButton.Click += new System.EventHandler(this.closeButton_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.Black;
            this.label1.Location = new System.Drawing.Point(32, 8);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(284, 41);
            this.label1.TabIndex = 24;
            this.label1.Text = "Registrar Miembro";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.BackColor = System.Drawing.Color.Transparent;
            this.label6.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.ForeColor = System.Drawing.Color.Black;
            this.label6.Location = new System.Drawing.Point(53, 28);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(314, 19);
            this.label6.TabIndex = 21;
            this.label6.Text = "Introduce los datos para la identidad del miembro";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.ForeColor = System.Drawing.Color.Black;
            this.label5.Location = new System.Drawing.Point(52, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(307, 28);
            this.label5.TabIndex = 22;
            this.label5.Text = "Datos Biométricos y Fotografía";
            // 
            // RegresarBut
            // 
            this.RegresarBut.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(46)))), ((int)(((byte)(38)))));
            this.RegresarBut.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.RegresarBut.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.RegresarBut.Location = new System.Drawing.Point(27, 355);
            this.RegresarBut.Name = "RegresarBut";
            this.RegresarBut.Size = new System.Drawing.Size(127, 34);
            this.RegresarBut.TabIndex = 26;
            this.RegresarBut.Text = "REGRESAR";
            this.RegresarBut.UseVisualStyleBackColor = false;
            this.RegresarBut.Click += new System.EventHandler(this.button2_Click);
            // 
            // AceptarBut
            // 
            this.AceptarBut.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(46)))), ((int)(((byte)(38)))));
            this.AceptarBut.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.AceptarBut.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.AceptarBut.Location = new System.Drawing.Point(178, 355);
            this.AceptarBut.Name = "AceptarBut";
            this.AceptarBut.Size = new System.Drawing.Size(127, 34);
            this.AceptarBut.TabIndex = 27;
            this.AceptarBut.Text = "ACEPTAR";
            this.AceptarBut.UseVisualStyleBackColor = false;
            this.AceptarBut.Click += new System.EventHandler(this.AceptarBut_Click);
            // 
            // panelBiometrico
            // 
            this.panelBiometrico.BackColor = System.Drawing.Color.Transparent;
            this.panelBiometrico.Controls.Add(this.label11);
            this.panelBiometrico.Controls.Add(this.label10);
            this.panelBiometrico.Controls.Add(this.panelContenedor);
            this.panelBiometrico.Controls.Add(this.AceptarBut);
            this.panelBiometrico.Controls.Add(this.RegresarBut);
            this.panelBiometrico.Controls.Add(this.label5);
            this.panelBiometrico.Controls.Add(this.pictureBox2);
            this.panelBiometrico.Controls.Add(this.label6);
            this.panelBiometrico.Location = new System.Drawing.Point(16, 483);
            this.panelBiometrico.MinimumSize = new System.Drawing.Size(1214, 56);
            this.panelBiometrico.Name = "panelBiometrico";
            this.panelBiometrico.Size = new System.Drawing.Size(1214, 56);
            this.panelBiometrico.TabIndex = 4;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label11.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(154)))), ((int)(((byte)(136)))), ((int)(((byte)(90)))));
            this.label11.Location = new System.Drawing.Point(53, 98);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(571, 76);
            this.label11.TabIndex = 30;
            this.label11.Text = resources.GetString("label11.Text");
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label10.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(154)))), ((int)(((byte)(136)))), ((int)(((byte)(90)))));
            this.label10.Location = new System.Drawing.Point(52, 67);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(142, 28);
            this.label10.TabIndex = 29;
            this.label10.Text = "Instrucciones:";
            // 
            // panelContenedor
            // 
            this.panelContenedor.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.panelContenedor.Location = new System.Drawing.Point(796, 10);
            this.panelContenedor.MaximumSize = new System.Drawing.Size(265, 389);
            this.panelContenedor.MinimumSize = new System.Drawing.Size(265, 389);
            this.panelContenedor.Name = "panelContenedor";
            this.panelContenedor.Size = new System.Drawing.Size(265, 389);
            this.panelContenedor.TabIndex = 28;
            // 
            // pictureBox2
            // 
            this.pictureBox2.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox2.Image")));
            this.pictureBox2.Location = new System.Drawing.Point(13, 10);
            this.pictureBox2.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(32, 32);
            this.pictureBox2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBox2.TabIndex = 21;
            this.pictureBox2.TabStop = false;
            // 
            // FormAgregar
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.WhiteSmoke;
            this.ClientSize = new System.Drawing.Size(1263, 572);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.panelBiometrico);
            this.Controls.Add(this.panelDatosper);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "FormAgregar";
            this.Text = "FormAgregar";
            this.Load += new System.EventHandler(this.FormAgregar_Load_1);
            this.panel3.ResumeLayout(false);
            this.panel3.PerformLayout();
            this.panelDatosper.ResumeLayout(false);
            this.panelDatosper.PerformLayout();
            this.panel5.ResumeLayout(false);
            this.panel5.PerformLayout();
            this.panel4.ResumeLayout(false);
            this.panel4.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panelBiometrico.ResumeLayout(false);
            this.panelBiometrico.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            this.ResumeLayout(false);

        }


        #endregion
        private Timer timer1;
        private Label label2;
        private Label label3;
        private Panel panel3;
        private TextBox NombreText;
        private Label label4;
        private PictureBox pictureBox1;
        private Panel panelDatosper;
        private Panel panel5;
        private TextBox EdadText;
        private Label label9;
        private Panel panel4;
        private TextBox DireccionText;
        private Label label8;
        private Panel panel2;
        private TextBox TelefonoText;
        private Label label7;
        private Button ContinuarBut;
        private Panel panel1;
        private Label label1;
        private Label label6;
        private PictureBox pictureBox2;
        private Label label5;
        private Button RegresarBut;
        private Button AceptarBut;
        private Panel panelBiometrico;
        private Label label11;
        private Label label10;
        private Button closeButton;
        private Panel panelContenedor;
    }
}