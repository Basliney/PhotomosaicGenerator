using MS.WindowsAPICodePack.Internal;
using PhotoGenerator.Models.Classes;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace PhotoGenerator.Services
{
    /// <summary>
    /// Сервис генерации картинки
    /// Допустимые разрешения
    /// 160 320 640 1280 2560 5120 10240 20480
    /// 158 316 632 1264 2528 5056 10112 20224
    /// 159 318 636 1272 2544 5088 10176 20352
    /// 182 364 728 1456 2912 5824 11648 23296
    /// 183 366 732 1464 2928 5856 11712 23424
    /// </summary>
    public class ImageConstructorService : IDisposable
    {
        private readonly Settings settings;

        private Random rnd = new Random();
        private bool disposedValue;

        /// <summary>
        /// Изображение над которым производятся изменения
        /// </summary>
        private Bitmap _middleLayer;

        /// <summary>
        /// Выходное изображение
        /// </summary>
        private Image OutputImage { get; set; }

        /// <summary>
        /// Общее количество разбиений
        /// </summary>
        public int CountOfCut { get; set; }

        /// <summary>
        /// Событие готовности изображения
        /// </summary>
        public Action<Bitmap, int> OnReadyImage;

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="settings"></param>
        public ImageConstructorService(Settings settings)
        {
            this.settings = settings;
        }

        /// <summary>
        /// Старт обработки
        /// </summary>
        /// <param name="img"></param>
        /// <returns></returns>
        async public Task StartGeneration(Bitmap img)
        {
            try
            {
                _middleLayer = new Bitmap(img, new Size(settings.Resolution, settings.Resolution));  // Передаем в промежуточный слой

                OutputImage = CutTheImage(ref _middleLayer, 0);
                using (Bitmap outBitmap = new Bitmap(OutputImage))
                {
                    outBitmap.Save(Guid.NewGuid().ToString() + ".jpg", ImageFormat.Jpeg);
                    OnReadyImage?.Invoke(new(outBitmap), CountOfCut);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Ошибка во время нарезки");
            }
            finally
            {
                GC.Collect();
            }
        }

        /// <summary>
        /// Рекурсивный метод нарезки и заполнения изображений
        /// </summary>
        /// <param name="Bitmap"></param>
        /// <param name="depth"></param>
        /// <returns></returns>
        private Bitmap CutTheImage(ref Bitmap Bitmap, int depth)
        {
            var width = Bitmap.Width; var height = Bitmap.Height;

            Bitmap firstBitmap = new Bitmap(Bitmap.Width / 2, Bitmap.Height / 2);
            Rectangle rectangle = new Rectangle(0, 0, firstBitmap.Width, firstBitmap.Height);
            firstBitmap = Bitmap.Clone(rectangle, PixelFormat.Format16bppRgb555);

            Bitmap lastCuttedBitmap = new Bitmap(Bitmap.Width / 2, Bitmap.Height / 2);
            rectangle = new Rectangle(lastCuttedBitmap.Width, lastCuttedBitmap.Height, lastCuttedBitmap.Width, lastCuttedBitmap.Height);
            lastCuttedBitmap = Bitmap.Clone(rectangle, PixelFormat.Format16bppRgb555);

            Bitmap prelastCuttedBitmap = new Bitmap(lastCuttedBitmap);
            rectangle = new Rectangle(0, prelastCuttedBitmap.Height, prelastCuttedBitmap.Width, prelastCuttedBitmap.Height);
            prelastCuttedBitmap = Bitmap.Clone(rectangle, PixelFormat.Format16bppRgb555);

            Bitmap secondCuttedBitmap = new Bitmap(lastCuttedBitmap);
            rectangle = new Rectangle(secondCuttedBitmap.Width, 0, secondCuttedBitmap.Width, secondCuttedBitmap.Height);
            secondCuttedBitmap = Bitmap.Clone(rectangle, PixelFormat.Format16bppRgb555);

            // Тут перевести битмапы к 4 на 4

            Color firstColor = MeanColor(firstBitmap);
            Color secondColor = MeanColor(secondCuttedBitmap);
            Color thirdColor = MeanColor(prelastCuttedBitmap);
            Color fourthColor = MeanColor(lastCuttedBitmap);

            // Перевод цветов в ЛАБ формат
            var labFC = StaticData.RGBToLab(firstColor);
            var labSC = StaticData.RGBToLab(secondColor);
            var labTC = StaticData.RGBToLab(thirdColor);
            var labLC = StaticData.RGBToLab(fourthColor);

            // Дистанции цветов всех соседей
            var dist1 = StaticData.GetDistance(labFC, labSC);
            var dist2 = StaticData.GetDistance(labSC, labTC);
            var dist3 = StaticData.GetDistance(labTC, labLC);
            var dist4 = StaticData.GetDistance(labLC, labFC);
            var dist5 = StaticData.GetDistance(labFC, labTC);
            var dist6 = StaticData.GetDistance(labSC, labLC);

            // TODO : привести битмапы к размерам 4 на 4 для быстрого анализа.

            if ((dist1 > settings.Density || dist5 > settings.Density || dist3 > settings.Density || dist6 > settings.Density) && width > settings.WidthMax)
            {
                var outer1 = Task.Factory.StartNew(() =>      // внешняя задача
                {
                    var result = CutTheImage(ref firstBitmap, depth + 1);
                    firstBitmap = result;
                });
                var outer2 = Task.Factory.StartNew(() =>      // внешняя задача
                {
                    var result = CutTheImage(ref secondCuttedBitmap, depth + 1);
                    secondCuttedBitmap = result;
                });
                var outer3 = Task.Factory.StartNew(() =>      // внешняя задача
                {
                    var result = CutTheImage(ref prelastCuttedBitmap, depth + 1);
                    prelastCuttedBitmap = result;
                });
                var outer4 = Task.Factory.StartNew(() =>      // внешняя задача
                {
                    var result = CutTheImage(ref lastCuttedBitmap, depth + 1);
                    lastCuttedBitmap = result;
                });
                outer1.Wait();
                outer2.Wait();
                outer3.Wait();
                outer4.Wait();

                lock (Bitmap)
                {
                    for (int i = 0; i < Math.Ceiling((decimal)width / 2); i++)
                    {
                        for (int j = 0; j < Math.Ceiling((decimal)height / 2); j++)
                        {
                            try
                            {
                                Bitmap.SetPixel(i, j, firstBitmap.GetPixel(Math.Min(i, firstBitmap.Width - 1), Math.Min(j, firstBitmap.Height - 1)));
                                Bitmap.SetPixel(i + width / 2, j, secondCuttedBitmap.GetPixel(Math.Min(i, secondCuttedBitmap.Width - 1), Math.Min(j, secondCuttedBitmap.Height - 1)));
                                Bitmap.SetPixel(i, j + height / 2, prelastCuttedBitmap.GetPixel(Math.Min(i, prelastCuttedBitmap.Width - 1), Math.Min(j, prelastCuttedBitmap.Height - 1)));
                                Bitmap.SetPixel(i + width / 2, j + height / 2, lastCuttedBitmap.GetPixel(Math.Min(i, lastCuttedBitmap.Width - 1), Math.Min(j, lastCuttedBitmap.Height - 1)));
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Ошибка при занесении пикселей с похожей фотки");
                                continue;
                            }
                        }
                    }
                    CountOfCut += 4;
                }
            }
            else
            {
                Bitmap firstSimilar = GetSimilarPhoto(labFC, ref firstBitmap, out var path1);
                Bitmap secondSimilar = GetSimilarPhoto(labSC, ref secondCuttedBitmap, out var path2);
                Bitmap thirdSimilar = GetSimilarPhoto(labTC, ref prelastCuttedBitmap, out var path3);
                Bitmap fourthSimilar = GetSimilarPhoto(labLC, ref lastCuttedBitmap, out var path4);

                if (path1.Equals(path2) && path2.Equals(path3) && path3.Equals(path4))
                {
                    Bitmap bmp = new Bitmap(Image.FromFile(path1), Bitmap.Size);
                    ColorTransform(ref Bitmap, ref bmp);
                    return bmp;
                }

                lock (Bitmap)
                {
                    for (int i = 0; i < Math.Ceiling((decimal)width / 2); i++)
                    {
                        for (int j = 0; j < Math.Ceiling((decimal)height / 2); j++)
                        {
                            try
                            {
                                Bitmap.SetPixel(i, j, firstSimilar.GetPixel(Math.Min(i, firstSimilar.Width - 1), Math.Min(j, firstSimilar.Height - 1)));
                                Bitmap.SetPixel(i + width / 2, j, secondSimilar.GetPixel(Math.Min(i, secondSimilar.Width - 1), Math.Min(j, secondSimilar.Height - 1)));
                                Bitmap.SetPixel(i, j + height / 2, thirdSimilar.GetPixel(Math.Min(i, thirdSimilar.Width - 1), Math.Min(j, thirdSimilar.Height - 1)));
                                Bitmap.SetPixel(i + width / 2, j + height / 2, fourthSimilar.GetPixel(Math.Min(i, fourthSimilar.Width - 1), Math.Min(j, fourthSimilar.Height - 1)));
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Ошибка при занесении пикселей с похожей фотки");
                                continue;
                            }
                        }
                    }
                    CountOfCut += 4;
                }

                firstSimilar.Dispose();
                secondSimilar.Dispose();
                thirdSimilar.Dispose();
                fourthSimilar.Dispose();
            }

            try
            {
                if (depth < 3)
                    OnReadyImage?.Invoke(new(Bitmap), CountOfCut);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("GDI+ error");
            }

            firstBitmap.Dispose();
            secondCuttedBitmap.Dispose();
            prelastCuttedBitmap.Dispose();
            lastCuttedBitmap.Dispose();

            return Bitmap;
        }

        private Bitmap GetSimilarPhoto((float, float, float) labColor, ref Bitmap stockBitmap, out string path)
        {
            Bitmap img;
            using (var fileWorker = new FileWorker())
            {
                var similarImages = fileWorker.GetMinDistance(labColor);
                Size size = new Size(stockBitmap.Width, stockBitmap.Height);

                if (similarImages.Count() > 0)
                {
                    var selectedObject = similarImages.ElementAt(rnd.Next(similarImages.Count()));
                    img = new Bitmap(Image.FromFile(selectedObject.Path), size);
                    path = selectedObject.Path;
                }
                else
                {
                    Random rnd = new Random();
                    var image = settings.FileWorker.DataImages[rnd.Next(settings.FileImagesCount)];
                    img = new Bitmap(Image.FromFile(image.Path), size);
                    path = image.Path;
                }
            }

            if (rnd.Next(100) > 50)
            {
                img.RotateFlip(RotateFlipType.RotateNoneFlipX);
            }

            ColorTransform(ref stockBitmap, ref img);
            return img;
        }

        private void ColorTransform(ref Bitmap stockBitmap, ref Bitmap img)
        {
            for (int i = 0; i < img.Width; i++)
            {
                for (int j = 0; j < img.Height; j++)
                {
                    try
                    {
                        Color meanColor = GetMeanColorOfPixels(img.GetPixel(i, j), stockBitmap.GetPixel(i, j));
                        img.SetPixel(i, j, meanColor);
                    }
                    catch
                    {
                        Console.WriteLine("Exception while color is changing");
                        return;
                    }
                }
            }
        }

        private Color GetMeanColorOfPixels(Color newColor, Color targetColor)
        {
            //return newColor; // не меняя заменяемые пиксы
            // При среднем изменении
            // return Color.FromArgb((newColor.R * 7 + targetColor.R * 3) / 10, (newColor.G * 7 + targetColor.G * 3) / 10, (newColor.B * 7 + targetColor.B * 3) / 10);
            return Color.FromArgb((newColor.R + targetColor.R) / 2, (newColor.G + targetColor.G) / 2, (newColor.B + targetColor.B) / 2);
        }

        private Color MeanColor(Bitmap Bitmap)
        {
            // Added
            Bitmap = new Bitmap(Bitmap, new Size(8, 8));

            int r = 0, g = 0, b = 0;
            int global_r = 0, global_g = 0, global_b = 0;
            int c = 1, j = 1;
            for (c = 0; c < Bitmap.Width; c++)//for (c = 0; c < Bitmap.Width / 2; c++)
            {
                for (j = 0; j < Bitmap.Height; j++)//for (j = 0; j < Bitmap.Height / 2; j++)
                {
                    r += Bitmap.GetPixel(c, j).R;
                    g += Bitmap.GetPixel(c, j).G;
                    b += Bitmap.GetPixel(c, j).B;
                }
                global_r += r; r = 0;
                global_g += g; g = 0;
                global_b += b; b = 0;
            }
            global_r /= (Math.Max(c, 1) * Math.Max(j, 1));
            global_g /= (Math.Max(c, 1) * Math.Max(j, 1));
            global_b /= (Math.Max(c, 1) * Math.Max(j, 1));

            //Bitmap.Dispose();
            GC.Collect(0, GCCollectionMode.Optimized);
            return Color.FromArgb(global_r, global_g, global_b);
        }

        /// <summary>
        /// Метод извеления изображения
        /// </summary>
        /// <returns>Ссылка на изображение</returns>
        public Bitmap GetImage()
        {
            return new Bitmap(OutputImage);
        }

        /// <summary>
        /// Метод освобождения ресурсов
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                _middleLayer.Dispose();
                OutputImage.Dispose();
                rnd = null;

                disposedValue = true;
            }
        }

        /// <summary>
        /// Деструктор
        /// </summary>
        ~ImageConstructorService()
        {
            Dispose(disposing: false);
        }

        /// <summary>
        /// Главный метод освобождения ресурсов
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
