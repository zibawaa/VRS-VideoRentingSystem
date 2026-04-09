namespace VideoRentalSystem;

public partial class Form1
{
    private System.ComponentModel.IContainer components = null;

    private TextBox txtDbPath;
    private Button btnBrowseDb;
    private Button btnConnect;
    private Label lblDb;

    private TextBox txtRentalId;
    private TextBox txtTitle;
    private TextBox txtGenre;
    private TextBox txtDirector;
    private TextBox txtYear;
    private TextBox txtPrice;
    private TextBox txtCopies;
    private Button btnAdd;

    private TextBox txtRemoveId;
    private Button btnRemove;

    private TextBox txtSearchTitle;
    private Button btnSearch;

    private Button btnShowAll;

    private TextBox txtRentId;
    private Button btnRent;
    private Button btnReturn;

    private DataGridView gridVideos;
    private Label lblStatus;
    private RentalAgentPanel rentalAgentPanel1;
    private Label lblAgent;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
            components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        txtDbPath = new TextBox();
        btnBrowseDb = new Button();
        btnConnect = new Button();
        lblDb = new Label();
        txtRentalId = new TextBox();
        txtTitle = new TextBox();
        txtGenre = new TextBox();
        txtDirector = new TextBox();
        txtYear = new TextBox();
        txtPrice = new TextBox();
        txtCopies = new TextBox();
        btnAdd = new Button();
        txtRemoveId = new TextBox();
        btnRemove = new Button();
        txtSearchTitle = new TextBox();
        btnSearch = new Button();
        btnShowAll = new Button();
        txtRentId = new TextBox();
        btnRent = new Button();
        btnReturn = new Button();
        gridVideos = new DataGridView();
        lblStatus = new Label();
        rentalAgentPanel1 = new RentalAgentPanel();
        lblAgent = new Label();
        SuspendLayout();

        lblDb.AutoSize = true;
        lblDb.Location = new Point(12, 15);
        lblDb.Text = "LocalDB .mdf file (runtime path):";

        txtDbPath.Location = new Point(220, 12);
        txtDbPath.Size = new Size(430, 23);
        txtDbPath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        btnBrowseDb.Location = new Point(656, 11);
        btnBrowseDb.Size = new Size(75, 25);
        btnBrowseDb.Text = "Browse...";
        btnBrowseDb.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnBrowseDb.Click += btnBrowseDb_Click;

        btnConnect.Location = new Point(737, 11);
        btnConnect.Size = new Size(85, 25);
        btnConnect.Text = "Connect";
        btnConnect.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnConnect.Click += btnConnect_Click;

        int y = 48;
        int labelX = 12;
        int fieldX = 120;
        int rowH = 28;

        AddLabel("RentalID", labelX, y);
        txtRentalId.Location = new Point(fieldX, y - 3);
        txtRentalId.Size = new Size(100, 23);
        y += rowH;

        AddLabel("Title", labelX, y);
        txtTitle.Location = new Point(fieldX, y - 3);
        txtTitle.Size = new Size(280, 23);
        y += rowH;

        AddLabel("Genre", labelX, y);
        txtGenre.Location = new Point(fieldX, y - 3);
        txtGenre.Size = new Size(200, 23);
        y += rowH;

        AddLabel("Director", labelX, y);
        txtDirector.Location = new Point(fieldX, y - 3);
        txtDirector.Size = new Size(200, 23);
        y += rowH;

        AddLabel("Year", labelX, y);
        txtYear.Location = new Point(fieldX, y - 3);
        txtYear.Size = new Size(80, 23);
        y += rowH;

        AddLabel("RentalPrice", labelX, y);
        txtPrice.Location = new Point(fieldX, y - 3);
        txtPrice.Size = new Size(80, 23);
        y += rowH;

        AddLabel("Copies", labelX, y);
        txtCopies.Location = new Point(fieldX, y - 3);
        txtCopies.Size = new Size(80, 23);
        y += rowH;

        btnAdd.Location = new Point(fieldX, y);
        btnAdd.Size = new Size(120, 28);
        btnAdd.Text = "Add record";
        btnAdd.Click += btnAdd_Click;
        y += rowH + 8;

        AddLabel("Remove ID", labelX, y);
        txtRemoveId.Location = new Point(fieldX, y - 3);
        txtRemoveId.Size = new Size(80, 23);
        btnRemove.Location = new Point(220, y - 4);
        btnRemove.Size = new Size(110, 28);
        btnRemove.Text = "Remove";
        btnRemove.Click += btnRemove_Click;
        y += rowH;

        AddLabel("Search title", labelX, y);
        txtSearchTitle.Location = new Point(fieldX, y - 3);
        txtSearchTitle.Size = new Size(200, 23);
        btnSearch.Location = new Point(330, y - 4);
        btnSearch.Size = new Size(90, 28);
        btnSearch.Text = "Search";
        btnSearch.Click += btnSearch_Click;
        y += rowH;

        btnShowAll.Location = new Point(fieldX, y);
        btnShowAll.Size = new Size(140, 28);
        btnShowAll.Text = "Show all videos";
        btnShowAll.Click += btnShowAll_Click;
        y += rowH + 8;

        AddLabel("Rent/Return ID", labelX, y);
        txtRentId.Location = new Point(fieldX, y - 3);
        txtRentId.Size = new Size(80, 23);
        btnRent.Location = new Point(220, y - 4);
        btnRent.Size = new Size(75, 28);
        btnRent.Text = "Rent 1";
        btnRent.Click += btnRent_Click;
        btnReturn.Location = new Point(305, y - 4);
        btnReturn.Size = new Size(85, 28);
        btnReturn.Text = "Return 1";
        btnReturn.Click += btnReturn_Click;
        y += rowH + 12;

        gridVideos.Location = new Point(12, y);
        gridVideos.Size = new Size(700, 240);
        gridVideos.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
        gridVideos.ReadOnly = true;
        gridVideos.AllowUserToAddRows = false;
        gridVideos.AllowUserToDeleteRows = false;
        gridVideos.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        gridVideos.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

        rentalAgentPanel1.Location = new Point(728, y);
        rentalAgentPanel1.Size = new Size(200, 240);
        rentalAgentPanel1.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        lblAgent.AutoSize = true;
        lblAgent.Location = new Point(728, y - 22);
        lblAgent.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        lblAgent.Text = "Extra: AI-style helper";

        lblStatus.Location = new Point(12, y + 245);
        lblStatus.Size = new Size(916, 36);
        lblStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        lblStatus.Text = "Type or browse to a .mdf path, then Connect (needs SQL Server LocalDB).";

        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(940, y + 290);
        MinimumSize = new Size(780, 520);
        Controls.Add(lblDb);
        Controls.Add(txtDbPath);
        Controls.Add(btnBrowseDb);
        Controls.Add(btnConnect);
        Controls.Add(txtRentalId);
        Controls.Add(txtTitle);
        Controls.Add(txtGenre);
        Controls.Add(txtDirector);
        Controls.Add(txtYear);
        Controls.Add(txtPrice);
        Controls.Add(txtCopies);
        Controls.Add(btnAdd);
        Controls.Add(txtRemoveId);
        Controls.Add(btnRemove);
        Controls.Add(txtSearchTitle);
        Controls.Add(btnSearch);
        Controls.Add(btnShowAll);
        Controls.Add(txtRentId);
        Controls.Add(btnRent);
        Controls.Add(btnReturn);
        Controls.Add(lblAgent);
        Controls.Add(rentalAgentPanel1);
        Controls.Add(gridVideos);
        Controls.Add(lblStatus);
        Text = "Video Rental Store (CST2550 prototype)";
        ResumeLayout(false);
        PerformLayout();
    }

    private void AddLabel(string text, int x, int y)
    {
        var lb = new Label
        {
            AutoSize = true,
            Location = new Point(x, y),
            Text = text
        };
        Controls.Add(lb);
    }
}
