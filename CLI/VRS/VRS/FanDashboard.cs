using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Rebar;

namespace VRS
{
    public partial class FanDashboard : Form
    {
        public FanDashboard()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var videos = Database.GetAllVideos();
            MessageBox.Show("Videos loaded: " + videos.Count);


            string message = "";
            foreach (var v in videos)
            {
                message += $"{v.VideoID} - {v.Title} - £{v.Price}\n";
            }

            MessageBox.Show(message);

        }

        private void button2_Click(object sender, EventArgs e)
        {
            string input = Microsoft.VisualBasic.Interaction.InputBox("Enter Video ID to purchase:", "Purchase Video");

            int videoID = int.Parse(input);
            double price = Database.GetVideoPrice(videoID);

            Database.AddPurchase(fanID, videoID, price);

            MessageBox.Show("Purchase successful!");

        }

        private void button3_Click(object sender, EventArgs e)
        {
            var purchases = Database.GetPurchasedVideos(fanID);

            string message = "";
            foreach (var p in purchases)
            {
                message += $"{p.VideoID} - {p.Title}\n";
            }

            MessageBox.Show(message);

        }

        private void button4_Click(object sender, EventArgs e)
        {
            this.Hide();
            Form1 login = new Form1();
            login.Show();

        }
    }
}
