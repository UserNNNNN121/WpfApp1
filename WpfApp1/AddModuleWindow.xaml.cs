using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1
{
    public partial class AddModuleWindow : Window
    {
        private Module _module;
        private List<ModuleItem> _moduleItems;
        private List<Module> _existingModules;

        public AddModuleWindow(List<Module> existingModules)
        {
            InitializeComponent();
            _module = new Module();
            _moduleItems = new List<ModuleItem>();
            _existingModules = existingModules;
            this.Title = "Добавление нового модуля";
        }

        public AddModuleWindow(Module module, List<ModuleItem> moduleItems, List<Module> existingModules)
        {
            InitializeComponent();
            _module = module;
            _moduleItems = new List<ModuleItem>(moduleItems); // Create a new list with the same items
            _existingModules = existingModules;
            this.Title = "Редактирование модуля";

            txtTitle.Text = module.Title;
            txtDescription.Text = module.Description;
            lstItems.ItemsSource = _moduleItems;
        }
        public Module GetModule()
        {
            _module.Title = txtTitle.Text;
            _module.Description = txtDescription.Text;
            _module.OrderIndex = _moduleItems.Count > 0 ? _moduleItems.Max(i => i.OrderIndex) + 1 : 1;
            return _module;
        }

        public List<ModuleItem> GetModuleItems()
        {
            return _moduleItems;
        }

        private void btnAddItem_Click(object sender, RoutedEventArgs e)
        {
            var itemWindow = new AddModuleItemWindow();
            if (itemWindow.ShowDialog() == true)
            {
                var newItem = itemWindow.GetModuleItem();
                newItem.OrderIndex = _moduleItems.Count > 0 ? _moduleItems.Max(i => i.OrderIndex) + 1 : 1;
                _moduleItems.Add(newItem);
                RefreshItemsList();
            }
        }

        private void btnEditItem_Click(object sender, RoutedEventArgs e)
        {
            if (lstItems.SelectedItem is ModuleItem selectedItem)
            {
                var itemWindow = new AddModuleItemWindow(selectedItem) { Owner = this };
                if (itemWindow.ShowDialog() == true)
                {
                    // Не удаляем сразу элемент, а ждем подтверждения сохранения всего модуля
                    var updatedItem = itemWindow.GetModuleItem();
                    selectedItem.Title = updatedItem.Title;
                    selectedItem.ItemType = updatedItem.ItemType;
                    selectedItem.ContentPath = updatedItem.ContentPath;
                    selectedItem.DurationMinutes = updatedItem.DurationMinutes;

                    RefreshItemsList();
                }
            }
        }


        private void btnDeleteItem_Click(object sender, RoutedEventArgs e)
        {
            if (lstItems.SelectedItem is ModuleItem selectedItem)
            {
                if (MessageBox.Show("Удалить этот элемент?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    _moduleItems.Remove(selectedItem);
                    RefreshItemsList();
                }
            }
        }

        private void RefreshItemsList()
        {
            lstItems.ItemsSource = null;
            lstItems.ItemsSource = _moduleItems;
        }
        public bool ItemTitleExists(string title, ModuleItem currentItem = null)
        {
            return _moduleItems.Any(i =>
                i.Title.Equals(title, StringComparison.OrdinalIgnoreCase) &&
                (currentItem == null || i.Id != currentItem.Id));
        }



        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTitle.Text))
            {
                MessageBox.Show("Введите название модуля");
                return;
            }

            // Check for duplicate module names in the same course
            if (_existingModules != null && _existingModules.Any(m =>
                m.Title.Equals(txtTitle.Text, StringComparison.OrdinalIgnoreCase) &&
                m.Id != _module.Id))
            {
                MessageBox.Show("Модуль с таким названием уже существует в этом курсе");
                return;
            }

            if (_moduleItems.Count == 0)
            {
                MessageBox.Show("Модуль должен содержать хотя бы один элемент");
                return;
            }

            // Проверка на дубликаты названий элементов
            var duplicateItems = _moduleItems
                .GroupBy(i => i.Title.ToLower())
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateItems.Any())
            {
                MessageBox.Show($"Найдены элементы с одинаковыми названиями: {string.Join(", ", duplicateItems)}");
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