using SkiaSharp;

namespace MvcApp.Common.Captcha;

/// <summary>
/// Renders a CAPTCHA challenge as a PNG.
///
/// This must be a raster. The previous implementation returned an SVG with the characters as
/// &lt;text&gt; elements, so the response body literally contained the answer — a client could read
/// the digits out of the markup with no OCR at all, which made the challenge worthless however
/// strictly it was validated server-side.
///
/// Noise and rotation are deliberately light: the goal is to defeat naive automation, not to make
/// six digits hard for a person to read, and accessibility still needs an alternative path.
/// </summary>
public static class CaptchaChallenge
{
    private const int Width = 180;
    private const int Height = 52;
    private const float FontSize = 28f;
    private const string FontFamily = "DejaVu Sans";

    public static byte[] RenderPng(string code)
    {
        var random = Random.Shared;

        var info = new SKImageInfo(Width, Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;

        canvas.Clear(SKColors.White);

        using (var speckle = new SKPaint { Color = new SKColor(205, 205, 205), IsAntialias = false, StrokeWidth = 1f })
        {
            for (var i = 0; i < 150; i++)
            {
                canvas.DrawPoint(random.Next(Width), random.Next(Height), speckle);
            }
        }

        using (var wave = new SKPaint { IsAntialias = true, StrokeWidth = 1.1f, Style = SKPaintStyle.Stroke })
        {
            for (var i = 0; i < 3; i++)
            {
                wave.Color = new SKColor(
                    (byte)random.Next(150, 215),
                    (byte)random.Next(150, 215),
                    (byte)random.Next(150, 215));

                canvas.DrawLine(
                    random.Next(Width), random.Next(Height),
                    random.Next(Width), random.Next(Height),
                    wave);
            }
        }

        using var typeface =
            SKTypeface.FromFamilyName(FontFamily, SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
            ?? SKTypeface.Default;

        using var font = new SKFont(typeface, FontSize);
        using var ink = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor((byte)random.Next(10, 60), (byte)random.Next(10, 60), (byte)random.Next(70, 140))
        };

        // Distribute the characters across slots with a small jitter so they are not evenly spaced.
        var slot = (float)Width / (code.Length + 1);

        for (var i = 0; i < code.Length; i++)
        {
            var x = slot * (i + 0.7f);
            var y = (Height * 0.7f) + random.Next(-4, 5);
            var angle = random.Next(-18, 19);

            canvas.Save();
            canvas.RotateDegrees(angle, x, y);
            canvas.DrawText(code[i].ToString(), x, y, font, ink);
            canvas.Restore();
        }

        using var image = surface.Snapshot();
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 90);

        return encoded.ToArray();
    }
}
