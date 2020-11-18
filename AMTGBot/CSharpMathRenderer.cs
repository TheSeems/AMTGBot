using CSharpMath.SkiaSharp;
using SkiaSharp;
using System.IO;

namespace AMTGBot
{
    internal sealed class CSharpMathRenderer : ILatexRenderer
    {
        const int LatexBorder = 50;

        public Stream Render(string latex)
        {
            var painter = new MathPainter { LaTeX = latex };
            var path = Path.GetTempFileName();
            var measures = painter.Measure();

            SKBitmap bitMap = new SKBitmap(
                width: ((int)measures.Width) + 2 * LatexBorder,
                height: ((int)measures.Height) + 2 * LatexBorder
            );

            var canvas = new SKCanvas(bitMap);
            painter.Draw(
                canvas: canvas,
                point: new SKPoint(LatexBorder, (measures.Height / 2.0f) + LatexBorder)
           );
            canvas.Flush();

            using (var image = SKImage.FromBitmap(bitMap))
            using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
            {
                // save the data to a stream
                using var streamf = File.OpenWrite(path);
                data.SaveTo(streamf);
            }

            return File.Open(path, FileMode.Open);
        }
    }
}
