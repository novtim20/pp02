using System;
using System.Linq;
using System.Windows;
using PP02.Connect;
using PP02.Classes.Specialties;
using MySql.Data.MySqlClient;
using System.Windows.Controls;

namespace PP02.Label.Dialogs
{
    /// <summary>
    /// Диалоговое окно для быстрого добавления специальности или группы
    /// </summary>
    public partial class AddSpecialtyGroupDialog : Window
    {
        private readonly string _connectionString = Connect.Connect.GetConnectionString();

        // Результаты диалога
        public int? NewSpecialtyId { get; private set; }
        public int? NewGroupId { get; private set; }

        public AddSpecialtyGroupDialog()
        {
            InitializeComponent();
            LoadSpecialties();
        }

        // Загрузка списка специальностей для ComboBox в вкладке группы
        private void LoadSpecialties()
        {
            try
            {
                var db = new DataProvider();
                db.DataSpecialties(_connectionString);

                GroupSpecialtyComboBox.ItemsSource = DataProvider.SpecialtyList
                    .Where(s => s.IsActive)
                    .ToList();

                if (DataProvider.SpecialtyList.Count > 0)
                {
                    GroupSpecialtyComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки справочников: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Кнопка "Отмена"
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // Кнопка "Сохранить"
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Определяем активную вкладку
                var selectedTab = MainTabControl.SelectedItem as TabItem;

                if (selectedTab?.Header?.ToString() == "📋 Специальность")
                {
                    SaveSpecialty();
                }
                else if (selectedTab?.Header?.ToString() == "👥 Группа")
                {
                    SaveGroup();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Сохранение специальности
        private void SaveSpecialty()
        {
            // Валидация
            if (string.IsNullOrWhiteSpace(SpecialtyNameTextBox.Text))
            {
                MessageBox.Show("Введите название специальности", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                SpecialtyNameTextBox.Focus();
                return;
            }

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();

                const string sql = @"
INSERT INTO `specialties` (name, active)
VALUES (@name, @active);
SELECT LAST_INSERT_ID();";

                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@name", SpecialtyNameTextBox.Text.Trim());
                    command.Parameters.AddWithValue("@active", SpecialtyIsActiveCheckBox.IsChecked == true);

                    var result = command.ExecuteScalar();
                    NewSpecialtyId = Convert.ToInt32(result);
                }
            }

            // Перезагружаем справочник
            var db = new DataProvider();
            db.DataSpecialties(_connectionString);

            // Обновляем ComboBox специальностей во вкладке "Группа"
            GroupSpecialtyComboBox.ItemsSource = DataProvider.SpecialtyList
                .Where(s => s.IsActive)
                .ToList();

            // Выбираем только что созданную специальность
            if (NewSpecialtyId.HasValue)
            {
                GroupSpecialtyComboBox.SelectedValue = NewSpecialtyId.Value;
            }

            MessageBox.Show($"Специальность \"{SpecialtyNameTextBox.Text}\" успешно добавлена!",
                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

            DialogResult = true;
            Close();
        }

        // Сохранение группы
        private void SaveGroup()
        {
            // Валидация
            if (string.IsNullOrWhiteSpace(GroupCodeTextBox.Text))
            {
                MessageBox.Show("Введите код группы", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                GroupCodeTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(GroupShortNameTextBox.Text))
            {
                MessageBox.Show("Введите сокращение группы", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                GroupShortNameTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(GroupNameTextBox.Text))
            {
                MessageBox.Show("Введите отображаемое название группы", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                GroupNameTextBox.Focus();
                return;
            }

            if (GroupSpecialtyComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите специальность", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                GroupSpecialtyComboBox.Focus();
                return;
            }

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();

                var specialtyId = GroupSpecialtyComboBox.SelectedValue;
                if (specialtyId == null || !(specialtyId is int))
                {
                    MessageBox.Show("Некорректно выбрана специальность", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                const string sql = @"
INSERT INTO `groups` (code, short_name, name, specialty_id, is_active)
VALUES (@code, @short_name, @name, @specialty_id, @is_active);
SELECT LAST_INSERT_ID();";

                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@code", GroupCodeTextBox.Text.Trim());
                    command.Parameters.AddWithValue("@short_name", GroupShortNameTextBox.Text.Trim());
                    command.Parameters.AddWithValue("@name", GroupNameTextBox.Text.Trim());
                    command.Parameters.AddWithValue("@specialty_id", (int)specialtyId);
                    command.Parameters.AddWithValue("@is_active", GroupIsActiveCheckBox.IsChecked == true);

                    var result = command.ExecuteScalar();
                    NewGroupId = Convert.ToInt32(result);
                }
            }

            // Перезагружаем справочник групп
            var db = new DataProvider();
            db.DataGroups(_connectionString);

            MessageBox.Show($"Группа \"{GroupNameTextBox.Text}\" успешно добавлена!",
                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

            DialogResult = true;
            Close();
        }
    }
}