
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace WpfApp1
{
    public partial class CertificateSettingsWindow : Window
    {
        private readonly int _courseId;
        public byte[] TemplatePdf { get; private set; }
        public byte[] SealImage { get; private set; }
        public byte[] SignatureImage { get; private set; }
        public bool EnableCertificate => chkEnableCertificate.IsChecked ?? false;
        /// <summary>
        /// Конструктор окна настроек сертификата.
        /// Принимает идентификатор курса и загружает существующие настройки сертификата.
        /// </summary>
        /// <param name="courseId">Идентификатор курса</param>
        public CertificateSettingsWindow(int courseId)
        {
            InitializeComponent();
            _courseId = courseId;
            LoadSettings();
        }
        /// <summary>
        /// Загружает сохранённые настройки сертификата из генератора.
        /// Если изображения печати и подписи существуют — отображает их в интерфейсе.
        /// </summary>
        private void LoadSettings()
        {
            var generator = new CertificateGenerator();
            var settings = generator.LoadCertificateSettings(_courseId);

            if (settings != null)
            {
                if (settings.SealImage != null)
                {
                    SealImage = settings.SealImage;
                    var sealBitmap = new BitmapImage();
                    sealBitmap.BeginInit();
                    sealBitmap.StreamSource = new MemoryStream(SealImage);
                    sealBitmap.EndInit();
                    imgSeal.Source = sealBitmap;
                }

                if (settings.SignatureImage != null)
                {
                    SignatureImage = settings.SignatureImage;
                    var signatureBitmap = new BitmapImage();
                    signatureBitmap.BeginInit();
                    signatureBitmap.StreamSource = new MemoryStream(SignatureImage);
                    signatureBitmap.EndInit();
                    imgSignature.Source = signatureBitmap;
                }
            }
        }

        /// <summary>
        /// Обработчик кнопки выбора шаблона сертификата.
        /// Открывает диалог выбора PDF-файла и сохраняет его в TemplatePdf.
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
        private void BtnSelectTemplate_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new OpenFileDialog
            {
                Filter = "PDF files (*.pdf)|*.pdf",
                Title = "Выберите шаблон сертификата (PDF)"
            };

            if (openDialog.ShowDialog() == true)
            {
                TemplatePdf = File.ReadAllBytes(openDialog.FileName);
                txtTemplatePath.Text = openDialog.FileName;
            }
        }
        /// <summary>
        /// Обработчик кнопки добавления изображения печати.
        /// Загружает PNG-изображение и отображает его в интерфейсе.
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
        private void BtnAddSeal_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new OpenFileDialog
            {
                Filter = "Изображения (*.png)|*.png",
                Title = "Выберите изображение печати"
            };

            if (openDialog.ShowDialog() == true)
            {
                SealImage = File.ReadAllBytes(openDialog.FileName);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = new MemoryStream(SealImage);
                bitmap.EndInit();
                imgSeal.Source = bitmap;
            }
        }
        /// <summary>
        /// Обработчик кнопки добавления изображения подписи.
        /// Загружает PNG-изображение и отображает его в интерфейсе.
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
        private void BtnAddSignature_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new OpenFileDialog
            {
                Filter = "Изображения (*.png)|*.png",
                Title = "Выберите изображение подписи"
            };

            if (openDialog.ShowDialog() == true)
            {
                SignatureImage = File.ReadAllBytes(openDialog.FileName);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = new MemoryStream(SignatureImage);
                bitmap.EndInit();
                imgSignature.Source = bitmap;
            }
        }
        /// <summary>
        /// Обработчик кнопки сохранения.
        /// Проверяет наличие PDF-шаблона, сохраняет настройки сертификата и закрывает окно с положительным результатом.
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (TemplatePdf == null)
            {
                MessageBox.Show("Необходимо выбрать PDF шаблон");
                return;
            }

            var generator = new CertificateGenerator();
            generator.SaveCertificateSettings(
                _courseId,
                TemplatePdf,
                SealImage,
                SignatureImage);

            DialogResult = true;
            Close();
        }
        /// <summary>
        /// Обработчик кнопки отмены.
        /// Закрывает окно без сохранения изменений.
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}