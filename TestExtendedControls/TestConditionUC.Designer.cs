﻿namespace DialogTest
{
    partial class TestConditionUC
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
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.buttonExt1 = new ExtendedControls.ExtButton();
            this.buttonExt2 = new ExtendedControls.ExtButton();
            this.buttonExt3 = new ExtendedControls.ExtButton();
            this.SuspendLayout();
            // 
            // buttonExt1
            // 
            this.buttonExt1.Location = new System.Drawing.Point(2, 1);
            this.buttonExt1.Name = "buttonExt1";
            this.buttonExt1.Size = new System.Drawing.Size(75, 23);
            this.buttonExt1.TabIndex = 0;
            this.buttonExt1.Text = "EList";
            this.buttonExt1.UseVisualStyleBackColor = true;
            this.buttonExt1.Click += new System.EventHandler(this.buttonEvents);
            // 
            // buttonExt2
            // 
            this.buttonExt2.Location = new System.Drawing.Point(102, 1);
            this.buttonExt2.Name = "buttonExt2";
            this.buttonExt2.Size = new System.Drawing.Size(75, 23);
            this.buttonExt2.TabIndex = 1;
            this.buttonExt2.Text = "Condition";
            this.buttonExt2.UseVisualStyleBackColor = true;
            this.buttonExt2.Click += new System.EventHandler(this.buttonCondition);
            // 
            // buttonExt3
            // 
            this.buttonExt3.Location = new System.Drawing.Point(210, 1);
            this.buttonExt3.Name = "buttonExt3";
            this.buttonExt3.Size = new System.Drawing.Size(75, 23);
            this.buttonExt3.TabIndex = 1;
            this.buttonExt3.Text = "ConditionL";
            this.buttonExt3.UseVisualStyleBackColor = true;
            this.buttonExt3.Click += new System.EventHandler(this.buttonExt3_Click);
            // 
            // TestConditionUC
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(521, 334);
            this.Controls.Add(this.buttonExt3);
            this.Controls.Add(this.buttonExt2);
            this.Controls.Add(this.buttonExt1);
            this.Name = "TestConditionUC";
            this.Text = "Condition";
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Timer timer1;
        private ExtendedControls.ExtButton buttonExt1;
        private ExtendedControls.ExtButton buttonExt2;
        private ExtendedControls.ExtButton buttonExt3;
    }
}