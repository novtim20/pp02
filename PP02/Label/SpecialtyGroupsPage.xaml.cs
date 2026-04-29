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
        private readonly string _connectionString = Connect.Connect.GetConnectionString();
        private bool _isEditingSpecialty = false;
        private int? _editingSpecialtyId = null;
        private bool _isEditingGroup = false;
        private int? _editingGroupId = null;

        public SpecialtyGroupsPage()
        {
            InitializeComponent();
            LoadData();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Перезагружаем данные при загрузке страницы
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
                db.DataGroups(_connectionString);

                // Заполняем ItemsControl списком групп специальностей
                SpecialtyGroupsItemsControl.ItemsSource = DataProvider.SpecialtyGroupList.ToList();

                // Заполняем DataGrid специальностей
                SpecialtiesDataGrid.ItemsSource = DataProvider.SpecialtyList.ToList();

                // Заполняем DataGrid групп (учебных)
                GroupsDataGrid.ItemsSource = DataProvider.GroupList.ToList();

                // Заполняем ComboBox специальностей для группы
                NewGroupSpecialtyComboBox.ItemsSource = DataProvider.SpecialtyList
                    .Where(s => s.IsActive)
                    .ToList();

                // Устанавливаем DataContext для ComboBox в DataGrid
                this.DataContext = this;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Свойство для доступа к списку групп из XAML
        public List<SpecialtyGroup> SpecialtyGroups => DataProvider.SpecialtyGroupList.ToList();

        // Добавление новой группы специальностей
        private void AddGroupButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация
                if (string.IsNullOrWhiteSpace(NewGroupNameTextBox.Text))
                {
                    MessageBox.Show("Введите название Отделения специальностей", "Ошибка",
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

                        MessageBox.Show($"Отделение специальностей \"{NewGroupNameTextBox.Text}\" успешно добавлена!\nID: {newId}",
                            "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }

                // Очищаем поля
                ClearSpecialtyGroupFields();

                // Перезагружаем данные
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Очистка полей группы специальностей
        private void ClearSpecialtyGroupFields()
        {
            NewGroupNameTextBox.Clear();
            NewGroupShortNameTextBox.Clear();
            NewGroupNameTextBox.Focus();
        }

        // Очистка полей группы (учебной)
        private void ClearGroupFields()
        {
            NewGroupCodeTextBox.Clear();
            NewGroupShortNameTextBox2.Clear();
            NewGroupNameTextBox2.Clear();
            NewGroupSpecialtyComboBox.SelectedIndex = -1;
            NewGroupIsActiveCheckBox.IsChecked = true;
            NewGroupCodeTextBox.Focus();
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

        // Обработка выбора строки в DataGrid специальностей
        private void SpecialtiesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Можно добавить логику при выборе строки
        }

        // Обработка изменения привязки специальности к группе
        private void SpecialtyGroupComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (sender is ComboBox comboBox && comboBox.SelectedItem is SpecialtyGroup selectedGroup)
                {
                    // Получаем специальность из DataContext строки
                    if (comboBox.DataContext is Specialty specialty)
                    {
                        // Обновляем group_id в базе данных
                        using (var connection = new MySqlConnection(_connectionString))
                        {
                            connection.Open();

                            const string sql = @"UPDATE `specialties` SET group_id = @groupId WHERE id = @id";

                            using (var command = new MySqlCommand(sql, connection))
                            {
                                command.Parameters.AddWithValue("@groupId", selectedGroup.Id);
                                command.Parameters.AddWithValue("@id", specialty.Id);
                                command.ExecuteNonQuery();
                            }
                        }

                        MessageBox.Show($"Специальность \"{specialty.Name}\" успешно привязана к группе \"{selectedGroup.Name}\"",
                            "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                        // Перезагружаем данные для обновления отображения
                        LoadData();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка привязки к группе: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Редактирование специальности (кнопка ✏️)
        private void BtnEditSpecialty_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is Specialty specialty)
                {
                    // Заполняем поля редактирования данными специальности
                    NewSpecialtyNameTextBox.Text = specialty.Name;
                    NewSpecialtyShortNameTextBox.Text = specialty.ShortName;
                    NewSpecialtyYearTextBox.Text = specialty.ValidFrom?.Year.ToString();
                    NewSpecialtyIsActiveCheckBox.IsChecked = specialty.IsActive;

                    _isEditingSpecialty = true;
                    _editingSpecialtyId = specialty.Id;

                    // Меняем текст кнопки
                    var addButton = FindName("AddSpecialtyButton") as Button;
                    if (addButton != null)
                    {
                        addButton.Content = "💾 Обновить специальность";
                    }

                    MessageBox.Show($"Редактирование специальности \"{specialty.Name}\".\nЗаполните поля и нажмите \"Обновить специальность\".",
                        "Редактирование", MessageBoxButton.OK, MessageBoxImage.Information);

                    NewSpecialtyNameTextBox.Focus();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при подготовке к редактированию: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Удаление специальности (кнопка 🗑)
        private void BtnDeleteSpecialty_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is int specialtyId)
                {
                    var specialty = DataProvider.SpecialtyList.FirstOrDefault(s => s.Id == specialtyId);
                    if (specialty == null)
                    {
                        MessageBox.Show("Специальность не найдена", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var result = MessageBox.Show(
                        $"Вы уверены, что хотите удалить специальность \"{specialty.Name}\"?\n\n" +
                        "Внимание: это действие нельзя отменить!",
                        "Подтверждение удаления",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result != MessageBoxResult.Yes) return;

                    using (var connection = new MySqlConnection(_connectionString))
                    {
                        connection.Open();

                        const string sql = @"DELETE FROM `specialties` WHERE id = @id";

                        using (var command = new MySqlCommand(sql, connection))
                        {
                            command.Parameters.AddWithValue("@id", specialtyId);
                            command.ExecuteNonQuery();
                        }
                    }

                    MessageBox.Show("Специальность успешно удалена", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    // Перезагружаем данные
                    LoadData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Переопределённый метод добавления/обновления специальности
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

                if (_isEditingSpecialty && _editingSpecialtyId.HasValue)
                {
                    // === ОБНОВЛЕНИЕ СУЩЕСТВУЮЩЕЙ СПЕЦИАЛЬНОСТИ ===
                    UpdateSpecialty(_editingSpecialtyId.Value);
                }
                else
                {
                    // === ДОБАВЛЕНИЕ НОВОЙ СПЕЦИАЛЬНОСТИ ===
                    AddNewSpecialty();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Добавление новой специальности
        private void AddNewSpecialty()
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();

                const string sql = @"
INSERT INTO `specialties` (name, short_name, active, data)
VALUES (@name, @short_name, 1, @data);
SELECT LAST_INSERT_ID();";

                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@name", NewSpecialtyNameTextBox.Text.Trim());
                    command.Parameters.AddWithValue("@short_name",
                        string.IsNullOrWhiteSpace(NewSpecialtyShortNameTextBox.Text)
                            ? (object)DBNull.Value
                            : NewSpecialtyShortNameTextBox.Text.Trim());

                    // Обработка года добавления
                    if (int.TryParse(NewSpecialtyYearTextBox.Text, out int year) && year > 0)
                    {
                        command.Parameters.AddWithValue("@data", new DateTime(year, 1, 1));
                    }
                    else
                    {
                        command.Parameters.AddWithValue("@data", DBNull.Value);
                    }

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

        // Обновление существующей специальности
        private void UpdateSpecialty(int specialtyId)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();

                const string sql = @"
UPDATE `specialties`
SET name = @name,
    short_name = @short_name,
    active = 1,
    data = @data
WHERE id = @id";

                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@name", NewSpecialtyNameTextBox.Text.Trim());
                    command.Parameters.AddWithValue("@short_name",
                        string.IsNullOrWhiteSpace(NewSpecialtyShortNameTextBox.Text)
                            ? (object)DBNull.Value
                            : NewSpecialtyShortNameTextBox.Text.Trim());

                    // Обработка года добавления
                    if (int.TryParse(NewSpecialtyYearTextBox.Text, out int year) && year > 0)
                    {
                        command.Parameters.AddWithValue("@data", new DateTime(year, 1, 1));
                    }
                    else
                    {
                        command.Parameters.AddWithValue("@data", DBNull.Value);
                    }

                    command.Parameters.AddWithValue("@id", specialtyId);

                    command.ExecuteNonQuery();

                    MessageBox.Show($"Специальность успешно обновлена!",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }

            // Сбрасываем режим редактирования
            _isEditingSpecialty = false;
            _editingSpecialtyId = null;

            // Возвращаем текст кнопки
            var addButton = FindName("AddSpecialtyButton") as Button;
            if (addButton != null)
            {
                addButton.Content = "💾 Сохранить специальность";
            }

            // Очищаем поля
            ClearSpecialtyFields();

            // Перезагружаем данные
            LoadData();
        }

        // ============================================================================
        // МЕТОДЫ ДЛЯ УПРАВЛЕНИЯ ГРУППАМИ (groups)
        // ============================================================================

        // Добавление новой группы (учебной)
        private void AddNewGroupButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_isEditingGroup && _editingGroupId.HasValue)
                {
                    UpdateGroup(_editingGroupId.Value);
                }
                else
                {
                    AddNewGroup();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Добавление новой группы (учебной) в БД
        private void AddNewGroup()
        {
            // Валидация
            if (string.IsNullOrWhiteSpace(NewGroupCodeTextBox.Text))
            {
                MessageBox.Show("Введите код группы", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NewGroupCodeTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(NewGroupShortNameTextBox2.Text))
            {
                MessageBox.Show("Введите сокращение группы", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NewGroupShortNameTextBox2.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(NewGroupNameTextBox2.Text))
            {
                MessageBox.Show("Введите отображаемое название группы", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NewGroupNameTextBox2.Focus();
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
                    command.Parameters.AddWithValue("@short_name", NewGroupShortNameTextBox2.Text.Trim());
                    command.Parameters.AddWithValue("@name", NewGroupNameTextBox2.Text.Trim());
                    command.Parameters.AddWithValue("@specialty_id", NewGroupSpecialtyComboBox.SelectedValue);
                    command.Parameters.AddWithValue("@is_active", NewGroupIsActiveCheckBox.IsChecked == true);

                    var result = command.ExecuteScalar();
                    int newId = Convert.ToInt32(result);

                    MessageBox.Show($"Группа \"{NewGroupNameTextBox2.Text}\" успешно добавлена!\nID: {newId}",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }

            ClearGroupFields();
            LoadData();
        }

        // Обновление существующей группы
        private void UpdateGroup(int groupId)
        {
            // Валидация
            if (string.IsNullOrWhiteSpace(NewGroupCodeTextBox.Text))
            {
                MessageBox.Show("Введите код группы", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NewGroupCodeTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(NewGroupShortNameTextBox2.Text))
            {
                MessageBox.Show("Введите сокращение группы", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NewGroupShortNameTextBox2.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(NewGroupNameTextBox2.Text))
            {
                MessageBox.Show("Введите отображаемое название группы", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NewGroupNameTextBox2.Focus();
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
UPDATE `groups`
SET code = @code,
    short_name = @short_name,
    name = @name,
    specialty_id = @specialty_id,
    is_active = @is_active
WHERE id = @id";

                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@code", NewGroupCodeTextBox.Text.Trim());
                    command.Parameters.AddWithValue("@short_name", NewGroupShortNameTextBox2.Text.Trim());
                    command.Parameters.AddWithValue("@name", NewGroupNameTextBox2.Text.Trim());
                    command.Parameters.AddWithValue("@specialty_id", NewGroupSpecialtyComboBox.SelectedValue);
                    command.Parameters.AddWithValue("@is_active", NewGroupIsActiveCheckBox.IsChecked == true);
                    command.Parameters.AddWithValue("@id", groupId);

                    command.ExecuteNonQuery();

                    MessageBox.Show($"Группа успешно обновлена!",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }

            _isEditingGroup = false;
            _editingGroupId = null;

            ClearGroupFields();
            LoadData();
        }

        // Редактирование группы (кнопка ✏️)
        private void BtnEditGroup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is Group group)
                {
                    NewGroupCodeTextBox.Text = group.Code;
                    NewGroupShortNameTextBox2.Text = group.ShortName;
                    NewGroupNameTextBox2.Text = group.Name;
                    NewGroupSpecialtyComboBox.SelectedValue = group.SpecialtyId;
                    NewGroupIsActiveCheckBox.IsChecked = group.IsActive;

                    _isEditingGroup = true;
                    _editingGroupId = group.Id;

                    MessageBox.Show($"Редактирование группы \"{group.Name}\".\nЗаполните поля и нажмите \"Сохранить группу\".",
                        "Редактирование", MessageBoxButton.OK, MessageBoxImage.Information);

                    NewGroupCodeTextBox.Focus();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при подготовке к редактированию: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Удаление группы (кнопка 🗑)
        private void BtnDeleteGroup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is int groupId)
                {
                    var group = DataProvider.GroupList.FirstOrDefault(g => g.Id == groupId);
                    if (group == null)
                    {
                        MessageBox.Show("Группа не найдена", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var result = MessageBox.Show(
                        $"Вы уверены, что хотите удалить группу \"{group.Name}\"?\n\n" +
                        "Внимание: это действие нельзя отменить!",
                        "Подтверждение удаления",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result != MessageBoxResult.Yes) return;

                    using (var connection = new MySqlConnection(_connectionString))
                    {
                        connection.Open();

                        const string sql = @"DELETE FROM `groups` WHERE id = @id";

                        using (var command = new MySqlCommand(sql, connection))
                        {
                            command.Parameters.AddWithValue("@id", groupId);
                            command.ExecuteNonQuery();
                        }
                    }

                    MessageBox.Show("Группа успешно удалена", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    LoadData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}