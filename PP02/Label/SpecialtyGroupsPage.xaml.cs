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
    /// Страница для управления группами специальностей (specialty_groups)
    /// Позволяет добавлять, редактировать и удалять группы специальностей
    ///以及管理每组内的专业
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

                // Заполняем ItemsControl списком групп специальностей
                // Данные уже отсортированы по дате создания (DESC) в DataProvider
                SpecialtyGroupsItemsControl.ItemsSource = DataProvider.SpecialtyGroupList.ToList();
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

                if (!NewGroupCreatedAtDatePicker.SelectedDate.HasValue)
                {
                    MessageBox.Show("Выберите дату создания группы", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    NewGroupCreatedAtDatePicker.Focus();
                    return;
                }

                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();

                    const string sql = @"
INSERT INTO `specialty_groups` (name, description, created_at, is_active)
VALUES (@name, @description, @created_at, @is_active);
SELECT LAST_INSERT_ID();";

                    using (var command = new MySqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@name", NewGroupNameTextBox.Text.Trim());
                        command.Parameters.AddWithValue("@description",
                            string.IsNullOrWhiteSpace(NewGroupDescriptionTextBox.Text)
                                ? (object)DBNull.Value
                                : NewGroupDescriptionTextBox.Text.Trim());
                        command.Parameters.AddWithValue("@created_at", NewGroupCreatedAtDatePicker.SelectedDate.Value);
                        command.Parameters.AddWithValue("@is_active", NewGroupIsActiveCheckBox.IsChecked == true);

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

        // Очистка полей группы
        private void ClearGroupFields()
        {
            NewGroupNameTextBox.Clear();
            NewGroupDescriptionTextBox.Clear();
            NewGroupCreatedAtDatePicker.SelectedDate = DateTime.Now;
            NewGroupIsActiveCheckBox.IsChecked = true;
            NewGroupNameTextBox.Focus();
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