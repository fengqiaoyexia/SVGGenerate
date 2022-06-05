using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace SingleToDuplciate
{
    internal class PNGGenerate
    {
        private Size MUG_PLATE_SIZE = new Size(1662, 600);

        public Stream GenerateDuplicateIamgeStream(Stream imageStream)
        {
            var thumbnailImage = GetThumbnailImage(imageStream);
            if (thumbnailImage.Width * 2 > MUG_PLATE_SIZE.Width) return null;

            var duplicateImage = DuplicateImage(thumbnailImage);

            return duplicateImage;
        }

        private Image GetThumbnailImage(Stream imageStream)
        {
            var image = Image.FromStream(imageStream);

            var imageInfo = ScaledImage(MUG_PLATE_SIZE.Height, image.Width, image.Height);
            return image.GetThumbnailImage(imageInfo.Width, imageInfo.Height, null, IntPtr.Zero); ;
        }

        private Size ScaledImage(int height, int imageWidth, int imageHeight)
        {
            var newSize = new Size { Width = imageWidth, Height = imageHeight };
            if (imageHeight <= height) return newSize;

            newSize.Height = (int)(height);
            newSize.Width = (int)(height * 1.0 / imageHeight * 1.0 * imageWidth * 1.0);

            return newSize;
        }

        private Stream DuplicateImage(Image image)
        {
            MemoryStream ms = new MemoryStream();
            Bitmap bitmap = new Bitmap(MUG_PLATE_SIZE.Width, MUG_PLATE_SIZE.Height);
            var g = Graphics.FromImage(bitmap);
            g.Clear(Color.White);

            var image1_x = (MUG_PLATE_SIZE.Width / 2 - image.Width) / 2;
            var image1_y = (MUG_PLATE_SIZE.Height - image.Height) / 2;
            var image2_x = image1_x * 3 + image.Width;

            g.DrawImage(image, image1_x, image1_y);
            g.DrawImage(image, image2_x, image1_y);
            g.Save();
            g.Dispose();
            //bitmap.Save(@"D:\RowanPersonalFile\PAInfo\DuplicateImage\NewSolutions\Images\pngDuplicate.png", ImageFormat.Png);
            bitmap.Save(ms, ImageFormat.Png);
            return ms;
        }
    }
}