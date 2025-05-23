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
        /// <summary>
        /// Конструктор окна создания теста.
        /// Инициализирует тест, заполняет список типов вопросов и устанавливает значение по умолчанию.
        /// </summary>
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
        /// <summary>
        /// Обработчик нажатия кнопки добавления вопроса.
        /// Валидирует ввод, создает новый вопрос и добавляет его в список.
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
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
        /// <summary>
        /// Обработчик нажатия кнопки добавления ответа.
        /// Валидирует ввод, создает новый ответ и добавляет его к текущему вопросу.
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
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
        /// <summary>
        /// Обновляет отображение списка вопросов в интерфейсе.
        /// </summary>
        private void RefreshQuestionsList()
        {
            lstQuestions.ItemsSource = null;
            lstQuestions.ItemsSource = _questions;
        }
        /// <summary>
        /// Обновляет отображение списка ответов текущего вопроса.
        /// </summary>
        private void RefreshAnswersList()
        {
            lstAnswers.ItemsSource = null;
            if (_currentQuestion != null && _currentQuestion.Type != "text")
            {
                lstAnswers.ItemsSource = _currentQuestion.Answers;
            }
        }
        /// <summary>
        /// Обновляет видимость элементов управления для ответов в зависимости от типа вопроса.
        /// Автоматически добавляет ответ для текстового типа.
        /// </summary>
        private void UpdateAnswerControlsVisibility()
        {
            bool isTextType = _currentQuestion?.Type == "text";
            txtAnswerText.Visibility = isTextType ? Visibility.Collapsed : Visibility.Visible;
            chkIsCorrect.Visibility = isTextType ? Visibility.Collapsed : Visibility.Visible;
            btnAddAnswer.Visibility = isTextType ? Visibility.Collapsed : Visibility.Visible;
            btnRemoveAnswer.Visibility = isTextType ? Visibility.Collapsed : Visibility.Visible;
            lstAnswers.Visibility = isTextType ? Visibility.Collapsed : Visibility.Visible;

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
        /// <summary>
        /// Обработчик изменения выбранного вопроса в списке.
        /// Обновляет текущий вопрос и связанные с ним элементы интерфейса.
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
        private void lstQuestions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstQuestions.SelectedItem is TestQuestion selectedQuestion)
            {
                _currentQuestion = selectedQuestion;
                RefreshAnswersList();
                UpdateAnswerControlsVisibility();
            }
        }
        /// <summary>
        /// Обработчик кнопки удаления вопроса.
        /// Удаляет выбранный вопрос и обновляет интерфейс.
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
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
        /// <summary>
        /// Обработчик кнопки удаления ответа.
        /// Удаляет выбранный ответ из текущего вопроса.
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
        private void btnRemoveAnswer_Click(object sender, RoutedEventArgs e)
        {
            if (_currentQuestion == null || _currentQuestion.Type == "text") return;

            if (lstAnswers.SelectedItem is TestAnswer selectedAnswer)
            {
                _currentQuestion.Answers.Remove(selectedAnswer);
                RefreshAnswersList();
            }
        }
        /// <summary>
        /// Обработчик кнопки сохранения.
        /// Проверяет корректность заполнения теста, валидирует баллы и правильность структуры.
        /// Закрывает окно при успешной проверке.
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
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
        /// <summary>
        /// Возвращает заполненные данные теста (вопросы и общее количество баллов).
        /// </summary>
        /// <returns>Объект TestData</returns>
        public TestData GetTestData()
        {
            if (int.TryParse(txtTotalPoints.Text, out int totalPoints))
            {
                _testData.TotalPoints = totalPoints;
            }
            _testData.Questions = _questions;
            return _testData;
        }
        /// <summary>
        /// Обработчик кнопки отмены.
        /// Закрывает окно без сохранения данных.
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
        /// <summary>
        /// Обработчик изменения типа вопроса (в настоящее время не используется).
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
        private void cmbQuestionType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }
        /// <summary>
        /// Обработчик завершения редактирования ячейки в таблице.
        /// Обрабатывает смену типа вопроса и соответствующим образом обновляет ответы.
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.Column.Header.ToString() == "Type" && e.EditAction == DataGridEditAction.Commit)
            {
                var comboBox = e.EditingElement as ComboBox;
                if (comboBox != null && _currentQuestion != null)
                {
                    string newType = comboBox.SelectedItem.ToString();

                    if (newType == "single" && _currentQuestion.Answers.Count(a => a.IsCorrect) > 1)
                    {
                        MessageBox.Show("Нельзя изменить тип на 'один ответ', так как уже есть несколько правильных ответов");
                        e.Cancel = true;
                        return;
                    }

                    _currentQuestion.Type = newType;

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