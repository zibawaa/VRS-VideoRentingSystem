using System.Drawing.Drawing2D;

namespace VideoRentalSystem;

// Extra coursework idea: small animated "AI helper" with arms (see marking scheme optional +10).
// Drawing uses WinForms GDI+ only (Microsoft, 2025) 'Graphics Class'. Available at: https://learn.microsoft.com/en-us/dotnet/api/system.drawing.graphics (Accessed: 9 April 2026).
public class RentalAgentPanel : Panel
{
    private readonly System.Windows.Forms.Timer _timer = new System.Windows.Forms.Timer();
    private int _armWobble;
    private int _dir = 1;

    public RentalAgentPanel()
    {
        DoubleBuffered = true;
        BackColor = Color.WhiteSmoke;
        BorderStyle = BorderStyle.FixedSingle;
        _timer.Interval = 110;
        _timer.Tick += (_, _) =>
        {
            _armWobble += _dir * 2;
            if (_armWobble > 20 || _armWobble < -20)
                _dir *= -1;
            Invalidate();
        };
        HandleCreated += (_, _) => _timer.Start();
        HandleDestroyed += (_, _) => _timer.Stop();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(BackColor);

        using (var head = new SolidBrush(Color.Silver))
            g.FillEllipse(head, 40, 14, 40, 40);

        using (var body = new SolidBrush(Color.SteelBlue))
            g.FillRectangle(body, 52, 54, 18, 44);

        int w = _armWobble;
        using (var pen = new Pen(Color.Black, 2f))
        {
            g.DrawLine(pen, 52, 64, 14, 64 + w);
            g.DrawLine(pen, 70, 64, 108, 64 - w);
            g.DrawLine(pen, 56, 98, 44, 132);
            g.DrawLine(pen, 66, 98, 78, 132);
        }

        using (var f = new Font(FontFamily.GenericSansSerif, 7.5f))
        using (var b = new SolidBrush(Color.DimGray))
        {
            g.DrawString("Rental helper\n(happy arms!)", f, b, 8, 138);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _timer.Dispose();
        base.Dispose(disposing);
    }
}
