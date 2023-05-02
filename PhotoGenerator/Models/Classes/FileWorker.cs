using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;

namespace PhotoGenerator.Models.Classes
{
    /// <summary>
    /// Класс для работы с файлами
    /// </summary>
    public class FileWorker : IDisposable
    {
        private bool disposedValue;

        /// <summary>
        /// База изображений
        /// </summary>
        public List<FileImage> DataImages { get; set; } = new List<FileImage>();

        /// <summary>
        /// Список путей к изображений
        /// </summary>
        public List<string> FilePathes { get; set; } = new List<string>();

        /// <summary>
        /// Путь к базе изображений
        /// </summary>
        public string CollectionPath { get; set; } = Application.ResourceAssembly.Location.Replace("PhotoGenerator.dll", "") + "dbImages";

        /// <summary>
        /// Конвейерное определение пути
        /// </summary>
        /// <param name="path">Путь</param>
        /// <returns></returns>
        public FileWorker SetCollectionPath(string path)
        {
            this.CollectionPath = string.IsNullOrEmpty(path) ? CollectionPath : path;
            return this;
        }

        /// <summary>
        /// Подгрузка изображений
        /// </summary>
        public FileWorker FileLoader(Action action)
        {
            if (FilePathes.Count == 0)
            {
                FilePathes.AddRange(Directory.GetFiles(CollectionPath));//"D:\\Downloads\\dataBase\\VG_100K_2"));// 
                PhotoAnalizer(FilePathes);
            }
            action?.Invoke();
            return this;
        }

        /// <summary>
        /// Получение набора изображений с минимальной разницей цветов
        /// </summary>
        /// <param name="labColor">Целевой цвет</param>
        /// <returns></returns>
        public List<FileImage> GetMinDistance((float, float, float) labColor)
        {
            List<FileImage> result = new List<FileImage>();
            List<FileImage> resultAlternative = new List<FileImage>();

            result = DataImages.OrderBy(x => StaticData.GetDistance(x.LABColor, labColor)).Take(10).ToList();

            return result.Count == 0 ? resultAlternative : result;
        }

        /// <summary>
        /// Анализ базы изображений
        /// </summary>
        /// <param name="filePathes"></param>
        private void PhotoAnalizer(List<string> filePathes)
        {
            for (int i = DataImages.Count; i < Math.Min(filePathes.Count, 1000); i++)
            {
                try
                {
                    using (var imgSmall = new Bitmap(Image.FromFile(filePathes[i]), 16, 16))
                    {
                        var meanColor = StaticData.MeanColor(imgSmall);
                        var meanLABColor = StaticData.RGBToLab(meanColor);
                        DataImages.Add(new FileImage(filePathes[i], meanColor, meanLABColor));
                    }
                }
                catch (Exception e)
                {
                    // TODO: Добавить вывод на лог
                    continue;
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    DataImages.Clear();
                    FilePathes.Clear();
                }
                disposedValue = true;
            }
        }

        ~FileWorker()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
