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
        public bool isAdminRegistration;
        public int speciality;
        private string _generatedCode;
        private Stopwatch _codeResendTimer;
        private const int ResendDelaySeconds = 60;

        public Registration(bool role, int speciality)
        {
            InitializeComponent();
            isAdminRegistration = role;
            _codeResendTimer = new Stopwatch();
            UpdateSendCodeButtonState();
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

                Window role = new Role();
                WindowProperties(role);
                role.Show();
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
                int newId;
                bool isAdmin = isAdminRegistration;

                if (isAdmin)
                {
                    newId = RegisterAdministrator(login, name, password, speciality, email);
                    MessageBox.Show($"Регистрация администратора прошла успешно! ID: {newId}", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    newId = RegisterUser(login, name, password, email);
                    MessageBox.Show($"Регистрация пользователя прошла успешно! ID: {newId}", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }

                Main main = new Main(newId, isAdmin);
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

        private int RegisterAdministrator(string login, string name, string password, int speciality, string email)
        {
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                // Check for existing login or email in both 'administrators' and 'users' tables
                string checkQuery = @"
            SELECT COUNT(*) 
            FROM administrators 
            WHERE login = @login OR mail = @email
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
                        long adminCount = reader.GetInt64(0); // First result (from administrators table)
                        reader.Read();
                        long userCount = reader.GetInt64(0);  // Second result (from users table)

                        if (adminCount > 0 || userCount > 0)
                        {
                            throw new Exception("Логин или email уже существует в системе!");
                        }
                    }
                }

                // Insert into administrators table
                string query = @"
            INSERT INTO administrators (login, password, name, confirmation, mail) 
            VALUES (@login, @password, @name, 0, @email);
            SELECT last_insert_rowid();";

                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@login", login);
                    command.Parameters.AddWithValue("@password", password);
                    command.Parameters.AddWithValue("@name", name);
                    command.Parameters.AddWithValue("@email", email);

                    int newAdminId = Convert.ToInt32(command.ExecuteScalar());
                    return newAdminId;
                }
            }
        }

        private int RegisterUser(string login, string name, string password, string email)
        {
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                // Check for existing login or email in both 'users' and 'administrators' tables
                string checkQuery = @"
            SELECT COUNT(*) 
            FROM administrators 
            WHERE login = @login OR mail = @email
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
                        long adminCount = reader.GetInt64(0); // First result (from administrators table)
                        reader.Read();
                        long userCount = reader.GetInt64(0);  // Second result (from users table)

                        if (adminCount > 0 || userCount > 0)
                        {
                            throw new Exception("Логин или email уже существует в системе!");
                        }
                    }
                }

                // Insert into users table
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