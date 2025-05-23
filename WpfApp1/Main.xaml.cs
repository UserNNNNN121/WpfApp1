using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
namespace WpfApp1
{

    public partial class Main : Window
    {
        private static readonly string ConnectionString =
    $"Data Source={Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "educatingsystem.sl3")};Version=3;";
        public int _userId;
        public bool _isAdmin;
        private int _userSpecialityId = 7;
        /// <summary>
        /// Конструктор окна Main.
        /// Инициализирует компоненты и подписывается на обновление доступности курсов.
        /// Также загружает список курсов для отображения.
        /// </summary>
        /// <param name="userId">ID пользователя</param>
        /// <param name="isAdmin">Флаг, указывающий, является ли пользователь администратором</param>
        public Main(int userId, bool isAdmin)
        {
            InitializeComponent();
            this._userId = userId;
            this._isAdmin = isAdmin;
            CourseAvailabilityManager.CoursesAvailabilityUpdated += OnCoursesAvailabilityUpdated;
            this.Closed += (s, e) => CourseAvailabilityManager.CoursesAvailabilityUpdated -= OnCoursesAvailabilityUpdated;
            _userSpecialityId = GetUserSpecialityId(_userId);
            btnBeginnerPrev.Click += (s, e) => ScrollCarousel(beginnerCoursesScroll, -300);
            btnBeginnerNext.Click += (s, e) => ScrollCarousel(beginnerCoursesScroll, 300);
            btnSpecialistPrev.Click += (s, e) => ScrollCarousel(specialistCoursesScroll, -300);
            btnSpecialistNext.Click += (s, e) => ScrollCarousel(specialistCoursesScroll, 300);

            LoadCourses();
        }
        /// <summary>
        /// Метод обработки события обновления доступности курсов.
        /// Вызывает оповещение для пользователя.
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
        /// Загружает курсы в зависимости от специальности пользователя и его подтвержденного статуса.
        /// Отображает соответствующий список курсов.
        /// </summary>
        private void LoadCourses()
        {
            try
            {
                var beginnerCourses = GetCoursesBySpeciality(7) 
                    .Concat(GetCoursesBySpeciality(8))          
                    .ToList();
                LoadCoursesToCarousel(beginnerCourses, beginnerCoursesItems, "Общие курсы:");

                bool isUserConfirmed = IsUserConfirmed(_userId, _isAdmin);


                var specialistCourses = _isAdmin ?
                    GetAllCourses().Where(c => GetCourseSpeciality(c.Id) != 8).ToList() :
                    GetCoursesBySpeciality(_userSpecialityId).Where(c => GetCourseSpeciality(c.Id) != 8).ToList();

                if (!isUserConfirmed && !_isAdmin)
                {
                    AddConfirmationOverlay(specialistCoursesScroll);
                }
                else
                {
                    RemoveConfirmationOverlay(specialistCoursesScroll);
                }

                LoadCoursesToCarousel(specialistCourses, specialistCoursesItems,
                    _isAdmin ? "Все курсы:" : "Курсы по вашей специальности:");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки курсов: {ex.Message}");
            }
        }
        /// <summary>
        /// Получает ID специальности для конкретного курса.
        /// </summary>
        /// <param name="courseId">ID курса</param>
        /// <returns>ID специальности</returns>
        private int GetCourseSpeciality(int courseId)
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                string query = "SELECT speciality_id FROM courses WHERE id = @CourseId";

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CourseId", courseId);
                    var result = command.ExecuteScalar();
                    return result != null ? Convert.ToInt32(result) : 7; 
                }
            }
        }
        /// <summary>
        /// Получает список всех курсов, доступных в системе.
        /// </summary>
        /// <returns>Список курсов</returns>
        private List<CourseInfo> GetAllCourses()
        {
            var courses = new List<CourseInfo>();

            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                string query = "SELECT id, title, description, available_until FROM courses WHERE availability = 1";

                using (var command = new SQLiteCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            courses.Add(new CourseInfo
                            {
                                Id = reader.GetInt32(0),
                                Title = reader.GetString(1),
                                Description = reader.IsDBNull(2) ? "Описание отсутствует" : reader.GetString(2),
                                AvailableUntil = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3)
                            });
                        }
                    }
                }
            }

            return courses;
        }
        /// <summary>
        /// Получает ID специальности пользователя из базы данных.
        /// </summary>
        /// <param name="userId">ID пользователя</param>
        /// <returns>ID специальности</returns>
        private int GetUserSpecialityId(int userId)
        {
            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();

                    if (_isAdmin == true) return 7;

                    string userQuery = "SELECT speciality FROM users WHERE id = @UserId";
                    using (var userCmd = new SQLiteCommand(userQuery, connection))
                    {
                        userCmd.Parameters.AddWithValue("@UserId", userId);
                        var result = userCmd.ExecuteScalar();
                        return result != null ? Convert.ToInt32(result) : 7;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка получения специальности пользователя: {ex.Message}");
                return -1;
            }
        }
        /// <summary>
        /// Проверяет, подтвержден ли пользователь или администратор.
        /// </summary>
        /// <param name="userId">ID пользователя</param>
        /// <param name="isAdmin">Флаг администратора</param>
        /// <returns>true, если подтвержден; иначе false</returns>
        private bool IsUserConfirmed(int userId, bool isAdmin)
        {
            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();

                    if (isAdmin)
                    {
                        string query = "SELECT confirmation FROM administrators WHERE id = @UserId";
                        using (var command = new SQLiteCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@UserId", userId);
                            var result = command.ExecuteScalar();
                            return result != null && Convert.ToInt32(result) == 1;
                        }
                    }
                    else
                    {
                        string query = "SELECT confirmation FROM users WHERE id = @UserId";
                        using (var command = new SQLiteCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@UserId", userId);
                            var result = command.ExecuteScalar();
                            return result != null && Convert.ToInt32(result) == 1;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка проверки подтверждения пользователя: {ex.Message}");
                return false;
            }
        }
        /// <summary>
        /// Добавляет затемняющий оверлей и сообщение, если пользователь не подтвержден.
        /// </summary>
        /// <param name="scrollViewer">ScrollViewer, к которому добавляется оверлей</param>
        private void AddConfirmationOverlay(ScrollViewer scrollViewer)
        {
            if (scrollViewer.Parent is Grid parentGrid && parentGrid.Children.OfType<Border>().Any(b => b.Name == "ConfirmationOverlay"))
                return;

            var overlay = new Border
            {
                Name = "ConfirmationOverlay",
                Background = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)),
                CornerRadius = new CornerRadius(8),
                Cursor = Cursors.Hand
            };

            var blurEffect = new BlurEffect
            {
                Radius = 5,
                KernelType = KernelType.Gaussian,
                RenderingBias = RenderingBias.Performance
            };
            scrollViewer.Effect = blurEffect;

            var stackPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var icon = new Image
            {
                Source = new BitmapImage(new Uri("/lock.png", UriKind.Relative)),
                Width = 50,
                Height = 50,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var textBlock = new TextBlock
            {
                Text = "Для доступа к курсам для специалистов дождитесь подтверждения администратором.\n(Оповещение придет на почту)",
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                MaxWidth = 300
            };

            stackPanel.Children.Add(icon);
            stackPanel.Children.Add(textBlock);
            overlay.Child = stackPanel;

            overlay.MouseLeftButtonDown += (s, e) =>
            {
                MessageBox.Show("Для доступа к курсам для специалистов дождитесь подтверждения администратором. (Оповещение о подтверждении придет на почту)");
            };

            if (scrollViewer.Parent is Grid grid)
            {
                grid.Children.Add(overlay);
                Grid.SetRow(overlay, Grid.GetRow(scrollViewer));
                Grid.SetColumn(overlay, Grid.GetColumn(scrollViewer));
            }
        }
        /// <summary>
        /// Удаляет оверлей подтверждения и эффект размытия.
        /// </summary>
        /// <param name="scrollViewer">ScrollViewer, с которого удаляется оверлей</param>
        private void RemoveConfirmationOverlay(ScrollViewer scrollViewer)
        {
            if (scrollViewer.Parent is Grid grid)
            {
                var overlay = grid.Children.OfType<Border>().FirstOrDefault(b => b.Name == "ConfirmationOverlay");
                if (overlay != null)
                {
                    grid.Children.Remove(overlay);
                }
            }
            scrollViewer.Effect = null;
        }
        /// <summary>
        /// Получает список курсов по ID специальности.
        /// </summary>
        /// <param name="specialityId">ID специальности</param>
        /// <returns>Список курсов</returns>
        private List<CourseInfo> GetCoursesBySpeciality(int specialityId)
        {
            var courses = new List<CourseInfo>();

            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();

                string query = @"SELECT id, title, description, available_until 
                 FROM courses 
                 WHERE speciality_id = @SpecialityId 
                 AND availability = 1";

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@SpecialityId", specialityId);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            courses.Add(new CourseInfo
                            {
                                Id = reader.GetInt32(0),
                                Title = reader.GetString(1),
                                Description = reader.IsDBNull(2) ? "Описание отсутствует" : reader.GetString(2),
                                AvailableUntil = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3)
                            });
                        }
                    }
                }
            }

            return courses;
        }
        /// <summary>
        /// Прокручивает ScrollViewer на заданное смещение по горизонтали.
        /// </summary>
        /// <param name="scrollViewer">Элемент ScrollViewer</param>
        /// <param name="offset">Смещение</param>
        private void ScrollCarousel(ScrollViewer scrollViewer, int offset)
        {
            scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset + offset);
        }
        /// <summary>
        /// Загружает курсы в интерфейс в виде карточек в StackPanel.
        /// </summary>
        /// <param name="courses">Список курсов</param>
        /// <param name="container">Контейнер StackPanel</param>
        /// <param name="headerText">Заголовок для группы курсов</param>
        private void LoadCoursesToCarousel(List<CourseInfo> courses, StackPanel container, string headerText)
        {
            if (container == null) return;

            container.Children.Clear();

            Dictionary<int, string> userCourseStatuses = GetUserCourseStatuses(_userId, _isAdmin);

            foreach (var course in courses)
            {
                var button = new Button
                {
                    Tag = course.Id,
                    Style = (Style)FindResource("CourseCardStyle"),
                    Cursor = Cursors.Hand
                };

                button.Click += Course_Click;

                var stack = new StackPanel();

                var title = new TextBlock
                {
                    Text = course.Title,
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                var description = new TextBlock
                {
                    Text = course.Description,
                    FontSize = 14,
                    TextWrapping = TextWrapping.Wrap,
                    MaxHeight = 80,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };

                var statusLabel = new TextBlock
                {
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 5, 0, 0),
                };

                if (userCourseStatuses.TryGetValue(course.Id, out string status))
                {
                    statusLabel.Text = $"Статус: {status}";
                }
                else
                {
                    statusLabel.Text = "Статус: Не начат";
                }

                if (course.AvailableUntil.HasValue)
                {
                    var availableLabel = new TextBlock
                    {
                        FontSize = 12,
                        FontWeight = FontWeights.SemiBold,
                        Margin = new Thickness(0, 5, 0, 0),
                        Text = $"Доступен до: {course.AvailableUntil.Value.ToString("dd.MM.yyyy")}"
                    };
                    stack.Children.Add(availableLabel);
                }

                stack.Children.Add(title);
                stack.Children.Add(description);
                stack.Children.Add(statusLabel);

                button.Content = stack;
                container.Children.Add(button);
            }
        }
        /// <summary>
        /// Получает словарь с курсами пользователя и их статусами.
        /// </summary>
        /// <param name="userId">ID пользователя</param>
        /// <param name="isAdmin">Флаг администратора</param>
        /// <returns>Словарь ID курса и его статуса</returns>
        private Dictionary<int, string> GetUserCourseStatuses(int userId, bool isAdmin)
        {
            var statuses = new Dictionary<int, string>();

            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                string query = @"
        SELECT c.id, s.name 
        FROM userscourses uc
        JOIN courses c ON uc.course = c.id
        JOIN statuses s ON uc.status = s.id
        WHERE uc.user = @UserId";

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            statuses.Add(reader.GetInt32(0), reader.GetString(1));
                        }
                    }
                }
            }

            return statuses;
        }
        /// <summary>
        /// Обрабатывает клик по курсу — добавляет курс пользователю и открывает окно курса.
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
        private void Course_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var courseId = (int)button.Tag;
            AddCourseToUser(courseId, _userId);
            Window course = new Course(courseId, _userId, _isAdmin);
            WindowProperties(course);
            course.Show();
            this.Close();
        }
        /// <summary>
        /// Добавляет курс пользователю в базу данных, если он ещё не был добавлен.
        /// </summary>
        /// <param name="courseId">ID курса</param>
        /// <param name="userId">ID пользователя</param>
        private void AddCourseToUser(int courseId, int userId)
        {
            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();

                    string checkQuery = "SELECT COUNT(*) FROM userscourses WHERE user = @UserId AND course = @CourseId";
                    using (var checkCmd = new SQLiteCommand(checkQuery, connection))
                    {
                        checkCmd.Parameters.AddWithValue("@UserId", userId);
                        checkCmd.Parameters.AddWithValue("@CourseId", courseId);
                        long count = (long)checkCmd.ExecuteScalar();

                        if (count > 0)
                        {

                            return;
                        }
                    }


                    string insertQuery = "INSERT INTO userscourses (user, course, status) VALUES (@UserId, @CourseId, 1)";
                    using (var insertCmd = new SQLiteCommand(insertQuery, connection))
                    {
                        insertCmd.Parameters.AddWithValue("@UserId", userId);
                        insertCmd.Parameters.AddWithValue("@CourseId", courseId);
                        insertCmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении курса: {ex.Message}");
            }
        }
        /// <summary>
        /// Применяет параметры текущего окна к новому окну (позиция, размеры, состояние).
        /// </summary>
        /// <param name="nextWindow">Новое окно</param>
        private void WindowProperties(Window nextWindow)
        {
            nextWindow.Left = this.Left;
            nextWindow.Top = this.Top;
            nextWindow.Height = this.Height;
            nextWindow.Width = this.Width;
            nextWindow.WindowState = this.WindowState;
        }

        /// <summary>
        /// Открывает окно поддержки.
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
        /// Открывает окно личного кабинета.
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
        private void btnAccount_Click(object sender, RoutedEventArgs e)
        {
            Window account = new Personal_Account(_isAdmin, _userId);
            WindowProperties(account);
            account.Show();
            this.Close();
        }
        /// <summary>
        /// Открывает окно часто задаваемых вопросов.
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
        /// Завершает текущую сессию и возвращает пользователя на главное окно.
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
        /// <summary>
        /// Перезагружает текущее окно (обновление интерфейса).
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
        private void btnMain_Click(object sender, RoutedEventArgs e)
        {
            Window main = new Main(_userId, _isAdmin);
            WindowProperties(main);
            main.Show();
            this.Close();
        }

    }
    /// <summary>
    /// Класс CourseInfo содержит основную информацию о курсе:
    /// идентификатор, название, описание и дату окончания доступности
    /// </summary>
    public class CourseInfo
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime? AvailableUntil { get; set; }
    }
}