using PhotoSauce.MagicScaler;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.IO;

namespace TightWiki.Library
{
    public static class Images
    {
        public enum ImageFormat
        {
            Png,
            Jpeg,
            Bmp,
            Tiff
        }

        public static byte[] ResizeImageBytes(byte[] imageBytes, int newWidth, int newHeight)
        {
            using (MemoryStream outStream = new())
            {
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
        }

        public static Image ResizeImage(Image image, int width, int height)
        {
            image.Mutate(x => x.Resize(width, height));
            //image.Save("output/fb.png");
            return image;
        }

        public static void ChangeImageType(Image img, ImageFormat format, MemoryStream ms)
        {
            switch (format)
            {
                case ImageFormat.Png:
                    img.SaveAsPng(ms);
                    break;
                case ImageFormat.Jpeg:
                    img.SaveAsJpeg(ms);
                    break;
                case ImageFormat.Bmp:
                    img.SaveAsBmp(ms);
                    break;
                case ImageFormat.Tiff:
                    img.SaveAsTiff(ms);
                    break;
                default:
                    throw new System.Exception("Unsupported image type.");
            }
        }
    }
}
