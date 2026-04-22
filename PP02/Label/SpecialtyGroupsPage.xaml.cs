using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MySql.Data.MySqlClient;
using PP02.Connect;
using PP02.Classes.Specialties;

namespace PP02.Label
{
    /// <summary>
    /// Страница для управления группами специальностей (specialty_groups) и специальностями (specialties)
    /// Позволяет добавлять, редактировать и удалять группы специальностей и специальности
    /// </summary>
    public partial class SpecialtyGroupsPage : Page
    {
        private readonly string _connectionString = "server=127.0.0.1;uid=root;pwd=root;database=pp022;port=3306;";

        public SpecialtyGroupsPage()
        {
            InitializeComponent();
            LoadData();
        }

        // Загрузка данных
        private void LoadData()
        {
            try
            {
                var db = new DataProvider();
                db.DataSpecialtyGroups(_connectionString);
                db.DataSpecialties(_connectionString);

                // Заполняем ItemsControl списком групп специальностей
                SpecialtyGroupsItemsControl.ItemsSource = DataProvider.SpecialtyGroupList.ToList();

                // Заполняем DataGrid специальностей
                SpecialtiesDataGrid.ItemsSource = DataProvider.SpecialtyList.ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Добавление новой группы специальностей
        private void AddGroupButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация
                if (string.IsNullOrWhiteSpace(NewGroupNameTextBox.Text))
                {
                    MessageBox.Show("Введите название группы специальностей", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    NewGroupNameTextBox.Focus();
                    return;
                }

                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();

                    const string sql = @"
INSERT INTO `specialty_groups` (name, short_name)
VALUES (@name, @short_name);
SELECT LAST_INSERT_ID();";

                    using (var command = new MySqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@name", NewGroupNameTextBox.Text.Trim());
                        command.Parameters.AddWithValue("@short_name",
                            string.IsNullOrWhiteSpace(NewGroupShortNameTextBox.Text)
                                ? (object)DBNull.Value
                                : NewGroupShortNameTextBox.Text.Trim());

                        var result = command.ExecuteScalar();
                        int newId = Convert.ToInt32(result);

                        MessageBox.Show($"Группа специальностей \"{NewGroupNameTextBox.Text}\" успешно добавлена!\nID: {newId}",
                            "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }

                // Очищаем поля
                ClearGroupFields();

                // Перезагружаем данные
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Добавление специальности
        private void AddSpecialtyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация
                if (string.IsNullOrWhiteSpace(NewSpecialtyNameTextBox.Text))
                {
                    MessageBox.Show("Введите название специальности", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    NewSpecialtyNameTextBox.Focus();
                    return;
                }

                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();

                    const string sql = @"
INSERT INTO `specialties` (name, short_name, is_active)
VALUES (@name, @short_name, @is_active);
SELECT LAST_INSERT_ID();";

                    using (var command = new MySqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@name", NewSpecialtyNameTextBox.Text.Trim());
                        command.Parameters.AddWithValue("@short_name",
                            string.IsNullOrWhiteSpace(NewSpecialtyShortNameTextBox.Text)
                                ? (object)DBNull.Value
                                : NewSpecialtyShortNameTextBox.Text.Trim());
                        command.Parameters.AddWithValue("@is_active", NewSpecialtyIsActiveCheckBox.IsChecked == true);

                        var result = command.ExecuteScalar();
                        int newId = Convert.ToInt32(result);

                        MessageBox.Show($"Специальность \"{NewSpecialtyNameTextBox.Text}\" успешно добавлена!\nID: {newId}",
                            "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }

                // Очищаем поля
                ClearSpecialtyFields();

                // Перезагружаем данные
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Очистка полей группы
        private void ClearGroupFields()
        {
            NewGroupNameTextBox.Clear();
            NewGroupShortNameTextBox.Clear();
            NewGroupNameTextBox.Focus();
        }

        // Очистка полей специальности
        private void ClearSpecialtyFields()
        {
            NewSpecialtyNameTextBox.Clear();
            NewSpecialtyShortNameTextBox.Clear();
            NewSpecialtyIsActiveCheckBox.IsChecked = true;
            NewSpecialtyNameTextBox.Focus();
        }

        // Кнопка Назад
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService != null && NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
        }
    }
}