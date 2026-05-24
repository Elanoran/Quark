using System.Drawing.Drawing2D;

namespace Quark.App;

public static class QuarkIconPainter
{
    public static void Paint(Graphics g, Rectangle bounds, bool hasError)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        float scale = Math.Min(bounds.Width, bounds.Height) / 128f;
        g.TranslateTransform(bounds.X, bounds.Y);
        g.ScaleTransform(scale, scale);

        using var bg = new LinearGradientBrush(
            new Rectangle(0, 0, 128, 128),
            Color.FromArgb(42, 26, 94),
            hasError ? Color.FromArgb(82, 24, 42) : Color.FromArgb(13, 8, 32),
            LinearGradientMode.ForwardDiagonal);
        using GraphicsPath bgPath = RoundedRect(new RectangleF(0, 0, 128, 128), 28);
        g.FillPath(bg, bgPath);

        using var border = new Pen(Color.FromArgb(90, 138, 100, 255), 1.5f);
        g.DrawPath(border, bgPath);

        DrawOrbit(g, Color.FromArgb(150, 138, 100, 255), 1.5f, -30, 42, 16);
        DrawOrbit(g, Color.FromArgb(95, 180, 140, 255), 1.2f, 30, 42, 16);
        DrawOrbit(g, Color.FromArgb(90, 108, 78, 255), 1.2f, 90, 42, 14);

        using var halo = new SolidBrush(Color.FromArgb(45, 109, 74, 255));
        g.FillEllipse(halo, 46, 46, 36, 36);

        using var core = new LinearGradientBrush(
            new Rectangle(53, 53, 22, 22),
            Color.FromArgb(200, 168, 255),
            hasError ? Color.FromArgb(220, 72, 96) : Color.FromArgb(74, 45, 181),
            LinearGradientMode.ForwardDiagonal);
        g.FillEllipse(core, 53, 53, 22, 22);

        using var shine = new SolidBrush(Color.FromArgb(100, 255, 255, 255));
        g.FillEllipse(shine, 56, 57, 8, 6);

        DrawParticle(g, 97, 53, 3.8f, Color.FromArgb(220, 200, 168, 255));
        DrawParticle(g, 35, 82, 3.3f, Color.FromArgb(205, 138, 100, 255));
        DrawParticle(g, 64, 22, 2.8f, Color.FromArgb(190, 170, 136, 255));

        g.ResetTransform();
    }

    private static void DrawOrbit(Graphics g, Color color, float width, float angle, float rx, float ry)
    {
        GraphicsState state = g.Save();
        g.TranslateTransform(64, 64);
        g.RotateTransform(angle);
        using var pen = new Pen(color, width);
        g.DrawEllipse(pen, -rx, -ry, rx * 2, ry * 2);
        g.Restore(state);
    }

    private static void DrawParticle(Graphics g, float x, float y, float radius, Color color)
    {
        using var brush = new SolidBrush(color);
        g.FillEllipse(brush, x - radius, y - radius, radius * 2, radius * 2);
        using var shine = new SolidBrush(Color.FromArgb(120, 255, 255, 255));
        g.FillEllipse(shine, x - radius / 2, y - radius / 2, radius * 0.65f, radius * 0.65f);
    }

    private static GraphicsPath RoundedRect(RectangleF bounds, float radius)
    {
        float d = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
