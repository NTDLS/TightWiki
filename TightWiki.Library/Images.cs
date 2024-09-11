using ImageMagick;
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

        public static byte[] ResizeGifImage(byte[] imageBytes, int width, int height)
        {
            using var imageCollection = new MagickImageCollection(imageBytes);

            if (imageCollection.Count > 10)
            {
                Parallel.ForEach(imageCollection, frame =>
                {
                    frame.Sample((uint)width, (uint)height);
                });
            }
            else
            {
                Parallel.ForEach(imageCollection, frame =>
                {
                    frame.Resize((uint)width, (uint)height);
                });
            }

            return imageCollection.ToByteArray();
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
                    throw new NotImplementedException("Use [ResizeGifImage] for saving animated images.");
                //image.SaveAsGif(ms);
                //return preferredContentType;
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
