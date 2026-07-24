namespace Sistema_Gimnasio
{
    partial class FormEditar
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormEditar));
            this.panel1 = new System.Windows.Forms.Panel();
            this.closeButton = new System.Windows.Forms.Button();
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
            this.label3 = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.panel3 = new System.Windows.Forms.Panel();
            this.NombreText = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            this.panelDatosper.SuspendLayout();
            this.panel5.SuspendLayout();
            this.panel4.SuspendLayout();
            this.panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.panel3.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.White;
            this.panel1.Controls.Add(this.closeButton);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1010, 40);
            this.panel1.TabIndex = 0;
            // 
            // closeButton
            // 
            this.closeButton.BackColor = System.Drawing.Color.Transparent;
            this.closeButton.FlatAppearance.BorderSize = 0;
            this.closeButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.closeButton.Image = global::Sistema_Gimnasio.Properties.Resources.icons8_close_25__1_;
            this.closeButton.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.closeButton.Location = new System.Drawing.Point(965, 4);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(42, 33);
            this.closeButton.TabIndex = 15;
            this.closeButton.UseVisualStyleBackColor = false;
            this.closeButton.Click += new System.EventHandler(this.closeButton_Click);
            // 
            // panelDatosper
            // 
            this.panelDatosper.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(39)))), ((int)(((byte)(28)))));
            this.panelDatosper.Controls.Add(this.label1);
            this.panelDatosper.Controls.Add(this.ContinuarBut);
            this.panelDatosper.Controls.Add(this.panel5);
            this.panelDatosper.Controls.Add(this.panel4);
            this.panelDatosper.Controls.Add(this.panel2);
            this.panelDatosper.Controls.Add(this.label3);
            this.panelDatosper.Controls.Add(this.pictureBox1);
            this.panelDatosper.Controls.Add(this.panel3);
            this.panelDatosper.Controls.Add(this.label2);
            this.panelDatosper.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelDatosper.Location = new System.Drawing.Point(0, 40);
            this.panelDatosper.Name = "panelDatosper";
            this.panelDatosper.Size = new System.Drawing.Size(1010, 487);
            this.panelDatosper.TabIndex = 4;
            // 
            // ContinuarBut
            // 
            this.ContinuarBut.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(39)))), ((int)(((byte)(28)))));
            this.ContinuarBut.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ContinuarBut.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.ContinuarBut.Location = new System.Drawing.Point(7, 374);
            this.ContinuarBut.Name = "ContinuarBut";
            this.ContinuarBut.Size = new System.Drawing.Size(127, 34);
            this.ContinuarBut.TabIndex = 24;
            this.ContinuarBut.Text = "GUARDAR";
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
            this.panel5.Size = new System.Drawing.Size(797, 65);
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
            this.label9.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(154)))), ((int)(((byte)(136)))), ((int)(((byte)(90)))));
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
            this.panel4.Size = new System.Drawing.Size(797, 67);
            this.panel4.TabIndex = 23;
            // 
            // DireccionText
            // 
            this.DireccionText.Font = new System.Drawing.Font("Segoe UI Semibold", 10.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.DireccionText.Location = new System.Drawing.Point(8, 38);
            this.DireccionText.Name = "DireccionText";
            this.DireccionText.Size = new System.Drawing.Size(983, 30);
            this.DireccionText.TabIndex = 20;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label8.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(154)))), ((int)(((byte)(136)))), ((int)(((byte)(90)))));
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
            this.panel2.Size = new System.Drawing.Size(797, 67);
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
            this.label7.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(154)))), ((int)(((byte)(136)))), ((int)(((byte)(90)))));
            this.label7.Location = new System.Drawing.Point(3, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(99, 28);
            this.label7.TabIndex = 19;
            this.label7.Text = "Teléfono:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(154)))), ((int)(((byte)(136)))), ((int)(((byte)(90)))));
            this.label3.Location = new System.Drawing.Point(52, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(173, 28);
            this.label3.TabIndex = 22;
            this.label3.Text = "Datos Personales";
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
            // panel3
            // 
            this.panel3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel3.Controls.Add(this.NombreText);
            this.panel3.Controls.Add(this.label4);
            this.panel3.Location = new System.Drawing.Point(7, 63);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(797, 67);
            this.panel3.TabIndex = 21;
            // 
            // NombreText
            // 
            this.NombreText.Font = new System.Drawing.Font("Segoe UI Semibold", 10.2F, System.Drawing.FontStyle.Bold);
            this.NombreText.Location = new System.Drawing.Point(8, 38);
            this.NombreText.Name = "NombreText";
            this.NombreText.Size = new System.Drawing.Size(983, 30);
            this.NombreText.TabIndex = 20;
            this.NombreText.TextChanged += new System.EventHandler(this.NombreText_TextChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(154)))), ((int)(((byte)(136)))), ((int)(((byte)(90)))));
            this.label4.Location = new System.Drawing.Point(3, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(191, 28);
            this.label4.TabIndex = 19;
            this.label4.Text = "Nombre Completo:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(154)))), ((int)(((byte)(136)))), ((int)(((byte)(90)))));
            this.label2.Location = new System.Drawing.Point(53, 28);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(350, 19);
            this.label2.TabIndex = 21;
            this.label2.Text = "Introduce los datos personales del miembro para editar.\r\n";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(154)))), ((int)(((byte)(136)))), ((int)(((byte)(90)))));
            this.label1.Location = new System.Drawing.Point(334, 405);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(676, 57);
            this.label1.TabIndex = 25;
            this.label1.Text = "1.- En caso de querer cambiar la huella, deberás borrar y volver a registrar el m" +
    "iembro\r\n2.- Para cambiar el estado del miembro es desde el apartado de Realizar " +
    "pago (Se actualiza automaticamente)\r\n\r\n";
            // 
            // FormEditar
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(1010, 527);
            this.Controls.Add(this.panelDatosper);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "FormEditar";
            this.Text = "FormEditar";
            this.panel1.ResumeLayout(false);
            this.panelDatosper.ResumeLayout(false);
            this.panelDatosper.PerformLayout();
            this.panel5.ResumeLayout(false);
            this.panel5.PerformLayout();
            this.panel4.ResumeLayout(false);
            this.panel4.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.panel3.ResumeLayout(false);
            this.panel3.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button closeButton;
        private System.Windows.Forms.Panel panelDatosper;
        private System.Windows.Forms.Button ContinuarBut;
        private System.Windows.Forms.Panel panel5;
        private System.Windows.Forms.TextBox EdadText;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Panel panel4;
        private System.Windows.Forms.TextBox DireccionText;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.TextBox TelefonoText;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.TextBox NombreText;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
    }
}