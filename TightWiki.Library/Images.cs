using PhotoSauce.MagicScaler;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace TightWiki.Library
{
    public static class Images
    {
        public enum ImageFormat
        {
            Png,
            Jpeg,
            Bmp,
            Tiff,
            Gif
        }

        public static byte[] ResizeImageBytes(byte[] imageBytes, int newWidth, int newHeight)
        {
            using MemoryStream outStream = new();
            ProcessImageSettings processImageSettings = new()
            {
                Width = newWidth,
                Height = newHeight,
                ResizeMode = CropScaleMode.Stretch,
                HybridMode = HybridScaleMode.Turbo
            };
            MagicImageProcessor.ProcessImage(imageBytes, outStream, processImageSettings);
            return outStream.ToArray();
        }

        public static Image ResizeImage(Image image, int width, int height)
        {
            image.Mutate(x => x.Resize(width, height));
            return image;
        }

        public static string BestEffortConvertImage(Image image, MemoryStream ms, string preferredContentType)
        {
            switch (preferredContentType.ToLower())
            {
                case "image/png":
                    image.SaveAsPng(ms);
                    return preferredContentType;
                case "image/jpeg":
                    image.SaveAsJpeg(ms);
                    return preferredContentType;
                case "image/bmp":
                    image.SaveAsBmp(ms);
                    return preferredContentType;
                case "image/gif":
                    image.SaveAsGif(ms);
                    return preferredContentType;
                case "image/tiff":
                    image.SaveAsTiff(ms);
                    return preferredContentType;
                default:
                    image.SaveAsPng(ms);
                    return "image/png";
            }
        }
    }
}
