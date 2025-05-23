
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

        public CertificateSettingsWindow(int courseId)
        {
            InitializeComponent();
            _courseId = courseId;
            LoadSettings();
        }

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

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}