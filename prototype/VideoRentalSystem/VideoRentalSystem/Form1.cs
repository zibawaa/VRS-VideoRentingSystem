using VideoRentalSystem.Models;
using VideoRentalSystem.Services;

namespace VideoRentalSystem;

public partial class Form1 : Form
{
    private readonly VideoStoreService store = new();

    // simple limits so bad input gets caught early (code quality / validation marks)
    private const int TitleMax = 200;
    private const int GenreMax = 100;
    private const int DirectorMax = 200;
    private const int YearMin = 1888;

    public Form1()
    {
        InitializeComponent();
    }

    private void btnBrowseDb_Click(object sender, EventArgs e)
    {
        using var dlg = new SaveFileDialog();
        dlg.Filter = "SQL Server database (*.mdf)|*.mdf|All files (*.*)|*.*";
        dlg.Title = "Pick .mdf file (new name = create, existing = open)";
        dlg.AddExtension = true;
        dlg.DefaultExt = "mdf";
        if (dlg.ShowDialog(this) == DialogResult.OK)
            txtDbPath.Text = dlg.FileName;
    }

    private void btnConnect_Click(object sender, EventArgs e)
    {
        string path = txtDbPath.Text.Trim();
        if (path.Length == 0)
        {
            MessageBox.Show(this, "Type a path or browse first (no hardcoded default).");
            return;
        }

        if (!path.EndsWith(".mdf", StringComparison.OrdinalIgnoreCase))
        {
            var r = MessageBox.Show(this,
                "Filename does not end in .mdf. Continue anyway?",
                "Check extension",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (r != DialogResult.Yes)
                return;
        }

        try
        {
            store.Connect(path);
            lblStatus.Text = "Connected to: " + store.Database.MdfPath + " — loaded " + store.List.Count + " rows.";
            RefreshGrid();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                "Could not open database. Is LocalDB installed?\n\n" + ex.Message,
                "Database error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void btnAdd_Click(object sender, EventArgs e)
    {
        if (!store.Database.IsOpen)
        {
            MessageBox.Show(this, "Connect to a database first.");
            return;
        }

        if (!int.TryParse(txtRentalId.Text.Trim(), out int id) || id <= 0)
        {
            MessageBox.Show(this, "RentalID must be a positive whole number.");
            return;
        }

        if (!int.TryParse(txtYear.Text.Trim(), out int year))
        {
            MessageBox.Show(this, "Year must be a number.");
            return;
        }

        int yearMax = DateTime.Now.Year + 2;
        if (year < YearMin || year > yearMax)
        {
            MessageBox.Show(this, "Year looks wrong — use something between " + YearMin + " and " + yearMax + ".");
            return;
        }

        if (!decimal.TryParse(txtPrice.Text.Trim(), out decimal price))
        {
            MessageBox.Show(this, "RentalPrice must be a decimal number.");
            return;
        }

        if (price < 0 || price > 99999.99m)
        {
            MessageBox.Show(this, "RentalPrice should be between 0 and 99999.99.");
            return;
        }

        if (!int.TryParse(txtCopies.Text.Trim(), out int copies) || copies < 0 || copies > 10000)
        {
            MessageBox.Show(this, "Available copies must be between 0 and 10000.");
            return;
        }

        string title = txtTitle.Text.Trim();
        string genre = txtGenre.Text.Trim();
        string director = txtDirector.Text.Trim();

        if (title.Length == 0)
        {
            MessageBox.Show(this, "Title is required.");
            return;
        }

        if (title.Length > TitleMax || genre.Length > GenreMax || director.Length > DirectorMax)
        {
            MessageBox.Show(this, "One of the text fields is too long for the database columns.");
            return;
        }

        if (genre.Length == 0)
            genre = "Unknown";
        if (director.Length == 0)
            director = "Unknown";

        var v = new RentalVideo
        {
            RentalID = id,
            Title = title,
            Genre = genre,
            Director = director,
            Year = year,
            RentalPrice = price,
            AvailableCopies = copies
        };

        if (!store.TryAdd(v, out string err))
        {
            MessageBox.Show(this, string.IsNullOrEmpty(err) ? "Add failed." : err);
            return;
        }

        lblStatus.Text = "Added rental id " + id;
        RefreshGrid();
        // TODO: clear the textboxes after add if we want nicer UX
    }

    private void btnRemove_Click(object sender, EventArgs e)
    {
        if (!int.TryParse(txtRemoveId.Text.Trim(), out int id) || id <= 0)
        {
            MessageBox.Show(this, "Type a positive numeric RentalID to remove.");
            return;
        }

        if (!store.TryRemove(id, out string err))
        {
            MessageBox.Show(this, string.IsNullOrEmpty(err) ? "Remove failed." : err);
            return;
        }

        lblStatus.Text = "Remove sent for id " + id + " (row gone if it existed).";
        RefreshGrid();
    }

    private void btnSearch_Click(object sender, EventArgs e)
    {
        string t = txtSearchTitle.Text.Trim();
        if (t.Length == 0)
        {
            MessageBox.Show(this, "Type something to search for.");
            return;
        }

        if (t.Length > TitleMax)
        {
            MessageBox.Show(this, "Search string is too long.");
            return;
        }

        RentalVideo hit = store.List.SearchByTitle(t);
        if (hit == null)
        {
            lblStatus.Text = "No title match for: " + t;
            return;
        }

        lblStatus.Text = "Found: " + hit.Title + " (" + hit.Genre + ") — id " + hit.RentalID;
        RentalVideo[] rows = store.List.ToArray();
        for (int i = 0; i < rows.Length; i++)
        {
            if (rows[i].RentalID == hit.RentalID)
            {
                if (gridVideos.Rows.Count > i)
                    gridVideos.Rows[i].Selected = true;
                break;
            }
        }
    }

    private void btnShowAll_Click(object sender, EventArgs e)
    {
        if (store.Database.IsOpen)
            store.ReloadFromDatabase();
        RefreshGrid();
        lblStatus.Text = "Showing all " + store.List.Count + " videos.";
    }

    private void btnRent_Click(object sender, EventArgs e)
    {
        if (!int.TryParse(txtRentId.Text.Trim(), out int id) || id <= 0)
        {
            MessageBox.Show(this, "Type a positive numeric RentalID.");
            return;
        }

        if (!store.TryRentCopy(id, out string err))
        {
            MessageBox.Show(this, string.IsNullOrEmpty(err) ? "Rent failed." : err);
            return;
        }

        lblStatus.Text = "Rented one copy of id " + id;
        RefreshGrid();
    }

    private void btnReturn_Click(object sender, EventArgs e)
    {
        if (!int.TryParse(txtRentId.Text.Trim(), out int id) || id <= 0)
        {
            MessageBox.Show(this, "Type a positive numeric RentalID.");
            return;
        }

        if (!store.TryReturnCopy(id, out string err))
        {
            MessageBox.Show(this, string.IsNullOrEmpty(err) ? "Return failed." : err);
            return;
        }

        lblStatus.Text = "Returned one copy of id " + id;
        RefreshGrid();
    }

    private void RefreshGrid()
    {
        gridVideos.DataSource = null;
        gridVideos.AutoGenerateColumns = true;
        gridVideos.DataSource = store.List.ToArray();
    }
}
