using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PP02.Connect;
using PP02.Classes.Dictionaries;
using PP02.Classes.Specialties;
using PP02.Classes.Person;
using System.Windows.Media;

namespace PP02.Label.Item
{
    public partial class PersonItem : UserControl
    {
        // 🔹 Строка подключения (замените на вашу или возьмите из конфигурации)
        private readonly string _connectionString = "server=127.0.0.1;uid=root;pwd=root;database=pp02;port=3306;";

        // 🔹 Текущая запись для редактирования
        private PersonViewModel _currentPerson;

        // 🔹 Флаг: были ли изменены данные
        private bool _isDirty = false;

        public PersonItem()
        {
            InitializeComponent();
            InitializeComboBoxes();
        }

        // === 🔹 ЗАВИСИМОСТЬ: Данные человека для привязки ===
        public static readonly DependencyProperty PersonDataProperty =
            DependencyProperty.Register(
                nameof(PersonData),
                typeof(PersonViewModel),
                typeof(PersonItem),
                new PropertyMetadata(null, OnPersonDataChanged));

        public PersonViewModel PersonData
        {
            get => (PersonViewModel)GetValue(PersonDataProperty);
            set => SetValue(PersonDataProperty, value);
        }

        // === 🔹 ОБРАБОТЧИК ИЗМЕНЕНИЯ ДАННЫХ ===
        private static void OnPersonDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PersonItem control && e.NewValue is PersonViewModel person)
            {
                control._currentPerson = person;
                control.LoadDataToUI();
            }
        }

        // === 🔹 ИНИЦИАЛИЗАЦИЯ COMBOBOX (привязка к справочникам) ===
        private void InitializeComboBoxes()
        {
            // Образование
            CmbEducation.ItemsSource = DataProvider.EducationList;
            CmbEducation.DisplayMemberPath = "Name";
            CmbEducation.SelectedValuePath = "Id";

            // Соц. происхождение
            CmbSocialOrigin.ItemsSource = DataProvider.SocialOriginList;
            CmbSocialOrigin.DisplayMemberPath = "Name";
            CmbSocialOrigin.SelectedValuePath = "Id";

            // Соц. положение
            CmbSocialStatus.ItemsSource = DataProvider.SocialStatusList;
            CmbSocialStatus.DisplayMemberPath = "Name";
            CmbSocialStatus.SelectedValuePath = "Id";

            // Партийность
            CmbParty.ItemsSource = DataProvider.PartyList;
            CmbParty.DisplayMemberPath = "Name";
            CmbParty.SelectedValuePath = "Id";

            // Группы - с поддержкой поиска по short_name (FULLTEXT)
            var groups = DataProvider.GroupList.ToList();
            CmbGroup.ItemsSource = groups;
            CmbGroup.DisplayMemberPath = "ShortName";
            CmbGroup.SelectedValuePath = "Id";
            CmbGroup.IsEditable = true;
            CmbGroup.StaysOpenOnEdit = true;

            // Специальности - основные + исторические алиасы
            var specialties = DataProvider.SpecialtyList.ToList();
            CmbSpecialty.ItemsSource = specialties;
            CmbSpecialty.DisplayMemberPath = "Name";
            CmbSpecialty.SelectedValuePath = "Id";
            CmbSpecialty.IsEditable = true;
            CmbSpecialty.StaysOpenOnEdit = true;

            // Исторические алиасы (для выпадающего списка при вводе старого кода)
            // Можно добавить отдельный ComboBox или использовать автодополнение

            // Подписка на изменения группы для авто-определения специальности
            CmbGroup.SelectionChanged += CmbGroup_SelectionChanged;
            CmbGroup.TextChanged += CmbGroup_TextChanged;

            // Подписка на изменения полей (для флага _isDirty)
            SubscribeToChanges();
        }

        // === 🔹 АВТОМАТИЧЕСКОЕ ОПРЕДЕЛЕНИЕ СПЕЦИАЛЬНОСТИ ПО ГРУППЕ ===
        private void CmbGroup_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbGroup.SelectedItem is Group selectedGroup)
            {
                // Если группа выбрана - автоматически устанавливаем её специальность
                var specialty = DataProvider.SpecialtyList.FirstOrDefault(s => s.Id == selectedGroup.SpecialtyId);
                if (specialty != null)
                {
                    CmbSpecialty.SelectedValue = specialty.Id;
                    _isDirty = true;
                }
            }
        }

        private void CmbGroup_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Если пользователь ввёл текст вручную - пытаемся найти группу
            if (string.IsNullOrEmpty(CmbGroup.Text))
                return;

            var foundGroup = DataProvider.GroupList.FirstOrDefault(g => 
                g.Code.Equals(CmbGroup.Text, StringComparison.OrdinalIgnoreCase) ||
                g.ShortName.Equals(CmbGroup.Text, StringComparison.OrdinalIgnoreCase));

            if (foundGroup != null)
            {
                CmbGroup.SelectedValue = foundGroup.Id;
                // Специальность установится через SelectionChanged
            }
        }

        // === 🔹 ПОДПИСКА НА ИЗМЕНЕНИЯ ПОЛЕЙ ===
        private void SubscribeToChanges()
        {
            // TextBox
            foreach (var tb in FindVisualChildren<TextBox>(this))
            {
                tb.TextChanged += (s, e) => _isDirty = true;
            }
            // ComboBox
            foreach (var cb in FindVisualChildren<ComboBox>(this))
            {
                cb.SelectionChanged += (s, e) => _isDirty = true;
            }
        }

        // === 🔹 ВСПОМОГАТЕЛЬНЫЙ МЕТОД: поиск элементов в визуальном дереве ===
        private static System.Collections.Generic.IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) yield break;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                if (child is T t) yield return t;
                foreach (T childOfChild in FindVisualChildren<T>(child)) yield return childOfChild;
            }
        }

        // === 🔹 ЗАГРУЗКА ДАННЫХ ИЗ МОДЕЛИ В ИНТЕРФЕЙС ===
        private void LoadDataToUI()
        {
            if (_currentPerson == null) return;

            // 🔹 Краткий вид (всегда виден)
            TxtFio.Text = _currentPerson.FullName;
            TxtGradYear.Text = $"Год: {_currentPerson.GraduationYear?.ToString() ?? "----"}";
            TxtGroup.Text = $"Группа: {_currentPerson.GroupName ?? "---"}";
            TxtSpecialty.Text = _currentPerson.CurrentSpecialtyName ?? _currentPerson.SpecialtyName ?? "Специальность";

            // 🔹 Подробный вид (редактирование)
            CmbRole.SelectedItem = CmbRole.Items.Cast<ComboBoxItem>()
                .FirstOrDefault(i => i.Content.ToString() == _currentPerson.Role)
                ?? CmbRole.Items[0] as ComboBoxItem;

            TxtEditGradYear.Text = _currentPerson.GraduationYear?.ToString();
            CmbGroup.SelectedValue = _currentPerson.GroupId;
            CmbSpecialty.SelectedValue = _currentPerson.SpecialtyId;

            CmbGender.SelectedItem = CmbGender.Items.Cast<ComboBoxItem>()
                .FirstOrDefault(i =>
                    (_currentPerson.Gender == "М" && i.Content.ToString() == "Мужской") ||
                    (_currentPerson.Gender == "Ж" && i.Content.ToString() == "Женский"))
                ?? CmbGender.Items[0] as ComboBoxItem;

            TxtEditBirthYear.Text = _currentPerson.BirthYear?.ToString();
            TxtEditBirthPlace.Text = _currentPerson.BirthPlace;
            TxtEditNationality.Text = _currentPerson.Nationality;

            CmbEducation.SelectedValue = _currentPerson.EducationId;
            CmbSocialOrigin.SelectedValue = _currentPerson.SocialOriginId;
            CmbSocialStatus.SelectedValue = _currentPerson.SocialStatusId;
            CmbParty.SelectedValue = _currentPerson.PartyId;

            TxtEditAddress.Text = _currentPerson.Address;
            TxtEditWorkAfter.Text = _currentPerson.WorkAfter;
            TxtEditSource.Text = _currentPerson.Source;

            _isDirty = false;
        }

        // === 🔹 СБОР ДАННЫХ ИЗ ИНТЕРФЕЙСА В МОДЕЛЬ ===
        private void SaveDataFromUI()
        {
            if (_currentPerson == null) return;

            _currentPerson.Role = (CmbRole.SelectedItem as ComboBoxItem)?.Content?.ToString();
            _currentPerson.GraduationYear = int.TryParse(TxtEditGradYear.Text, out var grad) ? grad : (int?)null;
            
            // Получаем ID группы из ComboBox
            _currentPerson.GroupId = CmbGroup.SelectedValue as int?;
            
            // Получаем ID специальности из ComboBox
            _currentPerson.SpecialtyId = CmbSpecialty.SelectedValue as int?;
            
            // Проверка: если группа выбрана, специальность должна соответствовать группе
            // (триггеры в БД trg_people_spec_check/trg_people_spec_check_upd также проверят это)
            if (_currentPerson.GroupId.HasValue && _currentPerson.GroupId.Value > 0)
            {
                var selectedGroup = DataProvider.GroupList.FirstOrDefault(g => g.Id == _currentPerson.GroupId.Value);
                if (selectedGroup != null && selectedGroup.SpecialtyId > 0)
                {
                    // Если у группы есть специальность - используем её
                    _currentPerson.SpecialtyId = selectedGroup.SpecialtyId;
                    CmbSpecialty.SelectedValue = selectedGroup.SpecialtyId;
                }
            }

            var genderItem = CmbGender.SelectedItem as ComboBoxItem;
            _currentPerson.Gender = genderItem?.Content?.ToString() == "Мужской" ? "М" :
                                   genderItem?.Content?.ToString() == "Женский" ? "Ж" : null;

            _currentPerson.BirthYear = int.TryParse(TxtEditBirthYear.Text, out var birth) ? birth : (int?)null;
            _currentPerson.BirthPlace = TxtEditBirthPlace.Text;
            _currentPerson.Nationality = TxtEditNationality.Text;

            _currentPerson.EducationId = CmbEducation.SelectedValue as int?;
            _currentPerson.SocialOriginId = CmbSocialOrigin.SelectedValue as int?;
            _currentPerson.SocialStatusId = CmbSocialStatus.SelectedValue as int?;
            _currentPerson.PartyId = CmbParty.SelectedValue as int?;

            _currentPerson.Address = TxtEditAddress.Text;
            _currentPerson.WorkAfter = TxtEditWorkAfter.Text;
            _currentPerson.Source = TxtEditSource.Text;
        }

        // === 🔹 ОБРАБОТЧИКИ КНОПОК ===

        // Кнопка "▼" — переход в режим редактирования
        private void BtnExpand_Click(object sender, RoutedEventArgs e)
        {
            if (ExpDetails != null)
            {
                ExpDetails.Visibility = Visibility.Visible;
                ExpDetails.IsExpanded = true;
            }
            if (BtnExpand != null) BtnExpand.Visibility = Visibility.Collapsed;
            if (BtnSave != null) BtnSave.Visibility = Visibility.Visible;
            if (BtnCancel != null) BtnCancel.Visibility = Visibility.Visible;
        }

        // Событие Expanded (опционально)
        private void ExpDetails_Expanded(object sender, RoutedEventArgs e)
        {
            // Можно добавить логику, например, подгрузку данных при открытии
        }

        // Кнопка "💾 Сохранить"
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPerson == null) return;

            try
            {
                // 1. Сбор данных из UI в модель
                SaveDataFromUI();

                // 2. Обновление в базе данных
                UpdatePersonInDatabase(_currentPerson);

                // 3. Обновление краткого вида после сохранения
                LoadDataToUI();

                // 4. Возврат в режим просмотра
                SetViewMode();

                _isDirty = false;
                MessageBox.Show("Данные успешно сохранены", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Кнопка "✕ Отмена"
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (_isDirty)
            {
                var result = MessageBox.Show("Есть несохранённые изменения. Отменить?",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes) return;
            }
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
            if (BtnSave != null) BtnSave.Visibility = Visibility.Collapsed;
            if (BtnCancel != null) BtnCancel.Visibility = Visibility.Collapsed;
        }

        // === 🔹 ОБНОВЛЕНИЕ ДАННЫХ В БАЗЕ (реальный SQL UPDATE) ===
        private void UpdatePersonInDatabase(PersonViewModel person)
        {
            using (var connection = new MySql.Data.MySqlClient.MySqlConnection(_connectionString))
            {
                connection.Open();

                const string sql = @"
UPDATE people SET
    role = @role,
    specialty_id = @specialty_id,
    education_id = @education_id,
    social_origin_id = @social_origin_id,
    social_status_id = @social_status_id,
    party_id = @party_id,
    graduation_year = @graduation_year,
    group_id = @group_id,
    gender = @gender,
    nationality = @nationality,
    birth_year = @birth_year,
    birth_place = @birth_place,
    address = @address,
    work_after = @work_after,
    source = @source,
    historical_alias_id = @historical_alias_id
WHERE id = @id";

                // Если группа выбрана, специальность должна соответствовать группе
                // (триггеры в БД также проверят это)
                int? specialtyId = person.SpecialtyId;
                if (person.GroupId.HasValue && person.GroupId.Value > 0)
                {
                    var group = DataProvider.GroupList.FirstOrDefault(g => g.Id == person.GroupId.Value);
                    if (group != null && group.SpecialtyId > 0)
                    {
                        specialtyId = group.SpecialtyId;
                    }
                }

                // Поиск исторического алиаса по выбранной специальности (если есть)
                int? historicalAliasId = null;
                if (specialtyId.HasValue)
                {
                    // Можно добавить логику выбора исторического алиаса
                    // Например, если пользователь выбрал старый код из ComboBox
                }

                using (var command = new MySql.Data.MySqlClient.MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@id", person.Id);
                    command.Parameters.AddWithValue("@role", (object)person.Role ?? DBNull.Value);
                    command.Parameters.AddWithValue("@specialty_id", (object)specialtyId ?? DBNull.Value);
                    command.Parameters.AddWithValue("@education_id", (object)person.EducationId ?? DBNull.Value);
                    command.Parameters.AddWithValue("@social_origin_id", (object)person.SocialOriginId ?? DBNull.Value);
                    command.Parameters.AddWithValue("@social_status_id", (object)person.SocialStatusId ?? DBNull.Value);
                    command.Parameters.AddWithValue("@party_id", (object)person.PartyId ?? DBNull.Value);
                    command.Parameters.AddWithValue("@graduation_year", (object)person.GraduationYear ?? DBNull.Value);
                    command.Parameters.AddWithValue("@group_id", (object)person.GroupId ?? DBNull.Value);
                    command.Parameters.AddWithValue("@gender", (object)person.Gender ?? DBNull.Value);
                    command.Parameters.AddWithValue("@nationality", (object)person.Nationality ?? DBNull.Value);
                    command.Parameters.AddWithValue("@birth_year", (object)person.BirthYear ?? DBNull.Value);
                    command.Parameters.AddWithValue("@birth_place", (object)person.BirthPlace ?? DBNull.Value);
                    command.Parameters.AddWithValue("@address", (object)person.Address ?? DBNull.Value);
                    command.Parameters.AddWithValue("@work_after", (object)person.WorkAfter ?? DBNull.Value);
                    command.Parameters.AddWithValue("@source", (object)person.Source ?? DBNull.Value);
                    command.Parameters.AddWithValue("@historical_alias_id", (object)historicalAliasId ?? DBNull.Value);

                    command.ExecuteNonQuery();
                }
            }
        }
    }
}