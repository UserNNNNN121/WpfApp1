// AddModuleItemWindow.xaml.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using System.IO;
using Newtonsoft.Json; 
namespace WpfApp1
{
    public partial class AddModuleItemWindow : Window
    {
        private ModuleItem _moduleItem;
        /// <summary>
        /// Конструктор окна добавления нового элемента модуля.
        /// Инициализирует компоненты, очищает и заполняет выпадающий список типов элементов.
        /// </summary>
        public AddModuleItemWindow()
        {
            InitializeComponent();
            _moduleItem = new ModuleItem();

            cmbItemType.Items.Clear();
            cmbItemType.Items.Add("lecture");
            cmbItemType.Items.Add("video");
            cmbItemType.Items.Add("test");

            cmbItemType.SelectedIndex = 0;
            this.Title = "Добавление элемента модуля";
        }
        /// <summary>
        /// Конструктор окна редактирования элемента модуля.
        /// Загружает данные существующего элемента и отображает их в соответствующих полях.
        /// </summary>
        /// <param name="item">Существующий элемент модуля для редактирования</param>
        public AddModuleItemWindow(ModuleItem item)
        {
            InitializeComponent();
            _moduleItem = item;
            this.Title = "Редактирование элемента модуля";

            cmbItemType.Items.Clear();
            cmbItemType.Items.Add("lecture");
            cmbItemType.Items.Add("video");
            cmbItemType.Items.Add("test");

            txtTitle.Text = item.Title;

            cmbItemType.SelectedItem = item.ItemType?.ToLower();

            txtFilePath.Text = item.ContentPath;
            txtDuration.Text = item.DurationMinutes?.ToString();
        }
        /// <summary>
        /// Возвращает объект ModuleItem, заполненный на основе данных, введенных пользователем.
        /// Производит валидацию типа элемента и продолжительности видео.
        /// </summary>
        /// <returns>Объект ModuleItem</returns>
        public ModuleItem GetModuleItem()
        {
            _moduleItem.Title = txtTitle.Text;


            var selectedType = cmbItemType.SelectedItem?.ToString()?.ToLower();
            _moduleItem.ItemType = new[] { "lecture", "video", "test" }.Contains(selectedType)
                ? selectedType
                : "lecture"; 

            _moduleItem.ContentPath = txtFilePath.Text;

            if (int.TryParse(txtDuration.Text, out int duration))
            {
                _moduleItem.DurationMinutes = duration;
            }
            else
            {
                _moduleItem.DurationMinutes = null;
            }

            return _moduleItem;
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Обзор".
        /// В зависимости от выбранного типа элемента открывает окно создания теста
        /// или диалог выбора файла (текст или видео).
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            if (cmbItemType.SelectedValue.ToString() == "test")
            {
                var testWindow = new CreateTestWindow();
                if (testWindow.ShowDialog() == true)
                {
                    var testData = testWindow.GetTestData();
                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(testData);
                    string tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".json");
                    File.WriteAllText(tempPath, json);
                    txtFilePath.Text = tempPath;
                }
                return;
            }

            var openFileDialog = new OpenFileDialog();
            switch (cmbItemType.SelectedValue.ToString())
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
                txtFilePath.Text = openFileDialog.FileName;
            }
        }
        /// <summary>
        /// Обработчик изменения выбранного значения в выпадающем списке типов элементов.
        /// Показывает или скрывает поле длительности в зависимости от типа "video".
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
        private void cmbItemType_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cmbItemType.SelectedValue != null)
            {
                lblDuration.Visibility = cmbItemType.SelectedValue.ToString() == "video" ?
                    Visibility.Visible : Visibility.Collapsed;
                txtDuration.Visibility = cmbItemType.SelectedValue.ToString() == "video" ?
                    Visibility.Visible : Visibility.Collapsed;
            }
        }
        /// <summary>
        /// Обработчик нажатия кнопки "Сохранить".
        /// Валидирует введенные данные, проверяет уникальность названия,
        /// проверяет корректность длительности для видео, закрывает окно при успешном вводе.
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTitle.Text))
            {
                MessageBox.Show("Введите название элемента");
                return;
            }

            var parentWindow = Owner as AddModuleWindow;
            if (parentWindow != null && parentWindow.ItemTitleExists(txtTitle.Text, _moduleItem))
            {
                MessageBox.Show("Элемент с таким названием уже существует в этом модуле");
                return;
            }

            if (cmbItemType.SelectedValue.ToString() == "video" &&
                !int.TryParse(txtDuration.Text, out _))
            {
                MessageBox.Show("Введите корректную длительность видео");
                return;
            }

            this.DialogResult = true;
            this.Close();
        }
        /// <summary>
        /// Обработчик нажатия кнопки "Отмена".
        /// Закрывает окно без сохранения данных.
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }

}