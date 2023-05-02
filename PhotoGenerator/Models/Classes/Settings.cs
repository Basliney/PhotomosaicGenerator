using System;
using System.Linq;

namespace PhotoGenerator.Models.Classes
{
    /// <summary>
    /// Класс настроек для работы с приложением
    /// </summary>
    public class Settings
    {
        /// <summary>
        /// Точность подбора изображения
        /// </summary>
        public float Density { get; private set; } = 2.3f;

        /// <summary>
        /// Минимальная ширина подставляемого изображения
        /// </summary>
        public int WidthMax { get; private set; } = 183;

        /// <summary>
        /// Разрешение выходного изображения
        /// </summary>
        public int Resolution { get; private set; } = 732;

        /// <summary>
        /// Поле готовности базы изображений
        /// </summary>
        public bool DataBaseReady { get; private set; }

        /// <summary>
        /// Обработчик файлов
        /// </summary>
        public FileWorker FileWorker { get; private set; }

        /// <summary>
        /// Количество изображений
        /// </summary>
        public int FileImagesCount
        {
            get
            {
                return (int)FileWorker?.DataImages.Count();
            }
        }

        /// <summary>
        /// Событие готовности базы изображений
        /// </summary>
        public Action OnDataBaseReady;

        /// <summary>
        /// Конструктор
        /// </summary>
        public Settings()
        {
            OnDataBaseReady += OnDataBaseReadyHandler;
            FileWorker = new FileWorker();
        }

        /// <summary>
        /// Запуск анализа базы изображений
        /// </summary>
        public void AnalyzeDB()
        {
            FileWorker = CreateFileWorker().SetCollectionPath("").FileLoader(OnDataBaseReady);
        }

        /// <summary>
        /// Запуск анализа базы изображений по пути
        /// </summary>
        /// <param name="path">Путь</param>
        public void AnalyzeDB(string path)
        {
            FileWorker.SetCollectionPath(path).FileLoader(OnDataBaseReady);
        }

        /// <summary>
        /// Обработка события готовности базы изображений
        /// </summary>
        public void OnDataBaseReadyHandler()
        {
            this.DataBaseReady = true;
        }

        /// <summary>
        /// Создание файлового помощника
        /// </summary>
        /// <param name="path">Путь к коллекции изображений</param>
        /// <returns></returns>
        public FileWorker CreateFileWorker()
        {
            return new FileWorker();
        }
    }
}
