using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace WpfApp1
{
    public partial class Support : Window
    {
        private static readonly string ConnectionString =
    $"Data Source={Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "educatingsystem.sl3")};Version=3;";
        public int _userId;
        public bool _isAdmin;
        private string _userEmail;
        private List<Attachment> _attachments = new List<Attachment>();

        public class Attachment
        {
            public string FilePath { get; set; }
            public string FileName { get; set; }
            public byte[] Data { get; set; }

            public string CompactName
            {
                get
                {
                    string name = Path.GetFileNameWithoutExtension(FileName);
                    string ext = Path.GetExtension(FileName);

                    if (name.Length > 8)
                        name = name.Substring(0, 6) + "..";

                    return $"{name}{ext}";
                }
            }
        }
        /// <summary>
        /// Конструктор окна поддержки. Инициализирует компоненты, назначает обработчики событий,
        /// загружает email пользователя, если он авторизован, и обновляет UI вложений.
        /// </summary>
        /// <param name="isAdmin">Флаг, указывающий, является ли пользователь администратором</param>
        /// <param name="userId">Идентификатор пользователя</param>
        public Support(bool isAdmin, int userId)
        {
            InitializeComponent();
            btnSend.Click += BtnSend_Click;
            btnAttach.Click += BtnAttach_Click;
            btnRemoveAllAttachments.Click += BtnRemoveAllAttachments_Click;

            this._userId = userId;
            this._isAdmin = isAdmin;

            if (_userId > 0)
            {
                chkAutoEmail.Visibility = Visibility.Visible;
                btnExit.Visibility = Visibility.Visible;
                LoadUserEmail();
                chkAutoEmail.Checked += ChkAutoEmail_Checked;
                chkAutoEmail.Unchecked += ChkAutoEmail_Unchecked;
            }
            else
            {
                chkAutoEmail.Visibility = Visibility.Collapsed;
                btnExit.Visibility = Visibility.Collapsed;
            }

            UpdateAttachmentsUI();
        }
        /// <summary>
        /// Обработчик события кнопки "Прикрепить".
        /// Открывает диалог выбора файлов, проверяет размер и количество,
        /// сохраняет содержимое файлов в список вложений.
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
        private void BtnAttach_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Supported files (*.jpg;*.jpeg;*.png;*.docx;*.pdf;*.txt)|*.jpg;*.jpeg;*.png;*.docx;*.pdf;*.txt",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                int remainingSlots = 5 - _attachments.Count;
                if (openFileDialog.FileNames.Length > remainingSlots)
                {
                    MessageBox.Show($"Вы можете прикрепить только {remainingSlots} дополнительных файлов (максимум 5).", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                foreach (var fileName in openFileDialog.FileNames)
                {
                    if (_attachments.Count >= 5) break;
                    var fileInfo = new FileInfo(fileName);
                    if (fileInfo.Length > 5 * 1024 * 1024) 
                    {
                        MessageBox.Show($"Файл {Path.GetFileName(fileName)} слишком большой (макс. 5MB)",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        continue;
                    }
                    _attachments.Add(new Attachment
                    {
                        FilePath = fileName,
                        FileName = Path.GetFileName(fileName),
                        Data = File.ReadAllBytes(fileName)
                    });
                }

                UpdateAttachmentsUI();
            }
        }
        /// <summary>
        /// Обработчик удаления одного вложения.
        /// Удаляет выбранное вложение из списка и обновляет интерфейс.
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
        private void BtnRemoveSingleAttachment_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Attachment attachment)
            {
                _attachments.Remove(attachment);
                UpdateAttachmentsUI();
            }
        }
        /// <summary>
        /// Обработчик кнопки "Удалить все вложения".
        /// Очищает список вложений и обновляет интерфейс.
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
        private void BtnRemoveAllAttachments_Click(object sender, RoutedEventArgs e)
        {
            _attachments.Clear();
            UpdateAttachmentsUI();
        }
        /// <summary>
        /// Обновляет интерфейс отображения вложений.
        /// Показывает или скрывает контейнер вложений и кнопку удаления.
        /// </summary>
        private void UpdateAttachmentsUI()
        {
            if (_attachments.Count > 0)
            {
                attachmentsContainer.ItemsSource = null;
                attachmentsContainer.ItemsSource = _attachments;
                attachmentsContainer.Visibility = Visibility.Visible;
                btnRemoveAllAttachments.Visibility = Visibility.Visible;

                var wrapPanel = attachmentsContainer.Template.FindName("itemsPanel", attachmentsContainer) as WrapPanel;
                if (wrapPanel != null)
                {
                    wrapPanel.Width = 800; 
                        wrapPanel.Height = 90;
                }
            }
            else
            {
                attachmentsContainer.Visibility = Visibility.Collapsed;
                btnRemoveAllAttachments.Visibility = Visibility.Collapsed;
            }
        }
        /// <summary>
        /// Загружает email пользователя из базы данных SQLite по его идентификатору.
        /// </summary>
        private void LoadUserEmail()
        {
            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    using (var command = new SQLiteCommand("SELECT mail FROM users WHERE id = @userId", connection))
                    {
                        command.Parameters.AddWithValue("@userId", _userId);
                        _userEmail = command.ExecuteScalar()?.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке email: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Обработчик активации флажка автозаполнения email.
        /// Подставляет email пользователя в текстовое поле.
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
        private void ChkAutoEmail_Checked(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_userEmail))
            {
                txtEmail.Text = _userEmail;
            }
            else
            {
                MessageBox.Show("Email не найден в вашем профиле", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                chkAutoEmail.IsChecked = false;
            }
        }
        /// <summary>
        /// Обработчик деактивации флажка автозаполнения email.
        /// Очищает и активирует поле ввода email.
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
        private void ChkAutoEmail_Unchecked(object sender, RoutedEventArgs e)
        {
            txtEmail.IsEnabled = true;
            txtEmail.Clear();
        }
        /// <summary>
        /// Обработчик кнопки "Отправить".
        /// Валидирует обращение, сохраняет его в базу данных и прикрепляет файлы, если они есть.
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
        private void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            string appealText = txtBoxAppeal.Text.Trim();
            string email = txtEmail.Text.Trim();

            if (string.IsNullOrEmpty(appealText))
            {
                MessageBox.Show("Пожалуйста, введите текст обращения", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string machineName = Environment.MachineName;

                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();

                    long appealId;
                    using (var command = new SQLiteCommand(connection))
                    {
                        command.CommandText = @"INSERT INTO appeals 
                                    (text, computername, user_email, userId) 
                                    VALUES (@text, @computerName, @email, @userId);
                                    SELECT last_insert_rowid();";
                        command.Parameters.AddWithValue("@text", appealText);
                        command.Parameters.AddWithValue("@computerName", machineName);
                        command.Parameters.AddWithValue("@email", string.IsNullOrEmpty(email) ? DBNull.Value : (object)email);
                        command.Parameters.AddWithValue("@userId", _userId > 0 ? (object)_userId : DBNull.Value);
                        appealId = (long)command.ExecuteScalar();
                    }

                    if (_attachments.Count > 0)
                    {
                        foreach (var attachment in _attachments)
                        {
                            using (var command = new SQLiteCommand(connection))
                            {
                                command.CommandText = @"INSERT INTO appeal_attachments
                                        (appeal_id, file_name, file_data)
                                        VALUES (@appealId, @fileName, @fileData)";
                                command.Parameters.AddWithValue("@appealId", appealId);
                                command.Parameters.AddWithValue("@fileName", attachment.FileName);
                                command.Parameters.AddWithValue("@fileData", attachment.Data);
                                command.ExecuteNonQuery();
                            }
                        }
                    }
                }

                MessageBox.Show("Ваше обращение успешно отправлено!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                ClearForm();
            }
            catch (SQLiteException ex)
            {
                MessageBox.Show($"Произошла ошибка при отправке обращения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла непредвиденная ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        /// <summary>
        /// Очищает все поля формы и сбрасывает флажки и вложения.
        /// </summary>
        private void ClearForm()
        {
            txtBoxAppeal.Clear();
            txtEmail.Clear();
            chkAutoEmail.IsChecked = false;
            _attachments.Clear();
            UpdateAttachmentsUI();
        }
        /// <summary>
        /// Копирует свойства текущего окна (размер, положение) в переданное окно.
        /// </summary>
        /// <param name="nextWindow">Окно, которому будут переданы параметры</param>
        private void WindowProperties(Window nextWindow)
        {
            nextWindow.Left = this.Left;
            nextWindow.Top = this.Top;
            nextWindow.Height = this.Height;
            nextWindow.Width = this.Width;
            nextWindow.WindowState = this.WindowState;
        }
        /// <summary>
        /// Обработчик кнопки "Главная".
        /// Открывает главное окно в зависимости от статуса пользователя (гость или авторизованный).
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
        private void btnMain_Click(object sender, RoutedEventArgs e)
        {
            if (_userId == -1) {
                var mainWindow = new MainWindow();
                WindowProperties(mainWindow);
                mainWindow.Show();
                Close();
            }
            else
            {
                var main = new Main(_userId, _isAdmin);
                WindowProperties(main);
                main.Show();
                Close();
            }
        }
        /// <summary>
        /// Обработчик кнопки "Админ".
        /// Открывает окно администрирования с передачей текущих параметров пользователя.
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
        /// Обработчик кнопки "Частые вопросы".
        /// Открывает окно с FAQ.
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
        private void btnFAQs_Click(object sender, RoutedEventArgs e)
        {
            var faqs = new FAQs(_isAdmin, _userId);
            WindowProperties(faqs);
            faqs.Show();
            Close();
        }
        /// <summary>
        /// Обработчик кнопки "Поддержка".
        /// Перезапускает текущее окно поддержки.
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
            Window mainWindow = new MainWindow();
            WindowProperties(mainWindow);
            mainWindow.Show();
            this.Close();
        }
        /// <summary>
        /// Обработчик кнопки "Аккаунт".
        /// Проверяет, авторизован ли пользователь, и открывает окно аккаунта,
        /// или показывает сообщение об ошибке для гостей.
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
                return;
            }
            else
            {
                Window account = new Personal_Account(_isAdmin, _userId);
                WindowProperties(account);
                account.Show();
                this.Close();
            }
        }
    }
}