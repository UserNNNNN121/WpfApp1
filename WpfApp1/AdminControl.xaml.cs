using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Net.Mail;
using System.Net;
using System.Diagnostics;
using System.IO;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Documents;

namespace WpfApp1
{

    public partial class AdminControl : Window
    {
        private static readonly string ConnectionString =
            $"Data Source={Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "educatingsystem.sl3")};Version=3;";
        private Grid originalContent;
        private DataGrid gridConfirmed;
        private DataGrid gridUnconfirmed;
        private DataGrid gridCourses;
        private DataGrid gridPartners;

        public int _userId;
        public bool _isAdmin;
        /// <summary>
        /// /// Инициализирует новый экземпляр окна AdminControl с указанным статусом администратора и идентификатором пользователя.
        /// Настраивает компоненты пользовательского интерфейса и инициализирует обработчики событий для обновления доступности курса.
        /// </summary>
        /// <param name="isAdmin">Указывает, обладает ли текущий пользователь правами администратора.</param>
        /// <param name="userId">Уникальный идентификатор текущего пользователя.</param>
        public AdminControl(bool isAdmin, int userId)
        {
            InitializeComponent();
            this._isAdmin = isAdmin;
            this._userId = userId;
            originalContent = (Grid)mainScrollViewer.Content;

            this.Resources.Add("ComboBoxStyle", new Style(typeof(ComboBox))
            {
                Setters =
                {
                    new Setter(Control.PaddingProperty, new Thickness(5)),
                    new Setter(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Stretch)
                }
            });
            CourseAvailabilityManager.CoursesAvailabilityUpdated += OnCoursesAvailabilityUpdated;
            this.Closed += (s, e) => CourseAvailabilityManager.CoursesAvailabilityUpdated -= OnCoursesAvailabilityUpdated;
        }

        /// <summary>
        /// Обрабатывает событие CoursesAvailabilityUpdated для уведомления об изменениях доступности курса.
        /// Отображает информационное сообщение для пользователя о возможных изменениях доступности курса.
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
        /// Represents administrator data including ID, login, name, and email.
        /// </summary>
        public class Admin
        {
            public int Id { get; set; }
            public string Login { get; set; }
            public string Name { get; set; }
            public string Email { get; set; }
        }
        /// <summary>
        /// Представляет данные пользователя, включая идентификатор, логин, имя, специальность, статус подтверждения и адрес электронной почты.
        /// </summary>
        public class User
        {
            public int Id { get; set; }
            public string Login { get; set; }
            public string Name { get; set; }
            public string Speciality { get; set; }
            public int Confirmation { get; set; }
            public string Email { get; set; }
        }
        /// <summary>
        /// Настраивает свойства следующего окна в соответствии с положением и размером текущего окна.
        /// </summary>
        /// <param name="nextWindow">Настраиваемое окно.</param>
        private void WindowProperties(Window nextWindow)
        {
            nextWindow.Left = this.Left;
            nextWindow.Top = this.Top;
            nextWindow.Height = this.Height;
            nextWindow.Width = this.Width;
            nextWindow.WindowState = this.WindowState;
        }
        /// <summary>
        /// Инициализирует таблицу курсов, включив в нее столбцы для идентификатора, названия, специальности, доступности и информации о партнере.
        /// Настраивает привязку данных и шаблонов редактирования для полей со списком в таблице.
        /// </summary>
        private void InitializeCourseGrid()
        {
            gridCourses.Columns.Clear();

            gridCourses.Columns.Add(new DataGridTextColumn()
            {
                Header = "ID",
                Binding = new Binding("Id"),
                Width = 40,
                IsReadOnly = true
            });

            gridCourses.Columns.Add(new DataGridTextColumn()
            {
                Header = "Название",
                Binding = new Binding("Name"),
                Width = 190,
                IsReadOnly = false
            });

            var specialityColumn = new DataGridTemplateColumn()
            {
                Header = "Специальность",
                Width = 150
            };

            var specialityCellTemplate = new DataTemplate();
            var specialityTextBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
            specialityTextBlockFactory.SetBinding(TextBlock.TextProperty, new Binding("SpecialityName"));
            specialityCellTemplate.VisualTree = specialityTextBlockFactory;
            specialityColumn.CellTemplate = specialityCellTemplate;

            var specialityEditTemplate = new DataTemplate();
            var specialityComboFactory = new FrameworkElementFactory(typeof(ComboBox));
            specialityComboFactory.SetBinding(ComboBox.ItemsSourceProperty, new Binding("Specialities"));
            specialityComboFactory.SetBinding(ComboBox.SelectedValueProperty, new Binding("SpecialityId"));
            specialityComboFactory.SetValue(ComboBox.DisplayMemberPathProperty, "Name");
            specialityComboFactory.SetValue(ComboBox.SelectedValuePathProperty, "Id");

            specialityComboFactory.AddHandler(ComboBox.SelectionChangedEvent, new SelectionChangedEventHandler((sender, e) =>
            {
                if (sender is ComboBox combo && combo.SelectedItem != null &&
                    gridCourses.SelectedItem is Course course)
                {
                    course.UpdateSpeciality(combo.SelectedItem);
                }
            }));

            specialityEditTemplate.VisualTree = specialityComboFactory;
            specialityColumn.CellEditingTemplate = specialityEditTemplate;
            gridCourses.Columns.Add(specialityColumn);

            var availabilityColumn = new DataGridTemplateColumn()
            {
                Header = "Доступность",
                Width = 120
            };

            var availabilityCellTemplate = new DataTemplate();
            var availabilityTextBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
            availabilityTextBlockFactory.SetBinding(TextBlock.TextProperty, new Binding("AvailabilityName"));
            availabilityCellTemplate.VisualTree = availabilityTextBlockFactory;
            availabilityColumn.CellTemplate = availabilityCellTemplate;

            var availabilityEditTemplate = new DataTemplate();
            var availabilityComboFactory = new FrameworkElementFactory(typeof(ComboBox));
            availabilityComboFactory.SetBinding(ComboBox.ItemsSourceProperty, new Binding("Availabilities"));
            availabilityComboFactory.SetBinding(ComboBox.SelectedValueProperty, new Binding("AvailabilityId"));
            availabilityComboFactory.SetValue(ComboBox.DisplayMemberPathProperty, "Name");
            availabilityComboFactory.SetValue(ComboBox.SelectedValuePathProperty, "Id");

            availabilityComboFactory.AddHandler(ComboBox.SelectionChangedEvent, new SelectionChangedEventHandler((sender, args) =>
            {
                if (sender is ComboBox combo && combo.SelectedItem != null &&
                    gridCourses.SelectedItem is Course course)
                {
                    course.UpdateAvailability(combo.SelectedItem);
                }
            }));

            availabilityEditTemplate.VisualTree = availabilityComboFactory;
            availabilityColumn.CellEditingTemplate = availabilityEditTemplate;
            gridCourses.Columns.Add(availabilityColumn);

            var availableUntilColumn = new DataGridTemplateColumn()
            {
                Header = "Доступен до",
                Width = 150
            };

            var availableUntilCellTemplate = new DataTemplate();
            var availableUntilTextBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
            availableUntilTextBlockFactory.SetBinding(TextBlock.TextProperty,
                new Binding("AvailableUntilFormatted")); 
            availableUntilCellTemplate.VisualTree = availableUntilTextBlockFactory;
            availableUntilColumn.CellTemplate = availableUntilCellTemplate;

            var availableUntilEditTemplate = new DataTemplate();
            var availableUntilDatePickerFactory = new FrameworkElementFactory(typeof(DatePicker));
            availableUntilDatePickerFactory.SetBinding(DatePicker.SelectedDateProperty,
                new Binding("AvailableUntil") { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            availableUntilDatePickerFactory.SetValue(DatePicker.DisplayDateStartProperty, DateTime.Today);
            availableUntilDatePickerFactory.SetValue(DatePicker.DisplayDateEndProperty, DateTime.Today.AddYears(10));
            availableUntilEditTemplate.VisualTree = availableUntilDatePickerFactory;
            availableUntilColumn.CellEditingTemplate = availableUntilEditTemplate;
            gridCourses.Columns.Add(availableUntilColumn);

            var partnershipColumn = new DataGridTemplateColumn()
            {
                Header = "Партнер",
                Width = 150
            };

            var partnershipCellTemplate = new DataTemplate();
            var partnershipTextBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
            partnershipTextBlockFactory.SetBinding(TextBlock.TextProperty, new Binding("PartnerName"));
            partnershipCellTemplate.VisualTree = partnershipTextBlockFactory;
            partnershipColumn.CellTemplate = partnershipCellTemplate;

            var partnershipEditTemplate = new DataTemplate();
            var partnershipComboFactory = new FrameworkElementFactory(typeof(ComboBox));
            partnershipComboFactory.SetBinding(ComboBox.ItemsSourceProperty, new Binding("Partners"));
            partnershipComboFactory.SetBinding(ComboBox.SelectedValueProperty, new Binding("PartnerId"));
            partnershipComboFactory.SetValue(ComboBox.DisplayMemberPathProperty, "Name");
            partnershipComboFactory.SetValue(ComboBox.SelectedValuePathProperty, "Id");
            partnershipComboFactory.SetValue(ComboBox.IsEditableProperty, false);

            partnershipComboFactory.AddHandler(ComboBox.SelectionChangedEvent, new SelectionChangedEventHandler((sender, e) =>
            {
                if (sender is ComboBox combo && combo.SelectedItem != null &&
                    gridCourses.SelectedItem is Course course)
                {
                    course.UpdatePartner(combo.SelectedItem);
                }
            }));

            partnershipEditTemplate.VisualTree = partnershipComboFactory;
            partnershipColumn.CellEditingTemplate = partnershipEditTemplate;
            gridCourses.Columns.Add(partnershipColumn);

        }
        /// <summary>
        /// Сохраняет информацию о модуле в базе данных и обновляет пользовательский интерфейс, чтобы отразить изменения.
        /// </summary>
        /// <param name="module">Объект модуля, содержащий сохраняемые данные.</param>
        private void SaveModule(Module module)
        {
            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    string query = @"UPDATE modules SET 
                title = @title, 
                description = @description,
                order_index = @orderIndex
                WHERE id = @id";

                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@title", module.Title);
                        command.Parameters.AddWithValue("@description", module.Description);
                        command.Parameters.AddWithValue("@orderIndex", module.OrderIndex);
                        command.Parameters.AddWithValue("@id", module.Id);

                        command.ExecuteNonQuery();
                    }
                }

                if (modulesGrid != null)
                {
                    modulesGrid.Items.Refresh();
                }

                if (itemsGrid != null && itemsGrid.ItemsSource is IEnumerable<ModuleItemWithModule> items)
                {
                    foreach (var item in items)
                    {
                        if (item.ModuleId == module.Id)
                        {
                            item.ModuleTitle = module.Title;
                        }
                    }
                    itemsGrid.Items.Refresh();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении модуля: {ex.Message}");
            }
        }
        /// <summary>
        /// Отображает содержимое элемента модуля в новом окне с вкладками для содержимого и информации.
        /// Поддерживает различные типы контента, включая текст, видео и тесты.
        /// </summary>
        /// <param name="sender">Кнопка, которая инициировала событие.</param>
        /// <param name="e">Аргументы события.</param>
        private void ViewModuleItem_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button) || !(button.Tag is int itemId)) return;

            var item = GetModuleItemById(itemId);
            if (item == null) return;

            var viewWindow = new Window
            {
                Title = $"Просмотр содержимого: {item.Title}",
                Width = 800,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            var tabControl = new TabControl();

            var contentTab = new TabItem { Header = "Содержимое" };
            var contentViewer = new ScrollViewer();

            if (item.ItemType == "text" || item.ItemType == "lecture")
            {
                var textContent = new TextBox
                {
                    Text = item.ContentData != null ? Encoding.UTF8.GetString(item.ContentData) : "Нет содержимого",
                    IsReadOnly = true,
                    TextWrapping = TextWrapping.Wrap,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
                };
                contentViewer.Content = textContent;
            }
            else if (item.ItemType == "video")
            {
                var videoPanel = new StackPanel();

                if (item.ContentData != null)
                {
                    try
                    {
                        string tempFile = Path.GetTempFileName() + ".mp4";
                        File.WriteAllBytes(tempFile, item.ContentData);

                        var mediaElement = new MediaElement
                        {
                            Source = new Uri(tempFile),
                            LoadedBehavior = MediaState.Manual,
                            UnloadedBehavior = MediaState.Close,
                            Stretch = Stretch.Uniform,
                            Volume = 0.5
                        };

                        var controlsPanel = new StackPanel { Orientation = Orientation.Horizontal };
                        var playButton = new Button { Content = "▶", Width = 30 };
                        var pauseButton = new Button { Content = "⏸", Width = 30 };
                        var stopButton = new Button { Content = "⏹", Width = 30 };

                        playButton.Click += (s, args) => mediaElement.Play();
                        pauseButton.Click += (s, args) => mediaElement.Pause();
                        stopButton.Click += (s, args) => mediaElement.Stop();

                        controlsPanel.Children.Add(playButton);
                        controlsPanel.Children.Add(pauseButton);
                        controlsPanel.Children.Add(stopButton);

                        videoPanel.Children.Add(controlsPanel);
                        videoPanel.Children.Add(mediaElement);
                    }
                    catch (Exception ex)
                    {
                        videoPanel.Children.Add(new TextBlock { Text = $"Ошибка загрузки видео: {ex.Message}" });
                    }
                }
                else if (!string.IsNullOrEmpty(item.ExternalUrl))
                {
                    var linkText = new TextBlock { Text = "Внешняя ссылка: " };
                    var hyperlink = new Hyperlink(new Run(item.ExternalUrl))
                    {
                        NavigateUri = new Uri(item.ExternalUrl)
                    };
                    hyperlink.RequestNavigate += (s, args) => Process.Start(new ProcessStartInfo(args.Uri.ToString()) { UseShellExecute = true });

                    var textBlock = new TextBlock();
                    textBlock.Inlines.Add(linkText);
                    textBlock.Inlines.Add(hyperlink);

                    videoPanel.Children.Add(textBlock);
                }
                else
                {
                    videoPanel.Children.Add(new TextBlock { Text = "Нет видео содержимого" });
                }

                contentViewer.Content = videoPanel;
            }
            else if (item.ItemType == "test")
            {
                try
                {
                    var test = GetTestByModuleItemId(item.Id);
                    if (test != null)
                    {
                        var testPanel = new StackPanel();
                        testPanel.Children.Add(new TextBlock { Text = $"Тест: {test.Title}", FontWeight = FontWeights.Bold });

                        var questions = GetTestQuestions(test.Id);
                        foreach (var question in questions)
                        {
                            var questionPanel = new StackPanel { Margin = new Thickness(0, 10, 0, 0) };
                            questionPanel.Children.Add(new TextBlock { Text = $"Вопрос: {question.Text}", FontWeight = FontWeights.SemiBold });

                            var answers = GetTestAnswers(question.Id);
                            foreach (var answer in answers)
                            {
                                var answerText = new TextBlock { Text = $"  - {answer.Text}" };
                                if (answer.IsCorrect)
                                {
                                    answerText.FontWeight = FontWeights.Bold;
                                    answerText.Foreground = Brushes.Green;
                                }
                                questionPanel.Children.Add(answerText);
                            }

                            testPanel.Children.Add(questionPanel);
                        }

                        contentViewer.Content = testPanel;
                    }
                    else
                    {
                        contentViewer.Content = new TextBlock { Text = "Тест не найден" };
                    }
                }
                catch (Exception ex)
                {
                    contentViewer.Content = new TextBlock { Text = $"Ошибка загрузки теста: {ex.Message}" };
                }
            }

            contentTab.Content = contentViewer;
            tabControl.Items.Add(contentTab);

            var infoTab = new TabItem { Header = "Информация" };
            var infoPanel = new StackPanel { Margin = new Thickness(10) };

            infoPanel.Children.Add(new TextBlock { Text = $"Тип: {item.ItemType}", FontWeight = FontWeights.Bold });
            infoPanel.Children.Add(new TextBlock { Text = $"Название: {item.Title}" });
            infoPanel.Children.Add(new TextBlock { Text = $"Длительность: {item.DurationMinutes?.ToString() ?? "Не указана"} мин" });
            infoPanel.Children.Add(new TextBlock { Text = $"Размер данных: {item.ContentData?.Length.ToString() ?? "0"} байт" });
            if (!string.IsNullOrEmpty(item.ExternalUrl))
            {
                infoPanel.Children.Add(new TextBlock { Text = "Внешняя ссылка:" });
                var hyperlink = new Hyperlink(new Run(item.ExternalUrl))
                {
                    NavigateUri = new Uri(item.ExternalUrl)
                };
                hyperlink.RequestNavigate += (s, args) => Process.Start(new ProcessStartInfo(args.Uri.ToString()) { UseShellExecute = true });
                var linkText = new TextBlock();
                linkText.Inlines.Add(hyperlink);
                infoPanel.Children.Add(linkText);
            }

            infoTab.Content = infoPanel;
            tabControl.Items.Add(infoTab);

            var closeButton = new Button
            {
                Content = "Закрыть",
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0),
                Width = 100
            };
            closeButton.Click += (s, args) => viewWindow.Close();

            var mainPanel = new StackPanel();
            mainPanel.Children.Add(tabControl);
            mainPanel.Children.Add(closeButton);

            viewWindow.Content = mainPanel;
            viewWindow.ShowDialog();
        }
        /// <summary>
        /// Создает описание содержимого элемента модуля на основе его типа и доступных данных.
        /// </summary>
        /// <param name="item">Элемент модуля, который требуется описать.</param>
        /// <returns>строку, описывающую содержимое элемента.</returns>
        private string GetContentDescription(EditModuleItem item)
        {
            if (item.ContentData != null && item.ContentData.Length > 0)
            {
                return "[Данные загружены]";
            }
            else if (!string.IsNullOrEmpty(item.ExternalUrl))
            {
                return $"Внешняя ссылка: {item.ExternalUrl}";
            }
            else
            {
                return "Нет содержимого";
            }
        }
        /// <summary>
        ///  Сохраняет изменения в элементе модуля в базе данных.
        /// Проверяет и обрабатывает настройки продолжительности для элементов типа видео.
        /// </summary>
        /// <param name="item">Элемент модуля, изменения в котором необходимо сохранить.</param>
        private void SaveModuleItem(ModuleItemWithModule item)
        {
            try
            {
                if (item.ItemType != "video" && item.DurationMinutes.HasValue)
                {
                    item.DurationMinutes = null; 
                }

                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    string query = @"UPDATE module_items SET 
                title = @title, 
                content_data = @contentData,
                external_url = @externalUrl,
                duration_minutes = @duration,
                order_index = @orderIndex
                WHERE id = @id";

                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@title", item.Title);
                        command.Parameters.AddWithValue("@contentData", item.ContentData ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@externalUrl", item.ExternalUrl ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@duration",
                            item.ItemType == "video" ? item.DurationMinutes ?? (object)DBNull.Value : (object)DBNull.Value);
                        command.Parameters.AddWithValue("@orderIndex", item.OrderIndex);
                        command.Parameters.AddWithValue("@id", item.Id);

                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении элемента модуля: {ex.Message}");
            }
        }
        private DataGrid modulesGrid;
        private DataGrid itemsGrid;
        /// <summary>
        /// Открывает окно расширенного редактирования курса с вкладками для управления модулями и элементами модуля.
        /// Предоставляет широкие возможности редактирования структуры и содержания курса.
        /// </summary>
        /// <param name="sender">Кнопка, которая инициировала событие.</param>
        /// <param name="e">Аргументы события.</param>
        private void AdvancedEditCourse_Click(object sender, RoutedEventArgs e)
        {
            if (gridCourses.SelectedItem == null)
            {
                MessageBox.Show("Выберите курс для редактирования");
                return;
            }

            var course = gridCourses.SelectedItem as Course;
            if (course == null) return;

            var advancedEditWindow = new Window
            {
                Title = $"Продвинутое редактирование курса: {course.Name}",
                Width = 1000,
                Height = 700,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Icon = new BitmapImage(new Uri("pack://application:,,,/logo2.png")) 
            };


            var tabControl = new TabControl();


            var modulesTab = new TabItem { Header = "Модули" };
            this.modulesGrid = new DataGrid
            {
                AutoGenerateColumns = false,
                CanUserAddRows = false,
                SelectionMode = DataGridSelectionMode.Single,
                IsReadOnly = true,
                Margin = new Thickness(5)
            };

            modulesGrid.Columns.Add(new DataGridTextColumn { Header = "ID", Binding = new Binding("Id"), Width = 50 });
            modulesGrid.Columns.Add(new DataGridTextColumn { Header = "Название", Binding = new Binding("Title"), Width = 200 });
            modulesGrid.Columns.Add(new DataGridTextColumn { Header = "Описание", Binding = new Binding("Description"), Width = 300 });


            var moduleActionColumn = new DataGridTemplateColumn { Header = "Действия", Width = 200 };
            var moduleActionTemplate = new DataTemplate();
            var moduleActionFactory = new FrameworkElementFactory(typeof(StackPanel));
            moduleActionFactory.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);

            var editModuleButton = new FrameworkElementFactory(typeof(Button));
            editModuleButton.SetValue(Button.ContentProperty, "Редактировать");
            editModuleButton.SetValue(Button.WidthProperty, 100.0);
            editModuleButton.SetValue(Button.MarginProperty, new Thickness(2));
            editModuleButton.SetValue(Button.PaddingProperty, new Thickness(5));
            editModuleButton.SetBinding(Button.TagProperty, new Binding("Id"));
            editModuleButton.AddHandler(Button.ClickEvent, new RoutedEventHandler(EditModule_Click));

            var deleteModuleButton = new FrameworkElementFactory(typeof(Button));
            deleteModuleButton.SetValue(Button.ContentProperty, "Удалить");
            deleteModuleButton.SetValue(Button.WidthProperty, 80.0);
            deleteModuleButton.SetValue(Button.MarginProperty, new Thickness(2));
            deleteModuleButton.SetValue(Button.PaddingProperty, new Thickness(5));
            deleteModuleButton.SetValue(Button.ForegroundProperty, Brushes.Red);
            deleteModuleButton.SetBinding(Button.TagProperty, new Binding("Id"));
            deleteModuleButton.AddHandler(Button.ClickEvent, new RoutedEventHandler(DeleteModule_Click));

            moduleActionFactory.AppendChild(editModuleButton);
            moduleActionFactory.AppendChild(deleteModuleButton);
            moduleActionTemplate.VisualTree = moduleActionFactory;
            moduleActionColumn.CellTemplate = moduleActionTemplate;
            modulesGrid.Columns.Add(moduleActionColumn);

            var modules = GetModulesByCourseId(course.Id);
            modulesGrid.ItemsSource = modules;

            var modulesButtonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(5, 10, 5, 5)
            };

            var refreshModulesButton = new Button
            {
                Content = "Обновить список модулей",
                Margin = new Thickness(5),
                Padding = new Thickness(5),
                Width = 180,
                ToolTip = "Обновить список всех модулей курса"
            };
            refreshModulesButton.Click += (s, args) =>
            {
                modulesGrid.ItemsSource = GetModulesByCourseId(course.Id);
            };

            var addModuleButton = new Button
            {
                Content = "Добавить модуль",
                Margin = new Thickness(5),
                Padding = new Thickness(5),
                Width = 150,
                Background = Brushes.LightGreen
            };
            addModuleButton.Click += (s, args) => AddNewModule(course.Id, modulesGrid);

            modulesButtonPanel.Children.Add(refreshModulesButton);
            modulesButtonPanel.Children.Add(addModuleButton);

            var modulesPanel = new StackPanel();
            modulesPanel.Children.Add(modulesGrid);
            modulesPanel.Children.Add(modulesButtonPanel);
            modulesTab.Content = modulesPanel;

            var itemsTab = new TabItem { Header = "Элементы модулей" };
            this.itemsGrid = new DataGrid
            {
                AutoGenerateColumns = false,
                CanUserAddRows = false,
                SelectionMode = DataGridSelectionMode.Single,
                IsReadOnly = false,
                Margin = new Thickness(5)
            };

            itemsGrid.Columns.Add(new DataGridTextColumn { Header = "ID", Binding = new Binding("Id"), Width = 50, IsReadOnly = true });

            var moduleColumn = new DataGridTemplateColumn()
            {
                Header = "Модуль",
                Width = 150
            };

            var moduleCellTemplate = new DataTemplate();
            var moduleTextBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
            moduleTextBlockFactory.SetBinding(TextBlock.TextProperty, new Binding("ModuleTitle"));
            moduleCellTemplate.VisualTree = moduleTextBlockFactory;
            moduleColumn.CellTemplate = moduleCellTemplate;

            var moduleEditTemplate = new DataTemplate();
            var moduleComboFactory = new FrameworkElementFactory(typeof(ComboBox));
            moduleComboFactory.SetBinding(ComboBox.ItemsSourceProperty, new Binding("AvailableModules"));
            moduleComboFactory.SetBinding(ComboBox.SelectedValueProperty, new Binding("ModuleId"));
            moduleComboFactory.SetValue(ComboBox.DisplayMemberPathProperty, "Title");
            moduleComboFactory.SetValue(ComboBox.SelectedValuePathProperty, "Id");
            moduleEditTemplate.VisualTree = moduleComboFactory;
            moduleColumn.CellEditingTemplate = moduleEditTemplate;
            itemsGrid.Columns.Add(moduleColumn);

            var typeColumn = new DataGridTextColumn
            {
                Header = "Тип",
                Binding = new Binding("ItemType"),
                Width = 80,
                IsReadOnly = true
            };
            itemsGrid.Columns.Add(typeColumn);


            itemsGrid.Columns.Add(new DataGridTextColumn { Header = "Название", Binding = new Binding("Title"), Width = 200 });
            itemsGrid.Columns.Add(new DataGridTextColumn { Header = "Длительность", Binding = new Binding("DurationMinutes"), Width = 80 });


            var itemActionColumn = new DataGridTemplateColumn()
            {
                Header = "Действия",
                Width = 200
            };

            var itemActionTemplate = new DataTemplate();
            var stackFactory = new FrameworkElementFactory(typeof(StackPanel));
            stackFactory.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);


            var viewButtonFactory = new FrameworkElementFactory(typeof(Button));
            viewButtonFactory.SetValue(Button.ContentProperty, "Просмотр");
            viewButtonFactory.SetValue(Button.WidthProperty, 100.0);
            viewButtonFactory.SetValue(Button.MarginProperty, new Thickness(2));
            viewButtonFactory.SetValue(Button.PaddingProperty, new Thickness(5));
            viewButtonFactory.SetBinding(Button.TagProperty, new Binding("Id"));
            viewButtonFactory.AddHandler(Button.ClickEvent, new RoutedEventHandler(ViewModuleItem_Click));
            stackFactory.AppendChild(viewButtonFactory);


            var deleteButtonFactory = new FrameworkElementFactory(typeof(Button));
            deleteButtonFactory.SetValue(Button.ContentProperty, "Удалить");
            deleteButtonFactory.SetValue(Button.WidthProperty, 80.0);
            deleteButtonFactory.SetValue(Button.MarginProperty, new Thickness(2));
            deleteButtonFactory.SetValue(Button.PaddingProperty, new Thickness(5));
            deleteButtonFactory.SetValue(Button.ForegroundProperty, Brushes.Red);
            deleteButtonFactory.SetBinding(Button.TagProperty, new Binding("Id"));
            deleteButtonFactory.AddHandler(Button.ClickEvent, new RoutedEventHandler(DeleteModuleItem_Click));
            stackFactory.AppendChild(deleteButtonFactory);

            itemActionTemplate.VisualTree = stackFactory;
            itemActionColumn.CellTemplate = itemActionTemplate;
            itemsGrid.Columns.Add(itemActionColumn);

            itemsGrid.AddHandler(Button.ClickEvent, new RoutedEventHandler(ItemGridButtonClick));

            var allItems = new List<ModuleItemWithModule>();
            foreach (var module in modules)
            {
                var items = GetModuleItemsByModuleId(module.Id);
                allItems.AddRange(items.Select(i => new ModuleItemWithModule
                {
                    Id = i.Id,
                    ModuleId = i.ModuleId,
                    ModuleTitle = module.Title,
                    ItemType = i.ItemType,
                    Title = i.Title,
                    ContentData = i.ContentData,
                    ExternalUrl = i.ExternalUrl,
                    DurationMinutes = i.DurationMinutes,
                    OrderIndex = i.OrderIndex,
                    AvailableModules = modules
                }));
            }
            itemsGrid.ItemsSource = allItems;

            var itemsButtonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(5, 10, 5, 5)
            };

            var refreshItemsButton = new Button
            {
                Content = "Обновить список элементов",
                Margin = new Thickness(5),
                Padding = new Thickness(5),
                Width = 180,
                ToolTip = "Обновить список всех элементов модулей"
            };
            refreshItemsButton.Click += (s, args) =>
            {
                RefreshAdvancedEditData(course.Id);
            };


            var moduleCombo = new ComboBox
            {
                Width = 200,
                Margin = new Thickness(5),
                DisplayMemberPath = "Title",
                ItemsSource = modules
            };

            var addItemButton = new Button
            {
                Content = "Добавить элемент",
                Width = 150,
                Margin = new Thickness(5),
                Padding = new Thickness(5),
                Background = Brushes.LightGreen
            };
            addItemButton.Click += (s, args) =>
            {
                if (moduleCombo.SelectedItem is Module selectedModule)
                {
                    AddNewModuleItem(selectedModule.Id, itemsGrid);
                }
                else
                {
                    MessageBox.Show("Выберите модуль для добавления элемента");
                }
            };

            itemsButtonPanel.Children.Add(refreshItemsButton);
            itemsButtonPanel.Children.Add(new Label { Content = "Модуль:", VerticalAlignment = VerticalAlignment.Center });
            itemsButtonPanel.Children.Add(moduleCombo);
            itemsButtonPanel.Children.Add(addItemButton);

            var itemsPanel = new StackPanel();
            itemsPanel.Children.Add(itemsGrid);
            itemsPanel.Children.Add(itemsButtonPanel);
            itemsTab.Content = itemsPanel;


            tabControl.Items.Add(modulesTab);
            tabControl.Items.Add(itemsTab);


            var saveButton = new Button
            {
                Content = "Сохранить все изменения",
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0),
                Padding = new Thickness(10, 5, 10, 5),
                Background = Brushes.LightGreen,
                Width = 200
            };

            saveButton.Click += (s, args) =>
            {
                try
                {
                   
                    if (modulesGrid.Items.Count == 0)
                    {
                        MessageBox.Show("Курс должен содержать хотя бы один модуль");
                        return;
                    }

               
                    foreach (var module in modulesGrid.Items.OfType<Module>())
                    {
                        SaveModule(module);
                    }

                    foreach (var item in itemsGrid.Items.OfType<ModuleItemWithModule>())
                    {
                        SaveModuleItem(item);
                    }

                    RefreshAdvancedEditData(course.Id);

                    MessageBox.Show("Все изменения сохранены");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сохранении: {ex.Message}");
                }
            };

            var mainPanel = new StackPanel();
            mainPanel.Children.Add(tabControl);
            mainPanel.Children.Add(saveButton);

            advancedEditWindow.Content = mainPanel;
            advancedEditWindow.Closed += (s, args) =>
            {
                LoadCourses();
            };
            advancedEditWindow.ShowDialog();
        }
        /// <summary>
        /// Обрабатывает события нажатия кнопки в сетке элементов, включая действия по просмотру и сохранению.
        /// Обновляет ссылки на название модуля при сохранении изменений в элементах.
        /// </summary>
        /// <param name="sender">Кнопка, которая инициировала событие.</param>
        /// <param name="e">Аргументы события.</param>
        private void ItemGridButtonClick(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is Button button)
            {
                if (button.Tag is int itemId)
                {
                    if (button.Content.ToString() == "Просмотр")
                    {
                        ViewModuleItem_Click(button, e);
                    }
                    else if (button.Content.ToString() == "Сохранить")
                    {
                        if (itemsGrid.SelectedItem is ModuleItemWithModule item)
                        {
                            var selectedModule = item.AvailableModules.FirstOrDefault(m => m.Id == item.ModuleId);
                            if (selectedModule != null)
                            {
                                item.ModuleTitle = selectedModule.Title;
                            }
                        }

                        SaveModuleItem_Click(button, e);
                        itemsGrid.Items.Refresh();
                    }
                }
            }
        }
        /// <summary>
        /// Запускает процесс сохранения элемента модуля при нажатии кнопки сохранить.
        /// Фиксирует изменения в таблице и уведомляет пользователя о том, что изменения будут сохранены.
        /// </summary>
        /// <param name="sender">Кнопка сохранения, которая была нажата.</param>
        /// <param name="e">Аргументы события.</param>
        private void SaveModuleItem_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button) || !(button.Tag is int itemId)) return;

            var dataGrid = FindParent<DataGrid>(button);
            dataGrid.CommitEdit(DataGridEditingUnit.Row, true);

            MessageBox.Show("Изменения будут сохранены при нажатии 'Сохранить все изменения'");
        }
        /// <summary>
        /// Обновляет данные для расширенного режима редактирования курса
        /// Загружает модули и элементы модулей, связывает их с интерфейсом
        /// </summary>
        /// <param name="courseId">ID курса, для которого загружаются данные</param>
        /// <param name="modulesGrid">Компонент DataGrid для отображения модулей</param>
        /// <param name="itemsGrid">Компонент DataGrid для отображения элементов модулей</param>
        private void RefreshAdvancedEditData(int courseId, DataGrid modulesGrid, DataGrid itemsGrid)
        {
            var modules = GetModulesByCourseId(courseId);
            modulesGrid.ItemsSource = modules;

            var allItems = new List<ModuleItemWithModule>();
            foreach (var module in modules)
            {
                var items = GetModuleItemsByModuleId(module.Id);
                allItems.AddRange(items.Select(i => new ModuleItemWithModule
                {
                    Id = i.Id,
                    ModuleId = i.ModuleId,
                    ModuleTitle = module.Title,
                    ItemType = i.ItemType,
                    Title = i.Title,
                    ContentData = i.ContentData,
                    ExternalUrl = i.ExternalUrl,
                    DurationMinutes = i.DurationMinutes,
                    OrderIndex = i.OrderIndex,
                    AvailableModules = modules
                }));
            }
            itemsGrid.ItemsSource = allItems;
        }
        /// <summary>
        /// Открывает окно для добавления нового элемента модуля в указанный модуль.
        /// Предоставляет варианты для разных типов элементов и загрузки контента.
        /// </summary>
        /// <param name="moduleId">ID модуля для добавления элемента.</param>
        /// <param name="parentItemsGrid">Сетка DataGrid для отображения элементов модуля, которую нужно обновить после добавления.</param>
        private void AddNewModuleItem(int moduleId, DataGrid parentItemsGrid)
        {
            var addItemWindow = new Window
            {
                Title = "Добавление элемента модуля",
                Width = 500,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Icon = new BitmapImage(new Uri("pack://application:,,,/logo2.png"))
            };

            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            var panel = new StackPanel { Margin = new Thickness(10) };
            scrollViewer.Content = panel;


            var typeLabel = new Label { Content = "Тип элемента:" };
            var typeCombo = new ComboBox
            {
                ItemsSource = new List<string> { "lecture", "video", "test" },
                SelectedIndex = 0,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var titleLabel = new Label { Content = "Название:" };
            var titleTextBox = new TextBox { Margin = new Thickness(0, 0, 0, 10) };

            var durationLabel = new Label { Content = "Длительность (мин):", Visibility = Visibility.Collapsed };
            var durationTextBox = new TextBox { Margin = new Thickness(0, 0, 0, 10), Visibility = Visibility.Collapsed };

            var fileLabel = new Label { Content = "Файл:", Margin = new Thickness(0, 0, 0, 5) };
            var fileTextBox = new TextBox { IsReadOnly = true, Margin = new Thickness(0, 0, 0, 10) };
            var browseButton = new Button { Content = "Обзор...", Width = 80, Margin = new Thickness(0, 0, 0, 10) };


            typeCombo.SelectionChanged += (s, e) =>
            {
                if (typeCombo.SelectedItem.ToString() == "video")
                {
                    durationLabel.Visibility = Visibility.Visible;
                    durationTextBox.Visibility = Visibility.Visible;
                }
                else
                {
                    durationLabel.Visibility = Visibility.Collapsed;
                    durationTextBox.Visibility = Visibility.Collapsed;
                }
            };


            browseButton.Click += (s, e) =>
            {
                if (typeCombo.SelectedItem.ToString() == "test")
                {
                    var testWindow = new Window
                    {
                        Title = "Создание теста",
                        Width = 600,
                        Height = 500,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Owner = addItemWindow,
                        Icon = new BitmapImage(new Uri("pack://application:,,,/logo2.png"))
                    };

                    var scrollViewerTest = new ScrollViewer
                    {
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                        HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
                    };

                    var testPanel = new StackPanel { Margin = new Thickness(10) };
                    scrollViewerTest.Content = testPanel;

                    var testTitleLabel = new Label { Content = "Название теста:" };
                    var testTitleTextBox = new TextBox { Margin = new Thickness(0, 0, 0, 10) };

                    var questionsLabel = new Label { Content = "Вопросы:", FontWeight = FontWeights.Bold };
                    var questionsStack = new StackPanel();

                    var addQuestionButton = new Button
                    {
                        Content = "Добавить вопрос",
                        Margin = new Thickness(0, 10, 0, 0),
                        HorizontalAlignment = HorizontalAlignment.Left
                    };

                    var saveTestButton = new Button
                    {
                        Content = "Сохранить тест",
                        Margin = new Thickness(0, 20, 0, 0),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Background = Brushes.LightGreen,
                        Padding = new Thickness(10, 5, 10, 5)
                    };

                    var questions = new ObservableCollection<EditTestQuestion>();

                    addQuestionButton.Click += (sender, args) =>
                    {
                        var question = new EditTestQuestion
                        {
                            Id = questions.Count + 1,
                            Text = "",
                            Type = "single",
                            Points = 1,
                            Answers = new ObservableCollection<EditTestAnswer>
                    {
                        new EditTestAnswer { Id = 1, Text = "", IsCorrect = false },
                        new EditTestAnswer { Id = 2, Text = "", IsCorrect = false }
                    }
                        };

                        questions.Add(question);
                        UpdateQuestionsUI(questionsStack, questions);
                    };

                    saveTestButton.Click += (sender, args) =>
                    {
                        if (string.IsNullOrWhiteSpace(testTitleTextBox.Text))
                        {
                            MessageBox.Show("Введите название теста");
                            return;
                        }

                        if (questions.Count == 0)
                        {
                            MessageBox.Show("Добавьте хотя бы один вопрос");
                            return;
                        }

                        foreach (var question in questions)
                        {
                            if (string.IsNullOrWhiteSpace(question.Text))
                            {
                                MessageBox.Show("Введите текст для всех вопросов");
                                return;
                            }

                            foreach (var answer in question.Answers)
                            {
                                if (string.IsNullOrWhiteSpace(answer.Text))
                                {
                                    MessageBox.Show("Введите текст для всех ответов");
                                    return;
                                }
                            }

                            if (question.Type != "text" && !question.Answers.Any(a => a.IsCorrect))
                            {
                                MessageBox.Show("Укажите хотя бы один правильный ответ для каждого вопроса");
                                return;
                            }
                        }

                        titleTextBox.Text = testTitleTextBox.Text;
                        fileTextBox.Text = "Тест создан";
                        testWindow.Close();
                    };

                    testPanel.Children.Add(testTitleLabel);
                    testPanel.Children.Add(testTitleTextBox);
                    testPanel.Children.Add(questionsLabel);
                    testPanel.Children.Add(questionsStack);
                    testPanel.Children.Add(addQuestionButton);
                    testPanel.Children.Add(saveTestButton);

                    testWindow.Content = scrollViewerTest;
                    testWindow.ShowDialog();
                    return;
                }

                var openFileDialog = new OpenFileDialog();
                switch (typeCombo.SelectedItem.ToString())
                {
                    case "lecture":
                        openFileDialog.Filter = "Текстовые файлы (*.txt)|*.txt";
                        break;
                    case "video":
                        openFileDialog.Filter = "Видео файлы (*.mp4, *.avi)|*.mp4;*.avi";
                        break;
                }

                if (openFileDialog.ShowDialog() == true)
                {
                    fileTextBox.Text = openFileDialog.FileName;
                }
            };

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            var okButton = new Button { Content = "Сохранить", Width = 100, Margin = new Thickness(5) };
            var cancelButton = new Button { Content = "Отмена", Width = 100, Margin = new Thickness(5) };

            okButton.Click += (s, args) =>
            {
                 if (string.IsNullOrWhiteSpace(titleTextBox.Text))
                    {
                        MessageBox.Show("Введите название элемента");
                        return;
                    }


                    if (typeCombo.SelectedItem.ToString() != "video" && !string.IsNullOrEmpty(durationTextBox.Text))
                    {
                        MessageBox.Show("Длительность можно устанавливать только для элементов типа 'video'");
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(titleTextBox.Text))
                {
                    MessageBox.Show("Введите название элемента");
                    return;
                }

                var existingItems = GetModuleItemsByModuleId(moduleId);
                if (existingItems.Any(i => i.Title.Equals(titleTextBox.Text, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show("Элемент с таким названием уже существует в этом модуле");
                    return;
                }

                try
                {
                    byte[] contentData = null;
                    string externalUrl = null;
                    int? duration = null;

                    if (typeCombo.SelectedItem.ToString() == "video")
                    {
                        if (!int.TryParse(durationTextBox.Text, out int dur))
                        {
                            MessageBox.Show("Введите корректную длительность (в минутах)");
                            return;
                        }
                        duration = dur;
                    }

                    if (!string.IsNullOrEmpty(fileTextBox.Text) && typeCombo.SelectedItem.ToString() != "test")
                    {
                        contentData = File.ReadAllBytes(fileTextBox.Text);
                    }

                    using (var connection = new SQLiteConnection(ConnectionString))
                    {
                        connection.Open();

                        string orderQuery = "SELECT COALESCE(MAX(order_index), 0) + 1 FROM module_items WHERE module_id = @moduleId";
                        int orderIndex;
                        using (var orderCommand = new SQLiteCommand(orderQuery, connection))
                        {
                            orderCommand.Parameters.AddWithValue("@moduleId", moduleId);
                            orderIndex = Convert.ToInt32(orderCommand.ExecuteScalar());
                        }

                        string query = @"INSERT INTO module_items 
                            (module_id, item_type, title, content_data, external_url, duration_minutes, order_index) 
                            VALUES (@moduleId, @itemType, @title, @contentData, @externalUrl, @duration, @orderIndex)";

                        using (var command = new SQLiteCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@moduleId", moduleId);
                            command.Parameters.AddWithValue("@itemType", typeCombo.SelectedItem.ToString());
                            command.Parameters.AddWithValue("@title", titleTextBox.Text);
                            command.Parameters.AddWithValue("@contentData", contentData ?? (object)DBNull.Value);
                            command.Parameters.AddWithValue("@externalUrl", externalUrl ?? (object)DBNull.Value);
                            command.Parameters.AddWithValue("@duration", duration ?? (object)DBNull.Value);
                            command.Parameters.AddWithValue("@orderIndex", orderIndex);

                            command.ExecuteNonQuery();

                            var module = GetModuleById(moduleId);
                            var modules = GetModulesByCourseId(module.CourseId);
                            var allItems = new List<ModuleItemWithModule>();
                            foreach (var mod in modules)
                            {
                                var items = GetModuleItemsByModuleId(mod.Id);
                                allItems.AddRange(items.Select(i => new ModuleItemWithModule
                                {
                                    Id = i.Id,
                                    ModuleId = i.ModuleId,
                                    ModuleTitle = mod.Title,
                                    ItemType = i.ItemType,
                                    Title = i.Title,
                                    ContentData = i.ContentData,
                                    ExternalUrl = i.ExternalUrl,
                                    DurationMinutes = i.DurationMinutes,
                                    OrderIndex = i.OrderIndex,
                                    AvailableModules = modules
                                }));
                            }
                            parentItemsGrid.ItemsSource = allItems;

                            addItemWindow.Close();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при добавлении элемента: {ex.Message}");
                }
            };

            cancelButton.Click += (s, args) => addItemWindow.Close();

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);

            panel.Children.Add(typeLabel);
            panel.Children.Add(typeCombo);
            panel.Children.Add(titleLabel);
            panel.Children.Add(titleTextBox);
            panel.Children.Add(durationLabel);
            panel.Children.Add(durationTextBox);
            panel.Children.Add(fileLabel);
            panel.Children.Add(fileTextBox);
            panel.Children.Add(browseButton);
            panel.Children.Add(buttonPanel);

            addItemWindow.Content = scrollViewer;
            addItemWindow.ShowDialog();
        }
        /// <summary>
        /// Открывает окно для добавления нового модуля в указанный курс.
        /// Обрабатывает валидацию и операции с базой данных для создания модуля.
        /// </summary>
        /// <param name="courseId">ID курса для добавления модуля.</param>
        /// <param name="modulesGrid">Сетка DataGrid для отображения модулей, которую нужно обновить после добавления.</param>
        private void AddNewModule(int courseId, DataGrid modulesGrid)
        {
            var addModuleWindow = new Window
            {
                Title = "Добавление нового модуля",
                Width = 400,
                Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Icon = new BitmapImage(new Uri("pack://application:,,,/logo2.png"))
            };

            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            var panel = new StackPanel { Margin = new Thickness(10) };
            scrollViewer.Content = panel;

            var titleLabel = new Label { Content = "Название модуля:" };
            var titleTextBox = new TextBox { Margin = new Thickness(0, 0, 0, 10) };

            var descLabel = new Label { Content = "Описание модуля:" };
            var descTextBox = new TextBox { Height = 100, Margin = new Thickness(0, 0, 0, 10), AcceptsReturn = true };

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            var okButton = new Button { Content = "Сохранить", Width = 100, Margin = new Thickness(5) };
            var cancelButton = new Button { Content = "Отмена", Width = 100, Margin = new Thickness(5) };

            okButton.Click += (s, args) =>
            {
                if (string.IsNullOrWhiteSpace(titleTextBox.Text))
                {
                    MessageBox.Show("Введите название модуля");
                    return;
                }

                var existingModules = GetModulesByCourseId(courseId);
                if (existingModules.Any(m => m.Title.Equals(titleTextBox.Text, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show("Модуль с таким названием уже существует в этом курсе");
                    return;
                }

                try
                {
                    using (var connection = new SQLiteConnection(ConnectionString))
                    {
                        connection.Open();

                        string orderQuery = "SELECT COALESCE(MAX(order_index), 0) + 1 FROM modules WHERE course_id = @courseId";
                        int orderIndex;
                        using (var orderCommand = new SQLiteCommand(orderQuery, connection))
                        {
                            orderCommand.Parameters.AddWithValue("@courseId", courseId);
                            orderIndex = Convert.ToInt32(orderCommand.ExecuteScalar());
                        }

                        string query = @"INSERT INTO modules (course_id, title, description, order_index) 
                            VALUES (@courseId, @title, @description, @orderIndex);
                            SELECT last_insert_rowid();";

                        using (var command = new SQLiteCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@courseId", courseId);
                            command.Parameters.AddWithValue("@title", titleTextBox.Text);
                            command.Parameters.AddWithValue("@description", descTextBox.Text);
                            command.Parameters.AddWithValue("@orderIndex", orderIndex);

                            int newId = Convert.ToInt32(command.ExecuteScalar());

                            var modules = GetModulesByCourseId(courseId);
                            modulesGrid.ItemsSource = modules;

                            var parentWindow = GetParentWindow(modulesGrid);
                            if (parentWindow != null)
                            {
                                var tabControl = FindVisualChild<TabControl>(parentWindow);
                                if (tabControl != null)
                                {
                                    var itemsTab = tabControl.Items[1] as TabItem;
                                    if (itemsTab != null)
                                    {
                                        var moduleCombo = FindVisualChild<ComboBox>(itemsTab.Content as DependencyObject);
                                        if (moduleCombo != null)
                                        {
                                            moduleCombo.ItemsSource = modules;
                                        }
                                    }
                                }
                            }

                            addModuleWindow.Close();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при добавлении модуля: {ex.Message}");
                }
            };

            cancelButton.Click += (s, args) => addModuleWindow.Close();

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);

            panel.Children.Add(titleLabel);
            panel.Children.Add(titleTextBox);
            panel.Children.Add(descLabel);
            panel.Children.Add(descTextBox);
            panel.Children.Add(buttonPanel);

            addModuleWindow.Content = scrollViewer;
            addModuleWindow.ShowDialog();
        }
        /// <summary>
        /// Обновляет интерфейс расширенного редактирования актуальными данными для указанного курса.
        /// Обновляет отображение как модулей, так и элементов модулей.
        /// </summary>
        /// <param name="courseId">ID курса для обновления данных.</param>
        private void RefreshAdvancedEditData(int courseId)
        {
            var modules = GetModulesByCourseId(courseId);
            modulesGrid.ItemsSource = modules;

            var allItems = new List<ModuleItemWithModule>();
            foreach (var module in modules)
            {
                var items = GetModuleItemsByModuleId(module.Id);
                allItems.AddRange(items.Select(i => new ModuleItemWithModule
                {
                    Id = i.Id,
                    ModuleId = i.ModuleId,
                    ModuleTitle = module.Title,
                    ItemType = i.ItemType,
                    Title = i.Title,
                    ContentData = i.ContentData,
                    ExternalUrl = i.ExternalUrl,
                    DurationMinutes = i.DurationMinutes,
                    OrderIndex = i.OrderIndex,
                    AvailableModules = modules
                }));
            }
            itemsGrid.ItemsSource = allItems;

            var parentWindow = GetParentWindow(itemsGrid);
            if (parentWindow != null)
            {
                var tabControl = FindVisualChild<TabControl>(parentWindow);
                if (tabControl != null)
                {
                    var itemsTab = tabControl.Items[1] as TabItem;
                    if (itemsTab != null)
                    {
                        var moduleCombo = FindVisualChild<ComboBox>(itemsTab.Content as DependencyObject);
                        if (moduleCombo != null)
                        {
                            moduleCombo.ItemsSource = modules;
                            if (modules.Any())
                            {
                                moduleCombo.SelectedIndex = 0;
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Обновляет UI вопросов в окне создания теста для отражения текущих данных вопросов и ответов.
        /// Управляет отображением и элементами взаимодействия для вопросов и ответов теста.
        /// </summary>
        /// <param name="questionsStack">StackPanel, содержащий UI вопросов.</param>
        /// <param name="questions">Коллекция вопросов для отображения.</param>
        private void UpdateQuestionsUI(StackPanel questionsStack, ObservableCollection<EditTestQuestion> questions)
        {
            questionsStack.Children.Clear();

            for (int i = 0; i < questions.Count; i++)
            {
                var question = questions[i];
                var questionPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 20) };

                var questionHeader = new StackPanel { Orientation = Orientation.Horizontal };
                var questionLabel = new Label { Content = $"Вопрос {i + 1}:", FontWeight = FontWeights.Bold };
                var deleteQuestionButton = new Button
                {
                    Content = "Удалить",
                    Margin = new Thickness(10, 0, 0, 0),
                    Tag = question.Id,
                    Foreground = Brushes.Red
                };

                deleteQuestionButton.Click += (sender, args) =>
                {
                    questions.Remove(question);
                    UpdateQuestionsUI(questionsStack, questions);
                };

                questionHeader.Children.Add(questionLabel);
                questionHeader.Children.Add(deleteQuestionButton);

                var questionTextBox = new TextBox
                {
                    Text = question.Text,
                    Margin = new Thickness(0, 0, 0, 5),
                    Tag = question.Id
                };
                questionTextBox.TextChanged += (sender, args) =>
                {
                    question.Text = questionTextBox.Text;
                };

                var typeCombo = new ComboBox
                {
                    ItemsSource = new List<string> { "single", "multiple", "text" },
                    SelectedItem = question.Type,
                    Margin = new Thickness(0, 0, 0, 5),
                    Tag = question.Id
                };
                typeCombo.SelectionChanged += (sender, args) =>
                {
                    question.Type = typeCombo.SelectedItem.ToString();
                };

                var pointsLabel = new Label { Content = "Баллы:" };
                var pointsTextBox = new TextBox
                {
                    Text = question.Points.ToString(),
                    Width = 50,
                    Margin = new Thickness(0, 0, 0, 5),
                    Tag = question.Id
                };
                pointsTextBox.TextChanged += (sender, args) =>
                {
                    if (int.TryParse(pointsTextBox.Text, out int points))
                    {
                        question.Points = points;
                    }
                };

                var answersStack = new StackPanel { Margin = new Thickness(20, 0, 0, 0) };
                var answersLabel = new Label { Content = "Ответы:" };

                var addAnswerButton = new Button
                {
                    Content = "Добавить ответ",
                    Margin = new Thickness(0, 5, 0, 5),
                    Tag = question.Id,
                    HorizontalAlignment = HorizontalAlignment.Left
                };

                addAnswerButton.Click += (sender, args) =>
                {
                    question.Answers.Add(new EditTestAnswer
                    {
                        Id = question.Answers.Max(a => a.Id) + 1,
                        Text = "",
                        IsCorrect = false
                    });
                    UpdateQuestionsUI(questionsStack, questions);
                };

                answersStack.Children.Add(answersLabel);

                for (int j = 0; j < question.Answers.Count; j++)
                {
                    var answer = question.Answers[j];
                    var answerPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 5) };

                    var isCorrectCheck = new CheckBox
                    {
                        IsChecked = answer.IsCorrect,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 5, 0),
                        Tag = new Tuple<int, int>(question.Id, answer.Id)
                    };
                    isCorrectCheck.Checked += (sender, args) =>
                    {
                        answer.IsCorrect = true;
                    };
                    isCorrectCheck.Unchecked += (sender, args) =>
                    {
                        answer.IsCorrect = false;
                    };

                    var answerTextBox = new TextBox
                    {
                        Text = answer.Text,
                        Width = 200,
                        Margin = new Thickness(0, 0, 5, 0),
                        Tag = new Tuple<int, int>(question.Id, answer.Id)
                    };
                    answerTextBox.TextChanged += (sender, args) =>
                    {
                        answer.Text = answerTextBox.Text;
                    };

                    var deleteAnswerButton = new Button
                    {
                        Content = "Удалить",
                        Foreground = Brushes.Red,
                        Tag = new Tuple<int, int>(question.Id, answer.Id)
                    };
                    deleteAnswerButton.Click += (sender, args) =>
                    {
                        question.Answers.Remove(answer);
                        UpdateQuestionsUI(questionsStack, questions);
                    };

                    answerPanel.Children.Add(isCorrectCheck);
                    answerPanel.Children.Add(answerTextBox);
                    answerPanel.Children.Add(deleteAnswerButton);
                    answersStack.Children.Add(answerPanel);
                }

                answersStack.Children.Add(addAnswerButton);

                questionPanel.Children.Add(questionHeader);
                questionPanel.Children.Add(questionTextBox);
                questionPanel.Children.Add(typeCombo);
                questionPanel.Children.Add(pointsLabel);
                questionPanel.Children.Add(pointsTextBox);
                questionPanel.Children.Add(answersStack);

                questionsStack.Children.Add(questionPanel);
            }
        }
        /// <summary>
        /// Представляет элемент модуля с дополнительной информацией о модуле, реализуя уведомления об изменении свойств.
        /// Расширяет базовые данные элемента модуля названием модуля и обработкой длительности для видео-элементов.
        /// </summary>
        public class ModuleItemWithModule : EditModuleItem, INotifyPropertyChanged
        {
            private string _moduleTitle;
            private int _moduleId;
            private int? _durationMinutes;

            public string ModuleTitle
            {
                get => _moduleTitle;
                set
                {
                    if (_moduleTitle != value)
                    {
                        _moduleTitle = value;
                        OnPropertyChanged();
                    }
                }
            }

            public int ModuleId
            {
                get => _moduleId;
                set
                {
                    if (_moduleId != value)
                    {
                        _moduleId = value;
                        var module = AvailableModules?.FirstOrDefault(m => m.Id == value);
                        if (module != null)
                        {
                            ModuleTitle = module.Title;
                        }
                        OnPropertyChanged();
                    }
                }
            }

            public new int? DurationMinutes
            {
                get => _durationMinutes;
                set
                {
                    if (ItemType != "video" && value.HasValue)
                    {
                        MessageBox.Show("Длительность можно устанавливать только для элементов типа 'video'");
                        return;
                    }
                    _durationMinutes = value;
                    OnPropertyChanged();
                }
            }

            public List<Module> AvailableModules { get; set; }

            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        /// <summary>
        /// Открывает окно редактирования выбранного модуля и применяет изменения при сохранении.
        /// Обновляет как список модулей, так и затронутые элементы модулей в UI.
        /// </summary>
        /// <param name="sender">Кнопка редактирования, которая была нажата.</param>
        /// <param name="e">Аргументы события.</param>
        private void EditModule_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button) || !(button.Tag is int moduleId)) return;

            var module = GetModuleById(moduleId);
            if (module == null) return;

            var editWindow = new Window
            {
                Title = "Редактирование модуля",
                Width = 400,
                Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Icon = new BitmapImage(new Uri("pack://application:,,,/logo2.png"))
            };

            var panel = new StackPanel { Margin = new Thickness(10) };

            var titleLabel = new Label { Content = "Название модуля:" };
            var titleTextBox = new TextBox { Text = module.Title, Margin = new Thickness(0, 0, 0, 10) };

            var descLabel = new Label { Content = "Описание модуля:" };
            var descTextBox = new TextBox { Text = module.Description, Height = 100, Margin = new Thickness(0, 0, 0, 10), AcceptsReturn = true };

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            var okButton = new Button { Content = "Сохранить", Width = 100, Margin = new Thickness(5) };
            var cancelButton = new Button { Content = "Отмена", Width = 100, Margin = new Thickness(5) };

            okButton.Click += (s, args) =>
            {
                if (string.IsNullOrWhiteSpace(titleTextBox.Text))
                {
                    MessageBox.Show("Введите название модуля");
                    return;
                }

                try
                {
                    module.Title = titleTextBox.Text;
                    module.Description = descTextBox.Text;

                    SaveModule(module);

                    editWindow.Close();

                    modulesGrid.Items.Refresh();

                    if (itemsGrid != null && itemsGrid.ItemsSource is IEnumerable<ModuleItemWithModule> items)
                    {
                        foreach (var item in items.Where(i => i.ModuleId == module.Id))
                        {
                            item.ModuleTitle = module.Title;
                        }
                        itemsGrid.Items.Refresh();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при обновлении модуля: {ex.Message}");
                }
            };

            cancelButton.Click += (s, args) => editWindow.Close();

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);

            panel.Children.Add(titleLabel);
            panel.Children.Add(titleTextBox);
            panel.Children.Add(descLabel);
            panel.Children.Add(descTextBox);
            panel.Children.Add(buttonPanel);

            editWindow.Content = panel;
            editWindow.ShowDialog();
        }
        /// <summary>
        /// Получает модуль из базы данных по его уникальному идентификатору.
        /// </summary>
        /// <param name="moduleId">ID модуля для получения.</param>
        /// <returns>Объект Module, если найден, иначе null.</returns>
        private Module GetModuleById(int moduleId)
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                string query = "SELECT id, course_id, title, description, order_index FROM modules WHERE id = @moduleId";

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@moduleId", moduleId);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Module
                            {
                                Id = reader.GetInt32(0),
                                CourseId = reader.GetInt32(1),
                                Title = reader.GetString(2),
                                Description = !reader.IsDBNull(3) ? reader.GetString(3) : "",
                                OrderIndex = reader.GetInt32(4)
                            };
                        }
                    }
                }
            }
            return null;
        }
        /// <summary>
        /// Удаляет выбранный модуль после подтверждения, включая все связанные элементы модуля.
        /// Обновляет UI после удаления и поддерживает целостность базы данных.
        /// </summary>
        /// <param name="sender">Кнопка удаления, которая была нажата.</param>
        /// <param name="e">Аргументы события.</param>
        private void DeleteModule_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button) || !(button.Tag is int moduleId)) return;

            var module = GetModuleById(moduleId);
            if (module == null) return;

            var result = MessageBox.Show(
                "Вы уверены, что хотите удалить этот модуль и все его элементы?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var connection = new SQLiteConnection(ConnectionString))
                    {
                        connection.Open();
                        using (var transaction = connection.BeginTransaction())
                        {
                            try
                            {
                                
                                string deleteItemsQuery = "DELETE FROM module_items WHERE module_id = @moduleId";
                                using (var command = new SQLiteCommand(deleteItemsQuery, connection, transaction))
                                {
                                    command.Parameters.AddWithValue("@moduleId", moduleId);
                                    command.ExecuteNonQuery();
                                }

                                string deleteModuleQuery = "DELETE FROM modules WHERE id = @moduleId";
                                using (var command = new SQLiteCommand(deleteModuleQuery, connection, transaction))
                                {
                                    command.Parameters.AddWithValue("@moduleId", moduleId);
                                    command.ExecuteNonQuery();
                                }

                                transaction.Commit();

                                var parentWindow = GetParentWindow(button);
                                if (parentWindow != null)
                                {
                                    var dataGrid = FindVisualChild<DataGrid>(parentWindow);
                                    if (dataGrid != null)
                                    {
                                        dataGrid.ItemsSource = GetModulesByCourseId(module.CourseId);

                                        var tabControl = FindVisualChild<TabControl>(parentWindow);
                                        if (tabControl != null)
                                        {
                                            var itemsTab = tabControl.Items[1] as TabItem;
                                            if (itemsTab != null)
                                            {
                                                var itemsGrid = FindVisualChild<DataGrid>(itemsTab.Content as DependencyObject);
                                                if (itemsGrid != null)
                                                {
                                                    var allItems = new List<ModuleItemWithModule>();
                                                    var modules = GetModulesByCourseId(module.CourseId);
                                                    foreach (var mod in modules)
                                                    {
                                                        var items = GetModuleItemsByModuleId(mod.Id);
                                                        allItems.AddRange(items.Select(i => new ModuleItemWithModule
                                                        {
                                                            Id = i.Id,
                                                            ModuleId = i.ModuleId,
                                                            ModuleTitle = mod.Title,
                                                            ItemType = i.ItemType,
                                                            Title = i.Title,
                                                            ContentData = i.ContentData,
                                                            ExternalUrl = i.ExternalUrl,
                                                            DurationMinutes = i.DurationMinutes,
                                                            OrderIndex = i.OrderIndex
                                                        }));
                                                    }
                                                    itemsGrid.ItemsSource = allItems;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                MessageBox.Show($"Ошибка при удалении модуля: {ex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка подключения: {ex.Message}");
                }
            }
        }


        /// <summary>
        /// Открывает окно редактирования выбранного элемента модуля и применяет изменения при сохранении.
        /// Обновляет список элементов модуля в UI после успешного редактирования.
        /// </summary>
        /// <param name="sender">Кнопка редактирования, которая была нажата.</param>
        /// <param name="e">Аргументы события.</param>
        private void EditModuleItem_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button) || !(button.Tag is int itemId)) return;

            var item = GetModuleItemById(itemId);
            if (item == null) return;

            var editWindow = new Window
            {
                Title = "Редактирование элемента модуля",
                Width = 500,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            var panel = new StackPanel { Margin = new Thickness(10) };


            var typeLabel = new Label { Content = "Тип элемента:" };
            var typeCombo = new ComboBox
            {
                ItemsSource = new List<string> { "lecture", "video", "test" },
                SelectedItem = item.ItemType,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var titleLabel = new Label { Content = "Название:" };
            var titleTextBox = new TextBox { Text = item.Title, Margin = new Thickness(0, 0, 0, 10) };

            var durationLabel = new Label
            {
                Content = "Длительность (мин):",
                Visibility = item.ItemType == "video" ? Visibility.Visible : Visibility.Collapsed
            };
            var durationTextBox = new TextBox
            {
                Text = item.DurationMinutes?.ToString(),
                Margin = new Thickness(0, 0, 0, 10),
                Visibility = item.ItemType == "video" ? Visibility.Visible : Visibility.Collapsed
            };

            var fileLabel = new Label { Content = "Файл:", Margin = new Thickness(0, 0, 0, 5) };
            var fileTextBox = new TextBox
            {
                Text = item.ContentData != null ? "[Данные загружены]" : "",
                IsReadOnly = true,
                Margin = new Thickness(0, 0, 0, 10)
            };
            var browseButton = new Button { Content = "Обзор...", Width = 80, Margin = new Thickness(0, 0, 0, 10) };

            typeCombo.SelectionChanged += (s, arg) =>
            {
                if (typeCombo.SelectedItem.ToString() == "video")
                {
                    durationLabel.Visibility = Visibility.Visible;
                    durationTextBox.Visibility = Visibility.Visible;
                }
                else
                {
                    durationLabel.Visibility = Visibility.Collapsed;
                    durationTextBox.Visibility = Visibility.Collapsed;
                }
            };

            browseButton.Click += (s, arg) =>
            {
                if (typeCombo.SelectedItem.ToString() == "test")
                {
                    var testWindow = new CreateTestWindow();
                    if (testWindow.ShowDialog() == true)
                    {
                        var testData = testWindow.GetTestData();
                        fileTextBox.Text = "Тест обновлен";
                    }
                    return;
                }

                var openFileDialog = new OpenFileDialog();
                switch (typeCombo.SelectedItem.ToString())
                {
                    case "lecture":
                        openFileDialog.Filter = "Текстовые файлы (*.txt)|*.txt";
                        break;
                    case "video":
                        openFileDialog.Filter = "Видео файлы (*.mp4, *.avi)|*.mp4;*.avi";
                        break;
                }

                if (openFileDialog.ShowDialog() == true)
                {
                    fileTextBox.Text = openFileDialog.FileName;
                }
            };

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            var okButton = new Button { Content = "Сохранить", Width = 100, Margin = new Thickness(5) };
            var cancelButton = new Button { Content = "Отмена", Width = 100, Margin = new Thickness(5) };

            okButton.Click += (s, args) =>
            {
                if (string.IsNullOrWhiteSpace(titleTextBox.Text))
                {
                    MessageBox.Show("Введите название элемента");
                    return;
                }

                try
                {
                    byte[] contentData = item.ContentData; 
                    string externalUrl = item.ExternalUrl;

                    if (!string.IsNullOrEmpty(fileTextBox.Text) && typeCombo.SelectedItem.ToString() != "test")
                    {
                        contentData = File.ReadAllBytes(fileTextBox.Text);
                    }

                    using (var connection = new SQLiteConnection(ConnectionString))
                    {
                        connection.Open();
                        string query = @"UPDATE module_items SET 
                        item_type = @itemType, 
                        title = @title, 
                        content_data = @contentData,
                        external_url = @externalUrl,
                        duration_minutes = @duration
                        WHERE id = @itemId";

                        using (var command = new SQLiteCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@itemId", itemId);
                            command.Parameters.AddWithValue("@itemType", typeCombo.SelectedItem.ToString());
                            command.Parameters.AddWithValue("@title", titleTextBox.Text);
                            command.Parameters.AddWithValue("@contentData", contentData ?? (object)DBNull.Value);
                            command.Parameters.AddWithValue("@externalUrl", externalUrl ?? (object)DBNull.Value);
                            command.Parameters.AddWithValue("@duration",
                                int.TryParse(durationTextBox.Text, out int duration) ? duration : (object)DBNull.Value);

                            command.ExecuteNonQuery();

                            var parentWindow = GetParentWindow(button);
                            if (parentWindow != null)
                            {
                                var tabControl = FindVisualChild<TabControl>(parentWindow);
                                if (tabControl != null)
                                {
                                    var itemsTab = tabControl.Items[1] as TabItem;
                                    if (itemsTab != null)
                                    {
                                        var itemsGrid = FindVisualChild<DataGrid>(itemsTab.Content as DependencyObject);
                                        if (itemsGrid != null)
                                        {
                                            var module = GetModuleById(item.ModuleId);
                                            var allItems = new List<ModuleItemWithModule>();
                                            var modules = GetModulesByCourseId(module.CourseId);
                                            foreach (var mod in modules)
                                            {
                                                var items = GetModuleItemsByModuleId(mod.Id);
                                                allItems.AddRange(items.Select(i => new ModuleItemWithModule
                                                {
                                                    Id = i.Id,
                                                    ModuleId = i.ModuleId,
                                                    ModuleTitle = mod.Title,
                                                    ItemType = i.ItemType,
                                                    Title = i.Title,
                                                    ContentData = i.ContentData,
                                                    ExternalUrl = i.ExternalUrl,
                                                    DurationMinutes = i.DurationMinutes,
                                                    OrderIndex = i.OrderIndex
                                                }));
                                            }
                                            itemsGrid.ItemsSource = allItems;
                                        }
                                    }
                                }
                            }
                            editWindow.Close();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при обновлении элемента: {ex.Message}");
                }
            };

            cancelButton.Click += (s, args) => editWindow.Close();

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);

            panel.Children.Add(typeLabel);
            panel.Children.Add(typeCombo);
            panel.Children.Add(titleLabel);
            panel.Children.Add(titleTextBox);
            panel.Children.Add(durationLabel);
            panel.Children.Add(durationTextBox);
            panel.Children.Add(fileLabel);
            panel.Children.Add(fileTextBox);
            panel.Children.Add(browseButton);
            panel.Children.Add(buttonPanel);

            editWindow.Content = panel;
            editWindow.ShowDialog();
        }
        /// <summary>
        /// Получает элемент модуля из базы данных по его уникальному идентификатору.
        /// </summary>
        /// <param name="itemId">ID элемента модуля для получения.</param>
        /// <returns>Объект EditModuleItem, если найден, иначе null.</returns>
        private EditModuleItem GetModuleItemById(int itemId)
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                string query = @"SELECT id, module_id, item_type, title, content_data, external_url, duration_minutes, order_index 
                FROM module_items WHERE id = @itemId";

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@itemId", itemId);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new EditModuleItem
                            {
                                Id = reader.GetInt32(0),
                                ModuleId = reader.GetInt32(1),
                                ItemType = reader.GetString(2),
                                Title = reader.GetString(3),
                                ContentData = !reader.IsDBNull(4) ? (byte[])reader["content_data"] : null,
                                ExternalUrl = !reader.IsDBNull(5) ? reader.GetString(5) : null,
                                DurationMinutes = !reader.IsDBNull(6) ? reader.GetInt32(6) : (int?)null,
                                OrderIndex = reader.GetInt32(7)
                            };
                        }
                    }
                }
            }
            return null;
        }
        /// <summary>
        /// Удаляет выбранный элемент модуля после подтверждения.
        /// Обновляет список элементов модуля в UI после успешного удаления.
        /// </summary>
        /// <param name="sender">Кнопка удаления, которая была нажата.</param>
        /// <param name="e">Аргументы события.</param>
        private void DeleteModuleItem_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button) || !(button.Tag is int itemId)) return;

            var item = GetModuleItemById(itemId);
            if (item == null) return;

            var result = MessageBox.Show(
                "Вы уверены, что хотите удалить этот элемент модуля?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var connection = new SQLiteConnection(ConnectionString))
                    {
                        connection.Open();
                        string query = "DELETE FROM module_items WHERE id = @itemId";

                        using (var command = new SQLiteCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@itemId", itemId);
                            command.ExecuteNonQuery();

                            var parentWindow = GetParentWindow(button);
                            if (parentWindow != null)
                            {
                                var tabControl = FindVisualChild<TabControl>(parentWindow);
                                if (tabControl != null)
                                {
                                    var itemsTab = tabControl.Items[1] as TabItem;
                                    if (itemsTab != null)
                                    {
                                        var itemsGrid = FindVisualChild<DataGrid>(itemsTab.Content as DependencyObject);
                                        if (itemsGrid != null)
                                        {
                                            var module = GetModuleById(item.ModuleId);
                                            var allItems = new List<ModuleItemWithModule>();
                                            var modules = GetModulesByCourseId(module.CourseId);
                                            foreach (var mod in modules)
                                            {
                                                var items = GetModuleItemsByModuleId(mod.Id);
                                                allItems.AddRange(items.Select(i => new ModuleItemWithModule
                                                {
                                                    Id = i.Id,
                                                    ModuleId = i.ModuleId,
                                                    ModuleTitle = mod.Title,
                                                    ItemType = i.ItemType,
                                                    Title = i.Title,
                                                    ContentData = i.ContentData,
                                                    ExternalUrl = i.ExternalUrl,
                                                    DurationMinutes = i.DurationMinutes,
                                                    OrderIndex = i.OrderIndex
                                                }));
                                            }
                                            itemsGrid.ItemsSource = allItems;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении элемента: {ex.Message}");
                }
            }
        }
        /// <summary>
        /// Находит родительское окно для элемента в визуальном дереве.
        /// </summary>
        /// <param name="child">Дочерний элемент, с которого начинается поиск.</param>
        /// <returns>Родительское окно, если найдено, иначе null.</returns>
        private Window GetParentWindow(DependencyObject child)
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;
            Window parent = parentObject as Window;
            return parent ?? GetParentWindow(parentObject);
        }
        /// <summary>
        /// Находит визуальный дочерний элемент указанного типа и необязательного имени в родительском элементе.
        /// </summary>
        /// <typeparam name="T">Тип искомого дочернего элемента.</typeparam>
        /// <param name="parent">Родительский элемент для поиска.</param>
        /// <param name="childName">Необязательное имя искомого дочернего элемента.</param>
        /// <returns>Найденный дочерний элемент или null, если не найден.</returns>
        private T FindVisualChild<T>(DependencyObject parent, string childName = null) where T : DependencyObject
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                var result = (child as T) ?? FindVisualChild<T>(child, childName);

                if (result != null && (childName == null || (result is FrameworkElement fe && fe.Name == childName)))
                {
                    return result;
                }
            }
            return null;
        }
        /// <summary>
        /// Представляет курс с уведомлениями об изменении свойств и возможностями валидации данных.
        /// Включает информацию о специальности курса, доступности и партнерских отношениях.
        /// </summary>
        public class Course : INotifyPropertyChanged, IDataErrorInfo
        {
            private int _id;
            private string _name;
            private string _description;
            private int _specialityId;
            private string _specialityName;
            private int _availabilityId;
            private string _availabilityName;
            private DateTime? _availableUntil;
            private int _partnerId;
            private string _partnerName;

            public int Id
            {
                get => _id;
                set { _id = value; OnPropertyChanged(); }
            }

            public string Name
            {
                get => _name;
                set { _name = value; OnPropertyChanged(); }
            }

            public string Description
            {
                get => _description;
                set { _description = value; OnPropertyChanged(); }
            }

            public int SpecialityId
            {
                get => _specialityId;
                set { _specialityId = value; OnPropertyChanged(); }
            }

            public string SpecialityName
            {
                get => _specialityName;
                set { _specialityName = value; OnPropertyChanged(); }
            }

            public int AvailabilityId
            {
                get => _availabilityId;
                set { _availabilityId = value; OnPropertyChanged(); }
            }

            public string AvailabilityName
            {
                get => _availabilityName;
                set { _availabilityName = value; OnPropertyChanged(); }
            }


            public DateTime? AvailableUntil
            {
                get => _availableUntil;
                set
                {
                    if (_availableUntil != value)
                    {
                        _availableUntil = value;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(AvailableUntilFormatted));
                    }
                }
            }

            public string AvailableUntilFormatted =>
                AvailableUntil?.ToString("dd.MM.yyyy") ?? "Не указано";

            public int PartnerId
            {
                get => _partnerId;
                set { _partnerId = value; OnPropertyChanged(); }
            }

            public string PartnerName
            {
                get => _partnerName;
                set { _partnerName = value; OnPropertyChanged(); }
            }

            public List<dynamic> Specialities { get; set; }
            public List<dynamic> Partners { get; set; }
            public List<dynamic> Availabilities { get; set; } = new List<dynamic>
    {
        new { Id = 1, Name = "Доступен" },
        new { Id = 0, Name = "Не доступен" }
    };

            public void UpdateSpeciality(dynamic selected)
            {
                SpecialityId = selected.Id;
                SpecialityName = selected.Name;
                OnPropertyChanged(nameof(SpecialityId));
                OnPropertyChanged(nameof(SpecialityName));
            }

            public void UpdateAvailability(dynamic selected)
            {
                AvailabilityId = selected.Id;
                AvailabilityName = selected.Name;
                OnPropertyChanged(nameof(AvailabilityId));
                OnPropertyChanged(nameof(AvailabilityName));
            }

            public void UpdatePartner(dynamic selected)
            {
                PartnerId = selected.Id;
                PartnerName = selected.Name;
                OnPropertyChanged(nameof(PartnerId));
                OnPropertyChanged(nameof(PartnerName));
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            public string Error => null;

            public string this[string columnName]
            {
                get
                {
                    if (columnName == "AvailableUntil" && AvailableUntil.HasValue && AvailableUntil < DateTime.Today)
                    {
                        return "Дата окончания не может быть в прошлом";
                    }
                    return null;
                }
            }
        }
        /// <summary>
        /// Представляет партнерскую организацию с ID и названием.
        /// </summary>
        public class Partner
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        /// <summary>
        /// Представляет модуль курса с основной информацией и индексом порядка.
        /// </summary>
        public class Module
        {
            public int Id { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public int OrderIndex { get; set; }
            public int CourseId { get; set; }
        }
        /// <summary>
        /// Представляет элемент внутри модуля курса с контентом и метаданными, специфичными для типа.
        /// </summary>
        public class ModuleItem
        {
            public int Id { get; set; }
            public int ModuleId { get; set; }
            public string ItemType { get; set; }
            public string Title { get; set; }
            public string ContentPath { get; set; }
            public string ExternalUrl { get; set; }
            public int? DurationMinutes { get; set; }
            public int OrderIndex { get; set; }
        }
        /// <summary>
        /// Получает все курсы из базы данных с информацией о связанных специальностях и партнерах.
        /// </summary>
        /// <returns>Список объектов Course, заполненных данными из базы данных.</returns>
        private List<Course> GetAllCourses()
        {
            var courses = new List<Course>();

            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                string query = @"SELECT c.id, c.title, c.description, 
               c.speciality_id, s.name as speciality_name, 
               c.availability, c.available_until,
               c.partnership as partner_id, p.name as partner_name
               FROM courses c
               LEFT JOIN specialities s ON c.speciality_id = s.id
               LEFT JOIN partners p ON c.partnership = p.id";

                using (var command = new SQLiteCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            courses.Add(new Course
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                Description = !reader.IsDBNull(2) ? reader.GetString(2) : "",
                                SpecialityId = reader.GetInt32(3),
                                SpecialityName = !reader.IsDBNull(4) ? reader.GetString(4) : "",
                                AvailabilityId = reader.IsDBNull(5) ? 0 : (reader.GetBoolean(5) ? 1 : 0),
                                AvailabilityName = reader.IsDBNull(5) ? "Не указано" : (reader.GetBoolean(5) ? "Доступен" : "Не доступен"),
                                AvailableUntil = !reader.IsDBNull(6) ? reader.GetDateTime(6) : (DateTime?)null,
                                PartnerId = reader.IsDBNull(7) ? 0 : reader.GetInt32(7),
                                PartnerName = !reader.IsDBNull(8) ? reader.GetString(8) : "",
                                Specialities = GetSpecialities(),
                                Partners = GetPartners()
                            });
                        }
                    }
                }
            }
            return courses;
        }
        public List<dynamic> Availabilities { get; set; } = new List<dynamic>
{
    new { Id = 1, Name = "Доступен" },
    new { Id = 0, Name = "Не доступен" }
};
        /// <summary>
        /// Получает модули для конкретного курса из базы данных, упорядоченные по их индексу.
        /// </summary>
        /// <param name="courseId">ID курса для получения модулей.</param>
        /// <returns>Список объектов Module, принадлежащих указанному курсу.</returns>
        private List<Module> GetModulesByCourseId(int courseId)
        {
            var modules = new List<Module>();

            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                string query = "SELECT id, title, description, order_index FROM modules WHERE course_id = @courseId ORDER BY order_index";

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@courseId", courseId);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            modules.Add(new Module
                            {
                                Id = reader.GetInt32(0),
                                Title = reader.GetString(1),
                                Description = !reader.IsDBNull(2) ? reader.GetString(2) : "",
                                OrderIndex = reader.GetInt32(3),
                                CourseId = courseId
                            });
                        }
                    }
                }
            }

            return modules;
        }
        /// <summary>
        /// Получает элементы модуля для конкретного модуля из базы данных, упорядоченные по их индексу.
        /// </summary>
        /// <param name="moduleId">ID модуля для получения элементов.</param>
        /// <returns>Список объектов EditModuleItem, принадлежащих указанному модулю.</returns>
        private List<EditModuleItem> GetModuleItemsByModuleId(int moduleId)
        {
            var items = new List<EditModuleItem>();

            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                string query = @"SELECT id, module_id, item_type, title, 
                content_data, external_url, duration_minutes, order_index 
                FROM module_items 
                WHERE module_id = @moduleId 
                ORDER BY order_index";

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@moduleId", moduleId);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            items.Add(new EditModuleItem
                            {
                                Id = reader.GetInt32(0),
                                ModuleId = reader.GetInt32(1),
                                ItemType = reader.GetString(2),
                                Title = reader.GetString(3),
                                ContentData = !reader.IsDBNull(4) ? (byte[])reader["content_data"] : null,
                                ExternalUrl = !reader.IsDBNull(5) ? reader.GetString(5) : null,
                                DurationMinutes = !reader.IsDBNull(6) ? reader.GetInt32(6) : (int?)null,
                                OrderIndex = reader.GetInt32(7)
                            });
                        }
                    }
                }
            }

            return items;
        }
        /// <summary>
        /// Сохраняет данные контента во временный файл и возвращает путь к файлу.
        /// </summary>
        /// <param name="blobData">Бинарные данные контента для сохранения.</param>
        /// <returns>Путь к временному файлу или null, если сохранение не удалось.</returns>
        private string SaveContentToTempFile(object blobData)
        {
            if (blobData == null || blobData == DBNull.Value)
                return null;

            try
            {
                byte[] data = (byte[])blobData;
                string tempPath = Path.GetTempFileName();
                File.WriteAllBytes(tempPath, data);
                return tempPath;
            }
            catch
            {
                return null;
            }
        }
        /// <summary>
        /// Загружает данные пользователей из базы данных и заполняет сетки подтвержденных и неподтвержденных пользователей.
        /// Разделяет пользователей по статусу подтверждения для административного просмотра.
        /// </summary>
        private void LoadUsers()
        {
            try
            {
                var confirmedUsers = new List<User>();
                var unconfirmedUsers = new List<User>();

                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();

                    string confirmedQuery = @"SELECT u.id, u.login, u.name, s.name as speciality_name, u.confirmation, u.mail 
                                   FROM users u
                                   LEFT JOIN specialities s ON u.speciality = s.id
                                   WHERE u.confirmation = 1";

                    using (var command = new SQLiteCommand(confirmedQuery, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                confirmedUsers.Add(new User
                                {
                                    Id = reader.GetInt32(0),
                                    Login = reader.GetString(1),
                                    Name = reader.GetString(2),
                                    Speciality = !reader.IsDBNull(3) ? reader.GetString(3) : string.Empty,
                                    Confirmation = reader.GetInt32(4),
                                    Email = !reader.IsDBNull(5) ? reader.GetString(5) : string.Empty
                                });
                            }
                        }
                    }
                    string unconfirmedQuery = @"SELECT u.id, u.login, u.name, s.name as speciality_name, u.confirmation, u.mail 
                                     FROM users u
                                     LEFT JOIN specialities s ON u.speciality = s.id
                                     WHERE u.confirmation = 0";

                    using (var command = new SQLiteCommand(unconfirmedQuery, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                unconfirmedUsers.Add(new User
                                {
                                    Id = reader.GetInt32(0),
                                    Login = reader.GetString(1),
                                    Name = reader.GetString(2),
                                    Speciality = !reader.IsDBNull(3) ? reader.GetString(3) : string.Empty,
                                    Confirmation = reader.GetInt32(4),
                                    Email = !reader.IsDBNull(5) ? reader.GetString(5) : string.Empty
                                });
                            }
                        }
                    }
                }

                gridConfirmed.ItemsSource = confirmedUsers;
                gridUnconfirmed.ItemsSource = unconfirmedUsers;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке пользователей: {ex.Message}");
            }
        }
        /// <summary>
        /// Подтверждает регистрацию пользователя, обновляя его статус в базе данных и отправляя email-уведомление.
        /// </summary>
        /// <param name="sender">Кнопка подтверждения, которая была нажата.</param>
        /// <param name="e">Аргументы события.</param>
        private void ConfirmUser_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button) || !(button.Tag is int userId)) return;

            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    string query = "UPDATE users SET confirmation = 1 WHERE id = @userId";

                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@userId", userId);
                        command.ExecuteNonQuery();
                    }
                }

                string userEmail = "";
                string userName = "";
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    string query = "SELECT mail, name FROM users WHERE id = @userId";

                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@userId", userId);
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                userEmail = reader.GetString(0);
                                userName = reader.GetString(1);
                            }
                        }
                    }
                }

                if (!string.IsNullOrEmpty(userEmail))
                {
                    SendUserNotification(userEmail, userName, true);
                }

                MessageBox.Show("Пользователь успешно подтверждён!");
                LoadUsers();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при подтверждении пользователя: {ex.Message}");
            }
        }

        /// <summary>
        /// Отклоняет регистрацию пользователя, удаляя его запись из базы данных и отправляя email-уведомление.
        /// </summary>
        /// <param name="sender">Кнопка отклонения, которая была нажата.</param>
        /// <param name="e">Аргументы события.</param>
        private void RejectUser_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button) || !(button.Tag is int userId)) return;

            var result = MessageBox.Show(
                "Вы уверены, что хотите отклонить этого пользователя? Его учетная запись будет удалена.",
                "Подтверждение отклонения",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    string userEmail = "";
                    string userName = "";
                    using (var connection = new SQLiteConnection(ConnectionString))
                    {
                        connection.Open();
                        string query = "SELECT mail, name FROM users WHERE id = @userId";

                        using (var command = new SQLiteCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@userId", userId);
                            using (var reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    userEmail = reader.GetString(0);
                                    userName = reader.GetString(1);
                                }
                            }
                        }
                    }

                    using (var connection = new SQLiteConnection(ConnectionString))
                    {
                        connection.Open();
                        string query = "DELETE FROM users WHERE id = @userId";

                        using (var command = new SQLiteCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@userId", userId);
                            command.ExecuteNonQuery();
                        }
                    }

                    if (!string.IsNullOrEmpty(userEmail))
                    {
                        SendUserNotification(userEmail, userName, false);
                    }

                    MessageBox.Show("Пользователь отклонён и удалён из системы.");
                    LoadUsers();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при отклонении пользователя: {ex.Message}");
                }
            }
        }
        /// <summary>
        /// Отправляет email-уведомление пользователю о статусе его регистрации (подтверждение или отклонение).
        /// </summary>
        /// <param name="email">Email-адрес получателя.</param>
        /// <param name="userName">Имя пользователя, которому отправляется уведомление.</param>
        /// <param name="isApproved">Флаг, указывающий, была ли регистрация подтверждена или отклонена.</param>
        private void SendUserNotification(string email, string userName, bool isApproved)
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

                    MailMessage mailMessage = new MailMessage();
                    mailMessage.From = new MailAddress(smtpUsername);
                    mailMessage.To.Add(email);

                    if (isApproved)
                    {
                        mailMessage.Subject = "Ваша учетная запись подтверждена";
                        mailMessage.Body = $"Уважаемый(ая) {userName},\n\n" +
                                         "Ваша учетная запись была успешно подтверждена администратором.\n\n" +
                                         "Теперь вы можете войти в систему и начать обучение.\n\n" +
                                         "С уважением,\nАдминистрация системы";
                    }
                    else
                    {
                        mailMessage.Subject = "Ваша регистрация отклонена";
                        mailMessage.Body = $"Уважаемый(ая) {userName},\n\n" +
                                         "К сожалению, ваша регистрация была отклонена администратором.\n\n" +
                                         "Если вы считаете, что это произошло по ошибке, пожалуйста, свяжитесь с поддержкой.\n\n" +
                                         "С уважением,\nАдминистрация системы";
                    }

                    smtpClient.Send(mailMessage);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отправке уведомления пользователю: {ex.Message}\n\n" +
                              "Операция выполнена, но уведомление не было отправлено.",
                              "Ошибка отправки", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        /// <summary>
        /// Получает пользователей из базы данных, отфильтрованных по статусу подтверждения.
        /// </summary>
        /// <param name="confirmationStatus">Статус подтверждения для фильтрации (0 - неподтвержденные, 1 - подтвержденные).</param>
        /// <returns>Список объектов User, соответствующих указанному статусу подтверждения.</returns>
        private List<User> GetUsersByConfirmation(int confirmationStatus)
        {
            var users = new List<User>();

            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                string query = @"SELECT u.id, u.login, u.name, s.name as speciality_name, u.confirmation 
                        FROM users u
                        LEFT JOIN specialities s ON u.speciality = s.id
                        WHERE u.confirmation = @confirmation";

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@confirmation", confirmationStatus);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            users.Add(new User
                            {
                                Id = reader.GetInt32(0),
                                Login = reader.GetString(1),
                                Name = reader.GetString(2),
                                Speciality = reader.GetString(3), 
                                Confirmation = reader.GetInt32(4)
                            });
                        }
                    }
                }
            }

            return users;
        }

        /// <summary>
        /// Обновляет статус подтверждения пользователя в базе данных.
        /// </summary>
        /// <param name="userId">ID пользователя для обновления.</param>
        /// <param name="newConfirmation">Новое значение статуса подтверждения.</param>
        private void UpdateUserConfirmation(int userId, int newConfirmation)
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                string query = "UPDATE users SET confirmation = @newConfirmation WHERE id = @userId";

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@newConfirmation", newConfirmation);
                    command.Parameters.AddWithValue("@userId", userId);
                    command.ExecuteNonQuery();
                }
            }
        }
        /// <summary>
        /// Удаляет пользователя из базы данных после подтверждения.
        /// </summary>
        /// <param name="sender">Кнопка удаления, которая была нажата.</param>
        /// <param name="e">Аргументы события.</param>
        private void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int userId)
            {
                var result = MessageBox.Show(
                    "Вы уверены, что хотите удалить этого пользователя?",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        DeleteUserFromDatabase(userId);
                        LoadUsers();
                        MessageBox.Show("Пользователь удалён!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
        /// <summary>
        /// Постоянно удаляет запись пользователя из базы данных.
        /// </summary>
        /// <param name="userId">ID пользователя для удаления.</param>
        private void DeleteUserFromDatabase(int userId)
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                string query = "DELETE FROM users WHERE id = @userId";

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@userId", userId);
                    command.ExecuteNonQuery();
                }
            }
        }
        /// <summary>
        /// Загружает и отображает интерфейс управления пользователями с сетками для подтвержденных и неподтвержденных пользователей.
        /// Включает функциональность для подтверждения, отклонения и удаления пользователей.
        /// </summary>
        /// <param name="sender">Кнопка пользователей, которая была нажата.</param>
        /// <param name="e">Аргументы события.</param>
        private void btnUsers_Click(object sender, RoutedEventArgs e)
        {
            ShowBackButton();
            var mainContainer = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 50, 0, 50),
                MinWidth = 1700
            };

            mainContainer.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            mainContainer.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            mainContainer.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

            var adminLabel = new Label
            {
                Content = "Кабинет Администратора",
                FontSize = 36,
                HorizontalAlignment = HorizontalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Width = 1700
            };
            Grid.SetRow(adminLabel, 0);

            var usersLabel = new Label
            {
                Content = "Управление пользователями:",
                FontSize = 28,
                HorizontalAlignment = HorizontalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 20),
                Width = 1700
            };
            Grid.SetRow(usersLabel, 1);

            var contentGrid = new Grid
            {
                MinWidth = 1700,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(20) });
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

            var leftPanel = new StackPanel
            {
                Width = 820,
                HorizontalAlignment = HorizontalAlignment.Left,
                Background = Brushes.WhiteSmoke,
            };

            var confirmedLabel = new Label
            {
                Content = "Подтвержденные пользователи",
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 5)
            };

            gridConfirmed = new DataGrid
            {
                FontSize = 14,
                AutoGenerateColumns = false,
                Width = 820,
                Height = 200,
                HorizontalAlignment = HorizontalAlignment.Left,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(0, 0, 0, 10),
                CanUserAddRows = false,
                Background = Brushes.White
            };

            gridConfirmed.Columns.Add(new DataGridTextColumn() { Header = "ID", Binding = new Binding("Id"), Width = 40, IsReadOnly = true });
            gridConfirmed.Columns.Add(new DataGridTextColumn() { Header = "Логин", Binding = new Binding("Login"), Width = 150, IsReadOnly = true });
            gridConfirmed.Columns.Add(new DataGridTextColumn() { Header = "Имя", Binding = new Binding("Name"), Width = 200, IsReadOnly = true });
            gridConfirmed.Columns.Add(new DataGridTextColumn() { Header = "Специальность", Binding = new Binding("Speciality"), Width = 150, IsReadOnly = true });

            var deleteButtonColumn = new DataGridTemplateColumn() { Header = "Операция", Width = 100 };
            var deleteButtonTemplate = new DataTemplate();
            var deleteButtonFactory = new FrameworkElementFactory(typeof(Button));
            deleteButtonFactory.SetValue(Button.ContentProperty, "Удалить");
            deleteButtonFactory.AddHandler(Button.ClickEvent, new RoutedEventHandler(DeleteUser_Click));
            deleteButtonFactory.SetBinding(Button.TagProperty, new Binding("Id"));
            deleteButtonFactory.SetValue(Button.MarginProperty, new Thickness(2));
            deleteButtonFactory.SetValue(Button.PaddingProperty, new Thickness(5));
            deleteButtonFactory.SetValue(Button.ForegroundProperty, Brushes.Red);
            deleteButtonFactory.SetValue(Button.WidthProperty, 80.0);
            deleteButtonTemplate.VisualTree = deleteButtonFactory;
            deleteButtonColumn.CellTemplate = deleteButtonTemplate;
            gridConfirmed.Columns.Add(deleteButtonColumn);

            var unconfirmedLabel = new Label
            {
                Content = "Новые пользователи",
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 5)
            };

            gridUnconfirmed = new DataGrid
            {
                FontSize = 14,
                AutoGenerateColumns = false,
                Width = 820,
                Height = 200, 
                HorizontalAlignment = HorizontalAlignment.Left,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(0, 0, 0, 10),
                CanUserAddRows = false,
                Background = Brushes.White
            };

            gridUnconfirmed.Columns.Add(new DataGridTextColumn() { Header = "ID", Binding = new Binding("Id"), Width = 40, IsReadOnly = true });
            gridUnconfirmed.Columns.Add(new DataGridTextColumn() { Header = "Логин", Binding = new Binding("Login"), Width = 150, IsReadOnly = true });
            gridUnconfirmed.Columns.Add(new DataGridTextColumn() { Header = "Имя", Binding = new Binding("Name"), Width = 200, IsReadOnly = true });
            gridUnconfirmed.Columns.Add(new DataGridTextColumn() { Header = "Специальность", Binding = new Binding("Speciality"), Width = 150, IsReadOnly = true });

            var actionColumn = new DataGridTemplateColumn() { Header = "Операции", Width = 200 };
            var actionTemplate = new DataTemplate();
            var actionStackFactory = new FrameworkElementFactory(typeof(StackPanel));
            actionStackFactory.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);

            var approveButtonFactory = new FrameworkElementFactory(typeof(Button));
            approveButtonFactory.SetValue(Button.ContentProperty, "Подтвердить");
            approveButtonFactory.AddHandler(Button.ClickEvent, new RoutedEventHandler(ConfirmUser_Click));
            approveButtonFactory.SetBinding(Button.TagProperty, new Binding("Id"));
            approveButtonFactory.SetValue(Button.MarginProperty, new Thickness(2));
            approveButtonFactory.SetValue(Button.PaddingProperty, new Thickness(5));
            approveButtonFactory.SetValue(Button.BackgroundProperty, Brushes.LightGreen);
            approveButtonFactory.SetValue(Button.WidthProperty, 100.0);

            var rejectButtonFactory = new FrameworkElementFactory(typeof(Button));
            rejectButtonFactory.SetValue(Button.ContentProperty, "Отклонить");
            rejectButtonFactory.AddHandler(Button.ClickEvent, new RoutedEventHandler(RejectUser_Click));
            rejectButtonFactory.SetBinding(Button.TagProperty, new Binding("Id"));
            rejectButtonFactory.SetValue(Button.MarginProperty, new Thickness(2));
            rejectButtonFactory.SetValue(Button.PaddingProperty, new Thickness(5));
            rejectButtonFactory.SetValue(Button.ForegroundProperty, Brushes.Red);
            rejectButtonFactory.SetValue(Button.WidthProperty, 80.0);

            actionStackFactory.AppendChild(approveButtonFactory);
            actionStackFactory.AppendChild(rejectButtonFactory);
            actionTemplate.VisualTree = actionStackFactory;
            actionColumn.CellTemplate = actionTemplate;
            gridUnconfirmed.Columns.Add(actionColumn);

            leftPanel.Children.Add(confirmedLabel);
            leftPanel.Children.Add(gridConfirmed);
            leftPanel.Children.Add(unconfirmedLabel);
            leftPanel.Children.Add(gridUnconfirmed);
            Grid.SetColumn(leftPanel, 0);

            var rightPanel = new StackPanel
            {
                Width = 820,
                HorizontalAlignment = HorizontalAlignment.Left,
                Background = Brushes.WhiteSmoke,
            };

            var completedCoursesLabel = new Label
            {
                Content = "Завершенные курсы",
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 5)
            };

            var completedCoursesGrid = new DataGrid
            {
                FontSize = 14,
                AutoGenerateColumns = false,
                Width = 820,
                Height = 200, 
                HorizontalAlignment = HorizontalAlignment.Left,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(0, 0, 0, 10),
                CanUserAddRows = false,
                Background = Brushes.White
            };

            completedCoursesGrid.Columns.Add(new DataGridTextColumn() { Header = "ID", Binding = new Binding("Id"), Width = 40, IsReadOnly = true });
            completedCoursesGrid.Columns.Add(new DataGridTextColumn() { Header = "Логин", Binding = new Binding("UserLogin"), Width = 150, IsReadOnly = true });
            completedCoursesGrid.Columns.Add(new DataGridTextColumn() { Header = "Пользователь", Binding = new Binding("UserName"), Width = 150, IsReadOnly = true });
            completedCoursesGrid.Columns.Add(new DataGridTextColumn() { Header = "Курс", Binding = new Binding("CourseName"), Width = 200, IsReadOnly = true });
            completedCoursesGrid.Columns.Add(new DataGridTextColumn() { Header = "Статус", Binding = new Binding("Status"), Width = 100, IsReadOnly = true });
            completedCoursesGrid.Columns.Add(new DataGridTextColumn() { Header = "Дата", Binding = new Binding("CompletionDate"), Width = 100, IsReadOnly = true });

            var adminFormLabel = new Label
            {
                Content = "Создание администратора",
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 5)
            };

            var adminFormPanel = new Grid
            {
                Margin = new Thickness(5),
                Background = Brushes.White,
                Height = 200 
            };
            adminFormPanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            adminFormPanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            adminFormPanel.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            adminFormPanel.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            adminFormPanel.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            adminFormPanel.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            adminFormPanel.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

            var nameLabel = new Label { Content = "Имя:", Margin = new Thickness(5) };
            Grid.SetRow(nameLabel, 0);
            Grid.SetColumn(nameLabel, 0);

            var nameTextBox = new TextBox { Margin = new Thickness(5) };
            Grid.SetRow(nameTextBox, 0);
            Grid.SetColumn(nameTextBox, 1);

            var emailLabel = new Label { Content = "Email:", Margin = new Thickness(5) };
            Grid.SetRow(emailLabel, 1);
            Grid.SetColumn(emailLabel, 0);

            var emailTextBox = new TextBox { Margin = new Thickness(5) };
            Grid.SetRow(emailTextBox, 1);
            Grid.SetColumn(emailTextBox, 1);

            var loginLabel = new Label { Content = "Логин:", Margin = new Thickness(5) };
            Grid.SetRow(loginLabel, 2);
            Grid.SetColumn(loginLabel, 0);

            var loginTextBox = new TextBox { Margin = new Thickness(5) };
            Grid.SetRow(loginTextBox, 2);
            Grid.SetColumn(loginTextBox, 1);

            var passwordLabel = new Label { Content = "Пароль:", Margin = new Thickness(5) };
            Grid.SetRow(passwordLabel, 3);
            Grid.SetColumn(passwordLabel, 0);

            var passwordBox = new PasswordBox { Margin = new Thickness(5) };
            Grid.SetRow(passwordBox, 3);
            Grid.SetColumn(passwordBox, 1);

            var createButton = new Button
            {
                Content = "Создать",
                FontSize = 14,
                Margin = new Thickness(5, 10, 5, 5),
                HorizontalAlignment = HorizontalAlignment.Center,
                Padding = new Thickness(10, 3, 10, 3),
                Background = Brushes.LightGreen,
                Width = 150
            };
            Grid.SetRow(createButton, 4);
            Grid.SetColumnSpan(createButton, 2);

            createButton.Click += (s, args) =>
            {
                if (string.IsNullOrWhiteSpace(nameTextBox.Text) ||
                    string.IsNullOrWhiteSpace(emailTextBox.Text) ||
                    string.IsNullOrWhiteSpace(loginTextBox.Text) ||
                    string.IsNullOrWhiteSpace(passwordBox.Password))
                {
                    MessageBox.Show("Заполните все обязательные поля");
                    return;
                }

                try
                {
                    var mailAddress = new System.Net.Mail.MailAddress(emailTextBox.Text);
                }
                catch
                {
                    MessageBox.Show("Введите корректный email адрес");
                    return;
                }

                try
                {
                    using (var connection = new SQLiteConnection(ConnectionString))
                    {
                        connection.Open();
                        string query = "INSERT INTO administrators (name, email, login, password, confirmation) " +
                                     "VALUES (@name, @email, @login, @password, 1)";

                        using (var command = new SQLiteCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@name", nameTextBox.Text.Trim());
                            command.Parameters.AddWithValue("@email", emailTextBox.Text.Trim());
                            command.Parameters.AddWithValue("@login", loginTextBox.Text.Trim());
                            command.Parameters.AddWithValue("@password", passwordBox.Password);
                            command.ExecuteNonQuery();
                        }
                    }
                    MessageBox.Show("Администратор успешно создан!");
                    nameTextBox.Text = "";
                    emailTextBox.Text = "";
                    loginTextBox.Text = "";
                    passwordBox.Password = "";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при создании администратора: {ex.Message}");
                }
            };

            adminFormPanel.Children.Add(nameLabel);
            adminFormPanel.Children.Add(nameTextBox);
            adminFormPanel.Children.Add(emailLabel);
            adminFormPanel.Children.Add(emailTextBox);
            adminFormPanel.Children.Add(loginLabel);
            adminFormPanel.Children.Add(loginTextBox);
            adminFormPanel.Children.Add(passwordLabel);
            adminFormPanel.Children.Add(passwordBox);
            adminFormPanel.Children.Add(createButton);

            rightPanel.Children.Add(completedCoursesLabel);
            rightPanel.Children.Add(completedCoursesGrid);
            rightPanel.Children.Add(adminFormLabel);
            rightPanel.Children.Add(adminFormPanel);
            Grid.SetColumn(rightPanel, 2);

            contentGrid.Children.Add(leftPanel);
            contentGrid.Children.Add(rightPanel);
            Grid.SetRow(contentGrid, 2);

            mainContainer.Children.Add(adminLabel);
            mainContainer.Children.Add(usersLabel);
            mainContainer.Children.Add(contentGrid);

            var outerScrollViewer = new ScrollViewer
            {
                Content = mainContainer,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            mainScrollViewer.Content = outerScrollViewer;
            LoadUsers();
            completedCoursesGrid.ItemsSource = GetCompletedCourses();
        }
        /// <summary>
        /// Загружает и отображает интерфейс управления курсами с сетками для курсов и партнеров.
        /// Предоставляет функциональность для редактирования, удаления курсов и управления партнерами.
        /// </summary>
        /// <param name="sender">Кнопка курсов, которая была нажата.</param>
        /// <param name="e">Аргументы события.</param>
        private void btnCourses_Click(object sender, RoutedEventArgs e)
        {
            ShowBackButton();
            var mainContainer = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 50, 0, 50),
                MinWidth = 1700,
                MaxHeight = 800
            };

            mainContainer.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            mainContainer.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            mainContainer.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

            var adminLabel = new Label()
            {
                Content = "Кабинет Администратора",
                FontSize = 36,
                HorizontalAlignment = HorizontalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Width = 1700
            };
            Grid.SetRow(adminLabel, 0);

            var coursesLabel = new Label()
            {
                Content = "Управление курсами:",
                FontSize = 28,
                HorizontalAlignment = HorizontalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 20),
                Width = 1700
            };
            Grid.SetRow(coursesLabel, 1);

            var contentGrid = new Grid
            {
                MinWidth = 1700,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(20) });
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

            var leftPanel = new StackPanel
            {
                Width = 820,
                HorizontalAlignment = HorizontalAlignment.Left,
                Background = Brushes.WhiteSmoke,
                Margin = new Thickness(10)
            };

            var allCoursesLabel = new Label()
            {
                Content = "Все курсы:",
                FontSize = 24,
                HorizontalAlignment = HorizontalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Width = 820,
                Margin = new Thickness(0, 0, 0, 10)
            };


            var buttonsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var advancedEditButton = new Button()
            {
                Content = "Продвинутое редактирование",
                Margin = new Thickness(5),
                Padding = new Thickness(10, 5, 10, 5),
                Width = 200,
                ToolTip = "Редактировать модули и элементы курса"
            };
            advancedEditButton.Click += AdvancedEditCourse_Click;

            var saveButton = new Button()
            {
                Content = "Сохранить",
                Margin = new Thickness(5),
                Padding = new Thickness(10, 5, 10, 5),
                Width = 100,
                Background = Brushes.LightGreen
            };
            saveButton.Click += SaveCourse_Click;

            var deleteButton = new Button()
            {
                Content = "Удалить",
                Margin = new Thickness(5),
                Padding = new Thickness(10, 5, 10, 5),
                Width = 100,
                Foreground = Brushes.Red
            };
            deleteButton.Click += DeleteCourse_Click;

            buttonsPanel.Children.Add(advancedEditButton);
            buttonsPanel.Children.Add(saveButton);
            buttonsPanel.Children.Add(deleteButton);

            gridCourses = new DataGrid()
            {
                FontSize = 16,
                AutoGenerateColumns = false,
                Width = 820,
                HorizontalAlignment = HorizontalAlignment.Left,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(0, 0, 0, 20),
                Background = Brushes.White,
                MaxHeight = 655,
                CanUserAddRows = false,
                SelectionMode = DataGridSelectionMode.Single 
            };

            InitializeCourseGrid();

            leftPanel.Children.Add(allCoursesLabel);
            leftPanel.Children.Add(buttonsPanel);
            leftPanel.Children.Add(gridCourses);
            Grid.SetColumn(leftPanel, 0);

            var rightPanel = new StackPanel
            {
                Width = 820,
                HorizontalAlignment = HorizontalAlignment.Left,
                Background = Brushes.WhiteSmoke,
                Margin = new Thickness(10)
            };

            var addButton = new Button()
            {
                Content = "Добавить курс",
                FontSize = 16,
                Margin = new Thickness(5, 15, 5, 5),
                HorizontalAlignment = HorizontalAlignment.Center,
                Padding = new Thickness(15, 5, 15, 5),
                Background = Brushes.LightBlue,
                BorderBrush = Brushes.Gray,
                Width = 200
            };

            addButton.Click += (s, args) =>
            {
                var addCourseWindow = new AddCourseWindow();
                if (addCourseWindow.ShowDialog() == true)
                {
                    LoadCourses();
                }
            };

            rightPanel.Children.Add(addButton);

            var partnersLabel = new Label()
            {
                Content = "Управление партнерами:",
                FontSize = 24,
                HorizontalAlignment = HorizontalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Width = 820,
                Margin = new Thickness(0, 20, 0, 10)
            };

            var partnersPanel = new Grid
            {
                Width = 800,
                Height = 300,
                Margin = new Thickness(10),
                Background = Brushes.White
            };

            gridPartners = new DataGrid()
            {
                FontSize = 16,
                AutoGenerateColumns = false,
                Width = 780,
                HorizontalAlignment = HorizontalAlignment.Left,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(5),
                Background = Brushes.White,
                MaxHeight = 150,
                CanUserAddRows = false
            };

            gridPartners.Columns.Add(new DataGridTextColumn()
            {
                Header = "ID",
                Binding = new Binding("Id"),
                Width = 50,
                IsReadOnly = true
            });

            gridPartners.Columns.Add(new DataGridTextColumn()
            {
                Header = "Название",
                Binding = new Binding("Name"),
                Width = 200,
                IsReadOnly = false
            });

            var deletePartnerColumn = new DataGridTemplateColumn()
            {
                Header = "Действие",
                Width = new DataGridLength(100)
            };

            var deleteTemplate = new DataTemplate();
            var deleteButtonFactory = new FrameworkElementFactory(typeof(Button));
            deleteButtonFactory.SetValue(Button.ContentProperty, "Удалить");
            deleteButtonFactory.AddHandler(Button.ClickEvent, new RoutedEventHandler(DeletePartner_Click));
            deleteButtonFactory.SetBinding(Button.TagProperty, new Binding("Id"));
            deleteButtonFactory.SetValue(Button.MarginProperty, new Thickness(2));
            deleteButtonFactory.SetValue(Button.PaddingProperty, new Thickness(5));
            deleteButtonFactory.SetValue(Button.ForegroundProperty, Brushes.Red);
            deleteButtonFactory.SetValue(Button.WidthProperty, 80.0);
            deleteTemplate.VisualTree = deleteButtonFactory;
            deletePartnerColumn.CellTemplate = deleteTemplate;
            gridPartners.Columns.Add(deletePartnerColumn);

            var partnerNameLabel = new Label() { Content = "Название:", FontSize = 16, Margin = new Thickness(5, 10, 5, 5) };
            var partnerNameTextBox = new TextBox() { FontSize = 16, Margin = new Thickness(5), Width = 300 };
            Grid.SetRow(partnerNameLabel, 0); Grid.SetColumn(partnerNameLabel, 0);
            Grid.SetRow(partnerNameTextBox, 0); Grid.SetColumn(partnerNameTextBox, 1);

            var addPartnerButton = new Button()
            {
                Content = "Добавить партнера",
                FontSize = 16,
                Margin = new Thickness(5, 15, 5, 5),
                HorizontalAlignment = HorizontalAlignment.Center,
                Padding = new Thickness(15, 5, 15, 5),
                Background = Brushes.LightBlue,
                BorderBrush = Brushes.Gray,
                Width = 200
            };
            Grid.SetRow(addPartnerButton, 1); Grid.SetColumnSpan(addPartnerButton, 2);

            addPartnerButton.Click += (s, args) =>
            {
                var partnerName = partnerNameTextBox.Text;
                if (string.IsNullOrEmpty(partnerName))
                {
                    MessageBox.Show("Введите имя партнера");
                    return;
                }

                AddPartner(partnerName);
                partnerNameTextBox.Text = "";
                LoadPartnersList();
            };

            partnersPanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            partnersPanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            partnersPanel.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            partnersPanel.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            partnersPanel.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

            partnersPanel.Children.Add(partnerNameLabel);
            partnersPanel.Children.Add(partnerNameTextBox);
            partnersPanel.Children.Add(addPartnerButton);
            partnersPanel.Children.Add(gridPartners);
            Grid.SetRow(gridPartners, 2); Grid.SetColumnSpan(gridPartners, 2);

            rightPanel.Children.Add(partnersLabel);
            rightPanel.Children.Add(partnersPanel);
            Grid.SetColumn(rightPanel, 2);

            contentGrid.Children.Add(leftPanel);
            contentGrid.Children.Add(rightPanel);
            Grid.SetRow(contentGrid, 2);

            mainContainer.Children.Add(adminLabel);
            mainContainer.Children.Add(coursesLabel);
            mainContainer.Children.Add(contentGrid);

            var outerScrollViewer = new ScrollViewer
            {
                Content = mainContainer,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            mainScrollViewer.Content = mainContainer;
            mainScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            mainScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            LoadCourses();
            LoadPartnersList();
        }
        /// <summary>
        /// Загружает данные курсов из базы данных и привязывает их к сетке курсов.
        /// Включает связанную информацию о специальностях и партнерах.
        /// </summary>
        private void LoadCourses()
        {
            try
            {
                var courses = new List<Course>();

                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    string query = @"SELECT c.id, c.title, c.description, 
                           c.speciality_id, s.name as speciality_name, 
                           c.availability,
                           c.partnership as partner_id, p.name as partner_name
                           FROM courses c
                           LEFT JOIN specialities s ON c.speciality_id = s.id
                           LEFT JOIN partners p ON c.partnership = p.id";

                    using (var command = new SQLiteCommand(query, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                courses.Add(new Course
                                {
                                    Id = reader.GetInt32(0),
                                    Name = reader.GetString(1),
                                    Description = !reader.IsDBNull(2) ? reader.GetString(2) : "",
                                    SpecialityId = reader.GetInt32(3),
                                    SpecialityName = !reader.IsDBNull(4) ? reader.GetString(4) : "",
                                    AvailabilityId = reader.IsDBNull(5) ? 0 : (reader.GetBoolean(5) ? 1 : 0),
                                    AvailabilityName = reader.IsDBNull(5) ? "Не указано" : (reader.GetBoolean(5) ? "Доступен" : "Не доступен"),
                                    PartnerId = reader.IsDBNull(6) ? 0 : reader.GetInt32(6),
                                    PartnerName = !reader.IsDBNull(7) ? reader.GetString(7) : "",
                                    Specialities = GetSpecialities(),
                                    Partners = GetPartners()
                                });
                            }
                        }
                    }
                }

                gridCourses.ItemsSource = courses;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке курсов: {ex.Message}\n{ex.StackTrace}");
            }
        }
        /// <summary>
        /// Получает данные о специальностях из базы данных для привязки к комбобоксу.
        /// </summary>
        /// <returns>Список динамических объектов, содержащих ID и названия специальностей.</returns>
        private List<dynamic> GetSpecialities()
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

            return specialities;
        }
        /// <summary>
        /// Загружает новые учетные записи администраторов, ожидающие подтверждения, из базы данных.
        /// </summary>
        private void LoadNewAdmins()
        {
            try
            {
                var newAdmins = new List<Admin>();

                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    string query = "SELECT id, login, name, email FROM administrators WHERE confirmation = 0";

                    using (var command = new SQLiteCommand(query, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                newAdmins.Add(new Admin
                                {
                                    Id = reader.GetInt32(0),
                                    Login = reader.GetString(1),
                                    Name = reader.GetString(2),
                                    Email = reader.GetString(3)
                                });
                            }
                        }
                    }
                }

                var gridNewAdmins = FindVisualChild<DataGrid>(mainScrollViewer.Content as DependencyObject, "gridNewAdmins");
                if (gridNewAdmins != null)
                {
                    gridNewAdmins.ItemsSource = newAdmins;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке новых администраторов: {ex.Message}");
            }
        }
        /// <summary>
        /// Подтверждает учетную запись администратора, обновляя его статус в базе данных.
        /// </summary>
        /// <param name="sender">Кнопка подтверждения, которая была нажата.</param>
        /// <param name="e">Аргументы события.</param>
        private void ConfirmAdmin_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int adminId)
            {
                try
                {
                    using (var connection = new SQLiteConnection(ConnectionString))
                    {
                        connection.Open();
                        string query = "UPDATE administrators SET confirmation = 1 WHERE id = @adminId";

                        using (var command = new SQLiteCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@adminId", adminId);
                            command.ExecuteNonQuery();
                        }
                    }

                    LoadNewAdmins();
                    MessageBox.Show("Администратор подтверждён!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Получает данные о партнерах из базы данных для привязки к комбобоксу.
        /// </summary>
        /// <returns>Список динамических объектов, содержащих ID и названия партнеров.</returns>
        private List<dynamic> GetPartners()
        {
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

            return partners;
        }
        /// <summary>
        /// Загружает данные в комбобоксы для редактирования курса (специальности, доступность, партнеры).
        /// </summary>
        /// <param name="specialityComboBox">Комбобокс для выбора специальности.</param>
        /// <param name="availabilityComboBox">Комбобокс для статуса доступности.</param>
        /// <param name="partnerComboBox">Комбобокс для выбора партнера.</param>
        private void LoadComboBoxData(ComboBox specialityComboBox, ComboBox availabilityComboBox, ComboBox partnerComboBox)
        {
            var specialities = GetSpecialities();
            specialityComboBox.ItemsSource = specialities;
            specialityComboBox.DisplayMemberPath = "Name";
            specialityComboBox.SelectedValuePath = "Id";
            var availabilities = new List<dynamic>
{
    new { Id = 1, Name = "Доступен" },
    new { Id = 0, Name = "Не доступен" }
};
            availabilityComboBox.ItemsSource = availabilities;
            availabilityComboBox.DisplayMemberPath = "Name";
            availabilityComboBox.SelectedValuePath = "Id";

            var partners = GetPartners();
            partnerComboBox.ItemsSource = partners;
            partnerComboBox.DisplayMemberPath = "Name";
            partnerComboBox.SelectedValuePath = "Id";
        }
        /// <summary>
        /// Добавляет новый курс в базу данных с указанными параметрами.
        /// </summary>
        /// <param name="name">Название нового курса.</param>
        /// <param name="specialityId">ID специальности, связанной с курсом.</param>
        /// <param name="availabilityId">ID статуса доступности для курса.</param>
        /// <param name="partnerId">ID партнерской организации, связанной с курсом.</param>
        private void AddCourse(string name, int? specialityId, int? availabilityId, int? partnerId)
        {
            if (string.IsNullOrEmpty(name) || !specialityId.HasValue || !availabilityId.HasValue || !partnerId.HasValue)
            {
                MessageBox.Show("Заполните все поля");
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
                            string query = "INSERT INTO courses (name, speciality, availability, partnership) " +
                                         "VALUES (@name, @speciality, @availability, @partnership)";

                            using (var command = new SQLiteCommand(query, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@name", name);
                                command.Parameters.AddWithValue("@speciality", specialityId.Value);
                                command.Parameters.AddWithValue("@availability", availabilityId.Value);
                                command.Parameters.AddWithValue("@partnership", partnerId.Value);

                                command.ExecuteNonQuery();
                                transaction.Commit();
                                MessageBox.Show("Курс успешно добавлен");
                                LoadCourses();
                            }
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            MessageBox.Show($"Ошибка при добавлении курса: {ex.Message}");
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
        /// Сохраняет изменения текущего выбранного курса в базе данных.
        /// Обрабатывает обновления названия, описания, специальности, доступности и информации о партнере.
        /// </summary>
        /// <param name="sender">Кнопка сохранения, которая была нажата.</param>
        /// <param name="e">Аргументы события.</param>
        private void SaveCourse_Click(object sender, RoutedEventArgs e)
        {
            if (gridCourses.SelectedItem == null) return;

            gridCourses.CommitEdit(); 

            var course = gridCourses.SelectedItem as Course;
            if (course == null) return;

            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    string query = @"UPDATE courses SET 
                title = @name, 
                description = @description,
                speciality_id = @speciality, 
                availability = @availability, 
                available_until = @availableUntil,
                partnership = @partnership 
                WHERE id = @id";

                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@name", course.Name);
                        command.Parameters.AddWithValue("@description",
                            string.IsNullOrEmpty(course.Description) ? (object)DBNull.Value : course.Description);
                        command.Parameters.AddWithValue("@speciality", course.SpecialityId);
                        command.Parameters.AddWithValue("@availability", course.AvailabilityId == 1);

                        if (course.AvailableUntil.HasValue)
                        {
                            command.Parameters.AddWithValue("@availableUntil", course.AvailableUntil.Value);
                        }
                        else
                        {
                            command.Parameters.AddWithValue("@availableUntil", DBNull.Value);
                        }

                        command.Parameters.AddWithValue("@partnership",
                            course.PartnerId > 0 ? (object)course.PartnerId : DBNull.Value);
                        command.Parameters.AddWithValue("@id", course.Id);

                        command.ExecuteNonQuery();
                        MessageBox.Show("Изменения сохранены!");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}");
            }
        }
        /// <summary>
        /// Удаляет выбранный курс из базы данных после подтверждения.
        /// Удаляет все связанные данные, включая модули, элементы и ассоциации пользователей.
        /// </summary>
        /// <param name="sender">Кнопка удаления, которая была нажата.</param>
        /// <param name="e">Аргументы события.</param>
        private void DeleteCourse_Click(object sender, RoutedEventArgs e)
        {
            if (gridCourses.SelectedItem == null)
            {
                MessageBox.Show("Выберите курс для удаления");
                return;
            }

            var course = gridCourses.SelectedItem as Course;
            if (course == null) return;

            var result = MessageBox.Show(
                "Вы уверены, что хотите удалить этот курс и все связанные данные?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var connection = new SQLiteConnection(ConnectionString))
                    {
                        connection.Open();
                        using (var transaction = connection.BeginTransaction())
                        {
                            try
                            {
                                string deleteTestAnswersQuery = @"
                            DELETE FROM test_answers 
                            WHERE question_id IN (
                                SELECT tq.id FROM test_questions tq
                                JOIN tests t ON tq.test_id = t.id
                                JOIN module_items mi ON t.module_item_id = mi.id
                                JOIN modules m ON mi.module_id = m.id
                                WHERE m.course_id = @courseId
                            )";
                                ExecuteDeleteQuery(connection, transaction, deleteTestAnswersQuery, course.Id, "@courseId");

                                string deleteTestQuestionsQuery = @"
                            DELETE FROM test_questions 
                            WHERE test_id IN (
                                SELECT t.id FROM tests t
                                JOIN module_items mi ON t.module_item_id = mi.id
                                JOIN modules m ON mi.module_id = m.id
                                WHERE m.course_id = @courseId
                            )";
                                ExecuteDeleteQuery(connection, transaction, deleteTestQuestionsQuery, course.Id, "@courseId");

                                string deleteTestsQuery = @"
                            DELETE FROM tests 
                            WHERE module_item_id IN (
                                SELECT mi.id FROM module_items mi
                                JOIN modules m ON mi.module_id = m.id
                                WHERE m.course_id = @courseId
                            )";
                                ExecuteDeleteQuery(connection, transaction, deleteTestsQuery, course.Id, "@courseId");

                                string deleteModuleItemsQuery = @"
                            DELETE FROM module_items 
                            WHERE module_id IN (
                                SELECT id FROM modules WHERE course_id = @courseId
                            )";
                                ExecuteDeleteQuery(connection, transaction, deleteModuleItemsQuery, course.Id, "@courseId");

                                string deleteModulesQuery = "DELETE FROM modules WHERE course_id = @courseId";
                                ExecuteDeleteQuery(connection, transaction, deleteModulesQuery, course.Id, "@courseId");

                                string deleteUserCoursesQuery = "DELETE FROM userscourses WHERE course = @courseId";
                                ExecuteDeleteQuery(connection, transaction, deleteUserCoursesQuery, course.Id, "@courseId");

                                string deleteCourseQuery = "DELETE FROM courses WHERE id = @courseId";
                                ExecuteDeleteQuery(connection, transaction, deleteCourseQuery, course.Id, "@courseId");

                                transaction.Commit();
                                MessageBox.Show("Курс и все связанные данные успешно удалены");
                                LoadCourses();
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                MessageBox.Show($"Ошибка при удалении курса: {ex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка подключения: {ex.Message}");
                }
            }
        }
        /// <summary>
        /// Выполняет запрос на удаление в рамках транзакции для операций удаления курсов.
        /// </summary>
        /// <param name="connection">Активное подключение к базе данных.</param>
        /// <param name="transaction">Текущая транзакция базы данных.</param>
        /// <param name="query">Запрос на удаление для выполнения.</param>
        /// <param name="parameterValue">Значение параметра запроса.</param>
        /// <param name="parameterName">Имя параметра запроса.</param>
        private void ExecuteDeleteQuery(SQLiteConnection connection, SQLiteTransaction transaction, string query, int parameterValue, string parameterName)
        {
            using (var command = new SQLiteCommand(query, connection, transaction))
            {
                command.Parameters.AddWithValue(parameterName, parameterValue);
                command.ExecuteNonQuery();
            }
        }
        /// <summary>
        /// Добавляет новую партнерскую организацию в базу данных.
        /// </summary>
        /// <param name="name">Название новой партнерской организации.</param>
        private void AddPartner(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Введите имя партнера");
                return;
            }

            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    string query = "INSERT INTO partners (name) VALUES (@name)";

                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@name", name);
                        command.ExecuteNonQuery();
                        MessageBox.Show("Партнер успешно добавлен");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении партнера: {ex.Message}");
            }
        }
        /// <summary>
        /// Добавляет новый элемент в модуль курса в базе данных.
        /// </summary>
        /// <param name="moduleId">ID модуля для добавления элемента.</param>
        /// <param name="itemType">Тип нового элемента (лекция, видео, тест).</param>
        /// <param name="title">Название нового элемента.</param>
        /// <param name="contentPath">Путь к файлу контента элемента.</param>
        /// <param name="externalUrl">Внешний URL для элемента, если применимо.</param>
        /// <param name="durationMinutes">Длительность в минутах для видео-элементов.</param>
        private void AddModuleItem(int moduleId, string itemType, string title, string contentPath, string externalUrl, int? durationMinutes)
        {
            try
            {
                int orderIndex = 1;
                var existingItems = GetModuleItemsByModuleId(moduleId);
                if (existingItems.Any())
                {
                    orderIndex = existingItems.Max(i => i.OrderIndex) + 1;
                }

                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    string query = @"INSERT INTO module_items 
                                   (module_id, item_type, title, content_path, external_url, duration_minutes, order_index) 
                                   VALUES 
                                   (@moduleId, @itemType, @title, @contentPath, @externalUrl, @durationMinutes, @orderIndex)";

                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@moduleId", moduleId);
                        command.Parameters.AddWithValue("@itemType", itemType);
                        command.Parameters.AddWithValue("@title", title);
                        command.Parameters.AddWithValue("@contentPath", contentPath);
                        command.Parameters.AddWithValue("@externalUrl", externalUrl ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@durationMinutes", durationMinutes ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@orderIndex", orderIndex);

                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении элемента модуля: {ex.Message}");
                throw;
            }
        }
        /// <summary>
        /// Находит родительский элемент указанного типа в визуальном дереве.
        /// </summary>
        /// <typeparam name="T">Тип искомого родительского элемента.</typeparam>
        /// <param name="child">Дочерний элемент, с которого начинается поиск.</param>
        /// <returns>Родительский элемент, если найден, иначе null.</returns>
        private static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;
            T parent = parentObject as T;
            return parent ?? FindParent<T>(parentObject);
        }
        /// <summary>
        /// Возвращает к основному представлению контента и скрывает кнопку "Назад".
        /// </summary>
        /// <param name="sender">Кнопка "Назад", которая была нажата.</param>
        /// <param name="e">Аргументы события.</param>
        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            mainScrollViewer.Content = originalContent;
            btnBack.Visibility = Visibility.Collapsed;
        }
        /// <summary>
        /// Выходит из панели администратора и возвращается в главное окно приложения.
        /// </summary>
        /// <param name="sender">Кнопка выхода, которая была нажата.</param>
        /// <param name="e">Аргументы события.</param>
        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            Window mainWindow = new MainWindow();
            WindowProperties(mainWindow);
            mainWindow.Show();
            this.Close();
        }
        /// <summary>
        /// Возвращает к главному представлению панели администратора.
        /// </summary>
        /// <param name="sender">Кнопка "Главная", которая была нажата.</param>
        /// <param name="e">Аргументы события.</param>
        private void btnMain_Click(object sender, RoutedEventArgs e)
        {
            Window main = new AdminControl(_isAdmin, _userId);
            WindowProperties(main);
            main.Show();
            this.Close();
        }
        /// <summary>
        /// Делает кнопку "Назад" видимой в интерфейсе.
        /// </summary>
        private void ShowBackButton()
        {
            if (btnBack != null)
            {
                btnBack.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Загружает данные о партнерах из базы данных и привязывает их к сетке партнеров.
        /// </summary>
        private void LoadPartnersList()
        {
            try
            {
                var partners = new List<Partner>();

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
                                partners.Add(new Partner
                                {
                                    Id = reader.GetInt32(0),
                                    Name = reader.GetString(1)
                                });
                            }
                        }
                    }
                }

                gridPartners.ItemsSource = partners;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке партнеров: {ex.Message}");
            }
        }
        /// <summary>
        /// Удаляет партнерскую организацию из базы данных после подтверждения.
        /// Проверяет существующие ассоциации курсов перед разрешением удаления.
        /// </summary>
        /// <param name="sender">Кнопка удаления, которая была нажата.</param>
        /// <param name="e">Аргументы события.</param>
        private void DeletePartner_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button) || !(button.Tag is int partnerId)) return;

            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();

                    string checkQuery = "SELECT COUNT(*) FROM courses WHERE partnership = @partnerId";
                    using (var checkCommand = new SQLiteCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@partnerId", partnerId);
                        int coursesCount = Convert.ToInt32(checkCommand.ExecuteScalar());

                        if (coursesCount > 0)
                        {
                            MessageBox.Show(
                                "Невозможно удалить партнера, так как он связан с одним или несколькими курсами. Сначала измените эти курсы.",
                                "Ошибка удаления",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                            return;
                        }
                    }

                    var result = MessageBox.Show(
                        "Вы уверены, что хотите удалить этого партнера?",
                        "Подтверждение удаления",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                    {
                        string deleteQuery = "DELETE FROM partners WHERE id = @partnerId";
                        using (var deleteCommand = new SQLiteCommand(deleteQuery, connection))
                        {
                            deleteCommand.Parameters.AddWithValue("@partnerId", partnerId);
                            deleteCommand.ExecuteNonQuery();
                        }

                        MessageBox.Show("Партнер успешно удален");
                        LoadPartnersList();

                        var comboBoxes = FindVisualChildren<ComboBox>(this);
                        foreach (var comboBox in comboBoxes)
                        {
                            if (comboBox.Name == "partnerComboBox" || comboBox.Tag?.ToString() == "partnerCombo")
                            {
                                comboBox.ItemsSource = GetPartners();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении партнера: {ex.Message}");
            }
        }
        /// <summary>
        /// Представляет обращение в поддержку со всей соответствующей информацией и статусом.
        /// </summary>
        public class Appeal
        {
            public int Id { get; set; }
            public string Text { get; set; }
            public string ComputerName { get; set; }
            public string UserEmail { get; set; }

            [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy HH:mm}")]
            public DateTime CreatedAt { get; set; }

            public int UserId { get; set; }
            public string Status { get; set; }
            public string Response { get; set; }

            [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy HH:mm}")]
            public DateTime? ResponseDate { get; set; }
        }
        /// <summary>
        /// Представляет вложение к обращению в поддержку с данными файла и метаданными.
        /// </summary>
        public class AppealAttachment
        {
            public int Id { get; set; }
            public int AppealId { get; set; }
            public string FileName { get; set; }
            public byte[] FileData { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        private DataGrid gridActiveAppeals;
        private DataGrid gridArchivedAppeals;
        private DataGrid gridAttachments;
        private TextBox responseTextBox;
        private ComboBox appealsComboBox;
        /// <summary>
        /// Загружает данные обращений из базы данных и разделяет их на активные и архивные сетки.
        /// </summary>
        private void LoadAppeals()
        {
            try
            {
                var activeAppeals = new List<dynamic>();
                var archivedAppeals = new List<Appeal>();

                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();

                    string activeQuery = "SELECT id, text, computername, user_email, created_at, userId FROM appeals " +
                                       "WHERE id NOT IN (SELECT appeal_id FROM appeal_responses)";

                    using (var command = new SQLiteCommand(activeQuery, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            activeAppeals.Add(new
                            {
                                Id = reader["id"] != DBNull.Value ? Convert.ToInt32(reader["id"]) : 0,
                                Text = reader["text"] != DBNull.Value ? reader["text"].ToString() : string.Empty,
                                ComputerName = reader["computername"] != DBNull.Value ? reader["computername"].ToString() : string.Empty,
                                UserEmail = reader["user_email"] != DBNull.Value ? reader["user_email"].ToString() : null,
                                CreatedAt = reader["created_at"] != DBNull.Value ? DateTime.Parse(reader["created_at"].ToString()) : DateTime.MinValue,
                                UserId = reader["userId"] != DBNull.Value ? Convert.ToInt32(reader["userId"]) : 0,
                                Status = "Активное"
                            });
                        }
                    }

                    string archivedQuery = @"SELECT a.id, a.text, a.computername, a.user_email, a.created_at, a.userId, 
                                  ar.response, ar.response_date 
                                  FROM appeals a 
                                  JOIN appeal_responses ar ON a.id = ar.appeal_id";

                    using (var command = new SQLiteCommand(archivedQuery, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            archivedAppeals.Add(new Appeal
                            {
                                Id = reader["id"] != DBNull.Value ? Convert.ToInt32(reader["id"]) : 0,
                                Text = reader["text"] != DBNull.Value ? reader["text"].ToString() : string.Empty,
                                ComputerName = reader["computername"] != DBNull.Value ? reader["computername"].ToString() : string.Empty,
                                UserEmail = reader["user_email"] != DBNull.Value ? reader["user_email"].ToString() : null,
                                CreatedAt = reader["created_at"] != DBNull.Value ? DateTime.Parse(reader["created_at"].ToString()) : DateTime.MinValue,
                                UserId = reader["userId"] != DBNull.Value ? Convert.ToInt32(reader["userId"]) : 0,
                                Response = reader["response"] != DBNull.Value ? reader["response"].ToString() : string.Empty,
                                ResponseDate = reader["response_date"] != DBNull.Value ? DateTime.Parse(reader["response_date"].ToString()) : (DateTime?)null,
                                Status = "Завершено"
                            });
                        }
                    }
                }

                gridActiveAppeals.ItemsSource = activeAppeals;
                gridArchivedAppeals.ItemsSource = archivedAppeals;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке обращений: {ex.Message}");
            }
        }
        /// <summary>
        /// Обработчик нажатия кнопки "Обработка обращений"
        /// Отображает административную панель с активными и архивными обращениями,
        /// интерфейсом ответа и возможностью просмотра вложений
        /// </summary>
        /// <param name="sender">Источник события (кнопка)</param>
        /// <param name="e">Аргументы события нажатия</param>
        private void btnProcessing_Click(object sender, RoutedEventArgs e)
        {
            ShowBackButton();
            var mainContainer = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 50, 0, 50),
                MinWidth = 1700
            };

            mainContainer.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            mainContainer.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            mainContainer.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

            var adminLabel = new Label()
            {
                Content = "Кабинет Администратора",
                FontSize = 36,
                HorizontalAlignment = HorizontalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Width = 1700
            };
            Grid.SetRow(adminLabel, 0);

            var appealsLabel = new Label()
            {
                Content = "Управление обращениями:",
                FontSize = 28,
                HorizontalAlignment = HorizontalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 20),
                Width = 1700
            };
            Grid.SetRow(appealsLabel, 1);

            var contentGrid = new Grid
            {
                MinWidth = 1700,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(20) });
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

            var leftPanel = new StackPanel
            {
                Width = 820,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            var activeLabel = new Label()
            {
                Content = "Активные обращения",
                FontSize = 24,
                HorizontalAlignment = HorizontalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Width = 820,
                Margin = new Thickness(0, 0, 0, 10)
            };
            gridActiveAppeals = new DataGrid()
            {
                FontSize = 16,
                AutoGenerateColumns = false,
                Width = 820,
                Height = 300,
                HorizontalAlignment = HorizontalAlignment.Left,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(0, 0, 0, 20),
                SelectionMode = DataGridSelectionMode.Single,
                CanUserAddRows = false
            };


            gridActiveAppeals.Columns.Add(new DataGridTextColumn() { Header = "ID", Binding = new Binding("Id"), Width = 40, IsReadOnly = true });
            gridActiveAppeals.Columns.Add(new DataGridTextColumn() { Header = "Текст", Binding = new Binding("Text"), Width = 250, IsReadOnly = true });
            gridActiveAppeals.Columns.Add(new DataGridTextColumn() { Header = "Компьютер", Binding = new Binding("ComputerName"), Width = 150, IsReadOnly = true });
            gridActiveAppeals.Columns.Add(new DataGridTextColumn() { Header = "Email", Binding = new Binding("UserEmail"), Width = 150, IsReadOnly = true });
            gridActiveAppeals.Columns.Add(new DataGridTextColumn() { Header = "Дата", Binding = new Binding("CreatedAt"), Width = 120, IsReadOnly = true });

            var activeAttachmentsColumn = new DataGridTemplateColumn()
            {
                Header = "Приложения",
                Width = new DataGridLength(100)
            };

            var activeAttachmentsTemplate = new DataTemplate();
            var activeAttachmentsFactory = new FrameworkElementFactory(typeof(Button));
            activeAttachmentsFactory.SetValue(Button.ContentProperty, "Просмотр");
            activeAttachmentsFactory.AddHandler(Button.ClickEvent, new RoutedEventHandler(ShowAttachments_Click));
            activeAttachmentsFactory.SetBinding(Button.TagProperty, new Binding("Id"));
            activeAttachmentsFactory.SetValue(Button.StyleProperty, FindResource("BottomButtonStyle"));
            activeAttachmentsFactory.SetValue(Button.PaddingProperty, new Thickness(5));
            activeAttachmentsFactory.SetValue(Button.MarginProperty, new Thickness(2));
            activeAttachmentsTemplate.VisualTree = activeAttachmentsFactory;
            activeAttachmentsColumn.CellTemplate = activeAttachmentsTemplate;
            gridActiveAppeals.Columns.Add(activeAttachmentsColumn);

            leftPanel.Children.Add(activeLabel);
            leftPanel.Children.Add(gridActiveAppeals);

            var responsePanel = new StackPanel
            {
                Margin = new Thickness(0, 10, 0, 10),
                Orientation = Orientation.Vertical
            };

            var responseLabel = new Label()
            {
                Content = "Ответ на обращение:",
                FontSize = 16,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            responseTextBox = new TextBox()
            {
                FontSize = 14,
                Height = 100,
                Width = 800,
                Margin = new Thickness(0, 5, 0, 10),
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            var sendResponseButton = new Button()
            {
                Content = "Отправить ответ",
                FontSize = 16,
                Margin = new Thickness(0, 5, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                Padding = new Thickness(15, 5, 15, 5),
                Background = Brushes.LightBlue,
                Width = 200
            };
            sendResponseButton.Click += SendResponseButton_Click;

            responsePanel.Children.Add(responseLabel);
            responsePanel.Children.Add(responseTextBox);
            responsePanel.Children.Add(sendResponseButton);

            leftPanel.Children.Add(responsePanel);
            Grid.SetColumn(leftPanel, 0);

            var rightPanel = new StackPanel
            {
                Width = 820,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            var archivedLabel = new Label()
            {
                Content = "Архивные обращения",
                FontSize = 24,
                HorizontalAlignment = HorizontalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Width = 820,
                Margin = new Thickness(0, 0, 0, 10)
            };

            gridArchivedAppeals = new DataGrid()
            {
                FontSize = 16,
                AutoGenerateColumns = false,
                Width = 820,
                Height = 595,
                HorizontalAlignment = HorizontalAlignment.Left,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(0, 0, 0, 20),
                CanUserAddRows = false
            };

            gridArchivedAppeals.Columns.Add(new DataGridTextColumn() { Header = "ID", Binding = new Binding("Id"), Width = 40, IsReadOnly = true });
            gridArchivedAppeals.Columns.Add(new DataGridTextColumn() { Header = "Текст", Binding = new Binding("Text"), Width = 200, IsReadOnly = true });
            gridArchivedAppeals.Columns.Add(new DataGridTextColumn() { Header = "Ответ", Binding = new Binding("Response"), Width = 200, IsReadOnly = true });
            gridArchivedAppeals.Columns.Add(new DataGridTextColumn() { Header = "Дата обращения", Binding = new Binding("CreatedAt"), Width = 120, IsReadOnly = true });
            gridArchivedAppeals.Columns.Add(new DataGridTextColumn() { Header = "Дата ответа", Binding = new Binding("ResponseDate"), Width = 120, IsReadOnly = true });

            var archivedAttachmentsColumn = new DataGridTemplateColumn()
            {
                Header = "Приложения",
                Width = new DataGridLength(100)
            };

            var archivedAttachmentsTemplate = new DataTemplate();
            var archivedAttachmentsFactory = new FrameworkElementFactory(typeof(Button));
            archivedAttachmentsFactory.SetValue(Button.ContentProperty, "Просмотр");
            archivedAttachmentsFactory.AddHandler(Button.ClickEvent, new RoutedEventHandler(ShowAttachments_Click));
            archivedAttachmentsFactory.SetBinding(Button.TagProperty, new Binding("Id"));
            archivedAttachmentsFactory.SetValue(Button.StyleProperty, FindResource("BottomButtonStyle"));
            archivedAttachmentsFactory.SetValue(Button.PaddingProperty, new Thickness(5));
            archivedAttachmentsFactory.SetValue(Button.MarginProperty, new Thickness(2));
            archivedAttachmentsTemplate.VisualTree = archivedAttachmentsFactory;
            archivedAttachmentsColumn.CellTemplate = archivedAttachmentsTemplate;
            gridArchivedAppeals.Columns.Add(archivedAttachmentsColumn);

            rightPanel.Children.Add(archivedLabel);
            rightPanel.Children.Add(gridArchivedAppeals);
            Grid.SetColumn(rightPanel, 2);

            contentGrid.Children.Add(leftPanel);
            contentGrid.Children.Add(rightPanel);
            Grid.SetRow(contentGrid, 2);

            mainContainer.Children.Add(adminLabel);
            mainContainer.Children.Add(appealsLabel);
            mainContainer.Children.Add(contentGrid);

            var outerScrollViewer = new ScrollViewer
            {
                Content = mainContainer,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            mainScrollViewer.Content = outerScrollViewer;
            LoadAppeals();
        }
        /// <summary>
        /// Отображает вложения для конкретного обращения в новом окне.
        /// </summary>
        /// <param name="sender">Кнопка просмотра вложений, которая была нажата.</param>
        /// <param name="e">Аргументы события.</param>
        private void ShowAttachments_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button) || !(button.Tag is int appealId)) return;

            var attachments = GetAppealAttachments(appealId);
            if (attachments.Count == 0)
            {
                MessageBox.Show("Нет приложений к этому обращению");
                return;
            }

            var window = new Window
            {

                Title = $"Приложения к обращению #{appealId}",
                Width = 600,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterOwner

            };

            var dataGrid = new DataGrid
            {
                AutoGenerateColumns = false,
                ItemsSource = attachments,
                Margin = new Thickness(10)
            };

            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Имя файла", Binding = new Binding("FileName"), Width = 200 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Размер", Binding = new Binding("FileData.Length"), Width = 100 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Дата", Binding = new Binding("CreatedAt"), Width = 150 });
            window.Icon = new BitmapImage(new Uri("pack://application:,,,/logo2.png"));
            var downloadColumn = new DataGridTemplateColumn { Header = "Действие", Width = new DataGridLength(100) };
            var downloadTemplate = new DataTemplate();
            var downloadFactory = new FrameworkElementFactory(typeof(Button));
            downloadFactory.SetValue(Button.ContentProperty, "Открыть");
            downloadFactory.AddHandler(Button.ClickEvent, new RoutedEventHandler(OpenAttachment_Click));
            downloadFactory.SetBinding(Button.TagProperty, new Binding("Id"));
            downloadTemplate.VisualTree = downloadFactory;
            downloadColumn.CellTemplate = downloadTemplate;
            dataGrid.Columns.Add(downloadColumn);

            window.Content = dataGrid;
            window.ShowDialog();
        }
        /// <summary>
        /// Получает все вложения для конкретного обращения из базы данных.
        /// </summary>
        /// <param name="appealId">ID обращения для получения вложений.</param>
        /// <returns>Список объектов AppealAttachment для указанного обращения.</returns>
        private List<AppealAttachment> GetAppealAttachments(int appealId)
        {
            var attachments = new List<AppealAttachment>();

            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                string query = "SELECT id, file_name, file_data, created_at FROM appeal_attachments WHERE appeal_id = @appealId";

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@appealId", appealId);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            attachments.Add(new AppealAttachment
                            {
                                Id = reader.GetInt32(0),
                                FileName = reader.GetString(1),
                                FileData = (byte[])reader["file_data"],
                                CreatedAt = DateTime.Parse(reader["created_at"].ToString())
                            });
                        }
                    }
                }
            }

            return attachments;
        }
        /// <summary>
        /// Отправляет ответ на активное обращение, перемещая его в архивный список.
        /// Обрабатывает обновления базы данных и email-уведомления отправителю обращения.
        /// </summary>
        /// <param name="sender">Кнопка отправки ответа, которая была нажата.</param>
        /// <param name="e">Аргументы события.</param>
        private void SendResponseButton_Click(object sender, RoutedEventArgs e)
        {
            if (gridActiveAppeals.SelectedItem == null)
            {
                MessageBox.Show("Выберите обращение для ответа");
                return;
            }

            if (string.IsNullOrWhiteSpace(responseTextBox.Text))
            {
                MessageBox.Show("Введите текст ответа");
                return;
            }

            dynamic selectedAppeal = gridActiveAppeals.SelectedItem;
            int appealId = selectedAppeal.Id;
            string responseText = responseTextBox.Text.Trim();

            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            string query = "INSERT INTO appeal_responses (appeal_id, response, response_date) VALUES (@appealId, @response, @responseDate)";
                            using (var command = new SQLiteCommand(query, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@appealId", appealId);
                                command.Parameters.AddWithValue("@response", responseText);
                                command.Parameters.AddWithValue("@responseDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                                command.ExecuteNonQuery();
                            }

                            if (!string.IsNullOrEmpty(selectedAppeal.UserEmail))
                            {
                                SendEmailResponse(selectedAppeal.UserEmail, responseText);
                            }

                            transaction.Commit();
                            MessageBox.Show("Ответ успешно отправлен");
                            responseTextBox.Text = "";
                            LoadAppeals(); 
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            MessageBox.Show($"Ошибка при сохранении ответа: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }
        /// <summary>
        /// Открывает вложение обращения в программе по умолчанию после сохранения во временный файл.
        /// </summary>
        /// <param name="sender">Кнопка открытия, которая была нажата.</param>
        /// <param name="e">Аргументы события.</param>
        private void OpenAttachment_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button) || !(button.Tag is int attachmentId)) return;

            try
            {
                AppealAttachment attachment = null;
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    string query = "SELECT file_name, file_data FROM appeal_attachments WHERE id = @id";

                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", attachmentId);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                attachment = new AppealAttachment
                                {
                                    FileName = reader.GetString(0),
                                    FileData = (byte[])reader["file_data"]
                                };
                            }
                        }
                    }
                }

                if (attachment == null || attachment.FileData == null)
                {
                    MessageBox.Show("Файл не найден или поврежден");
                    return;
                }

                string tempFilePath = Path.Combine(Path.GetTempPath(), attachment.FileName);
                File.WriteAllBytes(tempFilePath, attachment.FileData);

                Process.Start(new ProcessStartInfo
                {
                    FileName = tempFilePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии файла: {ex.Message}");
            }
        }
        /// <summary>
        /// Отправляет email-ответ отправителю обращения с ответом администратора.
        /// </summary>
        /// <param name="email">Email-адрес получателя.</param>
        /// <param name="responseText">Текст ответа для отправки.</param>
        private void SendEmailResponse(string email, string responseText)
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

                    MailMessage mailMessage = new MailMessage();
                    mailMessage.From = new MailAddress(smtpUsername);
                    mailMessage.To.Add(email);
                    mailMessage.Subject = "Ответ на ваше обращение";
                    mailMessage.Body = $"Здравствуйте!\n\nМы рассмотрели ваше обращение и готовы предоставить ответ:\n\n{responseText}\n\nС уважением,\nАдминистрация системы";

                    smtpClient.Send(mailMessage);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отправке email: {ex.Message}\n\nОтвет был сохранен в системе, но не отправлен на email.",
                    "Ошибка отправки", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        /// <summary>
        /// Находит визуальные дочерние элементы указанного типа в родительском элементе.
        /// </summary>
        /// <typeparam name="T">Тип искомых дочерних элементов.</typeparam>
        /// <param name="depObj">Родительский элемент для поиска.</param>
        /// <returns>Коллекцию соответствующих дочерних элементов.</returns>
        private IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }
        /// <summary>
        /// Получает завершенные курсы с информацией о пользователях из базы данных.
        /// </summary>
        /// <returns>Список объектов CourseUser, представляющих завершенные курсы.</returns>
        private List<CourseUser> GetCompletedCourses()
        {
            var completedCourses = new List<CourseUser>();

            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                string query = @"SELECT uc.id, u.login, u.name as user_name, c.title as course_name, 
                s.name as status, uc.completion_date, uc.certificate_id
                FROM userscourses uc
                JOIN users u ON uc.user = u.id
                LEFT JOIN courses c ON uc.course = c.id
                LEFT JOIN statuses s ON uc.status = s.id
                WHERE uc.status = 2";

                using (var command = new SQLiteCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            completedCourses.Add(new CourseUser
                            {
                                Id = reader.GetInt32(0),
                                UserLogin = !reader.IsDBNull(1) ? reader.GetString(1) : "Неизвестный логин",
                                UserName = !reader.IsDBNull(2) ? reader.GetString(2) : "Неизвестный пользователь",
                                CourseName = !reader.IsDBNull(3) ? reader.GetString(3) : "Неизвестный курс",
                                Status = !reader.IsDBNull(4) ? reader.GetString(4) : "Неизвестный статус",
                                CompletionDate = !reader.IsDBNull(5) ? reader.GetString(5) : "Не указана",
                                CertificateId = !reader.IsDBNull(6) ? reader.GetInt32(6) : (int?)null
                            });
                        }
                    }
                }
            }

            return completedCourses;
        }
        /// <summary>
        /// Переходит в окно личного кабинета.
        /// </summary>
        /// <param name="sender">Кнопка кабинета, которая была нажата.</param>
        /// <param name="e">Аргументы события.</param>
        private void btnAccount_Click(object sender, RoutedEventArgs e)
        {
            Window account = new Personal_Account(_isAdmin, _userId);
            WindowProperties(account);
            account.Show();
            this.Close();
        }
        /// <summary>
        /// Переходит в окно панели администратора.
        /// </summary>
        /// <param name="sender">Кнопка администратора, которая была нажата.</param>
        /// <param name="e">Аргументы события.</param>
        private void btnAdmin_Click(object sender, RoutedEventArgs e)
        {
            Window admin = new AdminControl(_isAdmin, _userId);
            WindowProperties(admin);
            admin.Show();
            this.Close();
        }
        /// <summary>
        /// Переходит в окно часто задаваемых вопросов.
        /// </summary>
        /// <param name="sender">Кнопка FAQ, которая была нажата.</param>
        /// <param name="e">Аргументы события.</param>
        private void btnFAQs_Click(object sender, RoutedEventArgs e)
        {
            Window faqs = new FAQs(_isAdmin, _userId);
            WindowProperties(faqs);
            faqs.Show();
            this.Close();
        }
        /// <summary>
        /// Получает данные теста для конкретного элемента модуля из базы данных.
        /// </summary>
        /// <param name="moduleItemId">ID элемента модуля, содержащего тест.</param>
        /// <returns>Объект Test, если найден, иначе null.</returns>
        private Test GetTestByModuleItemId(int moduleItemId)
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                string query = @"SELECT t.id, t.total_points, mi.title 
                        FROM tests t
                        JOIN module_items mi ON t.module_item_id = mi.id
                        WHERE t.module_item_id = @moduleItemId";

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@moduleItemId", moduleItemId);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Test
                            {
                                Id = reader.GetInt32(0),
                                TotalPoints = reader.GetInt32(1),
                                Title = reader.GetString(2)
                            };
                        }
                    }
                }
            }
            return null;
        }
        /// <summary>
        /// Получает все вопросы для конкретного теста из базы данных.
        /// </summary>
        /// <param name="testId">ID теста для получения вопросов.</param>
        /// <returns>Список объектов TestQuestion для указанного теста.</returns>
        private List<TestQuestion> GetTestQuestions(int testId)
        {
            var questions = new List<TestQuestion>();
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                string query = "SELECT id, question_text, question_type, points FROM test_questions WHERE test_id = @testId";

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@testId", testId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            questions.Add(new TestQuestion
                            {
                                Id = reader.GetInt32(0),
                                Text = reader.GetString(1),  
                                Type = reader.GetString(2),   
                                Points = reader.GetInt32(3)   
                            });
                        }
                    }
                }
            }
            return questions;
        }
        /// <summary>
        /// Получает все ответы для конкретного вопроса теста из базы данных.
        /// </summary>
        /// <param name="questionId">ID вопроса для получения ответов.</param>
        /// <returns>Список объектов TestAnswer для указанного вопроса.</returns>
        private List<TestAnswer> GetTestAnswers(int questionId)
        {
            var answers = new List<TestAnswer>();
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                string query = "SELECT id, answer_text, is_correct FROM test_answers WHERE question_id = @questionId";

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@questionId", questionId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            answers.Add(new TestAnswer
                            {
                                Id = reader.GetInt32(0),
                                Text = reader.GetString(1),     
                                IsCorrect = reader.GetBoolean(2) 
                            });
                        }
                    }
                }
            }
            return answers;
        }
        /// <summary>
        /// Представляет тест с базовой идентификационной и оценочной информацией.
        /// </summary>
        public class Test
        {
            public int Id { get; set; }
            public string Title { get; set; }
            public int TotalPoints { get; set; }
        }
        /// <summary>
        /// Представляет вопрос теста с текстом, типом и оценочной информацией.
        /// </summary>
        public class TestQuestion
        {
            public int Id { get; set; }
            public string Text { get; set; } 
            public string Type { get; set; }  
            public int Points { get; set; }
        }
        /// <summary>
        /// Представляет возможный ответ на вопрос теста с флагом правильности.
        /// </summary>
        public class TestAnswer
        {
            public int Id { get; set; }
            public string Text { get; set; }  // Должно соответствовать answer_text
            public bool IsCorrect { get; set; }
        }
    }

    /// <summary>
    /// Представляет редактируемый вопрос теста с уведомлениями об изменении свойств.
    /// Включает текст вопроса, тип, значение баллов и коллекцию возможных ответов.
    /// </summary>
    public class EditTestQuestion : INotifyPropertyChanged
    {
        private int _id;
        private string _text;
        private string _type;
        private int _points;
        private ObservableCollection<EditTestAnswer> _answers;

        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string Text
        {
            get => _text;
            set { _text = value; OnPropertyChanged(); }
        }

        public string Type
        {
            get => _type;
            set { _type = value; OnPropertyChanged(); }
        }

        public int Points
        {
            get => _points;
            set { _points = value; OnPropertyChanged(); }
        }

        public ObservableCollection<EditTestAnswer> Answers
        {
            get => _answers;
            set { _answers = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public class TestQuestionEdition : INotifyPropertyChanged
        {
            private int _id;
            private string _text;
            private string _type;
            private int _points;
            private ObservableCollection<EditTestAnswer> _answers;

            public int Id
            {
                get => _id;
                set { _id = value; OnPropertyChanged(); }
            }

            public string Text
            {
                get => _text;
                set { _text = value; OnPropertyChanged(); }
            }

            public string Type
            {
                get => _type;
                set { _type = value; OnPropertyChanged(); }
            }

            public int Points
            {
                get => _points;
                set { _points = value; OnPropertyChanged(); }
            }

            public ObservableCollection<EditTestAnswer> Answers
            {
                get => _answers;
                set { _answers = value; OnPropertyChanged(); }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
    /// <summary>
    /// Представляет редактируемый ответ на вопрос теста с уведомлениями об изменении свойств.
    /// Включает текст ответа и флаг правильности.
    /// </summary>
    public class EditTestAnswer : INotifyPropertyChanged
    {
        private int _id;
        private string _text;
        private bool _isCorrect;

        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string Text
        {
            get => _text;
            set { _text = value; OnPropertyChanged(); }
        }

        public bool IsCorrect
        {
            get => _isCorrect;
            set { _isCorrect = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    /// <summary>
    /// Представляет завершение пользователем курса с сопутствующей информацией.
    /// </summary>
    public class CourseUser
    {
        public int Id { get; set; }
        public string UserLogin { get; set; }
        public string UserName { get; set; }
        public string CourseName { get; set; }
        public string Status { get; set; }
        public string CompletionDate { get; set; }
        public int? CertificateId { get; set; }
    }
    /// <summary>
    /// Представляет элемент модуля при редактировании со всеми редактируемыми свойствами.
    /// </summary>
    public class EditModuleItem
    {
        public int Id { get; set; }
        public int ModuleId { get; set; }
        public string ItemType { get; set; }
        public string Title { get; set; }
        public byte[] ContentData { get; set; }
        public string ExternalUrl { get; set; }
        public int? DurationMinutes { get; set; }
        public int OrderIndex { get; set; }
    }
}