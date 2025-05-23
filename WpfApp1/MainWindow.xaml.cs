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
        /// <summary>
        /// Конструктор главного окна MainWindow.
        /// Инициализирует компоненты и подписывается на обновление доступности курсов.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            CourseAvailabilityManager.CoursesAvailabilityUpdated += OnCoursesAvailabilityUpdated;
            this.Closed += (s, e) => CourseAvailabilityManager.CoursesAvailabilityUpdated -= OnCoursesAvailabilityUpdated;
        }
        /// <summary>
        /// Обработчик события обновления доступности курсов.
        /// Показывает уведомление пользователю о возможных изменениях.
        /// </summary>
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
        /// <summary>
        /// Копирует свойства текущего окна (размер, положение, состояние) в новое окно.
        /// </summary>
        /// <param name="nextWindow">Окно, к которому применяются свойства</param>
        private void WindowProperties(Window nextWindow)
        {
            nextWindow.Left = this.Left;
            nextWindow.Top = this.Top;
            nextWindow.Height = this.Height;
            nextWindow.Width = this.Width;
            nextWindow.WindowState = this.WindowState;
        }
        /// <summary>
        /// Обработчик кнопки перехода к окну выбора роли (регистрация).
        /// Открывает новое окно и закрывает текущее.
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
        private void btnReg_Click(object sender, RoutedEventArgs e)
        {
            Window registr = new Role();
            WindowProperties(registr);
            registr.Show();
            this.Close();
        }
        /// <summary>
        /// Обработчик кнопки возвращения на главное окно.
        /// Перезапускает MainWindow.
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
        private void btnMain_Click(object sender, RoutedEventArgs e)
        {
            Window mainWindow = new MainWindow();
            WindowProperties(mainWindow);
            mainWindow.Show();
            this.Close();
        }
        /// <summary>
        /// Обработчик кнопки перехода в окно поддержки.
        /// Открывает окно Support с параметрами по умолчанию.
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
        private void btnSupport_Click(object sender, RoutedEventArgs e)
        {
            Window support = new Support(false, -1);
            WindowProperties(support);
            support.Show();
            this.Close();
        }
        /// <summary>
        /// Обработчик кнопки перехода к часто задаваемым вопросам (FAQ).
        /// Открывает окно FAQs.
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
        private void btnFAQs_Click(object sender, RoutedEventArgs e)
        {
            Window faqs = new FAQs(false, -1);
            WindowProperties(faqs);
            faqs.Show();
            this.Close();
        }
        /// <summary>
        /// Обработчик кнопки восстановления доступа.
        /// Открывает окно PasswordRecovery.
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
        private void btnAccessRecovery_Click(object sender, RoutedEventArgs e)
        {
            Window recovery = new PasswordRecovery(false, -1);
            WindowProperties(recovery);
            recovery.Show();
            this.Close();
        }
        /// <summary>
        /// Обработчик кнопки показа/скрытия пароля.
        /// Переключает между PasswordBox и TextBox, отображая или скрывая пароль.
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
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
        /// <summary>
        /// Обработчик кнопки входа в систему.
        /// Проверяет введенные логин и пароль, выполняет вход как администратор или пользователь.
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
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
        /// <summary>
        /// Обработчик кнопки "Личный кабинет".
        /// Показывает сообщение об ошибке, если пользователь не авторизован.
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
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