using ImageMagick;

namespace TightWiki.Library
{
    public static class ImagesUtility
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


        public static MagickImage ResizeImage(MagickImage image, uint width, uint height)
        {
            image.Resize(width, height);
            return image;
        }

        public static string BestEffortConvertImage(MagickImage image, MemoryStream ms, string preferredContentType)
        {
            switch (preferredContentType.ToLowerInvariant())
            {
                case "image/png":
                    image.Format = MagickFormat.Png;
                    image.Write(ms);
                    return preferredContentType;
                case "image/jpeg":
                    image.Format = MagickFormat.Jpeg;
                    image.Write(ms);
                    return preferredContentType;
                case "image/bmp":
                    image.Format = MagickFormat.Bmp;
                    image.Write(ms);
                    return preferredContentType;
                case "image/gif":
                    throw new NotImplementedException("Use [ResizeGifImage] for saving animated images.");
                case "image/tiff":
                    image.Format = MagickFormat.Tiff;
                    image.Write(ms);
                    return preferredContentType;
                default:
                    image.Format = MagickFormat.Png;
                    image.Write(ms);
                    return "image/png";
            }
        }

        /// <summary>
        /// Take a height and width and enforces a max on both dimensions while maintaining the ratio.
        /// </summary>
        public static (int Width, int Height) ScaleToMaxOf(int originalWidth, int originalHeight, int maxSize)
        {
            // Calculate aspect ratio
            float aspectRatio = (float)originalWidth / originalHeight;

            // Determine new dimensions based on the larger dimension
            int newWidth, newHeight;
            if (originalWidth > originalHeight)
            {
                // Scale down the width to the maxSize and calculate the height
                newWidth = maxSize;
                newHeight = (int)(maxSize / aspectRatio);
            }
            else
            {
                // Scale down the height to the maxSize and calculate the width
                newHeight = maxSize;
                newWidth = (int)(maxSize * aspectRatio);
            }

            return (newWidth, newHeight);
        }

        /// <summary>
        /// Crops an image to a centered square and returns the result as a byte array.
        /// </summary>
        /// <remarks>This method crops the input image to a square by centering the crop area and ensuring
        /// the largest possible square is extracted. The resulting image is encoded in WebP format regardless of the
        /// input format.</remarks>
        /// <param name="imageBytes">The byte array representing the input image. The image must be in a format supported by the underlying image
        /// processing library.</param>
        /// <returns>A byte array containing the cropped image in WebP format. If the input image is already square, it is
        /// returned unchanged in WebP format.</returns>
        public static byte[] CropImageToCenteredSquare(MemoryStream inputStream)
        {
            using var image = new MagickImage(inputStream);

            if (image.Width != image.Height)
            {
                int size = Math.Min((int)image.Width, (int)image.Height);
                int x = ((int)image.Width - size) / 2;
                int y = ((int)image.Height - size) / 2;

                image.Crop(new MagickGeometry(x, y, (uint)size, (uint)size));
                image.ResetPage(); // Reset the canvas offset after cropping
            }

            image.Format = MagickFormat.WebP;

            using var outputStream = new MemoryStream();
            image.Write(outputStream);
            return outputStream.ToArray();
        }
    }
}
