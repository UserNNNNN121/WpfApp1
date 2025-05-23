using System;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1
{
    public partial class Role : Window
    {
        private bool _role;
        /// <summary>
        /// Конструктор окна выбора роли.
        /// Инициализирует компоненты и назначает обработчики кликов по кнопкам ролей.
        /// </summary>
        public Role()
        {
            InitializeComponent();
            _role = false;

            btnEngineer.Click += RoleButton_Click;
            btnTechologist.Click += RoleButton_Click;
            btnLawyer.Click += RoleButton_Click;
            btnFinancist.Click += RoleButton_Click;
            btnITspec.Click += RoleButton_Click;
            btnAccountist.Click += RoleButton_Click;
        }
        /// <summary>
        /// Обработчик нажатия на кнопку выбора роли.
        /// Определяет специальность на основе выбранной кнопки,
        /// открывает окно регистрации с передачей кода специальности.
        /// </summary>
        /// <param name="sender">Источник события — нажатая кнопка</param>
        /// <param name="e">Аргументы события</param>
        private void RoleButton_Click(object sender, RoutedEventArgs e)
        {
            Button clickedButton = (Button)sender;
            int speciality = 7;

            if (clickedButton == btnEngineer)
                speciality = 4;
            else if (clickedButton == btnTechologist)
                speciality = 6;
            else if (clickedButton == btnLawyer)
                speciality = 5;
            else if (clickedButton == btnFinancist)
                speciality = 3;
            else if (clickedButton == btnITspec)
                speciality = 1;
            else if (clickedButton == btnAccountist)
                speciality = 2;

            Window registration = new Registration(speciality);
            WindowProperties(registration);
            registration.Show();
            this.Close();
        }
        /// <summary>
        /// Устанавливает позицию и размеры нового окна аналогично текущему окну.
        /// Используется для переноса внешнего вида между окнами.
        /// </summary>
        /// <param name="nextWindow">Окно, которому устанавливаются параметры</param>
        private void WindowProperties(Window nextWindow)
        {
            nextWindow.Left = this.Left;
            nextWindow.Top = this.Top;
            nextWindow.Height = this.Height;
            nextWindow.Width = this.Width;
            nextWindow.WindowState = this.WindowState;
        }
        /// <summary>
        /// Обработчик нажатия на кнопку перехода на главное окно.
        /// Открывает главное окно и закрывает текущее.
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
        private void btnMain_Click(object sender, RoutedEventArgs e)
        {
            Window mainWindow = new MainWindow();
            WindowProperties(mainWindow);
            mainWindow.Show();
            this.Close();
        }
        /// <summary>
        /// Обработчик нажатия кнопки "Поддержка".
        /// Открывает окно поддержки с передачей параметров и закрывает текущее окно.
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
        private void btnSupport_Click(object sender, RoutedEventArgs e)
        {
            Window support = new Support(false, -1);
            WindowProperties(support);
            support.Show();
            this.Close();
        }
        /// <summary>
        /// Обработчик нажатия кнопки "Часто задаваемые вопросы".
        /// Открывает окно FAQ и закрывает текущее.
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
        private void btnFAQs_Click(object sender, RoutedEventArgs e)
        {
            Window faqs = new FAQs(false, -1);
            WindowProperties(faqs);
            faqs.Show();
            this.Close();
        }
        /// <summary>
        /// Обработчик нажатия кнопки "Назад".
        /// Возвращает пользователя на главное окно.
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            Window mainWindow = new MainWindow();
            WindowProperties(mainWindow);
            mainWindow.Show();
            this.Close();
        }
        /// <summary>
        /// Обработчик нажатия кнопки "Аккаунт".
        /// Показывает сообщение об ошибке, если пользователь не авторизован.
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
        private void btnAccount_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
            "Ошибка: сначала зарегистрируйтесь или войдите в аккаунт",
            "Ошибка доступа",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
            return;
        }
    }
}