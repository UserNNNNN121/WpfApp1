using System;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1
{
    public partial class Role : Window
    {
        private bool _role; 

        public Role()
        {
            InitializeComponent();
            _role = false;

            btnEngineer.Click += RoleButton_Click;
            btnTechologist.Click += RoleButton_Click;
            btnLawyer.Click += RoleButton_Click;
            btnFinancist.Click += RoleButton_Click;
            btnITspec.Click += RoleButton_Click;
            btnAccountist.Click += RoleButton_Click;
        }

        private void RoleButton_Click(object sender, RoutedEventArgs e)
        {
            Button clickedButton = (Button)sender;
            int speciality = 7;

            if (clickedButton == btnEngineer)
                speciality = 4;
            else if (clickedButton == btnTechologist)
                speciality = 6;
            else if (clickedButton == btnLawyer)
                speciality = 5;
            else if (clickedButton == btnFinancist)
                speciality = 3;
            else if (clickedButton == btnITspec)
                speciality = 1;
            else if (clickedButton == btnAccountist)
                speciality = 2;

            Window registration = new Registration(_role, speciality);
            WindowProperties(registration);
            registration.Show();
            this.Close();
        }
        private void WindowProperties(Window nextWindow)
        {
            nextWindow.Left = this.Left;
            nextWindow.Top = this.Top;
            nextWindow.Height = this.Height;
            nextWindow.Width = this.Width;
            nextWindow.WindowState = this.WindowState;
        }
        private void btnMain_Click(object sender, RoutedEventArgs e)
        {
            Window mainWindow = new MainWindow();
            WindowProperties(mainWindow);
            mainWindow.Show();
            this.Close();
        }

        private void btnSupport_Click(object sender, RoutedEventArgs e)
        {
            Window support = new Support(false, -1);
            WindowProperties(support);
            support.Show();
            this.Close();
        }

        private void btnFAQs_Click(object sender, RoutedEventArgs e)
        {
            Window faqs = new FAQs(false, -1);
            WindowProperties(faqs);
            faqs.Show();
            this.Close();
        }
        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            Window mainWindow = new MainWindow();
            WindowProperties(mainWindow);
            mainWindow.Show();
            this.Close();
        }

        private void btnAccount_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
            "Ошибка: сначала зарегистрируйтесь или войдите в аккаунт",
            "Ошибка доступа",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
            return;
        }
    }
}