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

        public AddModuleItemWindow()
        {
            InitializeComponent();
            _moduleItem = new ModuleItem();

            // Clear any existing items and set allowed values
            cmbItemType.Items.Clear();
            cmbItemType.Items.Add("lecture");
            cmbItemType.Items.Add("video");
            cmbItemType.Items.Add("test");

            cmbItemType.SelectedIndex = 0;
            this.Title = "Добавление элемента модуля";
        }

        public AddModuleItemWindow(ModuleItem item)
        {
            InitializeComponent();
            _moduleItem = item;
            this.Title = "Редактирование элемента модуля";

            // Clear any existing items and set allowed values
            cmbItemType.Items.Clear();
            cmbItemType.Items.Add("lecture");
            cmbItemType.Items.Add("video");
            cmbItemType.Items.Add("test");

            // Fill the fields
            txtTitle.Text = item.Title;

            // Set the selected item (must match exactly, case-sensitive)
            cmbItemType.SelectedItem = item.ItemType?.ToLower(); // Ensure lowercase

            txtFilePath.Text = item.ContentPath;
            txtDuration.Text = item.DurationMinutes?.ToString();
        }

        public ModuleItem GetModuleItem()
        {
            _moduleItem.Title = txtTitle.Text;

            // Ensure valid item type
            var selectedType = cmbItemType.SelectedItem?.ToString()?.ToLower();
            _moduleItem.ItemType = new[] { "lecture", "video", "test" }.Contains(selectedType)
                ? selectedType
                : "lecture"; // Default value

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

        // In AddModuleItemWindow.xaml.cs, modify the btnBrowse_Click method for test type
        // В AddModuleItemWindow.xaml.cs изменяем метод btnBrowse_Click для теста
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

            // Остальной код для lecture и video остается без изменений
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
        // Modified AddModuleItemWindow.xaml.cs
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTitle.Text))
            {
                MessageBox.Show("Введите название элемента");
                return;
            }

            // Проверка на уникальность названия элемента
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
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }

}