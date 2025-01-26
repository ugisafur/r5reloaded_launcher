namespace patch_creator
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            textBox1 = new TextBox();
            label1 = new Label();
            label2 = new Label();
            textBox2 = new TextBox();
            button1 = new Button();
            button2 = new Button();
            button3 = new Button();
            comboBox1 = new ComboBox();
            label3 = new Label();
            richTextBox1 = new RichTextBox();
            label4 = new Label();
            progressBar1 = new ProgressBar();
            progressLabel = new Label();
            totalFilesLeft = new Label();
            SuspendLayout();
            // 
            // textBox1
            // 
            textBox1.Location = new Point(12, 71);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(534, 23);
            textBox1.TabIndex = 0;
            textBox1.TextChanged += textBox1_TextChanged;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 53);
            label1.Name = "label1";
            label1.Size = new Size(64, 15);
            label1.TabIndex = 1;
            label1.Text = "Game Files";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 97);
            label2.Name = "label2";
            label2.Size = new Size(81, 15);
            label2.TabIndex = 3;
            label2.Text = "Output Folder";
            // 
            // textBox2
            // 
            textBox2.Location = new Point(12, 115);
            textBox2.Name = "textBox2";
            textBox2.Size = new Size(534, 23);
            textBox2.TabIndex = 2;
            // 
            // button1
            // 
            button1.Location = new Point(12, 144);
            button1.Name = "button1";
            button1.Size = new Size(632, 23);
            button1.TabIndex = 4;
            button1.Text = "Create Patch";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // button2
            // 
            button2.Location = new Point(552, 115);
            button2.Name = "button2";
            button2.Size = new Size(92, 23);
            button2.TabIndex = 5;
            button2.Text = "Browse";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // button3
            // 
            button3.Location = new Point(552, 71);
            button3.Name = "button3";
            button3.Size = new Size(92, 23);
            button3.TabIndex = 6;
            button3.Text = "Browse";
            button3.UseVisualStyleBackColor = true;
            button3.Click += button3_Click;
            // 
            // comboBox1
            // 
            comboBox1.FormattingEnabled = true;
            comboBox1.Location = new Point(12, 27);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new Size(632, 23);
            comboBox1.TabIndex = 7;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(12, 9);
            label3.Name = "label3";
            label3.Size = new Size(78, 15);
            label3.TabIndex = 8;
            label3.Text = "Game Branch";
            // 
            // richTextBox1
            // 
            richTextBox1.DetectUrls = false;
            richTextBox1.Location = new Point(12, 209);
            richTextBox1.Name = "richTextBox1";
            richTextBox1.ReadOnly = true;
            richTextBox1.Size = new Size(632, 193);
            richTextBox1.TabIndex = 9;
            richTextBox1.Text = "";
            richTextBox1.WordWrap = false;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(12, 191);
            label4.Name = "label4";
            label4.Size = new Size(96, 15);
            label4.TabIndex = 10;
            label4.Text = "Clear Cache Files";
            // 
            // progressBar1
            // 
            progressBar1.Location = new Point(12, 408);
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new Size(632, 23);
            progressBar1.TabIndex = 11;
            // 
            // progressLabel
            // 
            progressLabel.Location = new Point(12, 434);
            progressLabel.Name = "progressLabel";
            progressLabel.Size = new Size(468, 20);
            progressLabel.TabIndex = 12;
            progressLabel.Text = "waiting";
            progressLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // totalFilesLeft
            // 
            totalFilesLeft.Location = new Point(510, 434);
            totalFilesLeft.Name = "totalFilesLeft";
            totalFilesLeft.Size = new Size(134, 20);
            totalFilesLeft.TabIndex = 13;
            totalFilesLeft.Text = "0/0";
            totalFilesLeft.TextAlign = ContentAlignment.MiddleRight;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(656, 453);
            Controls.Add(totalFilesLeft);
            Controls.Add(progressLabel);
            Controls.Add(progressBar1);
            Controls.Add(label4);
            Controls.Add(richTextBox1);
            Controls.Add(label3);
            Controls.Add(comboBox1);
            Controls.Add(button3);
            Controls.Add(button2);
            Controls.Add(button1);
            Controls.Add(label2);
            Controls.Add(textBox2);
            Controls.Add(label1);
            Controls.Add(textBox1);
            FormBorderStyle = FormBorderStyle.Fixed3D;
            Name = "Form1";
            Text = "Patch Creator";
            Load += Form1_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox textBox1;
        private Label label1;
        private Label label2;
        private TextBox textBox2;
        private Button button1;
        private Button button2;
        private Button button3;
        private ComboBox comboBox1;
        private Label label3;
        private RichTextBox richTextBox1;
        private Label label4;
        private ProgressBar progressBar1;
        private Label progressLabel;
        private Label totalFilesLeft;
    }
}
