using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Size = System.Drawing.Size;

namespace PhotoGenerator.Models.Classes
{
    public class DirectBitmap : IDisposable
    {
        public Bitmap Bitmap { get; set; }
        public Int32[] Bits { get; private set; }
        public bool Disposed { get; private set; }
        public int Height { get; private set; }
        public int Width { get; private set; }

        protected GCHandle BitsHandle { get; private set; }

        public DirectBitmap(Image img, Size size) : this(size.Width, size.Height)
        {
            Bitmap = new Bitmap(img, size.Width, size.Height);
            for (int i = 0; i < Bitmap.Width; i++)
            {
                for (int j = 0; j < Bitmap.Height; j++)
                {
                    SetPixel(j, i, Bitmap.GetPixel(i, j));
                }
            }
        }

        public DirectBitmap(DirectBitmap img, Size size) : this(size.Width, size.Height)
        {
            Bitmap = new Bitmap(img.Bitmap, size.Width, size.Height);
            for (int i = 0; i < Bitmap.Width; i++)
            {
                for (int j = 0; j < Bitmap.Height; j++)
                {
                    SetPixel(j, i, Bitmap.GetPixel(i, j));
                }
            }
        }

        public DirectBitmap(DirectBitmap dibitmap) : this(dibitmap, new Size(dibitmap.Width, dibitmap.Height)) { }

        public DirectBitmap(int width, int height)
        {
            Width = width;
            Height = height;
            Bits = new Int32[width * height];
            BitsHandle = GCHandle.Alloc(Bits, GCHandleType.Pinned);
            if (Bitmap == null)
                Bitmap = new Bitmap(width, height, width * 4, PixelFormat.Format32bppPArgb, BitsHandle.AddrOfPinnedObject());
        }


        public void SetPixel(int x, int y, Color colour)
        {
            int index = x + (y * Width);
            int col = colour.ToArgb();
            try
            {
                Bits[index] = col;
            }
            catch
            {
                Console.WriteLine("Error");
            }
        }

        public Color GetPixel(int x, int y)
        {
            int index = x + (y * Width);
            int col = Bits[index];
            Color result = Color.FromArgb(col);

            return result;
        }

        public void Dispose()
        {
            if (Disposed) return;
            Disposed = true;
            Bitmap.Dispose();
            BitsHandle.Free();
        }
    }
}
