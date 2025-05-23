using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1
{
    public partial class EditCourseWindow : Window
    {
        private int _courseId;
        private List<Module> _modules = new List<Module>();
        private List<ModuleItem> _moduleItems = new List<ModuleItem>();
        private static readonly string ConnectionString =
            $"Data Source={Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "educatingsystem.sl3")};Version=3;";

        public EditCourseWindow(int courseId)
        {
            InitializeComponent();
            _courseId = courseId;
            LoadCourseData();
            LoadModules();
        }

        private void LoadCourseData()
        {
            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    string query = @"SELECT c.title, c.description, 
                           c.speciality_id, s.name as speciality_name, 
                           c.availability,
                           c.partnership as partner_id, p.name as partner_name
                           FROM courses c
                           LEFT JOIN specialities s ON c.speciality_id = s.id
                           LEFT JOIN partners p ON c.partnership = p.id
                           WHERE c.id = @courseId";

                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@courseId", _courseId);
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                txtTitle.Text = reader.GetString(0);
                                txtDescription.Text = !reader.IsDBNull(1) ? reader.GetString(1) : "";
                                // Загрузка остальных данных курса...
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных курса: {ex.Message}");
            }
        }

        private void LoadModules()
        {
            try
            {
                _modules.Clear();
                _moduleItems.Clear();

                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();

                    // Загрузка модулей
                    string modulesQuery = "SELECT id, title, description, order_index FROM modules WHERE course_id = @courseId ORDER BY order_index";
                    using (var command = new SQLiteCommand(modulesQuery, connection))
                    {
                        command.Parameters.AddWithValue("@courseId", _courseId);
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                _modules.Add(new Module
                                {
                                    Id = reader.GetInt32(0),
                                    Title = reader.GetString(1),
                                    Description = !reader.IsDBNull(2) ? reader.GetString(2) : "",
                                    OrderIndex = reader.GetInt32(3),
                                    CourseId = _courseId
                                });
                            }
                        }
                    }

                    // Загрузка элементов модулей
                    string itemsQuery = @"SELECT mi.id, mi.module_id, mi.item_type, mi.title, 
                                        mi.content_path, mi.external_url, mi.duration_minutes, mi.order_index
                                        FROM module_items mi
                                        JOIN modules m ON mi.module_id = m.id
                                        WHERE m.course_id = @courseId
                                        ORDER BY mi.order_index";

                    using (var command = new SQLiteCommand(itemsQuery, connection))
                    {
                        command.Parameters.AddWithValue("@courseId", _courseId);
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                _moduleItems.Add(new ModuleItem
                                {
                                    Id = reader.GetInt32(0),
                                    ModuleId = reader.GetInt32(1),
                                    ItemType = reader.GetString(2),
                                    Title = reader.GetString(3),
                                    ContentPath = !reader.IsDBNull(4) ? reader.GetString(4) : null,
                                    ExternalUrl = !reader.IsDBNull(5) ? reader.GetString(5) : null,
                                    DurationMinutes = !reader.IsDBNull(6) ? reader.GetInt32(6) : (int?)null,
                                    OrderIndex = reader.GetInt32(7)
                                });
                            }
                        }
                    }
                }

                lstModules.ItemsSource = null;
                lstModules.ItemsSource = _modules;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке модулей: {ex.Message}");
            }
        }

        private void btnAddModule_Click(object sender, RoutedEventArgs e)
        {
            var moduleWindow = new AddModuleWindow(_modules);
            if (moduleWindow.ShowDialog() == true)
            {
                var newModule = moduleWindow.GetModule();
                newModule.Id = _modules.Count > 0 ? _modules.Max(m => m.Id) + 1 : 1;
                newModule.CourseId = _courseId;

                foreach (var item in moduleWindow.GetModuleItems())
                {
                    item.ModuleId = newModule.Id;
                    _moduleItems.Add(item);
                }

                _modules.Add(newModule);
                RefreshModulesList();
            }
        }

        private void btnEditModule_Click(object sender, RoutedEventArgs e)
        {
            if (lstModules.SelectedItem is Module selectedModule)
            {
                var moduleItems = _moduleItems.Where(i => i.ModuleId == selectedModule.Id).ToList();
                var moduleWindow = new AddModuleWindow(selectedModule, moduleItems, _modules);

                if (moduleWindow.ShowDialog() == true)
                {
                    selectedModule.Title = moduleWindow.GetModule().Title;
                    selectedModule.Description = moduleWindow.GetModule().Description;

                    _moduleItems.RemoveAll(i => i.ModuleId == selectedModule.Id);

                    foreach (var item in moduleWindow.GetModuleItems())
                    {
                        item.ModuleId = selectedModule.Id;
                        _moduleItems.Add(item);
                    }

                    RefreshModulesList();
                }
            }
        }

        private void btnDeleteModule_Click(object sender, RoutedEventArgs e)
        {
            if (lstModules.SelectedItem is Module selectedModule)
            {
                if (MessageBox.Show("Удалить этот модуль?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    _modules.Remove(selectedModule);
                    _moduleItems.RemoveAll(i => i.ModuleId == selectedModule.Id);
                    RefreshModulesList();
                }
            }
        }

        private void RefreshModulesList()
        {
            lstModules.ItemsSource = null;
            lstModules.ItemsSource = _modules;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Обновляем данные курса
                            string updateCourseQuery = @"UPDATE courses SET 
                                title = @title, 
                                description = @description
                                WHERE id = @courseId";

                            using (var command = new SQLiteCommand(updateCourseQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@title", txtTitle.Text);
                                command.Parameters.AddWithValue("@description", txtDescription.Text);
                                command.Parameters.AddWithValue("@courseId", _courseId);
                                command.ExecuteNonQuery();
                            }

                            // Сохраняем изменения в модулях и элементах
                            SaveModulesAndItems(connection, transaction);

                            transaction.Commit();
                            MessageBox.Show("Изменения сохранены успешно!");
                            this.DialogResult = true;
                            this.Close();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            MessageBox.Show($"Ошибка при сохранении изменений: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения: {ex.Message}");
            }
        }

        private void SaveModulesAndItems(SQLiteConnection connection, SQLiteTransaction transaction)
        {
            // Удаляем старые модули и элементы
            string deleteItemsQuery = "DELETE FROM module_items WHERE module_id IN (SELECT id FROM modules WHERE course_id = @courseId)";
            using (var command = new SQLiteCommand(deleteItemsQuery, connection, transaction))
            {
                command.Parameters.AddWithValue("@courseId", _courseId);
                command.ExecuteNonQuery();
            }

            string deleteModulesQuery = "DELETE FROM modules WHERE course_id = @courseId";
            using (var command = new SQLiteCommand(deleteModulesQuery, connection, transaction))
            {
                command.Parameters.AddWithValue("@courseId", _courseId);
                command.ExecuteNonQuery();
            }

            // Добавляем новые модули и элементы
            foreach (var module in _modules)
            {
                string insertModuleQuery = @"INSERT INTO modules 
                    (course_id, title, description, order_index) 
                    VALUES (@courseId, @title, @description, @orderIndex);
                    SELECT last_insert_rowid();";

                int moduleId;
                using (var command = new SQLiteCommand(insertModuleQuery, connection, transaction))
                {
                    command.Parameters.AddWithValue("@courseId", _courseId);
                    command.Parameters.AddWithValue("@title", module.Title);
                    command.Parameters.AddWithValue("@description", module.Description ?? "");
                    command.Parameters.AddWithValue("@orderIndex", module.OrderIndex);
                    moduleId = Convert.ToInt32(command.ExecuteScalar());
                }

                // Добавляем элементы модуля
                foreach (var item in _moduleItems.Where(i => i.ModuleId == module.Id))
                {
                    string insertItemQuery = @"INSERT INTO module_items 
                        (module_id, item_type, title, content_path, external_url, duration_minutes, order_index) 
                        VALUES (@moduleId, @itemType, @title, @contentPath, @externalUrl, @duration, @orderIndex)";

                    using (var command = new SQLiteCommand(insertItemQuery, connection, transaction))
                    {
                        command.Parameters.AddWithValue("@moduleId", moduleId);
                        command.Parameters.AddWithValue("@itemType", item.ItemType);
                        command.Parameters.AddWithValue("@title", item.Title);
                        command.Parameters.AddWithValue("@contentPath", item.ContentPath ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@externalUrl", item.ExternalUrl ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@duration", item.DurationMinutes ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@orderIndex", item.OrderIndex);
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}