using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static WpfApp1.AdminControl;

namespace WpfApp1
{
    public partial class FAQs : Window
    {

        public int _userId;
        public bool _isAdmin;
        /// <summary>
        /// Конструктор окна "Часто задаваемые вопросы".
        /// Учитывает роль пользователя и настраивает видимость элементов интерфейса.
        /// </summary>
        /// <param name="isAdmin">Флаг, указывающий, является ли пользователь администратором</param>
        /// <param name="userId">Идентификатор пользователя</param>
        public FAQs(bool isAdmin, int userId)
        {
            InitializeComponent();
            this._userId = userId;
            this._isAdmin = isAdmin;
            if (_userId == -1) btnExit.Visibility = Visibility.Collapsed;
            else if (!_isAdmin)
            {
                btnAdmin.Visibility = Visibility.Collapsed;
                btnSupport.Visibility = Visibility.Visible;
                notFoundPanel.Visibility = Visibility.Visible;
                btnMain.Visibility = Visibility.Visible;
                btnExit.Visibility = Visibility.Visible;
            }
            else
            {
                btnMain.Visibility = Visibility.Collapsed;
                notFoundPanel.Visibility = Visibility.Collapsed;
                btnAdmin.Visibility = Visibility.Visible;
                btnSupport.Visibility = Visibility.Collapsed;
                btnExit.Visibility = Visibility.Visible;
            }
            CreateFAQItems();
        }

        private List<FAQItem> _faqItems = new List<FAQItem>
        {
            new FAQItem
            {
                Question = "Как зарегистрироваться на платформе?",
                Answer = "Для регистрации на платформе нажмите кнопку 'Регистрация' на главной странице, выберите вашу роль, специализацию, заполните необходимые поля и подтвердите email."
            },
            new FAQItem
            {
                Question = "Как восстановить доступ к аккаунту?",
                Answer = "На главной странице входа нажмите 'Забыли пароль?', введите email, указанный при регистрации, и дождитесь кода. Затем задайте новый пароль, восстановив доступ."
            },
                        new FAQItem
            {
                Question = "Как изменить пароль?",
                Answer = "Через личный кабинет или через восстановление доступа."
            },
            new FAQItem
            {
                Question = "Можно ли получить сертификат после прохождения курса?",
                Answer = "Да, после успешного прохождения курса и сдачи финального теста вы получите сертификат в электронном виде."
            },
            new FAQItem
            {
                Question = "Как связаться с поддержкой?",
                Answer = "Вы можете написать в службу поддержки через кнопку меню 'Служба Поддержки' или кнопку 'Обратиться в службу поддержки' ниже. Заполните форму и отправите ваш запрос."
            }
        };
        /// <summary>
        /// Создает визуальные элементы FAQ на основе заранее заданного списка вопросов и ответов.
        /// Устанавливает стили и обработчики событий для раскрытия ответов.
        /// </summary>
        private void CreateFAQItems()
        {
            foreach (var item in _faqItems)
            {
                var faqGrid = new Grid();
                faqGrid.Style = (Style)FindResource("FAQItemStyle");

                faqGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                faqGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var questionGrid = new Grid();
                questionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                questionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var questionText = new TextBlock
                {
                    Text = item.Question,
                    Style = (Style)FindResource("QuestionTextStyle")
                };
                Grid.SetColumn(questionText, 0);

                var expandIcon = new Image
                {
                    Source = new BitmapImage(new Uri("hidden.png", UriKind.Relative)),
                    Width = 20,
                    Height = 20,
                    Margin = new Thickness(0, 0, 10, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(expandIcon, 1);

                questionGrid.Children.Add(questionText);
                questionGrid.Children.Add(expandIcon);
                Grid.SetRow(questionGrid, 0);

                var answerText = new TextBlock
                {
                    Text = item.Answer,
                    Style = (Style)FindResource("AnswerTextStyle"),
                    Visibility = Visibility.Collapsed
                };
                Grid.SetRow(answerText, 1);

                faqGrid.Children.Add(questionGrid);
                faqGrid.Children.Add(answerText);

                faqGrid.MouseLeftButtonDown += (sender, e) =>
                {
                    var grid = (Grid)sender;
                    var answer = (TextBlock)grid.Children[1];
                    var icon = (Image)((Grid)grid.Children[0]).Children[1];

                    if (answer.Visibility == Visibility.Visible)
                    {
                        answer.Visibility = Visibility.Collapsed;
                        icon.Source = new BitmapImage(new Uri("hidden.png", UriKind.Relative));
                    }
                    else
                    {
                        answer.Visibility = Visibility.Visible;
                        icon.Source = new BitmapImage(new Uri("hide.png", UriKind.Relative));
                    }
                };

                FAQPanel.Children.Add(faqGrid);
            }
        }
        /// <summary>
        /// Копирует свойства текущего окна (размер, позиция, состояние) в следующее окно.
        /// Используется для плавного перехода между окнами без изменения их внешнего вида.
        /// </summary>
        /// <param name="nextWindow">Окно, в которое копируются свойства</param>
        private void WindowProperties(Window nextWindow)
        {
            nextWindow.Left = this.Left;
            nextWindow.Top = this.Top;
            nextWindow.Height = this.Height;
            nextWindow.Width = this.Width;
            nextWindow.WindowState = this.WindowState;
        }
        /// <summary>
        /// Обработчик кнопки перехода на главное окно.
        /// В зависимости от наличия авторизации открывает либо Main, либо MainWindow.
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
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
        /// <summary>
        /// Обработчик кнопки повторного открытия окна FAQ.
        /// Перезапускает окно с сохранением позиции и состояния.
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
        /// Обработчик кнопки перехода в окно поддержки.
        /// Открывает окно поддержки и закрывает текущее.
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
        /// Обработчик кнопки перехода в окно администратора.
        /// Открывает административный интерфейс при наличии прав.
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
        /// Обработчик кнопки перехода в личный кабинет.
        /// Проверяет авторизацию пользователя, иначе показывает сообщение об ошибке.
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
        /// <summary>
        /// Обработчик кнопки выхода.
        /// Открывает главное окно и завершает текущее.
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
        /// Класс модели данных для одного элемента FAQ.
        /// Содержит вопрос и соответствующий ответ.
        /// </summary>
        public class FAQItem
        {
            public string Question { get; set; }
            public string Answer { get; set; }
        }
    }
}