using System.Drawing.Drawing2D;
using System.Globalization;
using VideoRentingSystem.Core.Core;
using VideoRentingSystem.Core.Data;
using VideoRentingSystem.Core.Models;

namespace VideoRentingSystem.App;

public sealed class MainForm : Form
{
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
    private readonly HashSet<int> _savedVideoIds = [];
    private readonly string _databasePath;
    private readonly Font _titleFont = new("Segoe UI Semibold", 13F, FontStyle.Bold, GraphicsUnit.Point);
    private readonly Font _cardTitleFont = new("Segoe UI Semibold", 11F, FontStyle.Bold, GraphicsUnit.Point);
    private readonly Font _smallLabelFont = new("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
    private readonly Font _navFont = new("Segoe UI Semibold", 8.5F, FontStyle.Bold, GraphicsUnit.Point);

    public MainForm()
    {
        _databasePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VideoRentingSystem",
            "videos.db");
        // sqlite file lives under LocalAppData so it persists per windows user

        Text = "VRS — Video Renting";
        ClientSize = new Size(1180, 720);
        MinimumSize = new Size(960, 600);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = VrsBlack;
        Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        DoubleBuffered = true;
        // window chrome: title, size, centre on screen, theme colours, default font, less flicker on resize

        Panel bottomStatus = new()
        {
            Dock = DockStyle.Bottom,
            Height = 26,
            BackColor = Color.FromArgb(18, 18, 22),
            Padding = new Padding(12, 4, 12, 0)
        };
        // thin strip docked to the bottom for status text

        _lblStatus = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = Color.Silver,
            BackColor = Color.Transparent,
            Text = "Status: Initializing...",
            TextAlign = ContentAlignment.MiddleLeft
        };
        // fills the status strip and shows ready and error messages

        bottomStatus.Controls.Add(_lblStatus);

        Panel bottomNav = BuildBottomNav();
        bottomNav.Dock = DockStyle.Bottom;
        bottomNav.Height = 58;
        // browse rented saved account upload row above the status strip

        _cardsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = VrsPanel,
            AutoScroll = true,
            Padding = new Padding(20, 12, 20, 12),
            WrapContents = true
        };
        // main catalogue area where cards or account admin panels go

        Panel mainFill = new()
        {
            Dock = DockStyle.Fill,
            BackColor = VrsPanel
        };
        // wrapper so the flow panel can fill between header and bottom chrome

        mainFill.Controls.Add(_cardsPanel);

        Panel header = BuildHeader();
        header.Dock = DockStyle.Top;
        header.Height = 124;
        // logo, search row, and header logout

        Controls.Add(mainFill);
        Controls.Add(header);
        Controls.Add(bottomNav);
        Controls.Add(bottomStatus);
        // dock order: fill first then top and bottom bars so layout stacks correctly

        InitializeStoreWithPreloadedData();
        // load db, seed if empty, open browse view
    }

    private Panel BuildBrandHeader()
    {
        Panel host = new()
        {
            Dock = DockStyle.Top,
            Height = 58,
            BackColor = VrsBlack
        };
        // black band that holds the centred brand row

        FlowLayoutPanel row = new()
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = VrsBlack
        };
        // horizontal strip: emoji then VRS wordmark

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
        // camera emoji plus red bold VRS text matching the coursework mock-up

        row.Controls.Add(lblCam);
        row.Controls.Add(lblVrs);
        host.Controls.Add(row);

        void CenterRow(object? sender, EventArgs e)
        {
            row.Left = Math.Max(0, (host.Width - row.Width) / 2);
            row.Top = Math.Max(0, (host.Height - row.Height) / 2);
        }
        // keep the brand row centred when the window or host resizes

        host.Layout += CenterRow;
        host.Resize += CenterRow;

        return host;
    }

    private Panel BuildBottomNav()
    {
        Panel bar = new()
        {
            BackColor = Color.FromArgb(6, 6, 8),
            Padding = new Padding(16, 10, 16, 10)
        };
        // dark padded bar behind the nav pills

        FlowLayoutPanel flow = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = false
        };
        // single row of pill buttons left to right

        _btnNavLibrary = CreateNavPill("Browse", true, (_, _) => SetView(ViewMode.Library, "Browse", "RENT"));
        _btnNavRented = CreateNavPill("Rented", false, (_, _) => SetView(ViewMode.Rented, "My rentals", "RENTED"));
        _btnNavSaved = CreateNavPill("Saved", false, (_, _) => SetView(ViewMode.Saved, "Saved videos", "SAVED"));
        _btnNavAccount = CreateNavPill("Account", false, (_, _) => SetView(ViewMode.Account, "Account", ""));
        _btnNavAdmin = CreateNavPill("Studio", false, (_, _) => SetView(ViewMode.Admin, "Studio dashboard", "STUDIO"));
        // each handler calls SetView with the right mode, header label text, and section tag chip
        _btnNavAdmin.Visible = false;
        // upload tab only appears after demo admin signs in

        flow.Controls.Add(_btnNavLibrary);
        flow.Controls.Add(_btnNavRented);
        flow.Controls.Add(_btnNavSaved);
        flow.Controls.Add(_btnNavAccount);
        flow.Controls.Add(_btnNavAdmin);
        // add pills left to right in browse rented saved account upload order

        bar.Controls.Add(flow);
        return bar;
    }

    private Panel BuildHeader()
    {
        Panel header = new() { BackColor = VrsBlack };
        // whole header: logout strip, brand, search strip

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
        // top-left pill; stays hidden until someone logs in

        _btnLogout.FlatAppearance.BorderColor = VrsPillBorder;
        _btnLogout.FlatAppearance.BorderSize = 1;
        _btnLogout.Region = new Region(GetRoundedPath(new Rectangle(0, 0, _btnLogout.Width, _btnLogout.Height), 14));
        // thin outline plus rounded clip matching other pills

        _btnLogout.SizeChanged += (_, _) =>
        {
            _btnLogout.Region = new Region(GetRoundedPath(new Rectangle(0, 0, _btnLogout.Width, _btnLogout.Height), 14));
        };
        // region must track auto-sized width and height

        _btnLogout.Click += (_, _) =>
        {
            _currentUser = null;
            UpdateRoleNavigation();
            _btnLogout.Visible = false;
            _lblStatus.Text = "Status: Logged out.";
            SetView(ViewMode.Library, "Browse", "RENT");
        };
        // sign out resets session state and returns to the catalogue shell

        Panel brandHeader = BuildBrandHeader();

        Panel searchRow = new()
        {
            Dock = DockStyle.Bottom,
            Height = 44,
            BackColor = VrsBlack,
            Padding = new Padding(120, 4, 120, 4)
        };
        // holds the section tag, search host, and action buttons

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
        // small label chip; text and visibility come from SetView

        _lblSectionTag.SizeChanged += (_, _) =>
        {
            _lblSectionTag.Region = new Region(GetRoundedPath(new Rectangle(0, 0, _lblSectionTag.Width, _lblSectionTag.Height), 12));
        };
        // rounded pill tracking label bounds

        _txtSearchTitle = new TextBox
        {
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
            Height = 32,
            BackColor = VrsInputBg,
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = "Search keyword (optional: genre:Sci-Fi price<=3.99)"
        };
        // shared filter for library rented and saved before account hides it

        _btnSearch = CreateAccentPillButton("Search", false, (_, _) =>
        {
            if (!EnsureStoreReady()) return;
            RenderCards(_txtSearchTitle.Text);
        });
        // rebind cards to the current string using the title index

        _btnDisplayAll = CreateMutedPillButton("Show all", (_, _) =>
        {
            _txtSearchTitle.Text = string.Empty;
            RenderCards();
        });
        // wipes filter text then redraws the unfiltered list for this tab

        FlowLayoutPanel searchFlow = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        // horizontal layout for tag + search + buttons

        Panel searchHost = new() { Width = 520, Height = 36 };
        // inner host so the textbox can stretch with spare width

        _txtSearchTitle.Location = new Point(0, 2);
        _txtSearchTitle.Width = searchHost.Width;
        // align inside the host and consume its width

        searchHost.Controls.Add(_txtSearchTitle);

        searchHost.Resize += (_, _) => { _txtSearchTitle.Width = searchHost.Width; };
        // when header width changes keep the textbox full width of host

        searchFlow.Controls.Add(_lblSectionTag);
        searchFlow.Controls.Add(searchHost);
        searchFlow.Controls.Add(_btnSearch);
        searchFlow.Controls.Add(_btnDisplayAll);
        // left to right: chip, growing search, search button, reset button

        searchRow.Controls.Add(searchFlow);

        Panel topRow = new() { Dock = DockStyle.Top, Height = 40, BackColor = VrsBlack };
        // thin bar that only contains logout so it hugs the top edge

        topRow.Controls.Add(_btnLogout);

        header.Controls.Add(searchRow);
        header.Controls.Add(brandHeader);
        header.Controls.Add(topRow);
        // order matters for vertical docking: search bottoms out, brand fills middle, logout pins top

        return header;
    }

    private Button CreateNavPill(string text, bool primary, EventHandler onClick)
    {
        Color back = primary ? VrsBurgundy : Color.FromArgb(34, 36, 46);
        // active tab uses burgundy; inactive tabs sit on a darker grey

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
        // uppercase label and fixed pill footprint for the bottom nav

        b.FlatAppearance.BorderColor = primary ? VrsRedOutline : VrsPillBorder;
        b.FlatAppearance.BorderSize = 1;
        b.Region = new Region(GetRoundedPath(new Rectangle(0, 0, b.Width, b.Height), 10));
        b.SizeChanged += (_, _) =>
        {
            b.Region = new Region(GetRoundedPath(new Rectangle(0, 0, b.Width, b.Height), 10));
        };
        // outline plus rounded shape, refreshed whenever the button resizes

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
        // slightly smaller pill for header actions like login or add

        b.FlatAppearance.BorderColor = burgundy ? VrsRedOutline : VrsPillBorder;
        b.FlatAppearance.BorderSize = 1;
        b.Region = new Region(GetRoundedPath(new Rectangle(0, 0, b.Width, b.Height), 8));
        b.SizeChanged += (_, _) =>
        {
            b.Region = new Region(GetRoundedPath(new Rectangle(0, 0, b.Width, b.Height), 8));
        };
        // same border and rounded region pattern with a smaller radius

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
        // secondary style for register, remove, show all

        b.FlatAppearance.BorderColor = VrsPillBorder;
        b.FlatAppearance.BorderSize = 1;
        b.Region = new Region(GetRoundedPath(new Rectangle(0, 0, b.Width, b.Height), 8));
        b.SizeChanged += (_, _) =>
        {
            b.Region = new Region(GetRoundedPath(new Rectangle(0, 0, b.Width, b.Height), 8));
        };
        // muted buttons still get the same rounded hit target treatment

        b.Click += onClick;

        return b;
    }

    private void SetView(ViewMode mode, string headerContext, string tag)
    {
        _ = headerContext;
        // reserved for a future header subtitle; call sites still pass context today

        _viewMode = mode;
        // RenderCards, empty-state copy, and row-vs-tile layout all branch on this

        UpdateNavSelection(mode);
        // pills are separate controls from mode, so appearance must be synced here

        _lblSectionTag.Visible = !string.IsNullOrEmpty(tag);
        _lblSectionTag.Text = tag;
        // hide the chip when tag is empty (account) and set RENT vs RENTED etc from the caller

        bool showSearch = mode is ViewMode.Library or ViewMode.Rented or ViewMode.Saved;
        // Account and Admin replace the grid; search only applies to catalogue-style lists

        _txtSearchTitle.Visible = showSearch;
        _btnSearch.Visible = showSearch;
        _btnDisplayAll.Visible = showSearch;
        // show or hide as a set so we never surface a search box without its actions

        if (!showSearch)
        {
            _txtSearchTitle.Text = string.Empty;
            // leaving Browse-style tabs should not leave hidden text affecting the next visit
        }

        _btnLogout.Visible = _currentUser != null && mode != ViewMode.Account;
        // Account panel already includes log out; the header duplicate would crowd the strip

        RenderCards(string.IsNullOrWhiteSpace(_txtSearchTitle.Text) ? null : _txtSearchTitle.Text);
        // tab change can alter eligible videos; re-run query and keep typed filter when visible
    }

    private void UpdateNavSelection(ViewMode mode)
    {
        SetNavActive(_btnNavLibrary, mode == ViewMode.Library);
        SetNavActive(_btnNavRented, mode == ViewMode.Rented);
        SetNavActive(_btnNavSaved, mode == ViewMode.Saved);
        SetNavActive(_btnNavAccount, mode == ViewMode.Account);
        SetNavActive(_btnNavAdmin, mode == ViewMode.Admin);
        // exactly one pill should read as selected: library catalogue, rented list, saved ids, account card, or admin tools
    }

    private static void SetNavActive(Button b, bool active)
    {
        b.BackColor = active ? VrsBurgundy : Color.FromArgb(34, 36, 46);
        b.FlatAppearance.BorderColor = active ? VrsRedOutline : VrsPillBorder;
        // active uses burgundy fill and red outline; inactive uses grey pairing
    }

    private void UpdateRoleNavigation()
    {
        bool studioVisible = _currentUser != null && (_currentUser.Role == UserRole.Publisher || _currentUser.Role == UserRole.Admin);
        _btnNavAdmin.Visible = studioVisible;
        if (!studioVisible && _viewMode == ViewMode.Admin)
        {
            _viewMode = ViewMode.Library;
        }
        // studio dashboard is limited to publisher/admin roles and hidden otherwise
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
        // shared themed field for account and admin numeric entry rows

        box.PlaceholderText = placeholder;
        // hint text when empty e.g. ID or Title

        return box;
    }

    private void RenderAccountView()
    {
        Panel p = new Panel
        {
            Width = 560,
            Height = _currentUser == null ? 560 : 250,
            BackColor = VrsCard,
            Margin = new Padding(8, 8, 8, 8)
        };
        // tall card for login form or shorter card for profile summary

        p.Region = new Region(GetRoundedPath(new Rectangle(0, 0, p.Width, p.Height), 14));
        p.SizeChanged += (_, _) =>
        {
            p.Region = new Region(GetRoundedPath(new Rectangle(0, 0, p.Width, p.Height), 14));
        };
        // clip the panel to rounded corners; refresh region if height changes between states

        Label lblTitle = new Label { Text = "Account", ForeColor = Color.White, Font = _titleFont, Left = 32, Top = 28, Width = 300 };

        p.Controls.Add(lblTitle);

        if (_currentUser != null)
        {
            Label lblUser = new Label
            {
                Text = $"Signed in as {_currentUser.Username} ({_currentUser.Role})\nUser ID {_currentUser.UserId}" + (string.IsNullOrWhiteSpace(_currentUser.StudioName) ? "" : $"\nStudio: {_currentUser.StudioName}"),
                ForeColor = Color.Gainsboro,
                Font = _cardTitleFont,
                Left = 32,
                Top = 78,
                Width = 440,
                Height = 72
            };
            // who is logged in from the in-memory user model

            Button btnOut = CreateMutedPillButton("Log out", (_, _) =>
            {
                _currentUser = null;
                UpdateRoleNavigation();
                _btnLogout.Visible = false;
                _lblStatus.Text = "Status: Logged out.";
                SetView(ViewMode.Library, "Browse", "RENT");
            });
            // same side effects as the header logout for consistency

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
            // mask password glyphs in the wireframe-friendly way

            y += 72;

            p.Controls.Add(new Label { Text = "ROLE", ForeColor = Color.Silver, Font = _smallLabelFont, Left = 32, Top = y, Width = 120 });
            ComboBox cbRole = new()
            {
                Left = 32,
                Top = y + 22,
                Width = 220,
                Height = 30,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = VrsInputBg,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            cbRole.Items.AddRange(["Customer", "Publisher", "Admin"]);
            cbRole.SelectedIndex = 0;
            p.Controls.Add(cbRole);
            // registration uses explicit account role instead of a hidden username shortcut

            y += 72;

            p.Controls.Add(new Label { Text = "STUDIO (optional for publisher/admin)", ForeColor = Color.Silver, Font = _smallLabelFont, Left = 32, Top = y, Width = 260 });
            TextBox tStudio = CreateSmallTextBox(32, y + 22, "", 220);
            p.Controls.Add(tStudio);

            y += 72;

            Button btnIn = CreateAccentPillButton("Login", true, (_, _) =>
            {
                User? u = _userStore?.Login(tUser.Text, tPass.Text);

                if (u != null)
                {
                    _currentUser = u;
                    UpdateRoleNavigation();
                    _btnLogout.Visible = true;
                    _lblStatus.Text = "Status: Welcome back.";
                    SetView(ViewMode.Library, "Browse", "RENT");
                    // any successful login shows header logout and jumps back to browse tiles
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
                UserRole selectedRole = cbRole.SelectedIndex switch
                {
                    1 => UserRole.Publisher,
                    2 => UserRole.Admin,
                    _ => UserRole.Customer
                };
                bool reg = _userStore?.RegisterUser(tUser.Text, tPass.Text, selectedRole, tStudio.Text) ?? false;
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
        // host the account card inside the same scrolling panel as video tiles
    }

    private void RenderAdminView()
    {
        if (_currentUser == null || (_currentUser.Role != UserRole.Publisher && _currentUser.Role != UserRole.Admin))
        {
            Label denied = new()
            {
                Width = 760,
                Height = 48,
                ForeColor = Color.Silver,
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Regular, GraphicsUnit.Point),
                Text = "Studio dashboard is available only for Publisher or Admin accounts."
            };
            _cardsPanel.Controls.Add(denied);
            return;
        }
        // the studio panel is role-gated so customer accounts cannot edit catalogue records

        Panel p = new Panel { Width = 930, Height = 580, BackColor = VrsCard, Margin = new Padding(8, 8, 8, 8) };
        p.Region = new Region(GetRoundedPath(new Rectangle(0, 0, p.Width, p.Height), 14));

        string scopeText = _currentUser.Role == UserRole.Admin
            ? "Admin mode: manage all publisher catalogue rows."
            : $"Publisher mode: managing studio '{_currentUser.StudioName ?? _currentUser.Username}'.";
        Label lblHint = new Label { Text = scopeText, ForeColor = Color.Silver, Font = _cardTitleFont, Left = 28, Top = 24, Width = 760 };
        p.Controls.Add(lblHint);

        Label lblAdd = new Label { Text = "Create / publish title", ForeColor = Color.White, Font = new Font("Segoe UI", 10F, FontStyle.Bold), Left = 28, Top = 72, Width = 220 };
        p.Controls.Add(lblAdd);

        TextBox tId = CreateSmallTextBox(28, 98, "ID", 74);
        TextBox tTitle = CreateSmallTextBox(110, 98, "Title", 210);
        TextBox tGenre = CreateSmallTextBox(328, 98, "Genre", 120);
        TextBox tYear = CreateSmallTextBox(456, 98, "Year", 68);
        ComboBox cbType = new()
        {
            Left = 532,
            Top = 98,
            Width = 96,
            Height = 30,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = VrsInputBg,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        cbType.Items.AddRange(["Movie", "Series"]);
        cbType.SelectedIndex = 0;

        TextBox tPrice = CreateSmallTextBox(636, 98, "Price", 80);
        TextBox tHours = CreateSmallTextBox(724, 98, "Hours", 70);
        CheckBox ckPublished = new()
        {
            Left = 804,
            Top = 104,
            Width = 100,
            Text = "Published",
            Checked = true,
            ForeColor = Color.Silver,
            BackColor = Color.Transparent
        };

        TextBox tOwnerId = CreateSmallTextBox(28, 138, "Owner ID", 94);
        tOwnerId.Visible = _currentUser.Role == UserRole.Admin;
        if (_currentUser.Role == UserRole.Admin)
        {
            tOwnerId.Text = _currentUser.UserId.ToString(CultureInfo.InvariantCulture);
        }

        Button btnAdd = CreateAccentPillButton("Save title", true, (_, _) =>
        {
            if (!TryReadId(tId.Text, out int id) || !TryReadYear(tYear.Text, out int year))
            {
                return;
            }

            if (!decimal.TryParse(tPrice.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal price) &&
                !decimal.TryParse(tPrice.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out price))
            {
                _lblStatus.Text = "Status: Price must be a number.";
                return;
            }

            if (!int.TryParse(tHours.Text, out int hours))
            {
                _lblStatus.Text = "Status: Hours must be an integer.";
                return;
            }

            int ownerId = _currentUser.Role == UserRole.Admin
                ? (int.TryParse(tOwnerId.Text, out int parsedOwner) ? parsedOwner : _currentUser.UserId)
                : _currentUser.UserId;

            try
            {
                VideoType type = cbType.SelectedIndex == 1 ? VideoType.Series : VideoType.Movie;
                Video video = new Video(id, tTitle.Text, tGenre.Text, year, false, ownerId, type, price, hours, ckPublished.Checked);
                bool added = _store!.AddVideo(video, _currentUser);
                _lblStatus.Text = added ? "Status: Title saved." : "Status: Save denied (duplicate ID or ownership rule).";
                if (added)
                {
                    RenderCards(_txtSearchTitle.Text);
                }
            }
            catch (Exception ex)
            {
                _lblStatus.Text = $"Status: {ex.Message}";
            }
        });
        btnAdd.Left = 804;
        btnAdd.Top = 138;
        btnAdd.Width = 100;

        p.Controls.Add(tId);
        p.Controls.Add(tTitle);
        p.Controls.Add(tGenre);
        p.Controls.Add(tYear);
        p.Controls.Add(cbType);
        p.Controls.Add(tPrice);
        p.Controls.Add(tHours);
        p.Controls.Add(ckPublished);
        p.Controls.Add(tOwnerId);
        p.Controls.Add(btnAdd);

        Label lblRem = new Label { Text = "Delete title", ForeColor = Color.White, Font = new Font("Segoe UI", 10F, FontStyle.Bold), Left = 28, Top = 196, Width = 200 };
        TextBox tRemId = CreateSmallTextBox(28, 222, "Video ID", 120);
        Button btnRem = CreateMutedPillButton("Remove", (_, _) =>
        {
            if (!TryReadId(tRemId.Text, out int id))
            {
                return;
            }

            bool removed = _store!.RemoveVideo(id, _currentUser);
            _lblStatus.Text = removed ? "Status: Title removed." : "Status: Removal denied or title missing.";
            if (removed)
            {
                tRemId.Text = "";
                RenderCards(_txtSearchTitle.Text);
            }
        });
        btnRem.Left = 156;
        btnRem.Top = 220;
        btnRem.Width = 96;
        p.Controls.Add(lblRem);
        p.Controls.Add(tRemId);
        p.Controls.Add(btnRem);

        Label lblSearch = new Label { Text = "Find by ID", ForeColor = Color.White, Font = new Font("Segoe UI", 10F, FontStyle.Bold), Left = 28, Top = 278, Width = 200 };
        TextBox tSearchId = CreateSmallTextBox(28, 304, "Video ID", 120);
        Button btnSearchId = CreateMutedPillButton("Search", (_, _) =>
        {
            if (!TryReadId(tSearchId.Text, out int id))
            {
                return;
            }

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
        btnSearchId.Top = 302;
        btnSearchId.Width = 96;
        p.Controls.Add(lblSearch);
        p.Controls.Add(tSearchId);
        p.Controls.Add(btnSearchId);

        int ownedCount = _currentUser.Role == UserRole.Admin ? _store!.DisplayAllVideos().Length : _store!.GetPublisherVideos(_currentUser.UserId).Length;
        Label hint = new Label
        {
            Text = _currentUser.Role == UserRole.Admin
                ? $"Admin visibility: {ownedCount} total titles in catalogue."
                : $"Studio visibility: {ownedCount} titles owned by your publisher account.",
            ForeColor = Color.Gray,
            Font = _smallLabelFont,
            Left = 28,
            Top = 520,
            Width = 860
        };
        p.Controls.Add(hint);

        _cardsPanel.Controls.Add(p);
    }

    private void RenderCards(string? titleFilter = null)
    {
        _cardsPanel.Controls.Clear();
        // drop previous tiles or placeholder labels before rebuilding

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
        // admin and account short-circuit because they are not card grids

        (string? keyword, string? genre, decimal? maxPrice) = ParseSearchFilters(titleFilter);
        Video[] videos = _store.FilterCatalog(keyword, genre, maxPrice);
        if (_viewMode == ViewMode.Library && !string.IsNullOrWhiteSpace(titleFilter))
        {
            _lblStatus.Text = $"Status: {videos.Length} match(es).";
        }
        // browse queries use keyword index plus optional genre and max-price filters

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
                Video[] all = string.IsNullOrWhiteSpace(titleFilter)
                    ? _store.DisplayPublishedVideos()
                    : _store.FilterCatalog(keyword, genre, maxPrice);
                List<Video> kept = [];
                foreach (Video v in all)
                {
                    if (_savedVideoIds.Contains(v.VideoId))
                    {
                        kept.Add(v);
                    }
                }

                videos = kept.ToArray();
                // keep only starred ids from the same baseline list search would have used
            }

            if (_currentUser == null)
            {
                _lblStatus.Text = "Status: Saved list is local to this session — log in to rent.";
                // stars live in a session hash set, not sqlite, until you wire persistence
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
        bool showSave = (_viewMode == ViewMode.Library || _viewMode == ViewMode.Saved) && _currentUser != null;
        // rentals use horizontal rows with a countdown chip; save star only when logged in on browse or saved

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
        // full-width row that stretches with the flow panel client area

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
        // decorative star matching the mock-up; not wired to favourites here

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
        // primary label line with metadata

        string daysText = "—";

        if (_store!.TryGetRentalInfo(userId, video.VideoId, out _, out DateTime expiryUtc, out decimal paidAmount))
        {
            TimeSpan remaining = expiryUtc - DateTime.UtcNow;
            int left = Math.Max(0, (int)Math.Ceiling(remaining.TotalDays));
            daysText = left == 1 ? $"1 DAY LEFT · ${paidAmount:F2}" : $"{left} DAYS LEFT · ${paidAmount:F2}";
            // expiry and paid amount come from persisted rental rows for pay-per-title visibility
        }
        else if (_store.TryGetRentDate(userId, video.VideoId, out DateTime rentAt))
        {
            int used = (DateTime.UtcNow.Date - rentAt.Date).Days;
            int left = Math.Max(0, RentalPeriodDays - used);
            daysText = left == 1 ? "1 DAY LEFT" : $"{left} DAYS LEFT";
            // keep old fallback path for records created before expiry columns existed
        }
        else
        {
            daysText = $"{RentalPeriodDays} DAYS LEFT";
            // if sqlite has no timestamp yet still show the window length chip
        }

        Label pill = new()
        {
            Text = daysText,
            AutoSize = false,
            Width = 170,
            Height = 30,
            Left = row.Width - 186,
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
            pill.Left = row.Width - 186;
        };
        // when the flow panel grows, widen the title and pin the pill to the right edge

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
        // fixed tile width; extra height when the save star row is visible

        card.Region = new Region(GetRoundedPath(new Rectangle(0, 0, card.Width, card.Height), 12));

        AttachSimpleCardHover(card, VrsCard, VrsCardHover);
        // brighten slightly on hover for affordance

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
            Text = $"{video.Genre} · {video.ReleaseYear} · {video.Type}"
        };
        // id, title, and meta lines stacked down the left margin

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
        // distinguish global IsRented from "this user is the renter" for button labels

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
            Width = 226,
            ForeColor = stateColor,
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Regular, GraphicsUnit.Point),
            Text = $"{stateText} · ${video.RentalPrice:F2} / {video.RentalHours}h"
        };
        // colour cues: green available, red taken

        int actionTop = showSaveToggle ? 128 : 120;

        Button btnAction = new()
        {
            Left = 126,
            Top = actionTop,
            Width = 108,
            Height = 30,
            FlatStyle = FlatStyle.Flat,
            BackColor = video.IsRented ? Color.FromArgb(56, 62, 78) : VrsBurgundy,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Regular, GraphicsUnit.Point),
            Cursor = Cursors.Hand,
            Text = video.IsRented ? (isRentedByMe ? "Return" : "Busy") : $"Rent ${video.RentalPrice:F2}"
        };
        // rent vs return vs disabled busy state on one control

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
            // refresh tiles so flags and chips match the store without losing an active search string
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
                // toggle session set then redraw so the star glyph updates immediately
            };

            card.Controls.Add(btnStar);

            btnAction.Left = 126;
            // nudge the rent button right so it clears the wider star column
        }

        return card;
    }

    private bool EnsureStoreReady()
    {
        if (_store != null) return true;

        _lblStatus.Text = "Status: Database not ready.";
        // user clicked search before init finished or init threw

        return false;
    }

    private static (string? keyword, string? genre, decimal? maxPrice) ParseSearchFilters(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return (null, null, null);
        }

        string? keyword = null;
        string? genre = null;
        decimal? maxPrice = null;
        string[] parts = raw.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (int i = 0; i < parts.Length; i++)
        {
            string token = parts[i];
            if (token.StartsWith("genre:", StringComparison.OrdinalIgnoreCase))
            {
                string parsedGenre = token["genre:".Length..].Trim();
                if (!string.IsNullOrWhiteSpace(parsedGenre))
                {
                    genre = parsedGenre;
                }

                continue;
            }

            if (token.StartsWith("price<=", StringComparison.OrdinalIgnoreCase))
            {
                string parsedPrice = token["price<=".Length..].Trim();
                if (decimal.TryParse(parsedPrice, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal max1) ||
                    decimal.TryParse(parsedPrice, NumberStyles.Number, CultureInfo.CurrentCulture, out max1))
                {
                    maxPrice = max1;
                }

                continue;
            }

            if (keyword == null)
            {
                keyword = token;
            }
            else
            {
                keyword = $"{keyword} {token}";
            }
        }
        // free text remains keyword; prefixed tokens become structured filters

        return (keyword, genre, maxPrice);
    }

    private void InitializeStoreWithPreloadedData()
    {
        try
        {
            SqliteVideoRepository repository = new(_databasePath);
            SqliteRentalRepository rentalRepo = new(_databasePath);
            SqliteUserRepository userRepo = new(_databasePath);
            // three repos share one sqlite file but each owns its own table access helpers

            repository.EnsureDatabaseAndSchema();

            _userStore = new UserStore(userRepo);
            _store = new VideoStore(repository, rentalRepo);

            _userStore.LoadFromRepository();
            _store.LoadFromRepository();
            // hydrate BST, AVL, hash map, and rental map from disk

            // keeps a seeded admin account for demos without manual SQL inserts
            _userStore.RegisterUser("123", "123", UserRole.Admin, "VRS Demo Studio");

            if (_store.Count == 0)
            {
                SeedDefaultData();
            }

            _lblStatus.Text = $"Status: Ready — {_store.Count} videos.";

            UpdateRoleNavigation();
            SetView(ViewMode.Library, "Browse", "RENT");
        }
        catch (Exception ex)
        {
            _lblStatus.Text = $"Status: Init failed — {ex.Message}";
            // surface driver or schema errors in the status strip instead of crashing silently
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
        AddIfMissing(new Video(1001, "Interstellar", "Sci-Fi", 2014, false, 0, VideoType.Movie, 3.99m, 72, true));
        AddIfMissing(new Video(1002, "The Dark Knight", "Action", 2008, false, 0, VideoType.Movie, 3.49m, 72, true));
        AddIfMissing(new Video(1003, "Inception", "Sci-Fi", 2010, false, 0, VideoType.Movie, 3.29m, 48, true));
        AddIfMissing(new Video(1004, "Pulp Fiction", "Crime", 1994, false, 0, VideoType.Movie, 2.79m, 48, true));
        AddIfMissing(new Video(1005, "Our Planet", "Documentary", 2019, false, 0, VideoType.Series, 4.49m, 96, true));
        // gives a non-empty catalogue on first launch; duplicates are ignored by id
    }

    private void AddIfMissing(Video video)
    {
        _store!.AddVideo(video);
        // AddVideo rejects duplicate primary keys so calling blindly is safe
    }

    private static void AttachQuickHover(Button button, Color normal, Color hover)
    {
        button.MouseEnter += (_, _) => button.BackColor = hover;
        button.MouseLeave += (_, _) => button.BackColor = normal;
        // simple hover feedback without custom painting
    }

    private static void AttachSimpleCardHover(Panel card, Color normal, Color hover)
    {
        card.MouseEnter += (_, _) => card.BackColor = hover;
        card.MouseLeave += (_, _) => card.BackColor = normal;
    }

    private static GraphicsPath GetRoundedPath(Rectangle rect, int radius)
    {
        GraphicsPath path = new();
        int diameter = radius * 2;
        path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        // four quarter-circle arcs close into a rounded rectangle usable as Region or fill
        return path;
    }
}
