using System;
using System.Data.SQLite;
using System.Windows;
using System.Windows.Controls;
using System.Net;
using System.Net.Mail;
using System.Diagnostics;
using System.Data;

namespace WpfApp1
{
    public partial class Registration : Window
    {
        private static readonly string connectionString =
            $"Data Source={System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "educatingsystem.sl3")};Version=3;";
        public int speciality;
        private string _generatedCode;
        private Stopwatch _codeResendTimer;
        private const int ResendDelaySeconds = 60;
        /// <summary>
        /// Конструктор окна регистрации.
        /// Принимает идентификатор специальности, инициализирует компоненты и таймер отправки кода.
        /// </summary>
        /// <param name="speciality">Идентификатор специальности</param>
        public Registration(int speciality)
        {
            InitializeComponent();
            this.speciality = speciality;
            _codeResendTimer = new Stopwatch();
            UpdateSendCodeButtonState();
        }
        /// <summary>
        /// Обработчик кнопки перехода на главное окно.
        /// Открывает MainWindow и закрывает текущее.
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
        /// Обработчик кнопки поддержки.
        /// Открывает окно поддержки и закрывает текущее.
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
        /// Обработчик кнопки вопросов и ответов.
        /// Открывает окно FAQ и закрывает текущее.
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
        /// Обработчик кнопки "Назад".
        /// Возвращает на окно выбора роли.
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            Window role = new Role();
            WindowProperties(role);
            role.Show();
            this.Close();
        }
        /// <summary>
        /// Применяет свойства текущего окна к следующему (размер, позиция, состояние).
        /// </summary>
        /// <param name="nextWindow">Окно, которому применяются свойства</param>
        private void WindowProperties(Window nextWindow)
        {
            nextWindow.Left = this.Left;
            nextWindow.Top = this.Top;
            nextWindow.Height = this.Height;
            nextWindow.Width = this.Width;
            nextWindow.WindowState = this.WindowState;
        }
        /// <summary>
        /// Обработчик кнопки "Отправить код".
        /// Валидирует email, генерирует код и отправляет его на почту пользователя.
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
        private void btnSendCode_Click(object sender, RoutedEventArgs e)
        {
            if (_codeResendTimer.IsRunning && _codeResendTimer.Elapsed.TotalSeconds < ResendDelaySeconds)
            {
                int secondsLeft = ResendDelaySeconds - (int)_codeResendTimer.Elapsed.TotalSeconds;
                MessageBox.Show($"Пожалуйста, подождите {secondsLeft} секунд перед повторной отправкой кода.",
                    "Ожидание", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string email = boxEmail.Text.Trim();

            if (string.IsNullOrWhiteSpace(email))
            {
                MessageBox.Show("Пожалуйста, введите email", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _generatedCode = GenerateRandomCode(6);

                SendEmail(email, "Код подтверждения регистрации",
                    $"Ваш код для подтверждения регистрации: {_generatedCode}");

                MessageBox.Show("Код подтверждения отправлен на вашу электронную почту", "Успешно",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                _codeResendTimer.Restart();
                UpdateSendCodeButtonState();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отправке кода: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        /// <summary>
        /// Обновляет состояние кнопки "Отправить код".
        /// Блокирует повторную отправку на заданное время.
        /// </summary>
        private void UpdateSendCodeButtonState()
        {
            if (_codeResendTimer.IsRunning && _codeResendTimer.Elapsed.TotalSeconds < ResendDelaySeconds)
            {
                int secondsLeft = ResendDelaySeconds - (int)_codeResendTimer.Elapsed.TotalSeconds;
                btnSendCode.Content = $"Отправить код ({secondsLeft} сек)";
                btnSendCode.IsEnabled = false;

                var timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(1);
                timer.Tick += (s, args) =>
                {
                    if (_codeResendTimer.Elapsed.TotalSeconds >= ResendDelaySeconds)
                    {
                        btnSendCode.Content = "Отправить код подтверждения";
                        btnSendCode.IsEnabled = true;
                        timer.Stop();
                    }
                    else
                    {
                        secondsLeft = ResendDelaySeconds - (int)_codeResendTimer.Elapsed.TotalSeconds;
                        btnSendCode.Content = $"Отправить код ({secondsLeft} сек)";
                    }
                };
                timer.Start();
            }
            else
            {
                btnSendCode.Content = "Отправить код подтверждения";
                btnSendCode.IsEnabled = true;
            }
        }
        /// <summary>
        /// Генерирует случайный цифровой код заданной длины.
        /// </summary>
        /// <param name="length">Длина кода</param>
        /// <returns>Сгенерированный код</returns>
        private string GenerateRandomCode(int length)
        {
            Random random = new Random();
            const string chars = "0123456789";
            char[] code = new char[length];

            for (int i = 0; i < length; i++)
            {
                code[i] = chars[random.Next(chars.Length)];
            }

            return new string(code);
        }

        private void SendEmail(string toAddress, string subject, string body)
        {
            try
            {
                string smtpServer = "smtp.mail.ru";
                int smtpPort = 587;
                string smtpUsername = "drm.k@bk.ru";
                string smtpPassword = "1Bgue1Az5MeiKW4PRZnN";

                using (SmtpClient smtpClient = new SmtpClient(smtpServer, smtpPort))
                {
                    smtpClient.EnableSsl = true;
                    smtpClient.UseDefaultCredentials = false;
                    smtpClient.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                    smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtpClient.EnableSsl = true;
                    MailMessage mailMessage = new MailMessage();
                    mailMessage.From = new MailAddress(smtpUsername);
                    mailMessage.To.Add(toAddress);
                    mailMessage.Subject = subject;
                    mailMessage.Body = body;

                    smtpClient.Send(mailMessage);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка SMTP", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }
        /// <summary>
        /// Обработчик кнопки "Зарегистрироваться".
        /// Проверяет введённые данные, валидирует код, пароли и регистрирует пользователя.
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
        private void btnAccept_Click(object sender, RoutedEventArgs e)
        {
            string login = boxLog.Text.Trim();
            string name = boxName.Text.Trim();
            string email = boxEmail.Text.Trim();
            string verificationCode = boxVerificationCode.Text.Trim();
            string password = boxPassw.Password;
            string repeatPassword = boxReppassw.Password;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(name) ||
                string.IsNullOrEmpty(email) || string.IsNullOrEmpty(verificationCode) ||
                string.IsNullOrEmpty(password) || string.IsNullOrEmpty(repeatPassword))
            {
                MessageBox.Show("Все поля должны быть заполнены!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (verificationCode != _generatedCode)
            {
                MessageBox.Show("Неверный код подтверждения!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (password != repeatPassword)
            {
                MessageBox.Show("Пароли не совпадают!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (password.Length < 8)
            {
                MessageBox.Show("Пароль должен содержать минимум 8 символов!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                int newId = RegisterUser(login, name, password, email);
                MessageBox.Show($"Регистрация пользователя прошла успешно! ID: {newId}", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                Main main = new Main(newId, false);
                WindowProperties(main);
                main.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при регистрации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        /// <summary>
        /// Регистрирует нового пользователя в базе данных.
        /// Проверяет уникальность логина и email.
        /// </summary>
        /// <param name="login">Логин</param>
        /// <param name="name">Имя</param>
        /// <param name="password">Пароль</param>
        /// <param name="email">Email</param>
        /// <returns>Идентификатор нового пользователя</returns>
        private int RegisterUser(string login, string name, string password, string email)
        {
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string checkQuery = @"
            SELECT COUNT(*) 
            FROM administrators 
            WHERE login = @login OR email = @email
            UNION ALL
            SELECT COUNT(*) 
            FROM users 
            WHERE login = @login OR mail = @email";

                using (SQLiteCommand checkCommand = new SQLiteCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@login", login);
                    checkCommand.Parameters.AddWithValue("@email", email);
                    using (SQLiteDataReader reader = checkCommand.ExecuteReader())
                    {
                        reader.Read();
                        long adminCount = reader.GetInt64(0);
                        reader.Read();
                        long userCount = reader.GetInt64(0);  

                        if (adminCount > 0 || userCount > 0)
                        {
                            throw new Exception("Логин или email уже существует в системе!");
                        }
                    }
                }

                string query = @"
            INSERT INTO users (login, password, name, speciality, confirmation, mail) 
            VALUES (@login, @password, @name, @speciality, 0, @email);
            SELECT last_insert_rowid();";

                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@login", login);
                    command.Parameters.AddWithValue("@password", password);
                    command.Parameters.AddWithValue("@name", name);
                    command.Parameters.AddWithValue("@speciality", speciality);
                    command.Parameters.AddWithValue("@email", email);

                    int newUserId = Convert.ToInt32(command.ExecuteScalar());
                    return newUserId;
                }
            }
        }
        /// <summary>
        /// Обработчик кнопки "Аккаунт".
        /// Показывает сообщение об ошибке при попытке входа без регистрации.
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