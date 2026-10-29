using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace HdrSwitcher.App;

/// <summary>
/// Gera o ícone da bandeja em tempo de execução: azul quando algum monitor está com HDR ligado,
/// cinza quando todos estão desligados.
/// </summary>
internal static class AppIconFactory
{
    public static Icon Create(bool hdrOn)
    {
        using var bitmap = new Bitmap(32, 32);

        using (Graphics graphics = Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            using var background = new SolidBrush(hdrOn ? Color.FromArgb(0, 120, 212) : Color.FromArgb(64, 64, 64));
            using GraphicsPath shape = RoundedRectangle(new Rectangle(0, 0, 32, 32), 7);
            graphics.FillPath(background, shape);

            using var font = new Font("Segoe UI", 11f, FontStyle.Bold, GraphicsUnit.Pixel);
            using var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
            };

            graphics.DrawString("HDR", font, Brushes.White, new RectangleF(0, 1, 32, 30), format);
        }

        IntPtr handle = bitmap.GetHicon();
        try
        {
            using Icon temporary = Icon.FromHandle(handle);
            return (Icon)temporary.Clone();
        }
        finally
        {
            NativeMethods.DestroyIcon(handle);
        }
    }

    private static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
    {
        int diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
