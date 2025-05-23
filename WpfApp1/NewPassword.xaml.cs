using System;
using System.Data.SQLite;
using System.Windows;
using static WpfApp1.AdminControl;
using System.IO;
namespace WpfApp1
{
    public partial class NewPassword : Window
    {
        public int _userId;
        public bool _isAdmin;
        public bool _isAuthorized;
        private static readonly string ConnectionString =
            $"Data Source={Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "educatingsystem.sl3")};Version=3;";
        /// <summary>
        /// Конструктор окна смены пароля.
        /// Устанавливает видимость кнопок в зависимости от роли и авторизации пользователя.
        /// </summary>
        /// <param name="isAdmin">Флаг администратора</param>
        /// <param name="userId">ID пользователя</param>
        /// <param name="isAuthorized">Флаг авторизации</param
        public NewPassword(bool isAdmin, int userId, bool isAuthorized)
        {
            InitializeComponent();
            this._userId = userId;
            this._isAdmin = isAdmin;
            this._isAuthorized = isAuthorized;
            if (!_isAuthorized)
            {
                btnAdmin.Visibility = Visibility.Collapsed;
                btnSupport.Visibility = Visibility.Visible;
                btnExit.Visibility = Visibility.Collapsed;
            }
            else
            {
                btnExit.Visibility = Visibility.Collapsed;
            }
            if (!_isAdmin)
            {
                btnAdmin.Visibility = Visibility.Collapsed;
                btnSupport.Visibility = Visibility.Visible;
                btnMain.Visibility = Visibility.Visible;
                btnSupport.Visibility = Visibility.Visible;
            }
            else
            {
                btnMain.Visibility = Visibility.Collapsed;
                btnSupport.Visibility = Visibility.Collapsed;
                btnAdmin.Visibility = Visibility.Visible;
                btnSupport.Visibility = Visibility.Collapsed;
            }
        }
        /// <summary>
        /// Обновляет пароль пользователя в базе данных SQLite.
        /// Определяет таблицу по роли пользователя (админ/обычный пользователь).
        /// </summary>
        /// <param name="userId">ID пользователя</param>
        /// <param name="newPassword">Новый пароль</param>
        private void UpdatePassword(int userId, string newPassword)
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();

                string tableName = _isAdmin ? "administrators" : "users";
                string passwordColumn = _isAdmin ? "password" : "password";

                var command = new SQLiteCommand(
                    $"UPDATE {tableName} SET {passwordColumn} = @newPassword WHERE id = @userId",
                    connection);

                command.Parameters.AddWithValue("@newPassword", newPassword);
                command.Parameters.AddWithValue("@userId", userId);
                command.ExecuteNonQuery();
            }
        }
        /// <summary>
        /// Обработчик события нажатия на кнопку "Восстановить".
        /// Проверяет совпадение паролей и минимальную длину, обновляет пароль и открывает личный кабинет.
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            string newPassword = boxPassw.Password;
            string repeatedPassword = boxReppassw.Password;

            if (newPassword != repeatedPassword)
            {
                MessageBox.Show("Пароли не совпадают. Пожалуйста, введите одинаковые пароли.",
                              "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (newPassword.Length < 8)
            {
                MessageBox.Show("Пароль должен содержать минимум 8 символов.",
                              "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                UpdatePassword(_userId, newPassword);
                MessageBox.Show("Пароль успешно изменен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                Personal_Account personalAccount = new Personal_Account(_isAdmin, _userId);
                WindowProperties(personalAccount);
                personalAccount.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при изменении пароля: {ex.Message}",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        /// <summary>
        /// Копирует размеры и позицию текущего окна на следующее.
        /// Используется для передачи UI-свойств между окнами.
        /// </summary>
        /// <param name="nextWindow">Окно, на которое переходят</param>
        private void WindowProperties(Window nextWindow)
        {
            nextWindow.Left = this.Left;
            nextWindow.Top = this.Top;
            nextWindow.Height = this.Height;
            nextWindow.Width = this.Width;
            nextWindow.WindowState = this.WindowState;
        }
        /// <summary>
        /// Обработчик нажатия кнопки "Главная".
        /// Открывает главное окно в зависимости от статуса авторизации.
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
        private void btnMain_Click(object sender, RoutedEventArgs e)
        {
            if(_isAuthorized)
            {
                Window main = new Main(_userId, _isAdmin);
                WindowProperties(main);
                main.Show();
                this.Close();
            }
            else
            {
                MainWindow mainWindow = new MainWindow();
                WindowProperties(mainWindow);
                mainWindow.Show();
                this.Close();
            }
        }
        /// <summary>
        /// Обработчик нажатия кнопки "Назад".
        /// Возвращает пользователя на окно входа.
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            WindowProperties(mainWindow);
            mainWindow.Show();
            this.Close();
        }
        /// <summary>
        /// Обработчик кнопки "Личный кабинет".
        /// Проверяет ID пользователя и открывает соответствующее окно.
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
        private void btnAccount_Click(object sender, RoutedEventArgs e)
        {
            if (_userId == -1)
            {
                MessageBox.Show(
                "Ошибка: сначала зарегистрируйтесь или войдите в аккаунт",
                "Ошибка доступа",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            }
            else
            {
                Window personalaccount = new Personal_Account(_isAdmin, _userId);
                WindowProperties(personalaccount);
                personalaccount.Show();
                this.Close();
            }
        }
        /// <summary>
        /// Обработчик кнопки "Админ панель".
        /// Открывает окно панели администратора.
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
        private void btnAdmin_Click(object sender, RoutedEventArgs e)
        {
            Window admin = new AdminControl(_isAdmin, _userId);
            WindowProperties(admin);
            admin.Show();
            this.Close();
        }
        /// <summary>
        /// Обработчик кнопки "Часто задаваемые вопросы".
        /// Открывает окно с разделом FAQ.
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
        private void btnFAQs_Click(object sender, RoutedEventArgs e)
        {
            Window faqs = new FAQs(_isAdmin, _userId);
            WindowProperties(faqs);
            faqs.Show();
            this.Close();
        }
        /// <summary>
        /// Обработчик кнопки "Поддержка".
        /// Открывает окно с информацией о службе поддержки.
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
        private void btnSupport_Click(object sender, RoutedEventArgs e)
        {
            Window support = new Support(_isAdmin, _userId);
            WindowProperties(support);
            support.Show();
            this.Close();
        }
        /// <summary>
        /// Обработчик кнопки "Выход".
        /// Возвращает пользователя на главное окно приложения.
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            WindowProperties(mainWindow);
            mainWindow.Show();
            this.Close();
        }


    }
}