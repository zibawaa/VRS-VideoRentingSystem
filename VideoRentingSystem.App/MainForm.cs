// Main window for the video shop. We kept all UI code here on purpose so it is easy to find while we are still
// learning WinForms — splitting into lots of small files looked clever but made it harder to jump around in lectures.
using System.Drawing.Drawing2D;
using VideoRentingSystem.Core.Core;
using VideoRentingSystem.Core.Data;
using VideoRentingSystem.Core.Models;

namespace VideoRentingSystem.App;

public sealed class MainForm : Form
{
    // One place to track which "screen" we are faking (catalogue, rentals, etc.). Enums read nicer than magic numbers.
    private enum ViewMode
    {
        Library,
        Rented,
        Saved,
        Account,
        Admin
    }

    private const int RentalPeriodDays = 28;

    private static readonly Color VrsBlack = Color.Black;
    private static readonly Color VrsPanel = Color.FromArgb(10, 10, 12);
    private static readonly Color VrsCard = Color.FromArgb(38, 44, 60);
    private static readonly Color VrsCardHover = Color.FromArgb(48, 55, 74);
    private static readonly Color VrsBurgundy = Color.FromArgb(72, 18, 28);
    private static readonly Color VrsBurgundyLight = Color.FromArgb(110, 32, 46);
    private static readonly Color VrsRedOutline = Color.FromArgb(230, 45, 60);
    private static readonly Color VrsInputBg = Color.FromArgb(28, 32, 42);
    private static readonly Color VrsPillBorder = Color.FromArgb(90, 95, 110);

    private TextBox _txtSearchTitle = null!;
    private FlowLayoutPanel _cardsPanel = null!;
    private Label _lblStatus = null!;
    private Label _lblSectionTag = null!;
    private Button _btnSearch = null!;
    private Button _btnDisplayAll = null!;
    private Button _btnLogout = null!;
    private Button _btnNavLibrary = null!;
    private Button _btnNavRented = null!;
    private Button _btnNavSaved = null!;
    private Button _btnNavAccount = null!;
    private Button _btnNavAdmin = null!;

    private VideoStore? _store;
    private UserStore? _userStore;
    private User? _currentUser;
    private ViewMode _viewMode = ViewMode.Library;
    // Starred titles for the "Saved" tab. In-memory only for now so we don't have to change the database coursework script mid-semester.
    private readonly HashSet<int> _savedVideoIds = [];
    private readonly string _databasePath;
    private readonly Font _titleFont = new("Segoe UI Semibold", 13F, FontStyle.Bold, GraphicsUnit.Point);
    private readonly Font _cardTitleFont = new("Segoe UI Semibold", 11F, FontStyle.Bold, GraphicsUnit.Point);
    private readonly Font _smallLabelFont = new("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
    private readonly Font _navFont = new("Segoe UI Semibold", 8.5F, FontStyle.Bold, GraphicsUnit.Point);

    public MainForm()
    {
        // Store the database next to other per-user app data so we don't need admin rights.
        _databasePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VideoRentingSystem",
            "videos.db");

        Text = "VRS — Video Renting";
        ClientSize = new Size(1180, 720);
        MinimumSize = new Size(960, 600);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = VrsBlack;
        Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        // Form-level double buffer stops the whole window flashing when we repaint; FlowLayoutPanel does not get this for free.
        DoubleBuffered = true;

        Panel bottomStatus = new()
        {
            Dock = DockStyle.Bottom,
            Height = 26,
            BackColor = Color.FromArgb(18, 18, 22),
            Padding = new Padding(12, 4, 12, 0)
        };

        _lblStatus = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = Color.Silver,
            BackColor = Color.Transparent,
            Text = "Status: Initializing...",
            TextAlign = ContentAlignment.MiddleLeft
        };
        bottomStatus.Controls.Add(_lblStatus);

        Panel bottomNav = BuildBottomNav();
        bottomNav.Dock = DockStyle.Bottom;
        bottomNav.Height = 58;

        _cardsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = VrsPanel,
            AutoScroll = true,
            Padding = new Padding(20, 12, 20, 12),
            WrapContents = true
        };
        // We tried switching on DoubleBuffered for the card panel via reflection, but it is confusing when you are new to C#
        // and it still flickers a bit anyway — skipping keeps the mental model simpler.

        Panel mainFill = new()
        {
            Dock = DockStyle.Fill,
            BackColor = VrsPanel
        };
        mainFill.Controls.Add(_cardsPanel);

        Panel header = BuildHeader();
        header.Dock = DockStyle.Top;
        // Slightly taller header so the logo row + search row never feel squashed on smaller laptops.
        header.Height = 124;

        // Dock order: fill area first, then top chrome, then bottom bars. WinForms stacks multiple Bottom docks bottom-to-top.
        Controls.Add(mainFill);
        Controls.Add(header);
        Controls.Add(bottomNav);
        Controls.Add(bottomStatus);

        InitializeStoreWithPreloadedData();
    }

    // ------- VRS logo row -------
    // We originally drew "VRS" with GraphicsPath to mimic the hollow Figma text, but on some machines the bottoms of the
    // letters were clipped. Plain Labels auto-size correctly, so we traded the hollow look for something stable.
    private Panel BuildBrandHeader()
    {
        Panel host = new()
        {
            Dock = DockStyle.Top,
            Height = 58,
            BackColor = VrsBlack
        };

        FlowLayoutPanel row = new()
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = VrsBlack
        };

        Label lblCam = new()
        {
            Text = "🎬",
            AutoSize = true,
            Font = new Font("Segoe UI Emoji", 20F, FontStyle.Regular, GraphicsUnit.Point),
            ForeColor = Color.LightGray,
            Margin = new Padding(0, 2, 10, 4),
            BackColor = VrsBlack
        };

        Label lblVrs = new()
        {
            Text = "VRS",
            AutoSize = true,
            Font = new Font("Segoe UI", 22F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point),
            ForeColor = VrsRedOutline,
            Margin = new Padding(0, 0, 0, 4),
            BackColor = VrsBlack
        };

        row.Controls.Add(lblCam);
        row.Controls.Add(lblVrs);
        host.Controls.Add(row);

        void CenterRow(object? sender, EventArgs e)
        {
            row.Left = Math.Max(0, (host.Width - row.Width) / 2);
            row.Top = Math.Max(0, (host.Height - row.Height) / 2);
        }

        host.Layout += CenterRow;
        host.Resize += CenterRow;
        return host;
    }

    // Bottom strip mimics the phone mock-up: big thumb-friendly targets, even though this is WinForms on desktop.
    private Panel BuildBottomNav()
    {
        Panel bar = new()
        {
            BackColor = Color.FromArgb(6, 6, 8),
            Padding = new Padding(16, 10, 16, 10)
        };

        FlowLayoutPanel flow = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = false
        };

        _btnNavLibrary = CreateNavPill("Browse", true, (_, _) => SetView(ViewMode.Library, "Browse", "RENT"));
        _btnNavRented = CreateNavPill("Rented", false, (_, _) => SetView(ViewMode.Rented, "My rentals", "RENTED"));
        _btnNavSaved = CreateNavPill("Saved", false, (_, _) => SetView(ViewMode.Saved, "Saved videos", "SAVED"));
        _btnNavAccount = CreateNavPill("Account", false, (_, _) => SetView(ViewMode.Account, "Account", ""));
        // Admin tab stays hidden until someone logs in as the lecturer demo account "123" — same rule as before, just styled differently.
        _btnNavAdmin = CreateNavPill("Upload", false, (_, _) => SetView(ViewMode.Admin, "Upload / manage", "UPLOAD"));
        _btnNavAdmin.Visible = false;

        flow.Controls.Add(_btnNavLibrary);
        flow.Controls.Add(_btnNavRented);
        flow.Controls.Add(_btnNavSaved);
        flow.Controls.Add(_btnNavAccount);
        flow.Controls.Add(_btnNavAdmin);

        bar.Controls.Add(flow);
        return bar;
    }

    // Top chrome: logout (when signed in), brand row is separate, then search controls.
    private Panel BuildHeader()
    {
        Panel header = new() { BackColor = VrsBlack };

        _btnLogout = new Button
        {
            Text = "LOG OUT",
            Visible = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(36, 38, 48),
            Cursor = Cursors.Hand,
            Padding = new Padding(14, 6, 14, 6),
            Margin = new Padding(12, 10, 0, 0)
        };
        _btnLogout.FlatAppearance.BorderColor = VrsPillBorder;
        _btnLogout.FlatAppearance.BorderSize = 1;
        _btnLogout.Region = new Region(GetRoundedPath(new Rectangle(0, 0, _btnLogout.Width, _btnLogout.Height), 14));
        _btnLogout.SizeChanged += (_, _) =>
        {
            _btnLogout.Region = new Region(GetRoundedPath(new Rectangle(0, 0, _btnLogout.Width, _btnLogout.Height), 14));
        };
        _btnLogout.Click += (_, _) =>
        {
            _currentUser = null;
            _btnNavAdmin.Visible = false;
            _btnLogout.Visible = false;
            _lblStatus.Text = "Status: Logged out.";
            SetView(ViewMode.Library, "Browse", "RENT");
        };

        Panel brandHeader = BuildBrandHeader();

        Panel searchRow = new()
        {
            Dock = DockStyle.Bottom,
            Height = 44,
            BackColor = VrsBlack,
            Padding = new Padding(120, 4, 120, 4)
        };

        _lblSectionTag = new Label
        {
            AutoSize = true,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
            BackColor = VrsBurgundy,
            Text = "RENT",
            Padding = new Padding(16, 5, 16, 5),
            Visible = false,
            Margin = new Padding(0, 0, 12, 0)
        };
        _lblSectionTag.SizeChanged += (_, _) =>
        {
            _lblSectionTag.Region = new Region(GetRoundedPath(new Rectangle(0, 0, _lblSectionTag.Width, _lblSectionTag.Height), 12));
        };

        _txtSearchTitle = new TextBox
        {
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
            Height = 32,
            BackColor = VrsInputBg,
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = "Search catalogue by title"
        };

        _btnSearch = CreateAccentPillButton("Search", false, (_, _) =>
        {
            if (!EnsureStoreReady()) return;
            RenderCards(_txtSearchTitle.Text);
        });

        _btnDisplayAll = CreateMutedPillButton("Show all", (_, _) =>
        {
            _txtSearchTitle.Text = string.Empty;
            RenderCards();
        });

        FlowLayoutPanel searchFlow = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };

        Panel searchHost = new() { Width = 520, Height = 36 };
        _txtSearchTitle.Location = new Point(0, 2);
        _txtSearchTitle.Width = searchHost.Width;
        searchHost.Controls.Add(_txtSearchTitle);
        searchHost.Resize += (_, _) => { _txtSearchTitle.Width = searchHost.Width; };

        searchFlow.Controls.Add(_lblSectionTag);
        searchFlow.Controls.Add(searchHost);
        searchFlow.Controls.Add(_btnSearch);
        searchFlow.Controls.Add(_btnDisplayAll);

        searchRow.Controls.Add(searchFlow);

        Panel topRow = new() { Dock = DockStyle.Top, Height = 40, BackColor = VrsBlack };
        topRow.Controls.Add(_btnLogout);

        header.Controls.Add(searchRow);
        header.Controls.Add(brandHeader);
        header.Controls.Add(topRow);

        return header;
    }

    private Button CreateNavPill(string text, bool primary, EventHandler onClick)
    {
        Color back = primary ? VrsBurgundy : Color.FromArgb(34, 36, 46);
        Button b = new()
        {
            Text = text.ToUpperInvariant(),
            Height = 38,
            Width = 118,
            Margin = new Padding(0, 0, 10, 0),
            FlatStyle = FlatStyle.Flat,
            Font = _navFont,
            ForeColor = Color.White,
            BackColor = back,
            Cursor = Cursors.Hand
        };
        b.FlatAppearance.BorderColor = primary ? VrsRedOutline : VrsPillBorder;
        b.FlatAppearance.BorderSize = 1;
        b.Region = new Region(GetRoundedPath(new Rectangle(0, 0, b.Width, b.Height), 10));
        b.SizeChanged += (_, _) =>
        {
            b.Region = new Region(GetRoundedPath(new Rectangle(0, 0, b.Width, b.Height), 10));
        };
        b.Click += onClick;
        return b;
    }

    private Button CreateAccentPillButton(string text, bool burgundy, EventHandler onClick)
    {
        Button b = new()
        {
            Text = text,
            Height = 32,
            Width = 92,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = burgundy ? VrsBurgundy : Color.FromArgb(52, 58, 74),
            Cursor = Cursors.Hand,
            Margin = new Padding(8, 0, 0, 0)
        };
        b.FlatAppearance.BorderColor = burgundy ? VrsRedOutline : VrsPillBorder;
        b.FlatAppearance.BorderSize = 1;
        b.Region = new Region(GetRoundedPath(new Rectangle(0, 0, b.Width, b.Height), 8));
        b.SizeChanged += (_, _) =>
        {
            b.Region = new Region(GetRoundedPath(new Rectangle(0, 0, b.Width, b.Height), 8));
        };
        b.Click += onClick;
        return b;
    }

    private Button CreateMutedPillButton(string text, EventHandler onClick)
    {
        Button b = new()
        {
            Text = text,
            Height = 32,
            Width = 88,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular),
            ForeColor = Color.Gainsboro,
            BackColor = Color.FromArgb(32, 35, 44),
            Cursor = Cursors.Hand,
            Margin = new Padding(6, 0, 0, 0)
        };
        b.FlatAppearance.BorderColor = VrsPillBorder;
        b.FlatAppearance.BorderSize = 1;
        b.Region = new Region(GetRoundedPath(new Rectangle(0, 0, b.Width, b.Height), 8));
        b.SizeChanged += (_, _) =>
        {
            b.Region = new Region(GetRoundedPath(new Rectangle(0, 0, b.Width, b.Height), 8));
        };
        b.Click += onClick;
        return b;
    }

    // headerContext is reserved if we later want to mirror the Figma subtitle under the logo; tag drives the little burgundy pill.
    private void SetView(ViewMode mode, string headerContext, string tag)
    {
        _ = headerContext;
        _viewMode = mode;
        UpdateNavSelection(mode);
        _lblSectionTag.Visible = !string.IsNullOrEmpty(tag);
        _lblSectionTag.Text = tag;

        bool showSearch = mode is ViewMode.Library or ViewMode.Rented or ViewMode.Saved;
        _txtSearchTitle.Visible = showSearch;
        _btnSearch.Visible = showSearch;
        _btnDisplayAll.Visible = showSearch;
        if (!showSearch)
        {
            _txtSearchTitle.Text = string.Empty;
        }

        // Hide the global logout while you are already on the account page — there is a second logout button there.
        _btnLogout.Visible = _currentUser != null && mode != ViewMode.Account;

        RenderCards(string.IsNullOrWhiteSpace(_txtSearchTitle.Text) ? null : _txtSearchTitle.Text);
    }

    private void UpdateNavSelection(ViewMode mode)
    {
        SetNavActive(_btnNavLibrary, mode == ViewMode.Library);
        SetNavActive(_btnNavRented, mode == ViewMode.Rented);
        SetNavActive(_btnNavSaved, mode == ViewMode.Saved);
        SetNavActive(_btnNavAccount, mode == ViewMode.Account);
        SetNavActive(_btnNavAdmin, mode == ViewMode.Admin);
    }

    private static void SetNavActive(Button b, bool active)
    {
        b.BackColor = active ? VrsBurgundy : Color.FromArgb(34, 36, 46);
        b.FlatAppearance.BorderColor = active ? VrsRedOutline : VrsPillBorder;
    }

    private TextBox CreateSmallTextBox(int left, int top, string placeholder, int width = 200)
    {
        TextBox box = new()
        {
            Left = left,
            Top = top,
            Width = width,
            Height = 30,
            BackColor = VrsInputBg,
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        box.PlaceholderText = placeholder;
        return box;
    }

    private void RenderAccountView()
    {
        Panel p = new Panel
        {
            Width = 560,
            Height = _currentUser == null ? 420 : 220,
            BackColor = VrsCard,
            Margin = new Padding(8, 8, 8, 8)
        };
        p.Region = new Region(GetRoundedPath(new Rectangle(0, 0, p.Width, p.Height), 14));
        p.SizeChanged += (_, _) =>
        {
            p.Region = new Region(GetRoundedPath(new Rectangle(0, 0, p.Width, p.Height), 14));
        };

        Label lblTitle = new Label { Text = "Account", ForeColor = Color.White, Font = _titleFont, Left = 32, Top = 28, Width = 300 };
        p.Controls.Add(lblTitle);

        if (_currentUser != null)
        {
            Label lblUser = new Label
            {
                Text = $"Signed in as {_currentUser.Username}\nUser ID {_currentUser.UserId}",
                ForeColor = Color.Gainsboro,
                Font = _cardTitleFont,
                Left = 32,
                Top = 78,
                Width = 440,
                Height = 48
            };
            Button btnOut = CreateMutedPillButton("Log out", (_, _) =>
            {
                _currentUser = null;
                _btnNavAdmin.Visible = false;
                _btnLogout.Visible = false;
                _lblStatus.Text = "Status: Logged out.";
                SetView(ViewMode.Library, "Browse", "RENT");
            });
            btnOut.Left = 32;
            btnOut.Top = 140;
            btnOut.Width = 120;
            p.Controls.Add(lblUser);
            p.Controls.Add(btnOut);
        }
        else
        {
            int y = 78;
            p.Controls.Add(new Label { Text = "USERNAME", ForeColor = Color.Silver, Font = _smallLabelFont, Left = 32, Top = y, Width = 120 });
            TextBox tUser = CreateSmallTextBox(32, y + 22, "", 220);
            y += 72;
            p.Controls.Add(new Label { Text = "PASSWORD", ForeColor = Color.Silver, Font = _smallLabelFont, Left = 32, Top = y, Width = 120 });
            TextBox tPass = CreateSmallTextBox(32, y + 22, "", 220);
            tPass.PasswordChar = '•';
            y += 72;
            p.Controls.Add(new Label { Text = "REGISTER: same fields, then click Register", ForeColor = Color.Gray, Font = _smallLabelFont, Left = 32, Top = y, Width = 440 });
            y += 36;

            Button btnIn = CreateAccentPillButton("Login", true, (_, _) =>
            {
                User? u = _userStore?.Login(tUser.Text, tPass.Text);
                if (u != null)
                {
                    _currentUser = u;
                    if (string.Equals(u.Username, "123", StringComparison.Ordinal))
                    {
                        _btnNavAdmin.Visible = true;
                    }

                    _btnLogout.Visible = true;
                    _lblStatus.Text = "Status: Welcome back.";
                    SetView(ViewMode.Library, "Browse", "RENT");
                }
                else
                {
                    _lblStatus.Text = "Status: Invalid login.";
                }
            });
            btnIn.Left = 32;
            btnIn.Top = y;
            btnIn.Width = 100;

            Button btnReg = CreateMutedPillButton("Register", (_, _) =>
            {
                bool reg = _userStore?.RegisterUser(tUser.Text, tPass.Text) ?? false;
                _lblStatus.Text = reg ? "Status: Registered — you can log in." : "Status: Registration failed (duplicate user or empty fields).";
            });
            btnReg.Left = 148;
            btnReg.Top = y;
            btnReg.Width = 110;

            p.Controls.Add(tUser);
            p.Controls.Add(tPass);
            p.Controls.Add(btnIn);
            p.Controls.Add(btnReg);
        }

        _cardsPanel.Controls.Add(p);
    }

    private void RenderAdminView()
    {
        Panel p = new Panel { Width = 920, Height = 520, BackColor = VrsCard, Margin = new Padding(8, 8, 8, 8) };
        p.Region = new Region(GetRoundedPath(new Rectangle(0, 0, p.Width, p.Height), 14));

        Label lblHint = new Label { Text = "Upload / database templates", ForeColor = Color.Silver, Font = _cardTitleFont, Left = 28, Top = 24, Width = 500 };
        p.Controls.Add(lblHint);

        Label lblAdd = new Label { Text = "Add video", ForeColor = Color.White, Font = new Font("Segoe UI", 10F, FontStyle.Bold), Left = 28, Top = 72, Width = 200 };
        TextBox tId = CreateSmallTextBox(28, 98, "ID", 86);
        TextBox tTitle = CreateSmallTextBox(124, 98, "Title", 220);
        TextBox tGenre = CreateSmallTextBox(356, 98, "Genre", 140);
        TextBox tYear = CreateSmallTextBox(508, 98, "Year", 72);

        Button btnAdd = CreateAccentPillButton("Add", true, (_, _) =>
        {
            if (!TryReadId(tId.Text, out int id) || !TryReadYear(tYear.Text, out int year)) return;
            try
            {
                bool added = _store!.AddVideo(new Video(id, tTitle.Text, tGenre.Text, year));
                _lblStatus.Text = added ? "Status: Video added." : "Status: ID already exists.";
                if (added)
                {
                    tId.Text = "";
                    tTitle.Text = "";
                    tGenre.Text = "";
                    tYear.Text = "";
                }
            }
            catch (Exception ex)
            {
                _lblStatus.Text = $"Status: {ex.Message}";
            }
        });
        btnAdd.Left = 596;
        btnAdd.Top = 96;
        btnAdd.Width = 88;

        p.Controls.Add(lblAdd);
        p.Controls.Add(tId);
        p.Controls.Add(tTitle);
        p.Controls.Add(tGenre);
        p.Controls.Add(tYear);
        p.Controls.Add(btnAdd);

        Label lblRem = new Label { Text = "Remove video", ForeColor = Color.White, Font = new Font("Segoe UI", 10F, FontStyle.Bold), Left = 28, Top = 150, Width = 200 };
        TextBox tRemId = CreateSmallTextBox(28, 176, "Video ID", 120);
        Button btnRem = CreateMutedPillButton("Remove", (_, _) =>
        {
            if (!TryReadId(tRemId.Text, out int id)) return;
            bool removed = _store!.RemoveVideo(id);
            _lblStatus.Text = removed ? "Status: Removed." : "Status: Not found.";
            if (removed) tRemId.Text = "";
        });
        btnRem.Left = 156;
        btnRem.Top = 174;
        btnRem.Width = 96;

        p.Controls.Add(lblRem);
        p.Controls.Add(tRemId);
        p.Controls.Add(btnRem);

        Label lblSearch = new Label { Text = "Find by ID (demo)", ForeColor = Color.White, Font = new Font("Segoe UI", 10F, FontStyle.Bold), Left = 28, Top = 232, Width = 200 };
        TextBox tSearchId = CreateSmallTextBox(28, 258, "Video ID", 120);
        Button btnSearchId = CreateMutedPillButton("Search", (_, _) =>
        {
            if (!TryReadId(tSearchId.Text, out int id)) return;
            if (_store!.TrySearchById(id, out Video? video) && video != null)
            {
                _cardsPanel.Controls.Clear();
                _cardsPanel.Controls.Add(p);
                Panel foundCard = CreateVideoCard(video, showSaveToggle: false);
                foundCard.Margin = new Padding(10, 10, 0, 0);
                _cardsPanel.Controls.Add(foundCard);
                _lblStatus.Text = "Status: Found.";
            }
            else
            {
                _lblStatus.Text = "Status: Not found.";
            }
        });
        btnSearchId.Left = 156;
        btnSearchId.Top = 256;
        btnSearchId.Width = 96;

        p.Controls.Add(lblSearch);
        p.Controls.Add(tSearchId);
        p.Controls.Add(btnSearchId);

        Label hint = new Label { Text = "SQLite file updates as you edit. Rental data uses the custom in-memory structures synced to disk.", ForeColor = Color.Gray, Font = _smallLabelFont, Left = 28, Top = 380, Width = 820 };
        p.Controls.Add(hint);

        _cardsPanel.Controls.Add(p);
    }

    // Clears the card panel then rebuilds whatever the current view needs. Keeping one method avoids copy/paste bugs.
    private void RenderCards(string? titleFilter = null)
    {
        _cardsPanel.Controls.Clear();
        if (_store == null)
        {
            return;
        }

        if (_viewMode == ViewMode.Admin)
        {
            RenderAdminView();
            return;
        }

        if (_viewMode == ViewMode.Account)
        {
            RenderAccountView();
            return;
        }

        Video[] videos;
        if (!string.IsNullOrWhiteSpace(titleFilter))
        {
            videos = _store.SearchByTitle(titleFilter);
            _lblStatus.Text = $"Status: {videos.Length} match(es).";
        }
        else
        {
            videos = _store.DisplayAllVideos();
        }

        if (_viewMode == ViewMode.Rented)
        {
            if (_currentUser != null)
            {
                videos = _store.GetUserRentedVideos(_currentUser.UserId);
            }
            else
            {
                videos = [];
                _lblStatus.Text = "Status: Log in to see rentals.";
            }
        }
        else if (_viewMode == ViewMode.Saved)
        {
            if (_savedVideoIds.Count == 0)
            {
                videos = [];
            }
            else
            {
                Video[] all = string.IsNullOrWhiteSpace(titleFilter) ? _store.DisplayAllVideos() : videos;
                List<Video> kept = [];
                foreach (Video v in all)
                {
                    if (_savedVideoIds.Contains(v.VideoId))
                    {
                        kept.Add(v);
                    }
                }

                videos = kept.ToArray();
            }

            if (_currentUser == null)
            {
                _lblStatus.Text = "Status: Saved list is local to this session — log in to rent.";
            }
        }

        if (videos.Length == 0)
        {
            Label empty = new()
            {
                Width = 720,
                Height = 48,
                ForeColor = Color.Silver,
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Regular, GraphicsUnit.Point),
                Text = _viewMode == ViewMode.Saved ? "No saved titles yet. Use ★ on Browse." : "Nothing to show in this view."
            };
            _cardsPanel.Controls.Add(empty);
            return;
        }

        bool listRows = _viewMode == ViewMode.Rented;
        // Parentheses matter: we only want the star button when (library OR saved) AND logged in, not library OR (saved AND login).
        bool showSave = (_viewMode == ViewMode.Library || _viewMode == ViewMode.Saved) && _currentUser != null;

        for (int i = 0; i < videos.Length; i++)
        {
            if (listRows && _currentUser != null)
            {
                _cardsPanel.Controls.Add(CreateRentedRow(videos[i], _currentUser.UserId));
            }
            else
            {
                _cardsPanel.Controls.Add(CreateVideoCard(videos[i], showSaveToggle: showSave));
            }
        }
    }

    private Panel CreateRentedRow(Video video, int userId)
    {
        Panel row = new()
        {
            Width = Math.Max(720, _cardsPanel.ClientSize.Width - 48),
            Height = 56,
            Margin = new Padding(4, 6, 4, 6),
            BackColor = VrsCard
        };
        row.Region = new Region(GetRoundedPath(new Rectangle(0, 0, row.Width, row.Height), 12));

        Button star = new()
        {
            Text = "★",
            Width = 36,
            Height = 36,
            Left = 10,
            Top = 10,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            BackColor = VrsBurgundy,
            Font = new Font("Segoe UI", 12F),
            Cursor = Cursors.Hand
        };
        star.FlatAppearance.BorderSize = 0;
        star.Region = new Region(GetRoundedPath(new Rectangle(0, 0, star.Width, star.Height), 10));

        Label lblTitle = new()
        {
            Text = $"{video.Title} — {video.Genre} ({video.ReleaseYear})",
            Left = 54,
            Top = 12,
            Width = row.Width - 220,
            Height = 32,
            ForeColor = Color.White,
            Font = _cardTitleFont
        };

        // UI assumes a fixed loan length so the list matches the Figma "28 DAYS LEFT" style chips; tweak RentalPeriodDays if the brief changes.
        string daysText = "—";
        if (_store!.TryGetRentDate(userId, video.VideoId, out DateTime rentAt))
        {
            int used = (DateTime.UtcNow.Date - rentAt.Date).Days;
            int left = RentalPeriodDays - used;
            if (left < 0)
            {
                left = 0;
            }

            daysText = left == 1 ? "1 DAY LEFT" : $"{left} DAYS LEFT";
        }
        else
        {
            daysText = $"{RentalPeriodDays} DAYS LEFT";
        }

        Label pill = new()
        {
            Text = daysText,
            AutoSize = false,
            Width = 130,
            Height = 30,
            Left = row.Width - 146,
            Top = 13,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.White,
            BackColor = VrsBurgundyLight,
            Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold)
        };
        pill.Region = new Region(GetRoundedPath(new Rectangle(0, 0, pill.Width, pill.Height), 10));

        row.Controls.Add(star);
        row.Controls.Add(lblTitle);
        row.Controls.Add(pill);
        row.SizeChanged += (_, _) =>
        {
            row.Region = new Region(GetRoundedPath(new Rectangle(0, 0, row.Width, row.Height), 12));
            lblTitle.Width = row.Width - 220;
            pill.Left = row.Width - 146;
        };

        return row;
    }

    private Panel CreateVideoCard(Video video, bool showSaveToggle)
    {
        Panel card = new()
        {
            Width = 248,
            Height = showSaveToggle ? 188 : 172,
            Margin = new Padding(10),
            BackColor = VrsCard
        };
        card.Region = new Region(GetRoundedPath(new Rectangle(0, 0, card.Width, card.Height), 12));
        // Simple colour swap on hover — no Timer/Lerp, easier to debug when you are still getting used to events.
        AttachSimpleCardHover(card, VrsCard, VrsCardHover);

        Label lblCode = new()
        {
            Left = 12,
            Top = 10,
            Width = 200,
            ForeColor = Color.FromArgb(130, 140, 160),
            Font = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point),
            Text = $"ID {video.VideoId}"
        };

        Label lblTitle = new()
        {
            Left = 12,
            Top = 36,
            Width = 218,
            ForeColor = Color.White,
            Font = _cardTitleFont,
            Text = video.Title
        };

        Label lblGenre = new()
        {
            Left = 12,
            Top = 62,
            Width = 218,
            ForeColor = Color.Silver,
            Font = _smallLabelFont,
            Text = $"{video.Genre} · {video.ReleaseYear}"
        };

        bool isRentedByMe = false;
        if (video.IsRented && _currentUser != null && _store != null)
        {
            Video[] myRentals = _store.GetUserRentedVideos(_currentUser.UserId);
            for (int j = 0; j < myRentals.Length; j++)
            {
                if (myRentals[j].VideoId == video.VideoId)
                {
                    isRentedByMe = true;
                    break;
                }
            }
        }

        string stateText = "Available";
        Color stateColor = Color.LightGreen;
        if (video.IsRented)
        {
            stateText = isRentedByMe ? "Rented by you" : "Rented";
            stateColor = VrsRedOutline;
        }

        Label lblState = new()
        {
            Left = 12,
            Top = 88,
            Width = 170,
            ForeColor = stateColor,
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Regular, GraphicsUnit.Point),
            Text = stateText
        };

        int actionTop = showSaveToggle ? 128 : 120;
        Button btnAction = new()
        {
            Left = 154,
            Top = actionTop,
            Width = 80,
            Height = 30,
            FlatStyle = FlatStyle.Flat,
            BackColor = video.IsRented ? Color.FromArgb(56, 62, 78) : VrsBurgundy,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Regular, GraphicsUnit.Point),
            Cursor = Cursors.Hand,
            Text = video.IsRented ? (isRentedByMe ? "Return" : "Busy") : "Rent"
        };
        btnAction.FlatAppearance.BorderSize = 0;
        btnAction.Region = new Region(GetRoundedPath(new Rectangle(0, 0, btnAction.Width, btnAction.Height), 8));
        AttachQuickHover(btnAction, video.IsRented ? Color.FromArgb(56, 62, 78) : VrsBurgundy, video.IsRented ? Color.FromArgb(70, 76, 92) : VrsBurgundyLight);

        btnAction.Click += (_, _) =>
        {
            if (_store == null) return;
            if (_currentUser == null)
            {
                _lblStatus.Text = "Status: Log in to rent or return.";
                return;
            }

            bool wasRented = video.IsRented;
            bool ok = wasRented
                ? _store.ReturnVideo(video.VideoId, _currentUser.UserId)
                : _store.RentVideo(video.VideoId, _currentUser.UserId);

            if (!ok)
            {
                _lblStatus.Text = "Status: Action not allowed.";
                return;
            }

            _lblStatus.Text = wasRented ? "Status: Returned." : "Status: Rented.";
            RenderCards(_txtSearchTitle.Visible && !string.IsNullOrWhiteSpace(_txtSearchTitle.Text) ? _txtSearchTitle.Text : null);
        };

        card.Controls.Add(lblCode);
        card.Controls.Add(lblTitle);
        card.Controls.Add(lblGenre);
        card.Controls.Add(lblState);
        card.Controls.Add(btnAction);

        if (showSaveToggle)
        {
            bool saved = _savedVideoIds.Contains(video.VideoId);
            Button btnStar = new()
            {
                Text = saved ? "★" : "☆",
                Left = 12,
                Top = actionTop,
                Width = 40,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = saved ? VrsBurgundy : Color.FromArgb(44, 50, 66),
                Font = new Font("Segoe UI", 11F),
                Cursor = Cursors.Hand
            };
            btnStar.FlatAppearance.BorderSize = 0;
            btnStar.Region = new Region(GetRoundedPath(new Rectangle(0, 0, btnStar.Width, btnStar.Height), 8));
            btnStar.Click += (_, _) =>
            {
                if (_savedVideoIds.Contains(video.VideoId))
                {
                    _savedVideoIds.Remove(video.VideoId);
                }
                else
                {
                    _savedVideoIds.Add(video.VideoId);
                }

                RenderCards(string.IsNullOrWhiteSpace(_txtSearchTitle.Text) ? null : _txtSearchTitle.Text);
            };
            card.Controls.Add(btnStar);
            btnAction.Left = 158;
        }

        return card;
    }

    private bool EnsureStoreReady()
    {
        if (_store != null) return true;
        _lblStatus.Text = "Status: Database not ready.";
        return false;
    }

    // Boots SQLite + our in-memory structures. Constructor finishes with data ready so button clicks never hit a null store silently.
    private void InitializeStoreWithPreloadedData()
    {
        try
        {
            SqliteVideoRepository repository = new(_databasePath);
            SqliteRentalRepository rentalRepo = new(_databasePath);
            SqliteUserRepository userRepo = new(_databasePath);

            repository.EnsureDatabaseAndSchema();

            _userStore = new UserStore(userRepo);
            _store = new VideoStore(repository, rentalRepo);

            _userStore.LoadFromRepository();
            _store.LoadFromRepository();

            // RegisterUser fails if the row already exists, so we can safely call this on every startup for the demo admin.
            _userStore.RegisterUser("123", "123");

            if (_store.Count == 0)
            {
                SeedDefaultData();
            }

            _lblStatus.Text = $"Status: Ready — {_store.Count} videos.";
            SetView(ViewMode.Library, "Browse", "RENT");
        }
        catch (Exception ex)
        {
            _lblStatus.Text = $"Status: Init failed — {ex.Message}";
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

    private static void AttachQuickHover(Button button, Color normal, Color hover)
    {
        button.MouseEnter += (_, _) => button.BackColor = hover;
        button.MouseLeave += (_, _) => button.BackColor = normal;
    }

    private static void AttachSimpleCardHover(Panel card, Color normal, Color hover)
    {
        card.MouseEnter += (_, _) => card.BackColor = hover;
        card.MouseLeave += (_, _) => card.BackColor = normal;
    }

    // Shared helper so every rounded button uses the same corner math instead of copy/pasting four arc calls.
    private static GraphicsPath GetRoundedPath(Rectangle rect, int radius)
    {
        GraphicsPath path = new();
        int diameter = radius * 2;
        path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
