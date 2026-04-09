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
    public partial class ArtistDashboard : Form
    {
        public ArtistDashboard()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string title = Microsoft.VisualBasic.Interaction.InputBox("Title:", "Upload Video");
            string genre = Microsoft.VisualBasic.Interaction.InputBox("Genre:", "Upload Video");
            int year = int.Parse(Microsoft.VisualBasic.Interaction.InputBox("Year:", "Upload Video"));
            double price = double.Parse(Microsoft.VisualBasic.Interaction.InputBox("Price:", "Upload Video"));

            Database.AddVideo(artistID, title, genre, year, price);

            MessageBox.Show("Video uploaded!");

        }

        private void button2_Click(object sender, EventArgs e)
        {
            int videoID = int.Parse(Microsoft.VisualBasic.Interaction.InputBox("Video ID:", "Edit Video"));
            string newTitle = Microsoft.VisualBasic.Interaction.InputBox("New Title:", "Edit Video");

            Database.UpdateVideoTitle(videoID, newTitle);

            MessageBox.Show("Video updated!");

        }

        private void button4_Click(object sender, EventArgs e)
        {
            int videoID = int.Parse(Microsoft.VisualBasic.Interaction.InputBox("Video ID:", "Delete Video"));

            Database.DeleteVideo(videoID);

            MessageBox.Show("Video deleted!");

        }

        private void button3_Click(object sender, EventArgs e)
        {
            double revenue = Database.GetArtistRevenue(artistID);
            MessageBox.Show("Your total revenue: £" + revenue);

        }

        private void button5_Click(object sender, EventArgs e)
        {
            this.Hide();
            Form1 login = new Form1();
            login.Show();

        }
    }
}
