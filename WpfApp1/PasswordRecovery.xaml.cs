using System;
using System.Net;
using System.Net.Mail;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System.Data.SQLite;
using System.IO;
using System.Text.RegularExpressions;
using static WpfApp1.AdminControl;

namespace WpfApp1
{
    public partial class PasswordRecovery : Window
    {
        private string _generatedCode;
        private string _userEmail;
        private Stopwatch _codeResendTimer;
        private const int ResendDelaySeconds = 60;
        private static readonly string ConnectionString =
            $"Data Source={System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "educatingsystem.sl3")};Version=3;";

        private int _foundUserId = -1;
        private bool _isAdmin;
        public int _userId;
        public bool _isAuthorized = true;
        private string _userEmailForSending = "";

        public PasswordRecovery(bool isAdmin, int userId, string userEmail)
        {
            InitializeComponent();
            this._userId = userId;
            this._isAdmin = isAdmin;
            boxLog.Text = userEmail;
            boxLog.IsEnabled = false;
            boxCode.IsEnabled = false;
            if (userId == -1) { btnExit.Visibility = Visibility.Collapsed; _isAuthorized = false; }
            else if (!_isAdmin)
            {
                btnAdmin.Visibility = Visibility.Collapsed;
                btnSupport.Visibility = Visibility.Visible;
                btnMain.Visibility = Visibility.Visible;
                btnExit.Visibility = Visibility.Visible;
            }
            else
            {
                btnMain.Visibility = Visibility.Collapsed;
                btnAdmin.Visibility = Visibility.Visible;
                btnSupport.Visibility = Visibility.Collapsed;
                btnExit.Visibility = Visibility.Visible;
            }
            _codeResendTimer = new Stopwatch();
            UpdateSendCodeButtonState();
        }

        public PasswordRecovery(bool isAdmin, int userId) : this(isAdmin, userId, "")
        {
            if (userId != -1)
            {
                // Получаем email из соответствующей таблицы
                string query = isAdmin
                    ? "SELECT email FROM administrators WHERE id = @id"
                    : "SELECT mail FROM users WHERE id = @id";

                try
                {
                    using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
                    {
                        connection.Open();
                        using (SQLiteCommand command = new SQLiteCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@id", userId);
                            var result = command.ExecuteScalar();
                            if (result != null)
                            {
                                boxLog.Text = result.ToString();
                                _userEmailForSending = result.ToString();
                                boxLog.IsEnabled = false;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при получении email: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    boxLog.IsEnabled = true;
                }
            }
            else
            {
                boxLog.IsEnabled = true;
            }
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
            if (_isAuthorized)
            {
                Main main = new Main(_userId, _isAdmin);
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

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            WindowProperties(mainWindow);
            mainWindow.Show();
            this.Close();
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

            string userInput = boxLog.Text.Trim();

            if (string.IsNullOrWhiteSpace(userInput))
            {
                MessageBox.Show("Пожалуйста, введите email или логин", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!FindUserInDatabase(userInput))
            {
                MessageBox.Show("Указанный email или логин не найден в системе", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(_userEmailForSending))
            {
                MessageBox.Show("Не удалось определить email для отправки кода", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                _generatedCode = GenerateRandomCode(6);
                SendEmail(_userEmailForSending, "Код восстановления пароля",
                    $"Ваш код для восстановления пароля: {_generatedCode}");
                MessageBox.Show("Код отправлен на вашу электронную почту", "Успешно",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                boxCode.IsEnabled = true;
                _codeResendTimer.Restart();
                UpdateSendCodeButtonState();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отправке кода: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool FindUserInDatabase(string emailOrLogin)
        {
            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    string query = "SELECT id, mail FROM users WHERE mail = @input OR login = @input";
                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@input", emailOrLogin);
                        using (SQLiteDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                _foundUserId = reader.GetInt32(0);
                                _userEmailForSending = reader.GetString(1);
                                _isAdmin = false;
                                return true;
                            }
                        }
                    }

                    query = "SELECT id, email FROM administrators WHERE email = @input OR login = @input";
                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@input", emailOrLogin);
                        using (SQLiteDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                _foundUserId = reader.GetInt32(0);
                                _userEmailForSending = reader.GetString(1);
                                _isAdmin = true;
                                return true;
                            }
                        }
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при проверке в базе данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
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
                        btnSendCode.Content = "Отправить код";
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
                btnSendCode.Content = "Отправить код";
                btnSendCode.IsEnabled = true;
            }
        }

        private void btnRestore_Click(object sender, RoutedEventArgs e)
        {
            string enteredCode = boxCode.Text.Trim();

            if (string.IsNullOrWhiteSpace(enteredCode))
            {
                MessageBox.Show("Пожалуйста, введите код", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (enteredCode == _generatedCode)
            {
                MessageBox.Show("Код верный! Теперь вы можете установить новый пароль.", "Успешно",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                Window newpassword = new NewPassword(_isAdmin, _foundUserId, _isAuthorized);
                WindowProperties(newpassword);
                newpassword.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Неверный код. Пожалуйста, проверьте и попробуйте снова.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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
                if (!IsValidEmail(toAddress))
                {
                    throw new ArgumentException("Некорректный формат email адреса");
                }

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
                MessageBox.Show($"Ошибка при отправке email: {ex.Message}", "Ошибка SMTP", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

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
                Window account = new Personal_Account(_isAdmin, _userId);
                WindowProperties(account);
                account.Show();
                this.Close();
            }
        }
        private void btnAdmin_Click(object sender, RoutedEventArgs e)
        {
            Window admin = new AdminControl(_isAdmin, _userId);
            WindowProperties(admin);
            admin.Show();
            this.Close();
        }
        private void btnFAQs_Click(object sender, RoutedEventArgs e)
        {
            Window faqs = new FAQs(_isAdmin, _userId);
            WindowProperties(faqs);
            faqs.Show();
            this.Close();
        }

        private void btnSupport_Click(object sender, RoutedEventArgs e)
        {
            Window support = new Support(_isAdmin, _userId);
            WindowProperties(support);
            support.Show();
            this.Close();
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            WindowProperties(mainWindow);
            mainWindow.Show();
            this.Close();
        }
    }
}