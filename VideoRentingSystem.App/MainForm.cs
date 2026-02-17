using VideoRentingSystem.Core.Core;
using VideoRentingSystem.Core.Data;
using VideoRentingSystem.Core.Models;

namespace VideoRentingSystem.App;

public sealed class MainForm : Form
{
    private enum ViewMode
    {
        Library,
        Rented
    }

    private TextBox _txtSearchTitle = null!;
    private TextBox _txtId = null!;
    private TextBox _txtTitle = null!;
    private TextBox _txtGenre = null!;
    private TextBox _txtYear = null!;
    private TextBox _txtSearchId = null!;
    private FlowLayoutPanel _cardsPanel = null!;
    private Label _lblStatus = null!;
    private Label _lblHeaderTitle = null!;

    private VideoStore? _store;
    private ViewMode _viewMode = ViewMode.Library;
    private readonly string _databasePath;
    private readonly Font _titleFont = new("Segoe UI Semibold", 14F, FontStyle.Bold, GraphicsUnit.Point);
    private readonly Font _cardTitleFont = new("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point);
    private readonly Font _smallLabelFont = new("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

    public MainForm()
    {
        _databasePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VideoRentingSystem",
            "videos.db");

        Text = "Video Renting System - 23Lab Software";
        ClientSize = new Size(1360, 760);
        MinimumSize = Size;
        MaximumSize = Size;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(15, 19, 28);
        Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        DoubleBuffered = true;

        Panel leftSidebar = BuildLeftSidebar();
        Panel topBar = BuildTopBar();
        Panel adminBar = BuildAdminBar();

        _cardsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(10, 16, 25),
            AutoScroll = true,
            Padding = new Padding(18),
            WrapContents = true
        };

        _lblStatus = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 28,
            Padding = new Padding(10, 5, 10, 0),
            ForeColor = Color.Gainsboro,
            BackColor = Color.FromArgb(21, 26, 36),
            Text = "Status: Initializing..."
        };

        Panel content = new()
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(10, 16, 25)
        };
        content.Controls.Add(_cardsPanel);
        content.Controls.Add(_lblStatus);
        content.Controls.Add(adminBar);
        content.Controls.Add(topBar);

        Controls.Add(content);
        Controls.Add(leftSidebar);

        InitializeStoreWithPreloadedData();
    }

    private Panel BuildLeftSidebar()
    {
        Panel sidebar = new()
        {
            Dock = DockStyle.Left,
            Width = 180,
            BackColor = Color.Black
        };

        Label logo = new()
        {
            Text = "23LabSoftware",
            ForeColor = Color.FromArgb(66, 194, 255),
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            Left = 14,
            Top = 20,
            Width = 150
        };

        Button btnLibrary = CreateNavButton("Library", 70, (_, _) =>
        {
            _viewMode = ViewMode.Library;
            _lblHeaderTitle.Text = "Browse Videos";
            RenderCards();
        });

        Button btnMyRentals = CreateNavButton("My Rentals", 108, (_, _) =>
        {
            _viewMode = ViewMode.Rented;
            _lblHeaderTitle.Text = "My Rentals";
            RenderCards();
        });

        Button btnGenres = CreateNavButton("Genres", 146, (_, _) =>
        {
            _lblStatus.Text = "Status: Genres view not required for coursework scope.";
        });

        Button btnAccount = CreateNavButton("Account", 184, (_, _) =>
        {
            _lblStatus.Text = "Status: Account view not required for coursework scope.";
        });

        Button btnSettings = CreateNavButton("Settings", 222, (_, _) =>
        {
            _lblStatus.Text = "Status: Settings view not required for coursework scope.";
        });

        sidebar.Controls.Add(logo);
        sidebar.Controls.Add(btnLibrary);
        sidebar.Controls.Add(btnMyRentals);
        sidebar.Controls.Add(btnGenres);
        sidebar.Controls.Add(btnAccount);
        sidebar.Controls.Add(btnSettings);
        return sidebar;
    }

    private Panel BuildTopBar()
    {
        Panel topBar = new()
        {
            Dock = DockStyle.Top,
            Height = 74,
            Padding = new Padding(18, 16, 18, 8),
            BackColor = Color.FromArgb(7, 12, 20)
        };

        _lblHeaderTitle = new Label
        {
            Text = "Browse Videos",
            ForeColor = Color.White,
            Font = _titleFont,
            Left = 18,
            Top = 22,
            Width = 300
        };

        _txtSearchTitle = new TextBox
        {
            Left = 670,
            Top = 22,
            Width = 280,
            Height = 30,
            BackColor = Color.FromArgb(25, 32, 45),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        Button btnSearch = CreatePrimaryButton("Search", 960, 20, SearchTitleClicked);
        Button btnDisplayAll = CreateSecondaryButton("Show All", 1052, 20, (_, _) =>
        {
            _txtSearchTitle.Text = string.Empty;
            RenderCards();
        });

        topBar.Controls.Add(_lblHeaderTitle);
        topBar.Controls.Add(_txtSearchTitle);
        topBar.Controls.Add(btnSearch);
        topBar.Controls.Add(btnDisplayAll);
        return topBar;
    }

    private Panel BuildAdminBar()
    {
        Panel admin = new()
        {
            Dock = DockStyle.Bottom,
            Height = 118,
            Padding = new Padding(12),
            BackColor = Color.FromArgb(12, 18, 27)
        };

        Button btnSearchId = CreateSecondaryButton("Search ID", 780, 10, SearchIdClicked);
        Button btnRemoveId = CreateSecondaryButton("Remove ID", 870, 10, RemoveVideoClicked);

        _txtId = CreateSmallTextBox(12, 10, "ID");
        _txtTitle = CreateSmallTextBox(120, 10, "Title");
        _txtGenre = CreateSmallTextBox(350, 10, "Genre");
        _txtYear = CreateSmallTextBox(520, 10, "Year");
        _txtSearchId = CreateSmallTextBox(640, 10, "Search ID");

        Button btnAdd = CreatePrimaryButton("Add Video", 900, 10, AddVideoClicked);
        Button btnRent = CreatePrimaryButton("Rent ID", 990, 10, RentVideoClicked);
        Button btnReturn = CreatePrimaryButton("Return ID", 1080, 10, ReturnVideoClicked);

        Label hint = new()
        {
            Left = 12,
            Top = 52,
            Width = 1200,
            Height = 40,
            ForeColor = Color.Silver,
            Font = _smallLabelFont,
            Text = "Database is preloaded automatically with dummy data. Users cannot select an external database."
        };

        admin.Controls.Add(btnSearchId);
        admin.Controls.Add(btnRemoveId);
        admin.Controls.Add(_txtId);
        admin.Controls.Add(_txtTitle);
        admin.Controls.Add(_txtGenre);
        admin.Controls.Add(_txtYear);
        admin.Controls.Add(_txtSearchId);
        admin.Controls.Add(btnAdd);
        admin.Controls.Add(btnRent);
        admin.Controls.Add(btnReturn);
        admin.Controls.Add(hint);
        return admin;
    }

    private TextBox CreateSmallTextBox(int left, int top, string placeholder)
    {
        TextBox box = new()
        {
            Left = left,
            Top = top,
            Width = placeholder == "Title" ? 220 : 100,
            Height = 30,
            BackColor = Color.FromArgb(24, 30, 42),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        box.PlaceholderText = placeholder;
        return box;
    }

    private Button CreateNavButton(string text, int top, EventHandler onClick)
    {
        Button button = new()
        {
            Text = text,
            Left = 12,
            Top = top,
            Width = 155,
            Height = 32,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.Gainsboro,
            BackColor = Color.FromArgb(20, 24, 32),
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Regular, GraphicsUnit.Point),
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderSize = 0;
        AttachButtonColorAnimation(button, Color.FromArgb(20, 24, 32), Color.FromArgb(33, 43, 58), false);
        button.Click += onClick;
        return button;
    }

    private Button CreatePrimaryButton(string text, int left, int top, EventHandler onClick)
    {
        Button button = new()
        {
            Text = text,
            Left = left,
            Top = top,
            Width = 84,
            Height = 30,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(0, 193, 255),
            ForeColor = Color.Black,
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Regular, GraphicsUnit.Point),
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderSize = 0;
        AttachButtonColorAnimation(button, Color.FromArgb(0, 193, 255), Color.FromArgb(66, 214, 255), true);
        button.Click += onClick;
        return button;
    }

    private Button CreateSecondaryButton(string text, int left, int top, EventHandler onClick)
    {
        Button button = new()
        {
            Text = text,
            Left = left,
            Top = top,
            Width = 82,
            Height = 30,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(33, 42, 58),
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Regular, GraphicsUnit.Point),
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderColor = Color.FromArgb(54, 66, 88);
        button.FlatAppearance.BorderSize = 1;
        AttachButtonColorAnimation(button, Color.FromArgb(33, 42, 58), Color.FromArgb(49, 62, 82), false);
        button.Click += onClick;
        return button;
    }

    private void AddVideoClicked(object? sender, EventArgs e)
    {
        if (!EnsureStoreReady())
        {
            return;
        }

        if (!TryReadId(_txtId.Text, out int id) || !TryReadYear(_txtYear.Text, out int year))
        {
            return;
        }

        try
        {
            Video video = new(id, _txtTitle.Text, _txtGenre.Text, year);
            bool added = _store!.AddVideo(video);
            _lblStatus.Text = added ? "Status: Video added." : "Status: Video ID already exists.";
            RenderCards();
        }
        catch (Exception ex)
        {
            _lblStatus.Text = $"Status: Add failed - {ex.Message}";
        }
    }

    private void RemoveVideoClicked(object? sender, EventArgs e)
    {
        if (!EnsureStoreReady() || !TryReadId(_txtSearchId.Text, out int id))
        {
            return;
        }

        bool removed = _store!.RemoveVideo(id);
        _lblStatus.Text = removed ? "Status: Video removed." : "Status: Video not found.";
        RenderCards();
    }

    private void SearchTitleClicked(object? sender, EventArgs e)
    {
        if (!EnsureStoreReady())
        {
            return;
        }

        RenderCards(_txtSearchTitle.Text);
    }

    private void SearchIdClicked(object? sender, EventArgs e)
    {
        if (!EnsureStoreReady() || !TryReadId(_txtSearchId.Text, out int id))
        {
            return;
        }

        if (_store!.TrySearchById(id, out Video? video) && video != null)
        {
            _cardsPanel.Controls.Clear();
            _cardsPanel.Controls.Add(CreateVideoCard(video));
            _lblStatus.Text = "Status: Video found by ID.";
            return;
        }

        _lblStatus.Text = "Status: Video ID not found.";
        _cardsPanel.Controls.Clear();
    }

    private void RentVideoClicked(object? sender, EventArgs e)
    {
        if (!EnsureStoreReady() || !TryReadId(_txtSearchId.Text, out int id))
        {
            return;
        }

        bool rented = _store!.RentVideo(id);
        _lblStatus.Text = rented ? "Status: Video rented." : "Status: Cannot rent (missing or already rented).";
        RenderCards();
    }

    private void ReturnVideoClicked(object? sender, EventArgs e)
    {
        if (!EnsureStoreReady() || !TryReadId(_txtSearchId.Text, out int id))
        {
            return;
        }

        bool returned = _store!.ReturnVideo(id);
        _lblStatus.Text = returned ? "Status: Video returned." : "Status: Cannot return (missing or not rented).";
        RenderCards();
    }

    private void SeedDefaultData()
    {
        AddIfMissing(new Video(1001, "Interstellar", "Sci-Fi", 2014));
        AddIfMissing(new Video(1002, "The Dark Knight", "Action", 2008));
        AddIfMissing(new Video(1003, "Inception", "Sci-Fi", 2010));
        AddIfMissing(new Video(1004, "Pulp Fiction", "Crime", 1994));
        AddIfMissing(new Video(1005, "Our Planet", "Documentary", 2019));
    }

    private void AddIfMissing(Video video)
    {
        _store!.AddVideo(video);
    }

    private void RenderCards(string? titleFilter = null)
    {
        _cardsPanel.Controls.Clear();
        if (_store == null)
        {
            return;
        }

        Video[] videos;
        if (!string.IsNullOrWhiteSpace(titleFilter))
        {
            videos = _store.SearchByTitle(titleFilter);
            _lblStatus.Text = $"Status: Found {videos.Length} video(s) for title search.";
        }
        else
        {
            videos = _store.DisplayAllVideos();
        }

        if (_viewMode == ViewMode.Rented)
        {
            videos = FilterRented(videos);
        }

        if (videos.Length == 0)
        {
            Label empty = new()
            {
                Width = 900,
                Height = 60,
                ForeColor = Color.Silver,
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Regular, GraphicsUnit.Point),
                Text = "No videos to display in this view."
            };
            _cardsPanel.Controls.Add(empty);
            return;
        }

        for (int i = 0; i < videos.Length; i++)
        {
            _cardsPanel.Controls.Add(CreateVideoCard(videos[i]));
        }

        AnimateCardsIn();
    }

    private static Video[] FilterRented(Video[] videos)
    {
        int count = 0;
        for (int i = 0; i < videos.Length; i++)
        {
            if (videos[i].IsRented)
            {
                count++;
            }
        }

        if (count == 0)
        {
            return [];
        }

        Video[] filtered = new Video[count];
        int index = 0;
        for (int i = 0; i < videos.Length; i++)
        {
            if (videos[i].IsRented)
            {
                filtered[index++] = videos[i];
            }
        }

        return filtered;
    }

    private Panel CreateVideoCard(Video video)
    {
        Panel card = new()
        {
            Width = 245,
            Height = 170,
            Margin = new Padding(12),
            BackColor = Color.FromArgb(24, 35, 52)
        };
        card.Padding = new Padding(2);
        AttachCardHoverAnimation(card, Color.FromArgb(24, 35, 52), Color.FromArgb(34, 47, 68));

        Label lblCode = new()
        {
            Left = 12,
            Top = 10,
            Width = 210,
            ForeColor = Color.FromArgb(113, 131, 154),
            Font = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point),
            Text = $"23LAB_PROD_{video.VideoId}"
        };

        Label lblTitle = new()
        {
            Left = 12,
            Top = 44,
            Width = 210,
            ForeColor = Color.White,
            Font = _cardTitleFont,
            Text = video.Title
        };

        Label lblGenre = new()
        {
            Left = 12,
            Top = 70,
            Width = 210,
            ForeColor = Color.Silver,
            Font = _smallLabelFont,
            Text = $"{video.Genre} • {video.ReleaseYear}"
        };

        Label lblState = new()
        {
            Left = 12,
            Top = 96,
            Width = 170,
            ForeColor = video.IsRented ? Color.OrangeRed : Color.LimeGreen,
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Regular, GraphicsUnit.Point),
            Text = video.IsRented ? "Currently Rented" : "Ready To Rent"
        };

        Button btnAction = new()
        {
            Left = 154,
            Top = 124,
            Width = 76,
            Height = 30,
            FlatStyle = FlatStyle.Flat,
            BackColor = video.IsRented ? Color.FromArgb(63, 72, 87) : Color.FromArgb(0, 220, 180),
            ForeColor = video.IsRented ? Color.Gainsboro : Color.Black,
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Regular, GraphicsUnit.Point),
            Cursor = Cursors.Hand,
            Text = video.IsRented ? "Return" : "Rent"
        };
        btnAction.FlatAppearance.BorderSize = 0;
        if (video.IsRented)
        {
            AttachButtonColorAnimation(btnAction, Color.FromArgb(63, 72, 87), Color.FromArgb(84, 95, 112), false);
        }
        else
        {
            AttachButtonColorAnimation(btnAction, Color.FromArgb(0, 220, 180), Color.FromArgb(78, 235, 206), true);
        }
        btnAction.Click += (_, _) =>
        {
            if (_store == null)
            {
                return;
            }

            bool wasRented = video.IsRented;
            bool ok = wasRented
                ? _store.ReturnVideo(video.VideoId)
                : _store.RentVideo(video.VideoId);

            if (!ok)
            {
                _lblStatus.Text = "Status: Action could not be completed.";
                return;
            }

            _lblStatus.Text = wasRented ? "Status: Video returned." : "Status: Video rented.";
            RenderCards(string.IsNullOrWhiteSpace(_txtSearchTitle.Text) ? null : _txtSearchTitle.Text);
        };

        card.Controls.Add(lblCode);
        card.Controls.Add(lblTitle);
        card.Controls.Add(lblGenre);
        card.Controls.Add(lblState);
        card.Controls.Add(btnAction);
        return card;
    }

    private bool EnsureStoreReady()
    {
        if (_store != null)
        {
            return true;
        }

        _lblStatus.Text = "Status: Database is still initializing. Please wait.";
        return false;
    }

    private void InitializeStoreWithPreloadedData()
    {
        try
        {
            SqliteVideoRepository repository = new(_databasePath);
            repository.EnsureDatabaseAndSchema();
            _store = new VideoStore(repository);
            _store.LoadFromRepository();

            if (_store.Count == 0)
            {
                SeedDefaultData();
            }

            _lblStatus.Text = $"Status: Ready. Preloaded database active ({_store.Count} videos).";
            RenderCards();
        }
        catch (Exception ex)
        {
            _lblStatus.Text = $"Status: Failed to initialize preloaded database - {ex.Message}";
        }
    }

    private bool TryReadId(string raw, out int id)
    {
        if (!int.TryParse(raw, out id) || id <= 0)
        {
            _lblStatus.Text = "Status: ID must be a positive integer.";
            return false;
        }

        return true;
    }

    private bool TryReadYear(string raw, out int year)
    {
        if (!int.TryParse(raw, out year))
        {
            _lblStatus.Text = "Status: Year must be an integer.";
            return false;
        }

        return true;
    }

    private void AttachButtonColorAnimation(Button button, Color normalColor, Color hoverColor, bool darkTextOnHover)
    {
        Color target = normalColor;
        System.Windows.Forms.Timer timer = new() { Interval = 15 };

        button.MouseEnter += (_, _) =>
        {
            target = hoverColor;
            timer.Start();
        };

        button.MouseLeave += (_, _) =>
        {
            target = normalColor;
            timer.Start();
        };

        timer.Tick += (_, _) =>
        {
            Color current = button.BackColor;
            Color next = LerpColor(current, target, 0.28f);
            button.BackColor = next;

            if (AreColorsClose(next, target))
            {
                button.BackColor = target;
                timer.Stop();
            }
        };

        if (darkTextOnHover)
        {
            button.ForeColor = Color.Black;
        }
    }

    private static void AttachCardHoverAnimation(Panel card, Color normalColor, Color hoverColor)
    {
        Color target = normalColor;
        System.Windows.Forms.Timer timer = new() { Interval = 15 };

        card.MouseEnter += (_, _) =>
        {
            target = hoverColor;
            timer.Start();
        };

        card.MouseLeave += (_, _) =>
        {
            target = normalColor;
            timer.Start();
        };

        timer.Tick += (_, _) =>
        {
            Color next = LerpColor(card.BackColor, target, 0.22f);
            card.BackColor = next;
            if (AreColorsClose(next, target))
            {
                card.BackColor = target;
                timer.Stop();
            }
        };
    }

    private void AnimateCardsIn()
    {
        if (_cardsPanel.Controls.Count == 0)
        {
            return;
        }

        for (int i = 0; i < _cardsPanel.Controls.Count; i++)
        {
            _cardsPanel.Controls[i].Visible = false;
        }

        int index = 0;
        System.Windows.Forms.Timer timer = new() { Interval = 45 };
        timer.Tick += (_, _) =>
        {
            if (index >= _cardsPanel.Controls.Count)
            {
                timer.Stop();
                timer.Dispose();
                return;
            }

            _cardsPanel.Controls[index].Visible = true;
            index++;
        };
        timer.Start();
    }

    private static Color LerpColor(Color from, Color to, float amount)
    {
        int r = from.R + (int)((to.R - from.R) * amount);
        int g = from.G + (int)((to.G - from.G) * amount);
        int b = from.B + (int)((to.B - from.B) * amount);
        return Color.FromArgb(r, g, b);
    }

    private static bool AreColorsClose(Color a, Color b)
    {
        const int threshold = 6;
        return Math.Abs(a.R - b.R) <= threshold &&
               Math.Abs(a.G - b.G) <= threshold &&
               Math.Abs(a.B - b.B) <= threshold;
    }
}
