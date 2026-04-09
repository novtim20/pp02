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
    /// Страница для полного управления группами и специальностями
    /// </summary>
    public partial class GroupSpecialtyPage : Page
    {
        private readonly string _connectionString = "server=127.0.0.1;uid=root;pwd=root;database=pp02;port=3306;";

        public GroupSpecialtyPage()
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
                db.DataSpecialties(_connectionString);
                db.DataGroups(_connectionString);

                // Заполняем DataGrid специальностей
                SpecialtiesDataGrid.ItemsSource = DataProvider.SpecialtyList.ToList();

                // Заполняем DataGrid групп
                GroupsDataGrid.ItemsSource = DataProvider.GroupList.ToList();

                // Заполняем ComboBox специальностей для добавления группы
                NewGroupSpecialtyComboBox.ItemsSource = DataProvider.SpecialtyList
                    .Where(s => s.IsActive)
                    .ToList();

                if (DataProvider.SpecialtyList.Count > 0)
                {
                    NewGroupSpecialtyComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Добавление специальности
        private void AddSpecialtyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация
                if (string.IsNullOrWhiteSpace(NewSpecialtyCodeTextBox.Text))
                {
                    MessageBox.Show("Введите код специальности", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    NewSpecialtyCodeTextBox.Focus();
                    return;
                }

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
INSERT INTO `specialties` (code, name, is_active)
VALUES (@code, @name, @is_active);
SELECT LAST_INSERT_ID();";

                    using (var command = new MySqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@code", NewSpecialtyCodeTextBox.Text.Trim());
                        command.Parameters.AddWithValue("@name", NewSpecialtyNameTextBox.Text.Trim());
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

        // Добавление группы
        private void AddGroupButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация
                if (string.IsNullOrWhiteSpace(NewGroupCodeTextBox.Text))
                {
                    MessageBox.Show("Введите код группы", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    NewGroupCodeTextBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(NewGroupShortNameTextBox.Text))
                {
                    MessageBox.Show("Введите сокращение группы", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    NewGroupShortNameTextBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(NewGroupNameTextBox.Text))
                {
                    MessageBox.Show("Введите отображаемое название группы", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    NewGroupNameTextBox.Focus();
                    return;
                }

                if (NewGroupSpecialtyComboBox.SelectedValue == null)
                {
                    MessageBox.Show("Выберите специальность", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    NewGroupSpecialtyComboBox.Focus();
                    return;
                }

                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();

                    const string sql = @"
INSERT INTO `groups` (code, short_name, name, specialty_id, is_active)
VALUES (@code, @short_name, @name, @specialty_id, @is_active);
SELECT LAST_INSERT_ID();";

                    using (var command = new MySqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@code", NewGroupCodeTextBox.Text.Trim());
                        command.Parameters.AddWithValue("@short_name", NewGroupShortNameTextBox.Text.Trim());
                        command.Parameters.AddWithValue("@name", NewGroupNameTextBox.Text.Trim());
                        command.Parameters.AddWithValue("@specialty_id", NewGroupSpecialtyComboBox.SelectedValue);
                        command.Parameters.AddWithValue("@is_active", NewGroupIsActiveCheckBox.IsChecked == true);

                        var result = command.ExecuteScalar();
                        int newId = Convert.ToInt32(result);

                        MessageBox.Show($"Группа \"{NewGroupNameTextBox.Text}\" успешно добавлена!\nID: {newId}",
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

        // Очистка полей специальности
        private void ClearSpecialtyFields()
        {
            NewSpecialtyCodeTextBox.Clear();
            NewSpecialtyNameTextBox.Clear();
            NewSpecialtyShortNameTextBox.Clear();
            NewSpecialtyIsActiveCheckBox.IsChecked = true;
            NewSpecialtyCodeTextBox.Focus();
        }

        // Очистка полей группы
        private void ClearGroupFields()
        {
            NewGroupCodeTextBox.Clear();
            NewGroupShortNameTextBox.Clear();
            NewGroupNameTextBox.Clear();
            NewGroupIsActiveCheckBox.IsChecked = true;
            if (DataProvider.SpecialtyList.Count > 0)
            {
                NewGroupSpecialtyComboBox.SelectedIndex = 0;
            }
            NewGroupCodeTextBox.Focus();
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