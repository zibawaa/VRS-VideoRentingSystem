using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VRS
{
    public partial class AdminDashboard : Form
    {
        public AdminDashboard()
        {
            InitializeComponent();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var artists = Database.GetAllArtists();

            string message = "";
            foreach (var a in artists)
            {
                message += $"{a.ArtistID} - {a.Name}\n";
            }

            MessageBox.Show(message);

        }

        private void button4_Click(object sender, EventArgs e)
        {
            this.Hide();
            Form1 login = new Form1();
            login.Show();

        }

        private void button3_Click(object sender, EventArgs e)
        {
            int videoID = int.Parse(Microsoft.VisualBasic.Interaction.InputBox("Video ID:", "Delete Video"));

            Database.DeleteVideo(videoID);

            MessageBox.Show("Video deleted!");

        }

        private void button1_Click(object sender, EventArgs e)
        {
            var videos = Database.GetAllVideos();

            string message = "";
            foreach (var v in videos)
            {
                message += $"{v.VideoID} - {v.Title} - {v.Genre}\n";
            }

            MessageBox.Show(message);

        }
    }
}
