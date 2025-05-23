using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.Linq;
using System.Data.SQLite;
using System.Xml.Linq;

namespace WpfApp1
{

    public partial class AddCourseWindow : Window
    {
        private static readonly string ConnectionString =
            $"Data Source={Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "educatingsystem.sl3")};Version=3;";

        private CourseModel _currentCourse;
        private List<Module> _modules = new List<Module>();
        private List<ModuleItem> _moduleItems = new List<ModuleItem>();
        private int _currentStep = 1;
        private byte[] _certificateTemplatePdf;
        private byte[] _sealImage;
        private byte[] _signatureImage;
        private bool _enableCertificate = false;
        /// <summary>
        /// Конструктор класса AddCourseWindow. Инициализирует компоненты окна, загружает данные для ComboBox,
        /// устанавливает начальный шаг и настраивает валидацию даты для DatePicker.
        /// </summary>
        public AddCourseWindow()
        {
            InitializeComponent();
            LoadComboBoxData();
            ShowStep1();
            dpAvailableUntil.DisplayDateStart = DateTime.Today;
            dpAvailableUntil.BlackoutDates.Add(new CalendarDateRange(DateTime.MinValue, DateTime.Today.AddDays(-1)));
            dpAvailableUntil.DateValidationError += DpAvailableUntil_DateValidationError;
        }
        /// <summary>
        /// Обработчик события валидации даты для DatePicker. Проверяет, что выбранная дата не раньше текущего дня.
        /// </summary>
        /// <param name="sender">Объект, вызвавший событие</param>
        /// <param name="e">Аргументы события валидации даты</param>
        private void DpAvailableUntil_DateValidationError(object sender, DatePickerDateValidationErrorEventArgs e)
        {
            if (e.Text == "")
            {
                return;
            }

            if (DateTime.TryParse(e.Text, out DateTime selectedDate))
            {
                if (selectedDate < DateTime.Today)
                {
                    MessageBox.Show("Дата не может быть раньше сегодняшнего дня");
                    e.ThrowException = true;
                    dpAvailableUntil.SelectedDate = DateTime.Today; 
                }
            }
            else
            {
                MessageBox.Show("Введите корректную дату");
                e.ThrowException = true;
                dpAvailableUntil.SelectedDate = DateTime.Today; 
            }
        }
        /// <summary>
        /// Обработчик события нажатия кнопки настроек сертификата. Открывает окно настроек сертификата.
        /// </summary>
        /// <param name="sender">Объект, вызвавший событие</param>
        /// <param name="e">Аргументы события</param>
        private void BtnCertificateSettings_Click(object sender, RoutedEventArgs e)
        {
            // Создаем окно с передачей courseId (0 для нового курса)
            var certWindow = new CertificateSettingsWindow(0);
            if (certWindow.ShowDialog() == true)
            {
                _enableCertificate = certWindow.EnableCertificate;
                _certificateTemplatePdf = certWindow.TemplatePdf;
                _sealImage = certWindow.SealImage;
                _signatureImage = certWindow.SignatureImage;
            }
        }
        /// <summary>
        /// Загружает данные для ComboBox из базы данных (специальности, доступность, партнеры).
        /// </summary>
        private void LoadComboBoxData()
        {
            var specialities = new List<dynamic>();
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                string query = "SELECT id, name FROM specialities";
                using (var command = new SQLiteCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            specialities.Add(new
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1)
                            });
                        }
                    }
                }
            }
            cmbSpeciality.ItemsSource = specialities;
            cmbSpeciality.DisplayMemberPath = "Name";
            cmbSpeciality.SelectedValuePath = "Id";

            var availabilities = new List<dynamic>{
                  new { Id = 1, Name = "Доступен" },
                  new { Id = 0, Name = "Не доступен" }};
            cmbAvailability.ItemsSource = availabilities;
            cmbAvailability.DisplayMemberPath = "Name";
            cmbAvailability.SelectedValuePath = "Id";
            cmbAvailability.ItemsSource = availabilities;
            cmbAvailability.DisplayMemberPath = "Name";
            cmbAvailability.SelectedValuePath = "Id";

            var partners = new List<dynamic>();
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                string query = "SELECT id, name FROM partners";
                using (var command = new SQLiteCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            partners.Add(new
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1)
                            });
                        }
                    }
                }
            }
            cmbPartner.ItemsSource = partners;
            cmbPartner.DisplayMemberPath = "Name";
            cmbPartner.SelectedValuePath = "Id";
        }
        /// <summary>
        /// Показывает первый шаг формы добавления курса (основная информация о курсе).
        /// </summary>
        private void ShowStep1()
        {
            _currentStep = 1;
            step1Panel.Visibility = Visibility.Visible;
            step2Panel.Visibility = Visibility.Collapsed;
            btnBack.Visibility = Visibility.Collapsed;

            btnNext.Content = "Далее";
        }
        /// <summary>
        /// Показывает второй шаг формы добавления курса (предпросмотр информации и управление модулями).
        /// </summary>
        private void ShowStep2()
        {
            _currentStep = 2;
            step1Panel.Visibility = Visibility.Collapsed;
            step2Panel.Visibility = Visibility.Visible;
            btnBack.Visibility = Visibility.Visible;
            btnNext.Content = "Сохранить курс";

            _currentCourse = new CourseModel
            {
                Name = txtName.Text,
                Description = txtDescription.Text, 
                SpecialityId = (cmbSpeciality.SelectedItem as dynamic)?.Id ?? 0,
                AvailabilityId = (cmbAvailability.SelectedItem as dynamic)?.Id ?? 0,
                PartnerId = (cmbPartner.SelectedItem as dynamic)?.Id ?? 0,
                AvailableUntil = dpAvailableUntil.SelectedDate
            };

            txtPreviewName.Text = _currentCourse.Name;
            txtPreviewDescription.Text = _currentCourse.Description; 
            txtPreviewSpeciality.Text = cmbSpeciality.Text;
            txtPreviewAvailability.Text = _currentCourse.AvailabilityId == 1 ? "Доступен" : "Не доступен";
            txtPreviewPartner.Text = cmbPartner.Text;
            txtPreviewAvailableUntil.Text = _currentCourse.AvailableUntil.HasValue
                ? $"Доступен до: {_currentCourse.AvailableUntil.Value.ToShortDateString()}"
                : "Без ограничения по времени";
        }
        /// <summary>
        /// Обработчик события нажатия кнопки "Далее/Сохранить". В зависимости от текущего шага либо переходит
        /// к следующему шагу, либо сохраняет курс.
        /// </summary>
        /// <param name="sender">Объект, вызвавший событие</param>
        /// <param name="e">Аргументы события</param>
        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStep == 1)
            {
                if (string.IsNullOrWhiteSpace(txtName.Text) ||
                    cmbSpeciality.SelectedItem == null ||
                    cmbAvailability.SelectedItem == null ||
                    cmbPartner.SelectedItem == null)
                {
                    MessageBox.Show("Заполните все поля");
                    return;
                }

                ShowStep2();
            }
            else if (_currentStep == 2)
            {
                // Проверяем наличие модулей перед сохранением
                if (_modules.Count == 0)
                {
                    MessageBox.Show("Добавьте хотя бы один модуль перед сохранением курса");
                    return;
                }

                // Сохраняем курс и модули
                SaveCourse();
            }
        }
        /// <summary>
        /// Обработчик события нажатия кнопки "Назад". Возвращает на предыдущий шаг формы.
        /// </summary>
        /// <param name="sender">Объект, вызвавший событие</param>
        /// <param name="e">Аргументы события</param>
        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStep == 2)
            {
                ShowStep1();
            }
        }
        /// <summary>
        /// Сохраняет курс, его модули и элементы модулей в базу данных. Включает транзакцию для обеспечения
        /// целостности данных.
        /// </summary>
        private void SaveCourse()
        {
            if (_modules.Count == 0)
            {
                MessageBox.Show("Нельзя сохранить курс без модулей. Добавьте хотя бы один модуль.");
                return;
            }
            if (_currentCourse.AvailableUntil.HasValue && _currentCourse.AvailableUntil.Value.Date < DateTime.Today)
            {
                MessageBox.Show("Дата окончания доступности не может быть раньше сегодняшнего дня");
                return;
            }
            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            string query = @"INSERT INTO courses 
    (title, description, speciality_id, availability, partnership, available_until) 
    VALUES (@name, @description, @speciality, @availability, @partnership, @availableUntil);
    SELECT last_insert_rowid();";



                            int courseId;
                            using (var command = new SQLiteCommand(query, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@name", _currentCourse.Name);
                                command.Parameters.AddWithValue("@description", _currentCourse.Description ?? "");
                                command.Parameters.AddWithValue("@speciality", _currentCourse.SpecialityId);
                                command.Parameters.AddWithValue("@availability", _currentCourse.AvailabilityId == 1);
                                command.Parameters.AddWithValue("@partnership", _currentCourse.PartnerId);
                                command.Parameters.AddWithValue("@availableUntil",
                                    _currentCourse.AvailableUntil ?? (object)DBNull.Value);

                                courseId = Convert.ToInt32(command.ExecuteScalar());
                            }

                            string courseFolder = Path.Combine("Courses", $"Course_{courseId}");
                            if (!Directory.Exists(courseFolder))
                            {
                                Directory.CreateDirectory(courseFolder);
                            }

                            foreach (var module in _modules)
                            {
                                string moduleQuery = @"INSERT INTO modules 
                        (course_id, title, description, order_index) 
                        VALUES (@courseId, @title, @description, @orderIndex);
                        SELECT last_insert_rowid();";

                                int moduleId;
                                using (var moduleCommand = new SQLiteCommand(moduleQuery, connection, transaction))
                                {
                                    moduleCommand.Parameters.AddWithValue("@courseId", courseId);
                                    moduleCommand.Parameters.AddWithValue("@title", module.Title);
                                    moduleCommand.Parameters.AddWithValue("@description", module.Description ?? "");
                                    moduleCommand.Parameters.AddWithValue("@orderIndex", module.OrderIndex);

                                    moduleId = Convert.ToInt32(moduleCommand.ExecuteScalar());
                                }

                                var moduleItems = _moduleItems.Where(i => i.ModuleId == module.Id).ToList();
                                foreach (var item in moduleItems)
                                {
                                    byte[] contentData = null;
                                    string externalUrl = null;

                                    if (item.ItemType != "test" && !string.IsNullOrEmpty(item.ContentPath))
                                    {
                                        try
                                        {
                                            contentData = File.ReadAllBytes(item.ContentPath);
                                        }
                                        catch (Exception ex)
                                        {
                                            MessageBox.Show($"Не удалось прочитать файл {item.ContentPath}: {ex.Message}");
                                            continue;
                                        }
                                    }

                                    string itemQuery = @"INSERT INTO module_items 
                            (module_id, item_type, title, content_data, external_url, duration_minutes, order_index) 
                            VALUES (@moduleId, @itemType, @title, @contentData, @externalUrl, @duration, @orderIndex);
                            SELECT last_insert_rowid();";

                                    int itemId;
                                    using (var itemCommand = new SQLiteCommand(itemQuery, connection, transaction))
                                    {
                                        itemCommand.Parameters.AddWithValue("@moduleId", moduleId);
                                        itemCommand.Parameters.AddWithValue("@itemType", item.ItemType);
                                        itemCommand.Parameters.AddWithValue("@title", item.Title);
                                        itemCommand.Parameters.AddWithValue("@contentData", contentData ?? (object)DBNull.Value);
                                        itemCommand.Parameters.AddWithValue("@externalUrl", externalUrl ?? (object)DBNull.Value);
                                        itemCommand.Parameters.AddWithValue("@duration", item.DurationMinutes ?? (object)DBNull.Value);
                                        itemCommand.Parameters.AddWithValue("@orderIndex", item.OrderIndex);

                                        itemId = Convert.ToInt32(itemCommand.ExecuteScalar());
                                    }

                                    if (item.ItemType == "test")
                                    {
                                        try
                                        {
                                            TestData testData = null;
                                            if (!string.IsNullOrEmpty(item.ContentPath))
                                            {
                                                string jsonContent = File.ReadAllText(item.ContentPath);
                                                testData = Newtonsoft.Json.JsonConvert.DeserializeObject<TestData>(jsonContent);
                                            }

                                            string testQuery = @"INSERT INTO tests 
                (module_item_id, total_points) 
                VALUES (@moduleItemId, @totalPoints);
                SELECT last_insert_rowid();";

                                            int testId;
                                            using (var testCommand = new SQLiteCommand(testQuery, connection, transaction))
                                            {
                                                testCommand.Parameters.AddWithValue("@moduleItemId", itemId);
                                                testCommand.Parameters.AddWithValue("@totalPoints",
                                                    testData?.TotalPoints ?? 100);
                                                testId = Convert.ToInt32(testCommand.ExecuteScalar());
                                            }

                                            if (testData?.Questions != null)
                                            {
                                                foreach (var question in testData.Questions)
                                                {
                                                    string questionQuery = @"INSERT INTO test_questions 
                                            (test_id, question_text, question_type, points, order_index) 
                                            VALUES (@testId, @questionText, @questionType, @points, @orderIndex);
                                            SELECT last_insert_rowid();";

                                                    int questionId;
                                                    using (var questionCommand = new SQLiteCommand(questionQuery, connection, transaction))
                                                    {
                                                        questionCommand.Parameters.AddWithValue("@testId", testId);
                                                        questionCommand.Parameters.AddWithValue("@questionText", question.Text);
                                                        questionCommand.Parameters.AddWithValue("@questionType", question.Type);
                                                        questionCommand.Parameters.AddWithValue("@points", question.Points);
                                                        questionCommand.Parameters.AddWithValue("@orderIndex", question.OrderIndex);

                                                        questionId = Convert.ToInt32(questionCommand.ExecuteScalar());
                                                    }

                                                    foreach (var answer in question.Answers)
                                                    {
                                                        string answerQuery = @"INSERT INTO test_answers 
                                                (question_id, answer_text, is_correct, order_index) 
                                                VALUES (@questionId, @answerText, @isCorrect, @orderIndex)";

                                                        using (var answerCommand = new SQLiteCommand(answerQuery, connection, transaction))
                                                        {
                                                            answerCommand.Parameters.AddWithValue("@questionId", questionId);
                                                            answerCommand.Parameters.AddWithValue("@answerText", answer.Text);
                                                            answerCommand.Parameters.AddWithValue("@isCorrect", answer.IsCorrect);
                                                            answerCommand.Parameters.AddWithValue("@orderIndex", answer.OrderIndex);

                                                            answerCommand.ExecuteNonQuery();
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            MessageBox.Show($"Ошибка при сохранении теста: {ex.Message}");
                                            continue;
                                        }
                                    }
                                }
                            }

                            SaveCourseStructure(courseFolder);

                            if (_enableCertificate)
                            {
                                SaveCertificateSettings(courseId, connection, transaction);
                            }

                            transaction.Commit();
                            MessageBox.Show("Курс успешно создан!");
                            this.DialogResult = true;
                            this.Close();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            MessageBox.Show($"Ошибка при сохранении курса: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения: {ex.Message}");
            }
        }
        /// <summary>
        /// Сохраняет структуру курса в JSON файл для последующего использования.
        /// </summary>
        /// <param name="courseFolder">Путь к папке курса</param>
        private void SaveCourseStructure(string courseFolder)
        {
            var courseStructure = new
            {
                course = new
                {
                    title = _currentCourse.Name,
                    description = _currentCourse.Description ?? ""
                },
                modules = _modules.Select(m => new
                {
                    id = m.Id,
                    title = m.Title,
                    description = m.Description ?? "",
                    items = _moduleItems.Where(i => i.ModuleId == m.Id).Select(i => new
                    {
                        type = i.ItemType,
                        title = i.Title,
                        duration = i.DurationMinutes,
                        content_in_db = true
                    })
                })
            };

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(courseStructure, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(Path.Combine(courseFolder, "course_structure.json"), json);
        }
        /// <summary>
        /// Сохраняет настройки сертификата для курса в базу данных.
        /// </summary>
        /// <param name="courseId">ID курса</param>
        /// <param name="connection">Подключение к базе данных</param>
        /// <param name="transaction">Текущая транзакция</param>
        private void SaveCertificateSettings(int courseId, SQLiteConnection connection, SQLiteTransaction transaction)
        {
            try
            {
                string query = @"
                INSERT OR REPLACE INTO certificates 
                (course_id, template_pdf, seal_image, signature_image, partner)
                VALUES (@courseId, @template, @seal, @signature, @partner)";

                using (var command = new SQLiteCommand(query, connection, transaction))
                {
                    command.Parameters.AddWithValue("@courseId", courseId);
                    command.Parameters.AddWithValue("@template", _certificateTemplatePdf ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@seal", _sealImage ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@signature", _signatureImage ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@partner", _currentCourse.PartnerId > 0 ? _currentCourse.PartnerId : (object)DBNull.Value);

                    int affectedRows = command.ExecuteNonQuery();

                    if (affectedRows == 0)
                    {
                        MessageBox.Show("Не удалось сохранить настройки сертификата");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении настроек сертификата: {ex.Message}");
            }
        }
        /// <summary>
        /// Обработчик события нажатия кнопки "Добавить модуль". Открывает окно добавления нового модуля.
        /// </summary>
        /// <param name="sender">Объект, вызвавший событие</param>
        /// <param name="e">Аргументы события</param>
        private void btnAddModule_Click(object sender, RoutedEventArgs e)
        {
            var moduleWindow = new AddModuleWindow(_modules);
            if (moduleWindow.ShowDialog() == true)
            {
                var newModule = moduleWindow.GetModule();
                newModule.Id = _modules.Count > 0 ? _modules.Max(m => m.Id) + 1 : 1; 

                foreach (var item in moduleWindow.GetModuleItems())
                {
                    item.ModuleId = newModule.Id;
                    _moduleItems.Add(item);
                }

                _modules.Add(newModule);
                RefreshModulesList();
            }
        }
        /// <summary>
        /// Обновляет список модулей в ListView.
        /// </summary>
        private void RefreshModulesList()
        {
            lstModules.ItemsSource = null;
            lstModules.ItemsSource = _modules;
        }
        /// <summary>
        /// Обработчик события нажатия кнопки "Редактировать модуль". Открывает окно редактирования выбранного модуля.
        /// </summary>
        /// <param name="sender">Объект, вызвавший событие</param>
        /// <param name="e">Аргументы события</param>
        private void btnEditModule_Click(object sender, RoutedEventArgs e)
        {
            if (lstModules.SelectedItem is Module selectedModule)
            {
                var moduleItems = _moduleItems.Where(i => i.ModuleId == selectedModule.Id).ToList();
                var moduleWindow = new AddModuleWindow(selectedModule, moduleItems, _modules);

                if (moduleWindow.ShowDialog() == true)
                {
                    selectedModule.Title = moduleWindow.GetModule().Title;
                    selectedModule.Description = moduleWindow.GetModule().Description;

                    _moduleItems.RemoveAll(i => i.ModuleId == selectedModule.Id);

                    foreach (var item in moduleWindow.GetModuleItems())
                    {
                        item.ModuleId = selectedModule.Id;
                        _moduleItems.Add(item);
                    }

                    RefreshModulesList();
                }
            }
        }
        /// <summary>
        /// Обработчик события нажатия кнопки "Удалить модуль". Удаляет выбранный модуль после подтверждения.
        /// </summary>
        /// <param name="sender">Объект, вызвавший событие</param>
        /// <param name="e">Аргументы события</param>
        private void btnDeleteModule_Click(object sender, RoutedEventArgs e)
        {
            if (lstModules.SelectedItem is Module selectedModule)
            {
                if (MessageBox.Show("Удалить этот модуль?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    _modules.Remove(selectedModule);
                    _moduleItems.RemoveAll(i => i.ModuleId == selectedModule.Id);
                    RefreshModulesList();
                }
            }
        }
    }

}