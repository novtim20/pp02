using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MySql.Data.MySqlClient;
using PP02.Connect;
using PP02.Classes.Specialties;

namespace PP02.Label.Item
{
    public partial class SpecialtyGroupItem : UserControl
    {
        private readonly string _connectionString = "server=127.0.0.1;uid=root;pwd=root;database=pp022;port=3306;";
        private SpecialtyGroup _currentGroup;
        private bool _isDirty = false;

        public SpecialtyGroupItem()
        {
            InitializeComponent();
        }

        // === 🔹 ЗАВИСИМОСТЬ: Данные группы специальностей для привязки ===
        public static readonly DependencyProperty GroupDataProperty =
            DependencyProperty.Register(
                nameof(GroupData),
                typeof(SpecialtyGroup),
                typeof(SpecialtyGroupItem),
                new PropertyMetadata(null, OnGroupDataChanged));

        public SpecialtyGroup GroupData
        {
            get => (SpecialtyGroup)GetValue(GroupDataProperty);
            set => SetValue(GroupDataProperty, value);
        }

        // === 🔹 ОБРАБОТЧИК ИЗМЕНЕНИЯ ДАННЫХ ===
        private static void OnGroupDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SpecialtyGroupItem control && e.NewValue is SpecialtyGroup group)
            {
                control._currentGroup = group;
                control.LoadDataToUI();
            }
        }

        // === 🔹 ЗАГРУЗКА ДАННЫХ ИЗ МОДЕЛИ В ИНТЕРФЕЙС ===
        private void LoadDataToUI()
        {
            if (_currentGroup == null) return;

            // Краткий вид
            TxtGroupName.Text = _currentGroup.Name;
            TxtCreatedDate.Text = _currentGroup.CreatedAt.HasValue
                ? $"Дата: {_currentGroup.CreatedAt.Value:dd.MM.yyyy}"
                : "Дата: ----";
            TxtSpecialtiesCount.Text = $"Специальностей: {_currentGroup.Specialties.Count}";

            // Загружаем список специальностей в DataGrid
            SpecialtiesDataGrid.ItemsSource = _currentGroup.Specialties.ToList();

            _isDirty = false;
        }

        // === 🔹 КНОПКА "▼" — переход в режим редактирования ===
        private void BtnExpand_Click(object sender, RoutedEventArgs e)
        {
            if (ExpDetails != null)
            {
                ExpDetails.Visibility = Visibility.Visible;
                ExpDetails.IsExpanded = true;
            }
            if (BtnExpand != null) BtnExpand.Visibility = Visibility.Collapsed;
            if (BtnCancel != null) BtnCancel.Visibility = Visibility.Visible;
        }

        // Событие Expanded (опционально)
        private void ExpDetails_Expanded(object sender, RoutedEventArgs e)
        {
            // Можно добавить логику, например, подгрузку данных при открытии
        }

        // === 🔹 КНОПКА "✕ Отмена" ===
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            // Перезагружаем данные из модели (отменяем изменения в UI)
            LoadDataToUI();
            SetViewMode();
        }

        // === 🔹 ПЕРЕКЛЮЧЕНИЕ В РЕЖИМ ПРОСМОТРА ===
        private void SetViewMode()
        {
            if (ExpDetails != null)
            {
                ExpDetails.Visibility = Visibility.Collapsed;
                ExpDetails.IsExpanded = false;
            }
            if (BtnExpand != null) BtnExpand.Visibility = Visibility.Visible;
            if (BtnCancel != null) BtnCancel.Visibility = Visibility.Collapsed;
        }

        // === 🔹 КНОПКА "+ Специальность" — быстрое добавление ===
        private void BtnAddSpecialty_Click(object sender, RoutedEventArgs e)
        {
            if (ExpDetails != null)
            {
                ExpDetails.Visibility = Visibility.Visible;
                ExpDetails.IsExpanded = true;
            }
            NewSpecialtyNameTextBox.Focus();
        }

        // === 🔹 КНОПКА "➕ Добавить" — добавление новой специальности в группу ===
        private void BtnAddNewSpecialty_Click(object sender, RoutedEventArgs e)
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

                if (_currentGroup == null)
                {
                    MessageBox.Show("Группа специальностей не выбрана", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();

                    const string sql = @"
INSERT INTO `specialties` (name, is_active, group_id)
VALUES (@name, @is_active, @group_id);
SELECT LAST_INSERT_ID();";

                    using (var command = new MySqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@name", NewSpecialtyNameTextBox.Text.Trim());
                        command.Parameters.AddWithValue("@is_active", NewSpecialtyIsActiveCheckBox.IsChecked == true);
                        command.Parameters.AddWithValue("@group_id", _currentGroup.Id);

                        var result = command.ExecuteScalar();
                        int newId = Convert.ToInt32(result);

                        MessageBox.Show($"Специальность \"{NewSpecialtyNameTextBox.Text}\" успешно добавлена в группу!\nID: {newId}",
                            "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }

                // Очищаем поле
                NewSpecialtyNameTextBox.Clear();
                NewSpecialtyIsActiveCheckBox.IsChecked = true;

                // Перезагружаем данные
                RefreshGroupData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // === 🔹 КНОПКА "🗑 Удалить" — удаление специальности из группы ===
        private void BtnRemoveSpecialty_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is int specialtyId)
                {
                    var result = MessageBox.Show(
                        "Вы уверены, что хотите удалить эту специальность из группы?\n\nВнимание: это не удалит специальность из базы данных, а только уберёт связь с группой.",
                        "Подтверждение удаления",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result != MessageBoxResult.Yes) return;

                    using (var connection = new MySqlConnection(_connectionString))
                    {
                        connection.Open();

                        const string sql = @"UPDATE `specialties` SET group_id = NULL WHERE id = @id";

                        using (var command = new MySqlCommand(sql, connection))
                        {
                            command.Parameters.AddWithValue("@id", specialtyId);
                            command.ExecuteNonQuery();
                        }
                    }

                    MessageBox.Show("Специальность успешно удалена из группы", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    // Перезагружаем данные
                    RefreshGroupData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // === 🔹 КНОПКА "🗑 Удалить группу" ===
        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentGroup == null) return;

                var result = MessageBox.Show(
                    $"Вы уверены, что хотите удалить группу специальностей \"{_currentGroup.Name}\"?\n\n" +
                    $"Все специальности этой группы останутся в базе данных, но потеряют связь с группой.",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes) return;

                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();

                    // Сначала убираем связь у всех специальностей
                    const string sqlUpdate = @"UPDATE `specialties` SET group_id = NULL WHERE group_id = @id";
                    using (var command = new MySqlCommand(sqlUpdate, connection))
                    {
                        command.Parameters.AddWithValue("@id", _currentGroup.Id);
                        command.ExecuteNonQuery();
                    }

                    // Затем удаляем саму группу
                    const string sqlDelete = @"DELETE FROM `specialty_groups` WHERE id = @id";
                    using (var command = new MySqlCommand(sqlDelete, connection))
                    {
                        command.Parameters.AddWithValue("@id", _currentGroup.Id);
                        command.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Группа специальностей успешно удалена", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // Скрываем элемент
                this.Visibility = Visibility.Collapsed;

                // Сообщаем родителю о необходимости обновить список
                if (this.Parent is Panel panel)
                {
                    // Можно вызвать событие или обновить список через ViewModel
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // === 🔹 ОБНОВЛЕНИЕ ДАННЫХ ГРУППЫ ===
        private void RefreshGroupData()
        {
            if (_currentGroup == null) return;

            try
            {
                var db = new DataProvider();
                db.DataSpecialtyGroups(_connectionString);

                // Находим обновлённую группу
                var updatedGroup = DataProvider.SpecialtyGroupList.FirstOrDefault(g => g.Id == _currentGroup.Id);
                if (updatedGroup != null)
                {
                    _currentGroup = updatedGroup;
                    LoadDataToUI();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}