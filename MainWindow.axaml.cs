using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace AvaloniaImageProcessing
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            DragDrop.SetAllowDrop(this, true);

            AddHandler(DragDrop.DragEnterEvent, OnDragEnter);
            //AddHandler(DragDrop.DragOverEvent, OnDragOver);
            AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
            AddHandler(DragDrop.DropEvent, OnDrop);

            mImagePanel = new ImagePanel();
            Content = mImagePanel;
        }

        private void OnDragLeave(object? sender, RoutedEventArgs e)
        {
            Background = Brushes.Transparent;
        }

        private void OnDragEnter(object? sender, DragEventArgs e)
        {
            Background = Brushes.LightGray;
        }

        private void OnDrop(object? sender, DragEventArgs e)
        {
            var file = e.Data.GetFileNames()?.ToList()[0];

            if (file == null)
                return;

            using (var stream = System.IO.File.OpenRead(file))
            {
                var image = WriteableBitmap.Decode(stream);
                Width = image.Size.Width;
                Height = image.Size.Height;
                mImagePanel.SetImage(image);
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        class ImagePanel : Panel
        {
            public void SetImage(WriteableBitmap image)
            {
                mImage = image;
                mImageCopy = new WriteableBitmap(
                    mImage.PixelSize,
                    mImage.Dpi,
                    PixelFormat.Bgra8888,
                    AlphaFormat.Premul);
            }

            public override void Render(DrawingContext context)
            {
                base.Render(context);

                if (mImage == null)
                    return;

                ReDrawImage();

                context.DrawImage(mImageCopy, new Rect(0, 0, mImageCopy.Size.Width, mImageCopy.Size.Height));
            }

            protected override void OnPointerMoved(PointerEventArgs e)
            {
                base.OnPointerMoved(e);
                mLastMousePoint = e.GetPosition(this);
                InvalidateVisual();
            }

            private unsafe void ReDrawImage()
            {
                using (var leftBuffer = mImage.Lock())
                using (var copyBuffer = mImageCopy.Lock())
                {
                    byte* buffer = (byte*)leftBuffer.Address.ToPointer();
                    byte* copybuffer = (byte*)copyBuffer.Address.ToPointer();

                    for (int x = 0; x < mImage.Size.Width/* - 1*/; x++)
                    {
                        for (int y = 0; y < mImage.Size.Height; y++)
                        {
                            int loc = (int)(x + y * mImage.Size.Width);
                            //int loc1 = (int)((x+1) + y * mImage.Size.Width);

                            byte b = buffer[loc * 4];
                            byte g = buffer[loc * 4 + 1];
                            byte r = buffer[loc * 4 + 2];
                            byte a = buffer[loc * 4 + 3];

                            /*byte b1 = buffer[loc1 * 4];
                            byte g1 = buffer[loc1 * 4 + 1];
                            byte r1 = buffer[loc1 * 4 + 2];
                            byte a1 = buffer[loc1 * 4 + 3];

                            int brightness1 = Brightness(r, g, b);
                            int brightness2 = Brightness(r1, g1, b1);

                            int diff = Math.Abs(brightness1 - brightness2);

                            byte targetColor;
                            if (diff > mLastMousePoint.X.Map(0, Bounds.Width, 2, 60))
                            {
                                targetColor = 0;
                            }
                            else
                            {
                                targetColor = 255;
                            }

                            copybuffer[loc * 4] = targetColor;
                            copybuffer[loc * 4 + 1] = targetColor;
                            copybuffer[loc * 4 + 2] = targetColor;
                            copybuffer[loc * 4 + 3] = a;

                            //byte targetColor;

                            /*if (brightness > mLastMousePoint.X)
                                targetColor = 255;
                            else
                                targetColor = 0;

                            copybuffer[loc * 4] = targetColor;
                            copybuffer[loc * 4 + 1] = targetColor;
                            copybuffer[loc * 4 + 2] = targetColor;
                            copybuffer[loc * 4 + 3] = a;*/

                            double distance = mLastMousePoint.Distance(x, y);
                            double factor = distance.Map(0, 200, 2, 0);

                            copybuffer[loc * 4] = Clamp(b * factor, 0, 255);
                            copybuffer[loc * 4 + 1] = Clamp(g * factor, 0, 255);
                            copybuffer[loc * 4 + 2] = Clamp(r * factor, 0, 255);
                            copybuffer[loc * 4 + 3] = a;
                        }
                    }
                }
            }

            private byte Clamp(double value, double min, double max)
            {
                if (value < min)
                    return (byte)min;
                if (value > max)
                    return (byte)max;
                return (byte)value;
            }

            private static byte Brightness(byte r, byte g, byte b)
            {
                return (byte)((r + g + b) / 3);
            }

            Point mLastMousePoint;
            WriteableBitmap mImage;
            WriteableBitmap mImageCopy;
        }
        ImagePanel mImagePanel;
    }

    static class ExtensionMethods
    {
        public static double Map (this double value, double fromSource, double toSource, double fromTarget, double toTarget)
        {
            return (value - fromSource) / (toSource - fromSource) * (toTarget - fromTarget) + fromTarget;
        }

        public static double Distance(this Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }

        public static double Distance(this Point p1, double x, double y)
        {
            return Math.Sqrt(Math.Pow(p1.X - x, 2) + Math.Pow(p1.Y - y, 2));
        }
    }
}