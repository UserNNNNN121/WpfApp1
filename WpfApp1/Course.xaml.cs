using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using Newtonsoft.Json;
using static WpfApp1.CertificateGenerator;

namespace WpfApp1
{
    public partial class Course : Window
    {
        public class CourseModule : Module
        {
            public List<ModuleItem> Items { get; set; } = new List<ModuleItem>();
        }

        private static readonly string ConnectionString =
            $"Data Source={Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "educatingsystem.sl3")};Version=3;";
        private readonly int _courseId;
        private readonly int _userId;
        private readonly bool _isAdmin;
        private List<CourseModule> _modules = new List<CourseModule>();
        private CourseModel _course;

        public Course(int courseId, int userId, bool isAdmin)
        {
            InitializeComponent();
            _courseId = courseId;
            _userId = userId;
            _isAdmin = isAdmin;
            LoadCourseData();
            InitializeSideMenu();
            ShowCourseOverview();
        }


        private void LoadCourseData()
        {
            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();

                    // Load course info
                    string courseQuery = "SELECT id, title, description, speciality_id, availability, partnership, available_until FROM courses WHERE id = @CourseId";
                    using (var cmd = new SQLiteCommand(courseQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@CourseId", _courseId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                _course = new CourseModel
                                {
                                    Id = reader.GetInt32(0),
                                    Title = reader.GetString(1),
                                    Description = reader.IsDBNull(2) ? "Описание отсутствует" : reader.GetString(2),
                                    SpecialityId = reader.GetInt32(3),
                                    AvailabilityId = reader.GetBoolean(4) ? 1 : 0,
                                    PartnerId = reader.GetInt32(5),
                                    AvailableUntil = reader.IsDBNull(6) ? (DateTime?)null : reader.GetDateTime(6)
                                };
                                CourseTitle.Text = _course.Title;
                                CourseDescription.Text = _course.Description;

                                // Set the AvailableUntilLabel text
                                if (_course.AvailableUntil.HasValue)
                                {
                                    AvailableUntilLabel.Text = $"Доступен до: {_course.AvailableUntil.Value:dd.MM.yyyy}";

                                    // Optional: Change color if course is about to expire
                                    if (_course.AvailableUntil.Value < DateTime.Now.AddDays(7))
                                    {
                                        AvailableUntilLabel.Foreground = Brushes.Red;
                                    }
                                }
                                else
                                {
                                    AvailableUntilLabel.Text = "Доступен без ограничений";
                                }
                            }
                        }
                    }

                    // Load modules
                    string modulesQuery = @"
                        SELECT id, title, description, order_index 
                        FROM modules 
                        WHERE course_id = @CourseId 
                        ORDER BY order_index";

                    using (var cmd = new SQLiteCommand(modulesQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@CourseId", _courseId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var module = new CourseModule
                                {
                                    Id = reader.GetInt32(0),
                                    Title = reader.GetString(1),
                                    Description = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                                    OrderIndex = reader.GetInt32(3)
                                };
                                _modules.Add(module);
                            }
                        }
                    }

                    // Load module items for each module
                    foreach (var module in _modules)
                    {
                        module.Items = LoadModuleItems(module.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных курса: {ex.Message}");
            }
        }

        private List<ModuleItem> LoadModuleItems(int moduleId)
        {
            var items = new List<ModuleItem>();

            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                string query = @"
            SELECT mi.id, mi.module_id, mi.item_type, mi.title, mi.content_data, 
                   mi.external_url, mi.duration_minutes, mi.order_index,
                   CASE WHEN ucmi.id IS NOT NULL THEN 1 ELSE 0 END as is_completed
            FROM module_items mi
            LEFT JOIN usersmoduleitems ucmi ON ucmi.module_item_id = mi.id 
                                          AND ucmi.user_id = @UserId 
            WHERE mi.module_id = @ModuleId 
            ORDER BY mi.order_index";

                using (var cmd = new SQLiteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@ModuleId", moduleId);
                    cmd.Parameters.AddWithValue("@UserId", _userId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new ModuleItem
                            {
                                Id = reader.GetInt32(0),
                                ModuleId = reader.GetInt32(1),
                                ItemType = reader.GetString(2),
                                Title = reader.GetString(3),
                                ExternalUrl = reader.IsDBNull(5) ? null : reader.GetString(5),
                                DurationMinutes = reader.IsDBNull(6) ? (int?)null : reader.GetInt32(6),
                                OrderIndex = reader.GetInt32(7),
                                IsCompleted = reader.GetInt32(8) == 1
                            };

                            if (!reader.IsDBNull(4))
                            {
                                item.ContentPath = "db:" + item.Id;
                            }

                            items.Add(item);
                        }
                    }
                }
            }

            return items;
        }

        private void MarkItemAsCompleted(int moduleItemId)
        {
            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();

                    // First check if already completed
                    string checkQuery = @"
                SELECT COUNT(*) 
                FROM usersmoduleitems 
                WHERE user_id = @UserId 
                AND module_item_id = @ModuleItemId";

                    using (var checkCmd = new SQLiteCommand(checkQuery, connection))
                    {
                        checkCmd.Parameters.AddWithValue("@UserId", _userId);
                        checkCmd.Parameters.AddWithValue("@ModuleItemId", moduleItemId);

                        int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                        if (count > 0) return; // Already completed
                    }

                    // Insert new completion record
                    string insertQuery = @"
                INSERT INTO usersmoduleitems 
                (user_id, module_item_id, completed_at)
                VALUES (@UserId, @ModuleItemId, datetime('now'))";

                    using (var insertCmd = new SQLiteCommand(insertQuery, connection))
                    {
                        insertCmd.Parameters.AddWithValue("@UserId", _userId);
                        insertCmd.Parameters.AddWithValue("@ModuleItemId", moduleItemId);

                        insertCmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении статуса элемента: {ex.Message}");
            }
        }
        private void InitializeSideMenu()
        {
            var overviewButton = new Button
            {
                Content = "Обзор курса",
                Style = (Style)FindResource("MenuButtonStyle"),
                Margin = new Thickness(0, 0, 0, 10)
            };
            overviewButton.Click += (s, e) => ShowCourseOverview();
            MenuItemsPanel.Children.Add(overviewButton);

            foreach (var module in _modules)
            {
                var moduleButton = new Button
                {
                    Content = module.Title,
                    Style = (Style)FindResource("MenuButtonStyle"),
                    Tag = module.Id,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                moduleButton.Click += ModuleButton_Click;
                MenuItemsPanel.Children.Add(moduleButton);
            }
        }

        private void ShowCourseOverview()
        {
            ContentGrid.Children.Clear();

            var stackPanel = new StackPanel { Margin = new Thickness(20) };

            var title = new TextBlock
            {
                Text = _course.Title,
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 20)
            };

            var description = new TextBlock
            {
                Text = _course.Description,
                FontSize = 16,
                TextWrapping = TextWrapping.Wrap
            };

            stackPanel.Children.Add(title);
            stackPanel.Children.Add(description);
            ContentGrid.Children.Add(stackPanel);
        }

        private void ModuleButton_Click(object sender, RoutedEventArgs e)
        {
            int moduleId = (sender as Button)?.Tag as int? ?? 0;
            var module = _modules.Find(m => m.Id == moduleId);
            if (module == null) return;

            ContentGrid.Children.Clear();

            var scrollViewer = new ScrollViewer();
            var stackPanel = new StackPanel { Margin = new Thickness(20) };

            var moduleTitle = new TextBlock
            {
                Text = module.Title,
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 20)
            };

            var moduleDescription = new TextBlock
            {
                Text = module.Description,
                FontSize = 16,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 30)
            };

            stackPanel.Children.Add(moduleTitle);
            stackPanel.Children.Add(moduleDescription);

            foreach (var item in module.Items)
            {
                var itemBorder = new Border
                {
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(5),
                    Margin = new Thickness(0, 0, 0, 20),
                    Padding = new Thickness(15)
                };

                var itemStack = new StackPanel();

                var itemTitle = new TextBlock
                {
                    Text = item.Title,
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                var itemType = new TextBlock
                {
                    Text = GetItemTypeText(item.ItemType),
                    FontSize = 14,
                    Foreground = Brushes.Gray,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                if (item.ItemType == "test")
                {
                    var testStatus = GetTestStatus(item.Id);
                    var statusText = new TextBlock
                    {
                        Text = $"Статус: {testStatus}",
                        FontSize = 14,
                        Margin = new Thickness(0, 0, 0, 10),
                        Foreground = testStatus.Contains("100%") ? Brushes.Green :
                                    testStatus.Contains("%") ? Brushes.Blue : Brushes.Black
                    };
                    itemStack.Children.Add(statusText);
                }
                var openButton = new Button
                {
                    Content = "Открыть",
                    Tag = item,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Width = 100
                };
                openButton.Click += (s, args) => ShowContent(item);

                itemStack.Children.Add(itemTitle);
                itemStack.Children.Add(itemType);
                itemStack.Children.Add(openButton);

                if (item.ItemType == "video" && item.DurationMinutes.HasValue)
                {
                    var duration = new TextBlock
                    {
                        Text = $"Длительность: {item.DurationMinutes} мин",
                        FontSize = 14,
                        Margin = new Thickness(0, 5, 0, 0)
                    };
                    itemStack.Children.Add(duration);
                }

                itemBorder.Child = itemStack;
                stackPanel.Children.Add(itemBorder);
            }

            scrollViewer.Content = stackPanel;
            ContentGrid.Children.Add(scrollViewer);
        }
        private int GetTestIdForModuleItem(int moduleItemId)
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                string query = @"
                    SELECT t.id 
                    FROM tests t
                    JOIN module_items mi ON t.module_item_id = mi.id
                    JOIN modules m ON mi.module_id = m.id
                    WHERE t.module_item_id = @ModuleItemId
                    AND m.course_id = @CourseId";

                using (var cmd = new SQLiteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@ModuleItemId", moduleItemId);
                    cmd.Parameters.AddWithValue("@CourseId", _courseId);
                    var result = cmd.ExecuteScalar();
                    return result != null ? Convert.ToInt32(result) : 0;
                }
            }
        }

        private void ShowContent(ModuleItem item)
        {
            if (item.ItemType == "test")
            {
                try
                {
                    // Verify test belongs to current course before opening
                    int testId = GetTestIdForModuleItem(item.Id);
                    if (testId > 0)
                    {
                        var testWindow = new TestWindow(
                            testId,
                            item.Title,
                            _userId,
                            _courseId
                        );
                        testWindow.Owner = this;
                        testWindow.Show();
                    }
                    else
                    {
                        MessageBox.Show("Тест не найден или не принадлежит текущему курсу");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки теста: {ex.Message}");
                }
            }
            else
            {
                ContentGrid.Children.Clear();

                var scrollViewer = new ScrollViewer();
                var contentPanel = new StackPanel { Margin = new Thickness(20) };

                switch (item.ItemType)
                {
                    case "lecture":
                        try
                        {
                            string lectureText = GetLectureContentFromDb(item.Id);
                            var textBlock = new TextBlock
                            {
                                Text = lectureText,
                                TextWrapping = TextWrapping.Wrap,
                                FontSize = 16,
                                Margin = new Thickness(0, 0, 0, 20)
                            };
                            contentPanel.Children.Add(textBlock);

                            // Add a "Mark as Read" checkbox for lectures
                            var markAsReadCheck = new CheckBox
                            {
                                Content = "Отметить как прочитанное",
                                IsChecked = item.IsCompleted,
                                Margin = new Thickness(0, 10, 0, 20)
                            };

                            markAsReadCheck.Checked += (s, e) =>
                            {
                                MarkItemAsCompleted(item.Id);
                                item.IsCompleted = true;
                            };

                            markAsReadCheck.Unchecked += (s, e) =>
                            {
                                // Optional: Add logic to unmark completion if needed
                            };

                            contentPanel.Children.Add(markAsReadCheck);
                        }
                        catch (Exception ex)
                        {
                            contentPanel.Children.Add(new TextBlock
                            {
                                Text = $"Ошибка загрузки лекции: {ex.Message}",
                                Foreground = Brushes.Red
                            });
                        }
                        break;

                    case "video":
                        var videoPanel = new StackPanel();
                        if (!string.IsNullOrEmpty(item.ExternalUrl))
                        {
                            var webBrowser = new WebBrowser();
                            webBrowser.Navigate(new Uri(item.ExternalUrl));
                            videoPanel.Children.Add(webBrowser);
                        }
                        else
                        {
                            try
                            {
                                byte[] videoData = GetVideoContentFromDb(item.Id);
                                if (videoData != null && videoData.Length > 0)
                                {
                                    string tempVideoPath = Path.GetTempFileName() + ".mp4";
                                    File.WriteAllBytes(tempVideoPath, videoData);

                                    var mediaElement = new MediaElement
                                    {
                                        Source = new Uri(tempVideoPath),
                                        LoadedBehavior = MediaState.Manual,
                                        UnloadedBehavior = MediaState.Stop,
                                        Margin = new Thickness(0, 0, 0, 20)
                                    };

                                    var controlsPanel = new StackPanel
                                    {
                                        Orientation = Orientation.Horizontal,
                                        HorizontalAlignment = HorizontalAlignment.Center,
                                        Margin = new Thickness(0, 10, 0, 0)
                                    };

                                    var playButton = new Button { Content = "Play", Margin = new Thickness(5) };
                                    playButton.Click += (s, args) => mediaElement.Play();

                                    var pauseButton = new Button { Content = "Pause", Margin = new Thickness(5) };
                                    pauseButton.Click += (s, args) => mediaElement.Pause();

                                    var stopButton = new Button { Content = "Stop", Margin = new Thickness(5) };
                                    stopButton.Click += (s, args) => mediaElement.Stop();

                                    controlsPanel.Children.Add(playButton);
                                    controlsPanel.Children.Add(pauseButton);
                                    controlsPanel.Children.Add(stopButton);

                                    videoPanel.Children.Add(mediaElement);
                                    videoPanel.Children.Add(controlsPanel);

                                    mediaElement.Unloaded += (s, e) =>
                                    {
                                        try { File.Delete(tempVideoPath); }
                                        catch { /* Ignore deletion errors */ }
                                    };
                                }
                                else
                                {
                                    videoPanel.Children.Add(new TextBlock
                                    {
                                        Text = "Видео недоступно",
                                        HorizontalAlignment = HorizontalAlignment.Center,
                                        FontSize = 18
                                    });
                                }
                            }
                            catch (Exception ex)
                            {
                                videoPanel.Children.Add(new TextBlock
                                {
                                    Text = $"Ошибка загрузки видео: {ex.Message}",
                                    Foreground = Brushes.Red
                                });
                            }
                        }

                        // Add completion button for videos
                        if (!item.IsCompleted)
                        {
                            var completeButton = new Button
                            {
                                Content = "Завершить просмотр",
                                Margin = new Thickness(0, 20, 0, 0),
                                HorizontalAlignment = HorizontalAlignment.Left,
                                Width = 150
                            };
                            completeButton.Click += (s, args) =>
                            {
                                MarkItemAsCompleted(item.Id);
                                item.IsCompleted = true;
                                MessageBox.Show("Видеоурок завершен!");
                                ShowContent(item); // Refresh the view
                            };
                            videoPanel.Children.Add(completeButton);
                        }
                        else
                        {
                            var completedLabel = new TextBlock
                            {
                                Text = "✓ Видеоурок завершен",
                                Foreground = Brushes.Green,
                                Margin = new Thickness(0, 20, 0, 0),
                                HorizontalAlignment = HorizontalAlignment.Left
                            };
                            videoPanel.Children.Add(completedLabel);
                        }

                        contentPanel.Children.Add(videoPanel);
                        break;
                }

                var backButton = new Button
                {
                    Content = "Назад",
                    Margin = new Thickness(0, 20, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Width = 100
                };
                backButton.Click += (s, args) => ModuleButton_Click(
                    MenuItemsPanel.Children
                        .OfType<Button>()
                        .FirstOrDefault(b => b.Tag is int id && id == _modules
                            .FirstOrDefault(m => m.Items.Any(i => i.Id == item.Id))?.Id),
                    null);

                contentPanel.Children.Add(backButton);
                scrollViewer.Content = contentPanel;
                ContentGrid.Children.Add(scrollViewer);
            }
        }
        private List<TestQuestion> LoadTestQuestions(int testId)
        {
            var questions = new List<TestQuestion>();

            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();

                string query = @"
            SELECT id, question_text, question_type, points, order_index 
            FROM test_questions 
            WHERE test_id = @TestId 
            ORDER BY order_index";

                using (var cmd = new SQLiteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@TestId", testId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var question = new TestQuestion
                            {
                                Text = reader.GetString(1),
                                Type = reader.GetString(2),
                                Points = reader.GetInt32(3),
                                OrderIndex = reader.GetInt32(4),
                                Answers = new List<TestAnswer>()
                            };

                            string answersQuery = @"
                        SELECT answer_text, is_correct, order_index 
                        FROM test_answers 
                        WHERE question_id = @QuestionId 
                        ORDER BY order_index";

                            using (var answersCmd = new SQLiteCommand(answersQuery, connection))
                            {
                                answersCmd.Parameters.AddWithValue("@QuestionId", reader.GetInt32(0));
                                using (var answersReader = answersCmd.ExecuteReader())
                                {
                                    while (answersReader.Read())
                                    {
                                        question.Answers.Add(new TestAnswer
                                        {
                                            Text = answersReader.GetString(0),
                                            IsCorrect = answersReader.GetBoolean(1),
                                            OrderIndex = answersReader.GetInt32(2)
                                        });
                                    }
                                }
                            }

                            questions.Add(question);
                        }
                    }
                }
            }

            return questions;
        }
        private TestData LoadTestDataFromDb(int moduleItemId)
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();

                // First get the test ID from module_item_id
                string testIdQuery = @"
            SELECT id 
            FROM tests 
            WHERE module_item_id = @ModuleItemId";

                int testId = 0;
                using (var testIdCmd = new SQLiteCommand(testIdQuery, connection))
                {
                    testIdCmd.Parameters.AddWithValue("@ModuleItemId", moduleItemId);
                    var result = testIdCmd.ExecuteScalar();
                    if (result != null)
                    {
                        testId = Convert.ToInt32(result);
                    }
                    else
                    {
                        return null; // No test found for this module item
                    }
                }

                // Now load the test data
                string testQuery = @"
            SELECT available_until, total_points 
            FROM tests 
            WHERE id = @TestId";

                using (var testCmd = new SQLiteCommand(testQuery, connection))
                {
                    testCmd.Parameters.AddWithValue("@TestId", testId);

                    using (var testReader = testCmd.ExecuteReader())
                    {
                        if (testReader.Read())
                        {
                            var test = new TestData
                            {
                                TotalPoints = testReader.GetInt32(1),
                                Questions = new List<TestQuestion>()
                            };

                            // Load questions for this test
                            string questionsQuery = @"
                        SELECT id, question_text, question_type, points, order_index 
                        FROM test_questions 
                        WHERE test_id = @TestId 
                        ORDER BY order_index";

                            using (var questionsCmd = new SQLiteCommand(questionsQuery, connection))
                            {
                                questionsCmd.Parameters.AddWithValue("@TestId", testId);

                                using (var questionsReader = questionsCmd.ExecuteReader())
                                {
                                    while (questionsReader.Read())
                                    {
                                        var question = new TestQuestion
                                        {
                                            Text = questionsReader.GetString(1),
                                            Type = questionsReader.GetString(2),
                                            Points = questionsReader.GetInt32(3),
                                            OrderIndex = questionsReader.GetInt32(4),
                                            Answers = new List<TestAnswer>()
                                        };

                                        // Load answers for this question
                                        string answersQuery = @"
                                    SELECT answer_text, is_correct, order_index 
                                    FROM test_answers 
                                    WHERE question_id = @QuestionId 
                                    ORDER BY order_index";

                                        using (var answersCmd = new SQLiteCommand(answersQuery, connection))
                                        {
                                            answersCmd.Parameters.AddWithValue("@QuestionId", questionsReader.GetInt32(0));

                                            using (var answersReader = answersCmd.ExecuteReader())
                                            {
                                                while (answersReader.Read())
                                                {
                                                    question.Answers.Add(new TestAnswer
                                                    {
                                                        Text = answersReader.GetString(0),
                                                        IsCorrect = answersReader.GetBoolean(1),
                                                        OrderIndex = answersReader.GetInt32(2)
                                                    });
                                                }
                                            }
                                        }

                                        test.Questions.Add(question);
                                    }
                                }
                            }

                            return test;
                        }
                    }
                }
            }
            return null;
        }
        private string GetLectureContentFromDb(int itemId)
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                string query = "SELECT content_data FROM module_items WHERE id = @ItemId AND item_type = 'lecture'";
                using (var cmd = new SQLiteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@ItemId", itemId);
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        byte[] data = (byte[])result;
                        return System.Text.Encoding.UTF8.GetString(data);
                    }
                }
            }
            return "Лекция не найдена";
        }

        private byte[] GetVideoContentFromDb(int itemId)
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                string query = "SELECT content_data FROM module_items WHERE id = @ItemId AND item_type = 'video'";
                using (var cmd = new SQLiteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@ItemId", itemId);
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        return (byte[])result;
                    }
                }
            }
            return null;
        }

        private string GetTestStatus(int moduleItemId)
        {
            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();

                    // Get test ID
                    int testId = GetTestIdForModuleItem(moduleItemId);
                    if (testId == 0) return "Не начат";

                    // Check if test has been started
                    string statusQuery = @"
                SELECT 
                    CASE 
                        WHEN COUNT(*) = 0 THEN 'Не начат'
                        WHEN MIN(score_percentage) = 100 THEN 'Завершен (100%)'
                        WHEN MIN(score_percentage) > 0 THEN CONCAT('Начат (', MIN(score_percentage), '%)')
                        ELSE 'Не начат'
                    END as status
                FROM test_results 
                WHERE user_id = @UserId AND test_id = @TestId";

                    using (var cmd = new SQLiteCommand(statusQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@UserId", _userId);
                        cmd.Parameters.AddWithValue("@TestId", testId);

                        var result = cmd.ExecuteScalar();
                        return result?.ToString() ?? "Не начат";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка получения статуса теста: {ex.Message}");
                return "Ошибка";
            }
        }
        private string GetItemTypeText(string type)
        {
            switch (type)
            {
                case "lecture": return "Лекционный материал";
                case "video": return "Видеоурок";
                case "test": return "Тест";
                default: return type;
            }
        }

        private bool HasCompletedAllItems()
        {
            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();

                    // Get total items count
                    string totalQuery = @"
                SELECT COUNT(*) 
                FROM module_items mi
                JOIN modules m ON mi.module_id = m.id
                WHERE m.course_id = @CourseId";

                    int totalItems = 0;
                    using (var cmd = new SQLiteCommand(totalQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@CourseId", _courseId);
                        totalItems = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    if (totalItems == 0) return false;

                    // Get completed non-test items count
                    string completedQuery = @"
                SELECT COUNT(*) 
                FROM usersmoduleitems ucmi
                JOIN module_items mi ON ucmi.module_item_id = mi.id
                JOIN modules m ON mi.module_id = m.id
                WHERE m.course_id = @CourseId 
                AND ucmi.user_id = @UserId
                AND mi.item_type != 'test'";

                    int completedItems = 0;
                    using (var cmd = new SQLiteCommand(completedQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@CourseId", _courseId);
                        cmd.Parameters.AddWithValue("@UserId", _userId);
                        completedItems = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    // Get count of test items in this course
                    string testItemsQuery = @"
                SELECT COUNT(*) 
                FROM module_items mi
                JOIN modules m ON mi.module_id = m.id
                WHERE m.course_id = @CourseId
                AND mi.item_type = 'test'";

                    int totalTestItems = 0;
                    using (var cmd = new SQLiteCommand(testItemsQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@CourseId", _courseId);
                        totalTestItems = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    // Get count of COMPLETED tests (with 100% score) for this user
                    string completedTestsQuery = @"
                SELECT COUNT(DISTINCT t.module_item_id)
                FROM test_results tr
                JOIN tests t ON tr.test_id = t.id
                JOIN module_items mi ON t.module_item_id = mi.id
                JOIN modules m ON mi.module_id = m.id
                WHERE m.course_id = @CourseId
                AND tr.user_id = @UserId
                AND tr.score_percentage = 100";  // Only count tests with 100% score

                    int completedTestItems = 0;
                    using (var cmd = new SQLiteCommand(completedTestsQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@CourseId", _courseId);
                        cmd.Parameters.AddWithValue("@UserId", _userId);
                        completedTestItems = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    return (completedItems + completedTestItems) >= totalItems;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка проверки завершения элементов: {ex.Message}");
                return false;
            }
        }
        private void btnToggleMenu_Click(object sender, RoutedEventArgs e)
        {
            SideMenuColumn.Width = SideMenuColumn.Width.Value == 60 ? new GridLength(200) : new GridLength(60);
        }

        private void btnMain_Click(object sender, RoutedEventArgs e)
        {
            TransferToWindow(new Main(_userId, _isAdmin));
        }

        private void btnAdmin_Click(object sender, RoutedEventArgs e)
        {
            TransferToWindow(new AdminControl(_isAdmin, _userId));
        }

        private void btnSupport_Click(object sender, RoutedEventArgs e)
        {
            TransferToWindow(new Support(_isAdmin, _userId));
        }

        private void btnAccount_Click(object sender, RoutedEventArgs e)
        {
            TransferToWindow(new Personal_Account(_isAdmin, _userId));
        }

        private void btnFAQs_Click(object sender, RoutedEventArgs e)
        {
            TransferToWindow(new FAQs(_isAdmin, _userId));
        }

        private void btnCompleteCourse_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!HasCompletedAllItems())
                {
                    MessageBox.Show("Вы должны завершить все элементы курса перед получением сертификата.");
                    return;
                }

                // First mark the course as completed in the database
                MarkCourseAsCompleted();

                var certificateData = GetCertificateData(_userId, _courseId);
                if (certificateData == null)
                {
                    MessageBox.Show("Курс успешно завершен, но сертификат для этого курса не доступен.");
                    return;
                }

                if (string.IsNullOrEmpty(certificateData.UserName))
                {
                    MessageBox.Show("Курс успешно завершен, но не удалось получить имя пользователя для сертификата.");
                    return;
                }

                if (certificateData.TemplatePdf == null)
                {
                    MessageBox.Show("Курс успешно завершен, но шаблон сертификата не найден.");
                    return;
                }

                var generator = new CertificateGenerator();
                var certificateBytes = generator.GenerateCertificate(
                    certificateData.UserName,
                    certificateData.CourseTitle,
                    certificateData.OrganisationName,
                    certificateData.TemplatePdf,
                    certificateData.SealImage,
                    certificateData.SignatureImage,
                    certificateData.CertificateNumber,
                    certificateData.CompletionDate
                );

                // Save the certificate to database
                SaveUserCertificate(_userId, certificateData.CertificateId, certificateBytes);

                var saveDialog = new SaveFileDialog
                {
                    Filter = "PDF files (*.pdf)|*.pdf",
                    FileName = $"Сертификат_{certificateData.CertificateNumber}.pdf"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    File.WriteAllBytes(saveDialog.FileName, certificateBytes);
                    MessageBox.Show($"Сертификат {certificateData.CertificateNumber} сохранен: {saveDialog.FileName}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при завершении курса: {ex.Message}");
            }
        }
        private void MarkCourseAsCompleted()
        {
            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();

                    // Check if the course is already marked as completed
                    string checkQuery = @"
                SELECT COUNT(*) 
                FROM userscourses 
                WHERE user = @userId 
                AND course = @courseId";

                    using (var checkCmd = new SQLiteCommand(checkQuery, connection))
                    {
                        checkCmd.Parameters.AddWithValue("@userId", _userId);
                        checkCmd.Parameters.AddWithValue("@courseId", _courseId);
                        int existingCount = Convert.ToInt32(checkCmd.ExecuteScalar());
                        if (existingCount > 0)
                        {
                            // Update existing record
                            string updateQuery = @"
                        UPDATE userscourses 
                        SET status = 2, completion_date = @completionDate
                        WHERE user = @userId 
                        AND course = @courseId";

                            using (var updateCmd = new SQLiteCommand(updateQuery, connection))
                            {
                                updateCmd.Parameters.AddWithValue("@completionDate", DateTime.Now.ToString("yyyy-MM-dd"));
                                updateCmd.Parameters.AddWithValue("@userId", _userId);
                                updateCmd.Parameters.AddWithValue("@courseId", _courseId);

                                updateCmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            // Insert new record
                            string insertQuery = @"
                        INSERT INTO userscourses 
                        (user, course, status, completion_date)
                        VALUES (@userId, @courseId, 2, @completionDate)";

                            using (var insertCmd = new SQLiteCommand(insertQuery, connection))
                            {
                                insertCmd.Parameters.AddWithValue("@userId", _userId);
                                insertCmd.Parameters.AddWithValue("@courseId", _courseId);
                                insertCmd.Parameters.AddWithValue("@completionDate", DateTime.Now.ToString("yyyy-MM-dd"));

                                insertCmd.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отметке курса как завершенного: {ex.Message}");
                throw;
            }
        }

        private void SaveUserCertificate(int userId, int certificateId, byte[] pdfData)
        {
            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();

                    string checkQuery = @"
                SELECT COUNT(*) 
                FROM userscertificates 
                WHERE user = @userId 
                AND certificate = @certificateId";

                    using (var checkCmd = new SQLiteCommand(checkQuery, connection))
                    {
                        checkCmd.Parameters.AddWithValue("@userId", userId);
                        checkCmd.Parameters.AddWithValue("@certificateId", certificateId);

                        int existingCount = Convert.ToInt32(checkCmd.ExecuteScalar());
                        if (existingCount > 0)
                        {
                            // Update existing record
                            string updateQuery = @"
                        UPDATE userscertificates 
                        SET pdf_data = @pdfData, date = @date
                        WHERE user = @userId 
                        AND certificate = @certificateId";

                            using (var updateCmd = new SQLiteCommand(updateQuery, connection))
                            {
                                updateCmd.Parameters.AddWithValue("@pdfData", pdfData);
                                updateCmd.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd"));
                                updateCmd.Parameters.AddWithValue("@userId", userId);
                                updateCmd.Parameters.AddWithValue("@certificateId", certificateId);

                                updateCmd.ExecuteNonQuery();
                            }
                            return;
                        }
                    }

                    // Insert new record
                    string insertQuery = @"
                INSERT INTO userscertificates 
                (user, certificate, date, pdf_data)
                VALUES (@userId, @certificateId, @date, @pdfData)";

                    using (var insertCmd = new SQLiteCommand(insertQuery, connection))
                    {
                        insertCmd.Parameters.AddWithValue("@userId", userId);
                        insertCmd.Parameters.AddWithValue("@certificateId", certificateId);
                        insertCmd.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd"));
                        insertCmd.Parameters.AddWithValue("@pdfData", pdfData);

                        insertCmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения сертификата пользователя: {ex.Message}");
                throw;
            }
        }
        private CertificateData GetCertificateData(int userId, int courseId)
        {
            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();

                    string query = @"
SELECT 
    u.name AS user_name,
    c.title AS course_title,
    IFNULL(p.name, 'Enterprices') AS organisation_name,
    cert.template_pdf,
    cert.seal_image,
    cert.signature_image,
    cert.id AS certificate_id
FROM courses c
JOIN certificates cert ON cert.course_id = c.id
LEFT JOIN partners p ON c.partnership = p.id
JOIN users u ON u.id = @UserId
WHERE c.id = @CourseId";

                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.Parameters.AddWithValue("@CourseId", courseId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Add null checks for all fields
                                return new CertificateData
                                {
                                    UserName = reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                                    CourseTitle = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                                    OrganisationName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                                    TemplatePdf = reader.IsDBNull(3) ? null : (byte[])reader["template_pdf"],
                                    SealImage = reader.IsDBNull(4) ? null : (byte[])reader["seal_image"],
                                    SignatureImage = reader.IsDBNull(5) ? null : (byte[])reader["signature_image"],
                                    CertificateId = reader.IsDBNull(6) ? 0 : reader.GetInt32(6),
                                    CompletionDate = DateTime.Now,
                                    CertificateNumber = new CertificateGenerator().GenerateCertificateNumber()
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка получения данных для сертификата: {ex.Message}");
            }
            return null;
        }

        private void TransferToWindow(Window nextWindow)
        {
            nextWindow.Left = this.Left;
            nextWindow.Top = this.Top;
            nextWindow.Height = this.Height;
            nextWindow.Width = this.Width;
            nextWindow.WindowState = this.WindowState;
            nextWindow.Show();
            this.Close();
        }

        public void RefreshTestStatus(int moduleItemId)
        {
            // Reload the module items to get fresh data
            foreach (var module in _modules)
            {
                module.Items = LoadModuleItems(module.Id);
            }

            // Find and refresh the module containing this test
            foreach (var child in MenuItemsPanel.Children)
            {
                if (child is Button button && button.Tag is int moduleId &&
                    _modules.Any(m => m.Id == moduleId && m.Items.Any(i => i.Id == moduleItemId)))
                {
                    ModuleButton_Click(button, null);
                    return;
                }
            }
        }
    }
}