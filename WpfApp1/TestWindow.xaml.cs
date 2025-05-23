using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WpfApp1
{
    public partial class TestWindow : Window
    {
        private readonly int _testId;
        private readonly string _testTitle;
        private readonly int _userId;
        private readonly int _courseId;
        private readonly List<QuestionViewModel> _questions;
        private int _currentQuestionIndex = 0;
        private int _totalScore = 0;
        private int _maxScore = 0;
        private bool _isSubmitted = false;
        private TextBlock _scoreText;
        private static readonly string ConnectionString =
            $"Data Source={Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "educatingsystem.sl3")};Version=3;";

        public TestWindow(int testId, string testTitle, int userId, int courseId)
        {
            InitializeComponent();
            _testId = testId;
            _testTitle = testTitle;
            _userId = userId;
            _courseId = courseId;
            _questions = LoadQuestions();
            _maxScore = _questions.Sum(q => q.Points);

            Title = testTitle;
            Width = 800;
            Height = 600;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            InitializeUI();
            ShowQuestion(0);
        }

        private List<QuestionViewModel> LoadQuestions()
        {
            var questions = new List<QuestionViewModel>();

            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();

                // First verify this test belongs to the course
                string verifyQuery = @"
            SELECT COUNT(*) 
            FROM tests t
            JOIN module_items mi ON t.module_item_id = mi.id
            JOIN modules m ON mi.module_id = m.id
            WHERE t.id = @TestId AND m.course_id = @CourseId";

                using (var verifyCmd = new SQLiteCommand(verifyQuery, connection))
                {
                    verifyCmd.Parameters.AddWithValue("@TestId", _testId);
                    verifyCmd.Parameters.AddWithValue("@CourseId", _courseId);

                    int count = Convert.ToInt32(verifyCmd.ExecuteScalar());
                    if (count == 0)
                    {
                        MessageBox.Show("Тест не принадлежит текущему курсу");
                        return questions;
                    }
                }

                // Load questions for this test
                string questionsQuery = @"
            SELECT id, question_text, question_type, points, order_index 
            FROM test_questions 
            WHERE test_id = @TestId 
            ORDER BY order_index";

                using (var questionsCmd = new SQLiteCommand(questionsQuery, connection))
                {
                    questionsCmd.Parameters.AddWithValue("@TestId", _testId);

                    using (var questionsReader = questionsCmd.ExecuteReader())
                    {
                        while (questionsReader.Read())
                        {
                            var question = new QuestionViewModel
                            {
                                Id = questionsReader.GetInt32(0),
                                Text = questionsReader.GetString(1),
                                Type = questionsReader.GetString(2),
                                Points = questionsReader.GetInt32(3),
                                OrderIndex = questionsReader.GetInt32(4),
                                Answers = new List<AnswerViewModel>()
                            };

                            // Load answers for this question
                            string answersQuery = @"
                        SELECT id, answer_text, is_correct, order_index 
                        FROM test_answers 
                        WHERE question_id = @QuestionId 
                        ORDER BY order_index";

                            using (var answersCmd = new SQLiteCommand(answersQuery, connection))
                            {
                                answersCmd.Parameters.AddWithValue("@QuestionId", question.Id);

                                using (var answersReader = answersCmd.ExecuteReader())
                                {
                                    while (answersReader.Read())
                                    {
                                        question.Answers.Add(new AnswerViewModel
                                        {
                                            Id = answersReader.GetInt32(0),
                                            Text = answersReader.GetString(1),
                                            IsCorrect = answersReader.GetBoolean(2),
                                            OrderIndex = answersReader.GetInt32(3),
                                            IsSelected = false
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
        private void InitializeUI()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });

            // Main question area
            var questionPanel = new StackPanel { Margin = new Thickness(20) };
            Grid.SetColumn(questionPanel, 0);

            // Side progress panel
            var progressPanel = new Border
            {
                Background = Brushes.LightGray,
                Margin = new Thickness(10),
                Child = new StackPanel { Margin = new Thickness(10) }
            };
            Grid.SetColumn(progressPanel, 1);

            grid.Children.Add(questionPanel);
            grid.Children.Add(progressPanel);

            Content = grid;
        }

        private void ShowQuestion(int index)
        {
            if (index < 0 || index >= _questions.Count) return;

            _currentQuestionIndex = index;
            var currentQuestion = _questions[index];
            _isSubmitted = currentQuestion.IsAnswered;

            var grid = (Grid)Content;
            var questionPanel = (StackPanel)grid.Children[0];
            var progressPanel = (Border)grid.Children[1];
            var progressStackPanel = (StackPanel)progressPanel.Child;

            questionPanel.Children.Clear();
            progressStackPanel.Children.Clear();

            // Question text
            var questionText = new TextBlock
            {
                Text = currentQuestion.Text,
                FontSize = 18,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 20)
            };
            questionPanel.Children.Add(questionText);

            // Answer options
            foreach (var answer in currentQuestion.Answers)
            {
                if (currentQuestion.Type == "single")
                {
                    var radioButton = new RadioButton
                    {
                        Content = answer.Text,
                        Tag = answer,
                        IsChecked = answer.IsSelected,
                        Margin = new Thickness(0, 0, 0, 10),
                        IsEnabled = !_isSubmitted
                    };

                    radioButton.Checked += (s, e) =>
                    {
                        foreach (var a in currentQuestion.Answers)
                            a.IsSelected = false;
                        answer.IsSelected = true;
                    };

                    if (_isSubmitted)
                    {
                        if (answer.IsCorrect && answer.IsSelected)
                        {
                            radioButton.Foreground = Brushes.Green;
                            radioButton.FontWeight = FontWeights.Bold;
                        }
                        else if (!answer.IsCorrect && answer.IsSelected)
                        {
                            radioButton.Foreground = Brushes.Red;
                            radioButton.FontWeight = FontWeights.Bold;
                        }
                        else if (answer.IsCorrect)
                        {
                            radioButton.Foreground = Brushes.Green;
                        }
                    }

                    questionPanel.Children.Add(radioButton);
                }
                else if (currentQuestion.Type == "multiple")
                {
                    var checkBox = new CheckBox
                    {
                        Content = answer.Text,
                        Tag = answer,
                        IsChecked = answer.IsSelected,
                        Margin = new Thickness(0, 0, 0, 10),
                        IsEnabled = !_isSubmitted
                    };

                    checkBox.Checked += (s, e) => answer.IsSelected = true;
                    checkBox.Unchecked += (s, e) => answer.IsSelected = false;

                    if (_isSubmitted)
                    {
                        if (answer.IsCorrect && answer.IsSelected)
                        {
                            checkBox.Foreground = Brushes.Green;
                            checkBox.FontWeight = FontWeights.Bold;
                        }
                        else if (!answer.IsCorrect && answer.IsSelected)
                        {
                            checkBox.Foreground = Brushes.Red;
                            checkBox.FontWeight = FontWeights.Bold;
                        }
                        else if (answer.IsCorrect)
                        {
                            checkBox.Foreground = Brushes.Green;
                        }
                    }

                    questionPanel.Children.Add(checkBox);
                }
                else // text answer
                {
                    var textBox = new TextBox
                    {
                        Margin = new Thickness(0, 0, 0, 10),
                        Height = 100,
                        TextWrapping = TextWrapping.Wrap,
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                        Text = answer.IsSelected ? answer.Text : string.Empty,
                        IsEnabled = !_isSubmitted
                    };

                    textBox.TextChanged += (s, e) =>
                    {
                        answer.Text = textBox.Text;
                        answer.IsSelected = !string.IsNullOrEmpty(textBox.Text);
                    };

                    if (_isSubmitted)
                    {
                        if (answer.IsCorrect)
                        {
                            textBox.Background = Brushes.LightGreen;
                            textBox.FontWeight = FontWeights.Bold;
                        }
                        else if (answer.IsSelected)
                        {
                            textBox.Background = Brushes.LightPink;
                            textBox.FontWeight = FontWeights.Bold;
                        }
                    }

                    questionPanel.Children.Add(textBox);
                }
            }

            // Navigation panel
            var navigationPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 0)
            };

            if (index > 0)
            {
                var prevButton = new Button { Content = "Назад", Width = 100 };
                prevButton.Click += (s, e) => ShowQuestion(index - 1);
                navigationPanel.Children.Add(prevButton);
            }

            if (!currentQuestion.IsAnswered)
            {
                var submitButton = new Button { Content = "Отправить", Width = 100, Margin = new Thickness(10, 0, 0, 0) };
                submitButton.Click += SubmitAnswer;
                navigationPanel.Children.Add(submitButton);
            }
            else
            {
                if (index < _questions.Count - 1)
                {
                    var nextButton = new Button { Content = "Далее", Width = 100, Margin = new Thickness(10, 0, 0, 0) };
                    nextButton.Click += (s, e) => ShowQuestion(index + 1);
                    navigationPanel.Children.Add(nextButton);
                }
                else
                {
                    var finishButton = new Button { Content = "Завершить", Width = 100, Margin = new Thickness(10, 0, 0, 0) };
                    finishButton.Click += FinishTest;
                    navigationPanel.Children.Add(finishButton);
                }

                // Correction button
                var correctButton = new Button
                {
                    Content = "Исправить",
                    Width = 100,
                    Margin = new Thickness(10, 0, 0, 0)
                };
                correctButton.Click += (s, e) =>
                {
                    currentQuestion.IsAnswered = false;
                    _isSubmitted = false;
                    ShowQuestion(_currentQuestionIndex);
                };
                navigationPanel.Children.Add(correctButton);
            }

            questionPanel.Children.Add(navigationPanel);

            // Progress panel
            var progressText = new TextBlock
            {
                Text = $"Прогресс: {index + 1}/{_questions.Count}",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 20)
            };
            progressStackPanel.Children.Add(progressText);

            var statusText = new TextBlock
            {
                Text = _isSubmitted ? "Ответ отправлен" : "Вопрос не отправлен",
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 20)
            };
            progressStackPanel.Children.Add(statusText);

            _scoreText = new TextBlock
            {
                Text = $"Баллы: {_totalScore}/{_maxScore}",
                FontSize = 14
            };
            progressStackPanel.Children.Add(_scoreText);
        }

        private async void FinishTest(object sender, RoutedEventArgs e)
        {
            double percentage = (_totalScore * 100.0) / _maxScore;

            // Save the test result
            await SaveTestResultAsync(percentage);

            // Show result window
            var resultWindow = new TestResultWindow(_testTitle, _totalScore, _maxScore, percentage);
            resultWindow.Show();

            // Refresh the test status in the parent window
            if (Owner is Course courseWindow)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    courseWindow.RefreshTestStatus(GetModuleItemIdForTest());
                });
            }

            this.Close();
        }

        private int GetModuleItemIdForTest()
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                string query = "SELECT module_item_id FROM tests WHERE id = @TestId";
                using (var cmd = new SQLiteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@TestId", _testId);
                    var result = cmd.ExecuteScalar();
                    return result != null ? Convert.ToInt32(result) : 0;
                }
            }
        }

        private void SubmitAnswer(object sender, RoutedEventArgs e)
        {
            var currentQuestion = _questions[_currentQuestionIndex];
            currentQuestion.IsAnswered = true;
            _isSubmitted = true;

            if (currentQuestion.WasAnsweredBefore)
            {
                _totalScore -= currentQuestion.PointsEarned;
            }

            currentQuestion.WasAnsweredBefore = true;
            currentQuestion.PointsEarned = 0;
            currentQuestion.IsFullyCorrect = false;

            if (currentQuestion.Type == "single" || currentQuestion.Type == "multiple")
            {
                bool allCorrect = true;
                foreach (var answer in currentQuestion.Answers)
                {
                    if (answer.IsSelected != answer.IsCorrect)
                    {
                        allCorrect = false;
                        break;
                    }
                }

                if (allCorrect)
                {
                    currentQuestion.PointsEarned = currentQuestion.Points;
                    _totalScore += currentQuestion.PointsEarned;
                    currentQuestion.IsFullyCorrect = true;
                }
            }
            else // text answer
            {
                if (currentQuestion.Answers.Any(a => a.IsSelected))
                {
                    currentQuestion.PointsEarned = currentQuestion.Points;
                    _totalScore += currentQuestion.PointsEarned;
                    currentQuestion.IsFullyCorrect = true;
                }
            }

            _scoreText.Text = $"Баллы: {_totalScore}/{_maxScore}";
            ShowQuestion(_currentQuestionIndex);
        }

        private async Task SaveTestResultAsync(double percentage)
        {
            await Task.Run(() =>
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();

                    string checkQuery = @"SELECT COUNT(*) FROM test_results 
                                      WHERE test_id = @TestId AND user_id = @UserId";
                    using (var checkCmd = new SQLiteCommand(checkQuery, connection))
                    {
                        checkCmd.Parameters.AddWithValue("@TestId", _testId);
                        checkCmd.Parameters.AddWithValue("@UserId", _userId);

                        int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                        if (count > 0)
                        {
                            string updateQuery = @"UPDATE test_results 
                                                SET score_percentage = @Percentage, 
                                                    completed_at = datetime('now')
                                                WHERE test_id = @TestId AND user_id = @UserId";
                            using (var updateCmd = new SQLiteCommand(updateQuery, connection))
                            {
                                updateCmd.Parameters.AddWithValue("@Percentage", percentage);
                                updateCmd.Parameters.AddWithValue("@TestId", _testId);
                                updateCmd.Parameters.AddWithValue("@UserId", _userId);
                                updateCmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            string insertQuery = @"INSERT INTO test_results 
                                               (user_id, test_id, score_percentage, completed_at) 
                                               VALUES 
                                               (@UserId, @TestId, @Percentage, datetime('now'))";
                            using (var insertCmd = new SQLiteCommand(insertQuery, connection))
                            {
                                insertCmd.Parameters.AddWithValue("@UserId", _userId);
                                insertCmd.Parameters.AddWithValue("@TestId", _testId);
                                insertCmd.Parameters.AddWithValue("@Percentage", percentage);
                                insertCmd.ExecuteNonQuery();
                            }
                        }
                    }
                }
            });
        }
    }

    public class TestResultWindow : Window
    {
        public TestResultWindow(string testTitle, int score, int maxScore, double percentage)
        {
            Title = $"Результат теста: {testTitle}";
            Width = 500;
            Height = 300;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            var stackPanel = new StackPanel
            {
                Margin = new Thickness(20),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var titleText = new TextBlock
            {
                Text = $"Тест: {testTitle}",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 20),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var resultText = new TextBlock
            {
                Text = $"Вы набрали {score} из {maxScore} возможных баллов",
                FontSize = 16,
                Margin = new Thickness(0, 0, 0, 10),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var percentageText = new TextBlock
            {
                Text = $"Процент выполнения: {percentage:F1}%",
                FontSize = 16,
                Margin = new Thickness(0, 0, 0, 20),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var closeButton = new Button
            {
                Content = "Закрыть",
                Width = 100,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            closeButton.Click += (s, e) => this.Close();

            stackPanel.Children.Add(titleText);
            stackPanel.Children.Add(resultText);
            stackPanel.Children.Add(percentageText);
            stackPanel.Children.Add(closeButton);

            Content = stackPanel;
        }
    }

    public class QuestionViewModel
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public string Type { get; set; }
        public int Points { get; set; }
        public int OrderIndex { get; set; }  // Added to match database column
        public List<AnswerViewModel> Answers { get; set; }
        public bool IsAnswered { get; set; }
        public bool WasAnsweredBefore { get; set; }
        public int PointsEarned { get; set; }
        public bool IsFullyCorrect { get; set; }
    }

    public class AnswerViewModel
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public bool IsCorrect { get; set; }
        public bool IsSelected { get; set; }
        public int OrderIndex { get; set; }  // Added to match database column
    }
}