using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

internal static class PictureService
{
    public static bool MergePictures(
        string stampPath,
        string signPath,
        string outputPath,
        int padding = 20,
        float signScale = 1f)
    {
        try
        {
            using Image<Rgba32> stamp = Image.Load<Rgba32>(stampPath);
            using Image<Rgba32> sign = Image.Load<Rgba32>(signPath);

            if (signScale != 1f)
            {
                int newWidth = (int)(sign.Width * signScale);
                int newHeight = (int)(sign.Height * signScale);

                sign.Mutate(x => x.Resize(newWidth, newHeight));
            }

            int finalWidth = Math.Max(stamp.Width, sign.Width) + padding * 2;
            int finalHeight = Math.Max(stamp.Height, sign.Height) + padding * 2;
            using Image<Rgba32> result = new Image<Rgba32>(finalWidth, finalHeight);

            Random rnd = new();
            float maxRotation = 35f;
            float angle = (float)(rnd.NextDouble() * 2 * maxRotation - maxRotation); 
            stamp.Mutate(x => x.Rotate(angle, KnownResamplers.Bicubic));

            result.Mutate(ctx =>
            {
                int stampX = (finalWidth - stamp.Width) / 2;
                int stampY = (finalHeight - stamp.Height) / 2;
                ctx.DrawImage(stamp, new Point(stampX, stampY), 1f);

                int signX = (finalWidth - sign.Width) / 2;
                int signY = stampY + (stamp.Height - sign.Height) / 2;

                ctx.DrawImage(sign, new Point(signX, signY), 1f);
            });

            result.Save(outputPath);
            return true;
        }
        catch
        {
            return false;
        }
    }
}