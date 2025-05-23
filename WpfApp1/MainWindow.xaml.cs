using System;
using System.Data.SQLite;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.IO;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        private static readonly string ConnectionString =
            $"Data Source={System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "educatingsystem.sl3")};Version=3;";

        public MainWindow()
        {
            InitializeComponent();
            CourseAvailabilityManager.CoursesAvailabilityUpdated += OnCoursesAvailabilityUpdated;
            this.Closed += (s, e) => CourseAvailabilityManager.CoursesAvailabilityUpdated -= OnCoursesAvailabilityUpdated;
        }

        private void OnCoursesAvailabilityUpdated()
        {
            Dispatcher.Invoke(() =>
            {
                MessageBox.Show("Доступность курсов была обновлена. Возможно, некоторые курсы стали недоступны.",
                              "Обновление доступности",
                              MessageBoxButton.OK,
                              MessageBoxImage.Information);
            });
        }

        private void WindowProperties(Window nextWindow)
        {
            nextWindow.Left = this.Left;
            nextWindow.Top = this.Top;
            nextWindow.Height = this.Height;
            nextWindow.Width = this.Width;
            nextWindow.WindowState = this.WindowState;
        }

        private void btnReg_Click(object sender, RoutedEventArgs e)
        {
            Window registr = new Role();
            WindowProperties(registr);
            registr.Show();
            this.Close();
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

        private void btnAccessRecovery_Click(object sender, RoutedEventArgs e)
        {
            Window recovery = new PasswordRecovery(false, -1);
            WindowProperties(recovery);
            recovery.Show();
            this.Close();
        }

        private void btnTogglePassword_Click(object sender, RoutedEventArgs e)
        {
            if (passwBox.Visibility == Visibility.Visible)
            {
                boxPassword.Visibility = Visibility.Visible;
                passwBox.Visibility = Visibility.Collapsed;
                boxPassword.Text = passwBox.Password;
                imgPasswordIcon.Source = new BitmapImage(new Uri("/eye.png", UriKind.Relative));
            }
            else
            {
                passwBox.Visibility = Visibility.Visible;
                boxPassword.Visibility = Visibility.Collapsed;
                passwBox.Password = boxPassword.Text;
                imgPasswordIcon.Source = new BitmapImage(new Uri("/hideeye.png", UriKind.Relative));
            }
        }

        private void btnSignUp_Click(object sender, RoutedEventArgs e)
        {
            string login = boxLogin.Text;
            string password = passwBox.Visibility == Visibility.Visible ? passwBox.Password : boxPassword.Text;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Пожалуйста, заполните все поля.");
                return;
            }

            using (SQLiteConnection conn = new SQLiteConnection(ConnectionString))
            {
                try
                {
                    conn.Open();

                    string adminQuery = "SELECT id FROM administrators WHERE login = @login AND password = @password";
                    using (SQLiteCommand cmd = new SQLiteCommand(adminQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@login", login);
                        cmd.Parameters.AddWithValue("@password", password);

                        object adminId = cmd.ExecuteScalar();

                        if (adminId != null)
                        {
                            MessageBox.Show("Добро пожаловать, Администратор!");
                            Window admin = new AdminControl(true, Convert.ToInt32(adminId));
                            WindowProperties(admin);
                            admin.Show();
                            this.Close();
                            return;
                        }
                    }

                    string userQuery = "SELECT id FROM users WHERE login = @login AND password = @password";
                    using (SQLiteCommand cmd = new SQLiteCommand(userQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@login", login);
                        cmd.Parameters.AddWithValue("@password", password);

                        object userId = cmd.ExecuteScalar();

                        if (userId != null)
                        {
                            MessageBox.Show("Добро пожаловать!");
                            Window main = new Main(Convert.ToInt32(userId), false);
                            WindowProperties(main);
                            main.Show();
                            this.Close();
                        }
                        else
                        {
                            MessageBox.Show("Неверный логин или пароль.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка подключения к базе данных: {ex.Message}");
                }
            }
        }

        private void btnAccount_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Ошибка: сначала зарегистрируйтесь или войдите в аккаунт",
                "Ошибка доступа",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}