using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.IO;
using System.Windows.Input;
using Microsoft.Win32;
using System.Windows.Controls.Primitives;

namespace WpfApp1
{
    public partial class Personal_Account : Window
    {
        public int _userId;
        public bool _isAdmin;
        public bool authorized = true;
        private static readonly string ConnectionString =
            $"Data Source={Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "educatingsystem.sl3")};Version=3;";

        public Personal_Account(bool isAdmin, int userId)
        {
            InitializeComponent(); 

            this._userId = userId;
            this._isAdmin = isAdmin;
            CourseAvailabilityManager.CoursesAvailabilityUpdated += OnCoursesAvailabilityUpdated;
            this.Closed += (s, e) => CourseAvailabilityManager.CoursesAvailabilityUpdated -= OnCoursesAvailabilityUpdated;
            startedCoursesPanel.Orientation = Orientation.Horizontal;
            completedCoursesPanel.Orientation = Orientation.Horizontal;

            if (!_isAdmin)
            {
                btnAdmin.Visibility = Visibility.Collapsed;
                btnSupport.Visibility = Visibility.Visible;
                btnMain.Visibility = Visibility.Visible;
                txtStartedCourses.Visibility = Visibility.Visible;
                startedCoursesCarousel.Visibility = Visibility.Visible;
                txtCompletedCourses.Visibility = Visibility.Visible;
                completedCoursesCarousel.Visibility = Visibility.Visible;
                txtCertificates.Visibility = Visibility.Visible;
                certificatesCarousel.Visibility = Visibility.Visible;
            }
            else
            {
                btnAdmin.Visibility = Visibility.Visible;
                btnSupport.Visibility = Visibility.Collapsed;
                btnMain.Visibility = Visibility.Collapsed;
                txtStartedCourses.Visibility = Visibility.Collapsed;
                startedCoursesCarousel.Visibility = Visibility.Collapsed;
                txtCompletedCourses.Visibility = Visibility.Collapsed;
                completedCoursesCarousel.Visibility = Visibility.Collapsed;
                txtCertificates.Visibility = Visibility.Collapsed;
                certificatesCarousel.Visibility = Visibility.Collapsed;
            }

            LoadUserInfo();
            LoadUserCourses();
            LoadUserCertificates();
        }
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
        private void LoadUserInfo()
        {
            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();

                    string query = _isAdmin ?
                        "SELECT name, email FROM administrators WHERE id = @userId" :
                        "SELECT name, speciality FROM users WHERE id = @userId";

                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@userId", _userId);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                txtName.Text = "ФИО: " + reader.GetString(0);

                                if (!_isAdmin)
                                {
                                    int specialityId = reader.GetInt32(1);
                                    string specialityName = GetSpecialityName(specialityId);
                                    txtSpeciality.Text = "Специальность: " + specialityName;
                                }
                                else
                                {
                                    txtSpeciality.Text = "Специальность: Администратор";
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки информации о пользователе: {ex.Message}");
            }
        }

        private string GetSpecialityName(int specialityId)
        {
            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    string query = "SELECT name FROM specialities WHERE id = @specialityId";

                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@specialityId", specialityId);
                        return command.ExecuteScalar()?.ToString() ?? "Неизвестная специальность";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка получения названия специальности: {ex.Message}");
                return "Ошибка загрузки";
            }
        }

        private void LoadUserCourses()
        {
            if (_isAdmin) return; // Пропускаем загрузку для администратора

            try
            {
                var courses = GetUserCourses(_userId);
                var startedCourses = courses.FindAll(c => c.StatusId == 1);
                var completedCourses = courses.FindAll(c => c.StatusId == 2);

                LoadCoursesToPanel(startedCourses, startedCoursesPanel, "Начатые курсы:");
                LoadCoursesToPanel(completedCourses, completedCoursesPanel, "Завершенные курсы:");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки курсов пользователя: {ex.Message}");
            }
        }

        private void LoadUserCertificates()
        {

            try
            {
                certificatesCarouselItems.Children.Clear();

                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    string query = @"
                SELECT c.title, uc.date, uc.pdf_data, uc.id
                FROM userscertificates uc
                JOIN certificates cert ON uc.certificate = cert.id
                JOIN courses c ON cert.course_id = c.id
                WHERE uc.user = @userId
                ORDER BY uc.id DESC";

                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@userId", _userId);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var certificateCard = new Border
                                {
                                    Background = Brushes.White,
                                    CornerRadius = new CornerRadius(10),
                                    BorderBrush = Brushes.LightGray,
                                    BorderThickness = new Thickness(1),
                                    Margin = new Thickness(10),
                                    Padding = new Thickness(15),
                                    Width = 300,
                                    Height = 100,
                                    Cursor = Cursors.Hand,
                                    Tag = reader.GetValue(2) // Store PDF data in Tag
                                };

                                var content = new StackPanel();
                                content.Children.Add(new TextBlock
                                {
                                    Text = "Сертификат #" + reader.GetInt32(3),
                                    FontSize = 18,
                                    FontWeight = FontWeights.Bold,
                                    HorizontalAlignment = HorizontalAlignment.Center,
                                    Margin = new Thickness(0, 0, 0, 10)
                                });

                                content.Children.Add(new TextBlock
                                {
                                    Text = reader.GetString(0),
                                    FontSize = 16,
                                    TextWrapping = TextWrapping.Wrap,
                                    HorizontalAlignment = HorizontalAlignment.Center
                                });

                                content.Children.Add(new TextBlock
                                {
                                    Text = "Дата: " + reader.GetString(1),
                                    FontSize = 14,
                                    FontStyle = FontStyles.Italic,
                                    HorizontalAlignment = HorizontalAlignment.Center,
                                    Margin = new Thickness(0, 10, 0, 0)
                                });

                                certificateCard.Child = content;
                                certificateCard.MouseLeftButtonDown += CertificateCard_MouseLeftButtonDown;
                                certificatesCarouselItems.Children.Add(certificateCard);
                            }
                        }
                    }
                }

                if (certificatesCarouselItems.Children.Count == 0)
                {
                    certificatesCarouselItems.Children.Add(new TextBlock
                    {
                        Text = "Нет сертификатов",
                        FontSize = 16,
                        FontStyle = FontStyles.Italic,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки сертификатов: {ex.Message}");
            }
        }

        private void CertificateCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is byte[] pdfData)
            {
                try
                {
                    var saveDialog = new SaveFileDialog
                    {
                        Filter = "PDF files (*.pdf)|*.pdf",
                        FileName = $"Сертификат_{DateTime.Now:yyyyMMddHHmmss}.pdf"
                    };

                    if (saveDialog.ShowDialog() == true)
                    {
                        File.WriteAllBytes(saveDialog.FileName, pdfData);
                        MessageBox.Show($"Сертификат сохранен: {saveDialog.FileName}");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сохранении сертификата: {ex.Message}");
                }
            }
        }

        private List<UserCourse> GetUserCourses(int userId)
        {
            var courses = new List<UserCourse>();

            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                string query = @"
SELECT 
    uc.course as id, 
    CASE WHEN c.id IS NULL THEN '[Удаленный курс]' ELSE c.title END as title, 
    uc.status as statusId, 
    s.name as statusName,
    CASE WHEN c.id IS NULL THEN 1 ELSE 0 END as isDeleted,
    uc.completion_date,
    uc.certificate_id
FROM 
    userscourses uc
LEFT JOIN 
    courses c ON uc.course = c.id
JOIN 
    statuses s ON uc.status = s.id
WHERE 
    uc.user = @userId
ORDER BY uc.id DESC";  // Сортировка по ID в обратном порядке

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@userId", userId);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            bool isDeleted = reader.GetInt32(4) == 1;

                            courses.Add(new UserCourse
                            {
                                Id = reader.GetInt32(0),
                                Title = isDeleted ? "[Удаленный курс]" : reader.GetString(1),
                                StatusId = reader.GetInt32(2),
                                Status = reader.GetString(3),
                                IsDeleted = isDeleted,
                            });
                        }
                    }
                }
            }

            return courses;
        }
        private void LoadCoursesToPanel(List<UserCourse> courses, StackPanel panel, string headerText)
        {
            panel.Children.Clear();

            foreach (var course in courses)
            {
                var courseCard = new Border
                {
                    Background = course.IsDeleted ? Brushes.LightGray : Brushes.White,
                    CornerRadius = new CornerRadius(10),
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(10),
                    Padding = new Thickness(15),
                    Width = 300,
                    Height = 100,
                    Cursor = course.IsDeleted ? Cursors.No : Cursors.Hand,
                    Opacity = course.IsDeleted ? 0.7 : 1.0
                };

                var content = new StackPanel();
                content.Children.Add(new TextBlock
                {
                    Text = course.Title,
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = course.IsDeleted ? Brushes.DarkGray : Brushes.Black
                });

                content.Children.Add(new TextBlock
                {
                    Text = course.Status,
                    FontSize = 14,
                    FontStyle = FontStyles.Italic,
                    Margin = new Thickness(0, 5, 0, 0),
                    Foreground = course.IsDeleted ? Brushes.DarkGray : Brushes.Black
                });

                if (course.IsDeleted)
                {
                    content.Children.Add(new TextBlock
                    {
                        Text = "Курс удален",
                        FontSize = 12,
                        FontStyle = FontStyles.Italic,
                        Foreground = Brushes.Red,
                        Margin = new Thickness(0, 5, 0, 0)
                    });
                }

                courseCard.Child = content;
                courseCard.Tag = course.Id;

                if (!course.IsDeleted)
                {
                    courseCard.MouseLeftButtonDown += (sender, e) =>
                    {
                        int courseId = (int)((Border)sender).Tag;
                        OpenCourseWindow(courseId);
                    };
                }

                panel.Children.Add(courseCard);
            }

            if (courses.Count == 0)
            {
                var noCoursesText = new TextBlock
                {
                    Text = "Нет курсов",
                    FontSize = 18,
                    FontStyle = FontStyles.Italic,
                    Margin = new Thickness(10)
                };
                panel.Children.Add(noCoursesText);
            }
        }

        private void OpenCourseWindow(int courseId)
        {
            Window course = new Course(courseId, _userId, _isAdmin);
            WindowProperties(course);
            course.Show();
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

        private void btnMain_Click(object sender, RoutedEventArgs e)
        {
            if (_userId != -1)
            {
                Window main = new Main(_userId, _isAdmin);
                WindowProperties(main);
                main.Show();
                this.Close();
            }
            else
            {
                Window mainWindow = new MainWindow();
                WindowProperties(mainWindow);
                mainWindow.Show();
                this.Close();
            }
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

        private void btnAdmin_Click(object sender, RoutedEventArgs e)
        {
            Window admin = new AdminControl(_isAdmin, _userId);
            WindowProperties(admin);
            admin.Show();
            this.Close();
        }

        private bool CheckCurrentPassword(int userId, string password, bool isAdmin)
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();

                if (isAdmin)
                {
                    var adminCommand = new SQLiteCommand("SELECT password FROM administrators WHERE id = @userId", connection);
                    adminCommand.Parameters.AddWithValue("@userId", userId);
                    var adminPassword = adminCommand.ExecuteScalar()?.ToString();
                    return adminPassword == password;
                }
                else
                {
                    var userCommand = new SQLiteCommand("SELECT password FROM users WHERE id = @userId", connection);
                    userCommand.Parameters.AddWithValue("@userId", userId);
                    var userPassword = userCommand.ExecuteScalar()?.ToString();
                    return userPassword == password;
                }
            }
        }

        private void btnChange_Click(object sender, RoutedEventArgs e)
        {
            string currentPassword = boxPassword.Password;

            if (string.IsNullOrWhiteSpace(currentPassword))
            {
                MessageBox.Show("Введите текущий пароль");
                return;
            }

            if (CheckCurrentPassword(_userId, currentPassword, _isAdmin))
            {
                Window newpassword = new NewPassword(_isAdmin, _userId, authorized);
                WindowProperties(newpassword);
                newpassword.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Неправильно введен пароль. Попробуйте снова.");
                boxPassword.Clear();
                boxPassword.Focus();
            }
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            WindowProperties(mainWindow);
            mainWindow.Show();
            this.Close();
        }

        private void btnAccount_Click(object sender, RoutedEventArgs e)
        {
            Window account = new Personal_Account(_isAdmin, _userId);
            WindowProperties(account);
            account.Show();
            this.Close();
        }
    }

    public class UserCourse
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int StatusId { get; set; }
        public string Status { get; set; }
        public bool IsDeleted { get; set; }
    }
}