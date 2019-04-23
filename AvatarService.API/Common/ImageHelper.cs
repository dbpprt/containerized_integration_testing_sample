using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using SixLabors.Shapes;

namespace AvatarService.API.Common
{
    public static class ImageHelper
    {
        public static byte[] ToAvatar(byte[] picture, int size = 800, int cornerRadius = 100)
        {
            using (var img = Image.Load(picture))
            using (var destRound = img.Clone(x => x.ConvertToAvatar(new Size(size, size), cornerRadius)))
            using (var result = new MemoryStream())
            {
                destRound.Mutate(x => x.AutoOrient());

                destRound.SaveAsJpeg(result);
                result.Position = 0;
                return result.ToArray();
            }
        }

        private static IImageProcessingContext<Rgba32> ConvertToAvatar(this IImageProcessingContext<Rgba32> processingContext, Size size, float cornerRadius)
        {
            return processingContext.Resize(new ResizeOptions
            {
                Size = size,
                Mode = ResizeMode.Max
            });
        }

        private static void ApplyRoundedCorners(Image<Rgba32> img, float cornerRadius)
        {
            var corners = BuildCorners(img.Width, img.Height, cornerRadius);

            var graphicOptions = new GraphicsOptions(true)
            {
                BlenderMode = PixelBlenderMode.Src
            };

            img.Mutate(x => x.Fill(graphicOptions, Rgba32.Transparent, corners));
        }

        private static IPathCollection BuildCorners(int imageWidth, int imageHeight, float cornerRadius)
        {
            var rect = new RectangularPolygon(-0.5f, -0.5f, cornerRadius, cornerRadius);

            var cornerTopLeft = rect.Clip(new EllipsePolygon(cornerRadius - 0.5f, cornerRadius - 0.5f, cornerRadius));

            var rightPos = imageWidth - cornerTopLeft.Bounds.Width + 1;
            var bottomPos = imageHeight - cornerTopLeft.Bounds.Height + 1;

            var cornerTopRight = cornerTopLeft.RotateDegree(90).Translate(rightPos, 0);
            var cornerBottomLeft = cornerTopLeft.RotateDegree(-90).Translate(0, bottomPos);
            var cornerBottomRight = cornerTopLeft.RotateDegree(180).Translate(rightPos, bottomPos);

            return new PathCollection(cornerTopLeft, cornerBottomLeft, cornerTopRight, cornerBottomRight);
        }
    }
}
