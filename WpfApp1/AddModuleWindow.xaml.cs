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
        /// <summary>
        /// Конструктор окна добавления нового модуля.
        /// Инициализирует компонент, список элементов модуля и список существующих модулей.
        /// </summary>
        /// <param name="existingModules">Список существующих модулей курса</param>
        public AddModuleWindow(List<Module> existingModules)
        {
            InitializeComponent();
            _module = new Module();
            _moduleItems = new List<ModuleItem>();
            _existingModules = existingModules;
            this.Title = "Добавление нового модуля";
        }
        /// <summary>
        /// Конструктор окна редактирования существующего модуля.
        /// Загружает данные модуля и его элементы, устанавливает значения в поля формы.
        /// </summary>
        /// <param name="module">Редактируемый модуль</param>
        /// <param name="moduleItems">Список элементов модуля</param>
        /// <param name="existingModules">Список существующих модулей курса</param>
        public AddModuleWindow(Module module, List<ModuleItem> moduleItems, List<Module> existingModules)
        {
            InitializeComponent();
            _module = module;
            _moduleItems = new List<ModuleItem>(moduleItems); 
            _existingModules = existingModules;
            this.Title = "Редактирование модуля";

            txtTitle.Text = module.Title;
            txtDescription.Text = module.Description;
            lstItems.ItemsSource = _moduleItems;
        }
        /// <summary>
        /// Возвращает объект модуля с актуальными данными из формы.
        /// Обновляет название, описание и порядковый индекс модуля.
        /// </summary>
        /// <returns>Объект модуля</returns>
        public Module GetModule()
        {
            _module.Title = txtTitle.Text;
            _module.Description = txtDescription.Text;
            _module.OrderIndex = _moduleItems.Count > 0 ? _moduleItems.Max(i => i.OrderIndex) + 1 : 1;
            return _module;
        }
        /// <summary>
        /// Возвращает текущий список элементов модуля.
        /// </summary>
        /// <returns>Список элементов модуля</returns>
        public List<ModuleItem> GetModuleItems()
        {
            return _moduleItems;
        }
        /// <summary>
        /// Обработчик нажатия кнопки добавления элемента.
        /// Открывает окно создания нового элемента и добавляет его в список при подтверждении.
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
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
        /// <summary>
        /// Обработчик нажатия кнопки редактирования элемента.
        /// Открывает окно редактирования выбранного элемента и обновляет его данные после сохранения.
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
        private void btnEditItem_Click(object sender, RoutedEventArgs e)
        {
            if (lstItems.SelectedItem is ModuleItem selectedItem)
            {
                var itemWindow = new AddModuleItemWindow(selectedItem) { Owner = this };
                if (itemWindow.ShowDialog() == true)
                {
                    var updatedItem = itemWindow.GetModuleItem();
                    selectedItem.Title = updatedItem.Title;
                    selectedItem.ItemType = updatedItem.ItemType;
                    selectedItem.ContentPath = updatedItem.ContentPath;
                    selectedItem.DurationMinutes = updatedItem.DurationMinutes;

                    RefreshItemsList();
                }
            }
        }

        /// <summary>
        /// Обработчик нажатия кнопки удаления элемента.
        /// Удаляет выбранный элемент из списка после подтверждения пользователя.
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
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
        /// <summary>
        /// Обновляет отображение списка элементов модуля в интерфейсе.
        /// Используется после добавления, удаления или изменения элемента.
        /// </summary>
        private void RefreshItemsList()
        {
            lstItems.ItemsSource = null;
            lstItems.ItemsSource = _moduleItems;
        }
        /// <summary>
        /// Проверяет наличие элемента с заданным названием в текущем списке элементов модуля.
        /// Учитывает, что текущий редактируемый элемент может быть пропущен из проверки.
        /// </summary>
        /// <param name="title">Название элемента</param>
        /// <param name="currentItem">Текущий элемент (может быть null)</param>
        /// <returns>True, если элемент с таким названием уже существует</returns>
        public bool ItemTitleExists(string title, ModuleItem currentItem = null)
        {
            return _moduleItems.Any(i =>
                i.Title.Equals(title, StringComparison.OrdinalIgnoreCase) &&
                (currentItem == null || i.Id != currentItem.Id));
        }


        /// <summary>
        /// Обработчик нажатия кнопки "Сохранить".
        /// Проверяет корректность ввода, уникальность названий модуля и его элементов.
        /// Закрывает окно с положительным результатом при успешной проверке.
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTitle.Text))
            {
                MessageBox.Show("Введите название модуля");
                return;
            }

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
        /// <summary>
        /// Обработчик нажатия кнопки "Отмена".
        /// Закрывает окно без сохранения изменений.
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