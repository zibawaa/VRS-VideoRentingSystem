namespace VRS
{
    partial class FanDashboard
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
            this.btnViewVideos = new System.Windows.Forms.Button();
            this.btnPurchase = new System.Windows.Forms.Button();
            this.btnViewPurchased = new System.Windows.Forms.Button();
            this.btnLogout = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnViewVideos
            // 
            this.btnViewVideos.Location = new System.Drawing.Point(174, 138);
            this.btnViewVideos.Name = "btnViewVideos";
            this.btnViewVideos.Size = new System.Drawing.Size(123, 23);
            this.btnViewVideos.TabIndex = 0;
            this.btnViewVideos.Text = "View All Videos";
            this.btnViewVideos.UseVisualStyleBackColor = true;
            this.btnViewVideos.Click += new System.EventHandler(this.button1_Click);
            // 
            // btnPurchase
            // 
            this.btnPurchase.Location = new System.Drawing.Point(174, 167);
            this.btnPurchase.Name = "btnPurchase";
            this.btnPurchase.Size = new System.Drawing.Size(123, 23);
            this.btnPurchase.TabIndex = 1;
            this.btnPurchase.Text = "Purchase Video\n\n";
            this.btnPurchase.UseVisualStyleBackColor = true;
            this.btnPurchase.Click += new System.EventHandler(this.button2_Click);
            // 
            // btnViewPurchased
            // 
            this.btnViewPurchased.Location = new System.Drawing.Point(152, 196);
            this.btnViewPurchased.Name = "btnViewPurchased";
            this.btnViewPurchased.Size = new System.Drawing.Size(172, 23);
            this.btnViewPurchased.TabIndex = 2;
            this.btnViewPurchased.Text = "View Purchased Videos\n\n";
            this.btnViewPurchased.UseVisualStyleBackColor = true;
            this.btnViewPurchased.Click += new System.EventHandler(this.button3_Click);
            // 
            // btnLogout
            // 
            this.btnLogout.Location = new System.Drawing.Point(174, 225);
            this.btnLogout.Name = "btnLogout";
            this.btnLogout.Size = new System.Drawing.Size(123, 26);
            this.btnLogout.TabIndex = 3;
            this.btnLogout.Text = "Logout";
            this.btnLogout.UseVisualStyleBackColor = true;
            this.btnLogout.Click += new System.EventHandler(this.button4_Click);
            // 
            // FanDashboard
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(576, 568);
            this.Controls.Add(this.btnLogout);
            this.Controls.Add(this.btnViewPurchased);
            this.Controls.Add(this.btnPurchase);
            this.Controls.Add(this.btnViewVideos);
            this.Name = "FanDashboard";
            this.Text = "FanDashboard";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnViewVideos;
        private System.Windows.Forms.Button btnPurchase;
        private System.Windows.Forms.Button btnViewPurchased;
        private System.Windows.Forms.Button btnLogout;
    }
}