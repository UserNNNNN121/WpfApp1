using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1
{
    public partial class CreateTestWindow : Window
    {
        private TestData _testData;
        private TestQuestion _currentQuestion;
        private List<TestQuestion> _questions = new List<TestQuestion>();

        public CreateTestWindow()
        {
            InitializeComponent();
            _testData = new TestData
            {
                Questions = new List<TestQuestion>(),
                TotalPoints = 100
            };

            cmbQuestionType.ItemsSource = new List<string> { "single", "multiple", "text" };
            cmbQuestionType.SelectedIndex = 0;
            txtTotalPoints.Text = _testData.TotalPoints.ToString();
        }

        private void btnAddQuestion_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtQuestionText.Text))
            {
                MessageBox.Show("Введите текст вопроса");
                return;
            }

            if (!int.TryParse(txtQuestionPoints.Text, out int points) || points <= 0)
            {
                MessageBox.Show("Введите корректное количество баллов");
                return;
            }

            var newQuestion = new TestQuestion
            {
                Text = txtQuestionText.Text,
                Type = cmbQuestionType.SelectedItem.ToString(),
                Points = points,
                OrderIndex = _questions.Count + 1,
                Answers = new List<TestAnswer>()
            };

            _questions.Add(newQuestion);
            _currentQuestion = newQuestion;

            RefreshQuestionsList();
            RefreshAnswersList();
            UpdateAnswerControlsVisibility();

            txtQuestionText.Clear();
            txtQuestionPoints.Text = "1";
        }

        private void btnAddAnswer_Click(object sender, RoutedEventArgs e)
        {
            if (_currentQuestion == null)
            {
                MessageBox.Show("Сначала создайте вопрос");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtAnswerText.Text))
            {
                MessageBox.Show("Введите текст ответа");
                return;
            }

            bool isCorrect = _currentQuestion.Type == "text" ? true : chkIsCorrect.IsChecked ?? false;

            if (_currentQuestion.Type == "single" && isCorrect &&
                _currentQuestion.Answers.Any(a => a.IsCorrect))
            {
                MessageBox.Show("Вопрос с одним правильным ответом может иметь только один правильный ответ");
                return;
            }

            var newAnswer = new TestAnswer
            {
                Text = txtAnswerText.Text,
                IsCorrect = isCorrect,
                OrderIndex = _currentQuestion.Answers.Count + 1
            };

            _currentQuestion.Answers.Add(newAnswer);
            RefreshAnswersList();

            txtAnswerText.Clear();
            if (_currentQuestion.Type != "text")
            {
                chkIsCorrect.IsChecked = false;
            }
        }

        private void RefreshQuestionsList()
        {
            lstQuestions.ItemsSource = null;
            lstQuestions.ItemsSource = _questions;
        }

        private void RefreshAnswersList()
        {
            lstAnswers.ItemsSource = null;
            if (_currentQuestion != null && _currentQuestion.Type != "text")
            {
                lstAnswers.ItemsSource = _currentQuestion.Answers;
            }
        }

        private void UpdateAnswerControlsVisibility()
        {
            bool isTextType = _currentQuestion?.Type == "text";
            txtAnswerText.Visibility = isTextType ? Visibility.Collapsed : Visibility.Visible;
            chkIsCorrect.Visibility = isTextType ? Visibility.Collapsed : Visibility.Visible;
            btnAddAnswer.Visibility = isTextType ? Visibility.Collapsed : Visibility.Visible;
            btnRemoveAnswer.Visibility = isTextType ? Visibility.Collapsed : Visibility.Visible;
            lstAnswers.Visibility = isTextType ? Visibility.Collapsed : Visibility.Visible;

            // Для текстовых вопросов автоматически добавляем правильный ответ
            if (isTextType && _currentQuestion.Answers.Count == 0)
            {
                var defaultAnswer = new TestAnswer
                {
                    Text = "text",
                    IsCorrect = true,
                    OrderIndex = 1
                };
                _currentQuestion.Answers.Add(defaultAnswer);
            }
        }

        private void lstQuestions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstQuestions.SelectedItem is TestQuestion selectedQuestion)
            {
                _currentQuestion = selectedQuestion;
                RefreshAnswersList();
                UpdateAnswerControlsVisibility();
            }
        }

        private void btnRemoveQuestion_Click(object sender, RoutedEventArgs e)
        {
            if (lstQuestions.SelectedItem is TestQuestion selectedQuestion)
            {
                _questions.Remove(selectedQuestion);
                if (_currentQuestion == selectedQuestion)
                {
                    _currentQuestion = null;
                    RefreshAnswersList();
                    UpdateAnswerControlsVisibility();
                }
                RefreshQuestionsList();
            }
        }

        private void btnRemoveAnswer_Click(object sender, RoutedEventArgs e)
        {
            if (_currentQuestion == null || _currentQuestion.Type == "text") return;

            if (lstAnswers.SelectedItem is TestAnswer selectedAnswer)
            {
                _currentQuestion.Answers.Remove(selectedAnswer);
                RefreshAnswersList();
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (_questions.Count == 0)
            {
                MessageBox.Show("Добавьте хотя бы один вопрос");
                return;
            }

            if (!int.TryParse(txtTotalPoints.Text, out int totalPoints) || totalPoints <= 0)
            {
                MessageBox.Show("Введите корректное общее количество баллов");
                return;
            }

            int currentTotalPoints = _questions.Sum(q => q.Points);
            if (currentTotalPoints != totalPoints)
            {
                MessageBox.Show($"Сумма баллов всех вопросов ({currentTotalPoints}) не соответствует указанному общему количеству баллов ({totalPoints})");
                return;
            }

            foreach (var question in _questions)
            {
                if (question.Type != "text" && question.Answers.Count == 0)
                {
                    MessageBox.Show($"Вопрос '{question.Text}' должен иметь хотя бы один ответ");
                    return;
                }

                if (question.Type == "single")
                {
                    int correctCount = question.Answers.Count(a => a.IsCorrect);
                    if (correctCount != 1)
                    {
                        MessageBox.Show($"Вопрос '{question.Text}' (один правильный ответ) должен иметь ровно один правильный ответ");
                        return;
                    }
                }
            }

            this.DialogResult = true;
            this.Close();
        }

        public TestData GetTestData()
        {
            if (int.TryParse(txtTotalPoints.Text, out int totalPoints))
            {
                _testData.TotalPoints = totalPoints;
            }
            _testData.Questions = _questions;
            return _testData;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void cmbQuestionType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Этот обработчик теперь не используется для изменения типа существующего вопроса
        }

        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.Column.Header.ToString() == "Type" && e.EditAction == DataGridEditAction.Commit)
            {
                var comboBox = e.EditingElement as ComboBox;
                if (comboBox != null && _currentQuestion != null)
                {
                    string newType = comboBox.SelectedItem.ToString();

                    // Проверка при смене на single, если уже есть несколько правильных ответов
                    if (newType == "single" && _currentQuestion.Answers.Count(a => a.IsCorrect) > 1)
                    {
                        MessageBox.Show("Нельзя изменить тип на 'один ответ', так как уже есть несколько правильных ответов");
                        e.Cancel = true;
                        return;
                    }

                    _currentQuestion.Type = newType;

                    // Если меняем на text, очищаем ответы и добавляем один правильный
                    if (newType == "text")
                    {
                        _currentQuestion.Answers.Clear();
                        var defaultAnswer = new TestAnswer
                        {
                            Text = "text",
                            IsCorrect = true,
                            OrderIndex = 1
                        };
                        _currentQuestion.Answers.Add(defaultAnswer);
                    }

                    RefreshAnswersList();
                    UpdateAnswerControlsVisibility();
                }
            }
        }
    }
}