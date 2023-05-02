using PhotoGenerator.Models.Classes;
using PhotoGenerator.Services;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Image = System.Drawing.Image;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace PhotoGenerator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ImageConstructorService ics;

        private Settings Settings { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            AddPhoto_btn.IsEnabled = false;
            Info_lbl.Text = "Анализ стандартной базы изображений";

            //Параллельно запускаем анализ дефолтной базы изображений
            StartAnalyzDatabase();

            AddPhoto_btn.Width = AddSource_btn.Width;
        }

        private void StartAnalyzDatabase()
        {
            var outerTask = Task.Factory.StartNew(() =>
            {
                Settings = new Settings();
                Settings.OnDataBaseReady += OnDatabaseReadyHandler;
                Settings.AnalyzeDB();
            });
        }

        private void OnDatabaseReadyHandler()
        {
            Settings.OnDataBaseReady -= OnDatabaseReadyHandler;
            Dispatcher.BeginInvoke(DispatcherPriority.Normal,
            (ThreadStart)delegate ()
            {
                AddPhoto_btn.IsEnabled = true;
                Info_lbl.Text = "Стандартная база изображений подгружена. Выберите изображение для начала генерации";
            });
        }

        private void AddSource_btn_Loaded(object sender, EventArgs e)
        {
            AddPhoto_btn.Width = AddSource_btn.Width;
        }

        private void AddPhoto_btn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = false;
            ofd.Title = "Выбрать изображение";
            ofd.InitialDirectory = @"C:\";
            ofd.Filter = "Файлы изображений (*.bmp;*.png;*.jpg)|*.bmp;*.png;*.jpg";
            Bitmap bitmap;

            if (ofd.ShowDialog() != true) { return; }

            inputedImage_img.Source = new BitmapImage(new Uri(ofd.FileName));
            bitmap = new Bitmap(Image.FromFile(ofd.FileName));

            ics = new ImageConstructorService(Settings);

            Info_lbl.Text = "Изображение загружено. Пошла жара";
            var outer1 = Task.Factory.StartNew(() =>      // внешняя задача
            {
                ics.OnReadyImage += Ics_OnReadyImage;
                ics.StartGeneration(bitmap).Wait();
                ics.OnReadyImage -= Ics_OnReadyImage;
            });
        }

        private void Ics_OnReadyImage(Bitmap bmp, int countOfCut)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal,
            (ThreadStart)delegate ()
            {
                inputedImage_img.Source = ImageSourceFromBitmap(bmp);
                Info_lbl.Text = $"Количество разбиений: {countOfCut}";
            });
        }

        public void ChangeCountOfCut(int countOfPieces)
        {
            Info_lbl.Text = countOfPieces.ToString();
        }

        public ImageSource ImageSourceFromBitmap(Bitmap bmp)
        {
            var handle = bmp.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally { GC.Collect(); }
        }

        private void AddSource_btn_Click(object sender, RoutedEventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string[] files = Directory.GetFiles(fbd.SelectedPath);
                    var selectOnlyPhotos = files.Where(x => x.EndsWith("jpeg") || x.EndsWith("jpg") || x.EndsWith("png"));
                    if (selectOnlyPhotos.Count() < 400)
                    {
                        MessageBox.Show("Рекомендуемое количество картинок в коллекции - от 400 до 700. Иначе картинки будут чаще повторяться :(");
                    }
                    else if (selectOnlyPhotos.Count() > 700)
                    {
                        MessageBox.Show("Максимальное число картинок в коллекции - 700. Остальные будут игнорированы");
                    }
                    Info_lbl.Text = "Выбран источник фотографий";
                    Settings.FileWorker.SetCollectionPath(fbd.SelectedPath);
                }
            }
        }

        private void Grid_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if(e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
        
        private void Settings_btn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Close_btn_Click(object sender, RoutedEventArgs e)
        {
            App.Current.Shutdown();
        }

        private void About_btn_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
