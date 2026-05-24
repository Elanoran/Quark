using System.Runtime.InteropServices;

namespace Quark.App;

public static class TrayIconRenderer
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    public static Icon Create(int? unreadCount, bool hasError)
    {
        using var bitmap = new Bitmap(32, 32);
        using Graphics g = Graphics.FromImage(bitmap);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        Color body = hasError ? Color.FromArgb(180, 64, 64) : Color.FromArgb(76, 93, 219);
        using var bodyBrush = new SolidBrush(body);
        using var linePen = new Pen(Color.White, 2.2f);

        var envelope = new RectangleF(4, 8, 24, 17);
        g.FillRoundedRectangle(bodyBrush, envelope, 4);
        g.DrawLine(linePen, 6, 10, 16, 18);
        g.DrawLine(linePen, 26, 10, 16, 18);

        if (unreadCount is > 0)
        {
            string text = unreadCount > 99 ? "99+" : unreadCount.Value.ToString();
            using var badgeBrush = new SolidBrush(Color.FromArgb(222, 46, 59));
            using var badgePen = new Pen(Color.White, 1.5f);
            var badge = new RectangleF(14, 0, 18, 18);
            g.FillEllipse(badgeBrush, badge);
            g.DrawEllipse(badgePen, badge);

            float size = text.Length > 2 ? 6.5f : 9f;
            using var font = new Font("Segoe UI", size, FontStyle.Bold, GraphicsUnit.Point);
            using var textBrush = new SolidBrush(Color.White);
            var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(text, font, textBrush, badge, format);
        }

        IntPtr handle = bitmap.GetHicon();
        try
        {
            return (Icon)Icon.FromHandle(handle).Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    private static void FillRoundedRectangle(this Graphics graphics, Brush brush, RectangleF bounds, float radius)
    {
        using var path = new System.Drawing.Drawing2D.GraphicsPath();
        float diameter = radius * 2;
        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        graphics.FillPath(brush, path);
    }
}
