using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PP02.Connect;
using PP02.Classes.Dictionaries;
using PP02.Classes.Specialties;
using PP02.Classes.Person;
using MySql.Data.MySqlClient;

namespace PP02.Label
{
    public partial class AddStudentPage : Page
    {
        // 🔹 Строка подключения к БД
        private readonly string _connectionString = "server=127.0.0.1;uid=root;pwd=root;database=pp02;port=3306;";

        public AddStudentPage()
        {
            InitializeComponent();
            LoadDictionaries();
        }

        // === 🔹 ЗАГРУЗКА СПРАВОЧНИКОВ ===
        private void LoadDictionaries()
        {
            try
            {
                var db = new DataProvider();

                // Загружаем все справочники
                db.DataEducation(_connectionString);
                db.DataSocialOrigin(_connectionString);
                db.DataSocialStatus(_connectionString);
                db.DataParty(_connectionString);
                db.DataSpecialties(_connectionString);
                db.DataGroups(_connectionString);

                // 🔹 Привязка ComboBox к справочникам

                // Образование
                EducationComboBox.ItemsSource = new List<Education>
                    { new Education { Id = 0, Name = "(не выбрано)" } }
                    .Concat(DataProvider.EducationList)
                    .ToList();
                EducationComboBox.SelectedValuePath = "Id";
                EducationComboBox.DisplayMemberPath = "Name";
                EducationComboBox.SelectedIndex = 0;

                // Соц. происхождение
                SocialOriginComboBox.ItemsSource = new List<SocialOrigin>
                    { new SocialOrigin { Id = 0, Name = "(не выбрано)" } }
                    .Concat(DataProvider.SocialOriginList)
                    .ToList();
                SocialOriginComboBox.SelectedValuePath = "Id";
                SocialOriginComboBox.DisplayMemberPath = "Name";
                SocialOriginComboBox.SelectedIndex = 0;

                // Соц. положение
                SocialStatusComboBox.ItemsSource = new List<SocialStatus>
                    { new SocialStatus { Id = 0, Name = "(не выбрано)" } }
                    .Concat(DataProvider.SocialStatusList)
                    .ToList();
                SocialStatusComboBox.SelectedValuePath = "Id";
                SocialStatusComboBox.DisplayMemberPath = "Name";
                SocialStatusComboBox.SelectedIndex = 0;

                // Партийность
                PartyComboBox.ItemsSource = new List<Party>
                    { new Party { Id = 0, Name = "(не выбрано)" } }
                    .Concat(DataProvider.PartyList)
                    .ToList();
                PartyComboBox.SelectedValuePath = "Id";
                PartyComboBox.DisplayMemberPath = "Name";
                PartyComboBox.SelectedIndex = 0;

                // Группа - выпадающий список
                GroupComboBox.ItemsSource = new List<PP02.Classes.Specialties.Group>
                    { new PP02.Classes.Specialties.Group { Id = 0, Code = "", ShortName = "", Name = "(не выбрано)", SpecialtyId = 0, IsActive = true, SpecialtyName = "" } }
                    .Concat(DataProvider.GroupList)
                    .ToList();
                GroupComboBox.SelectedIndex = 0;

                // Специальность - выпадающий список
                SpecialtyComboBox.ItemsSource = new List<Specialty>
                    { new Specialty { Id = 0, Code = "", Name = "(не выбрано)", IsActive = true } }
                    .Concat(DataProvider.SpecialtyList)
                    .ToList();
                SpecialtyComboBox.SelectedIndex = 0;

                // Пол по умолчанию
                GenderComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки справочников: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // === 🔹 АВТОМАТИЧЕСКОЕ ОПРЕДЕЛЕНИЕ СПЕЦИАЛЬНОСТИ ПО ГРУППЕ ===
        private void GroupComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GroupComboBox.SelectedItem is PP02.Classes.Specialties.Group selectedGroup && selectedGroup.Id > 0)
            {
                // Если группа выбрана - автоматически устанавливаем её специальность
                var specialty = DataProvider.SpecialtyList.FirstOrDefault(s => s.Id == selectedGroup.SpecialtyId);
                if (specialty != null)
                {
                    SpecialtyComboBox.SelectedItem = specialty;
                }
            }
        }

        // === 🔹 ТОЛЬКО ЦИФРЫ ДЛЯ ГОДА ===
        private void NumbersOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
        }

        // === 🔹 КНОПКА "ОТМЕНА" ===
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Переход на страницу поиска
            NavigationService?.Navigate(new search());
        }

        // === 🔹 КНОПКА "СОХРАНИТЬ" ===
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. Валидация обязательных полей
                if (string.IsNullOrWhiteSpace(FullNameTextBox.Text))
                {
                    MessageBox.Show("Поле ФИО является обязательным", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    FullNameTextBox.Focus();
                    return;
                }

                // 2. Сбор данных из формы
                var newPerson = CollectFormData();

                // 3. Проверка корректности данных
                if (!ValidateFormData(newPerson))
                {
                    return;
                }

                // 4. Сохранение в базу данных
                int newId = SavePersonToDatabase(newPerson);

                // 5. Успешное сохранение
                MessageBox.Show($"Студент \"{newPerson.FullName}\" успешно добавлен!\nID записи: {newId}",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                // 6. Очистка формы
                ClearForm();

                // 7. Переход на страницу поиска для просмотра нового студента
                NavigationService?.Navigate(new search());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // === 🔹 СБОР ДАННЫХ ИЗ ФОРМЫ ===
        private PersonViewModel CollectFormData()
        {
            return new PersonViewModel
            {
                FullName = FullNameTextBox.Text.Trim(),
                Role = (RoleComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Студент",

                // ID связей
                GroupId = GetSelectedId(GroupComboBox),
                SpecialtyId = GetSelectedId(SpecialtyComboBox),
                EducationId = GetSelectedId(EducationComboBox),
                SocialOriginId = GetSelectedId(SocialOriginComboBox),
                SocialStatusId = GetSelectedId(SocialStatusComboBox),
                PartyId = GetSelectedId(PartyComboBox),

                // Основные данные
                GraduationYear = ParseInt(GraduationYearTextBox.Text),
                Gender = (GenderComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString(),
                BirthYear = ParseInt(BirthYearTextBox.Text),
                BirthPlace = BirthPlaceTextBox.Text.Trim(),
                Nationality = NationalityTextBox.Text.Trim(),
                DiplomaDate = DiplomaDatePicker.SelectedDate,
                Address = AddressTextBox.Text.Trim(),
                WorkAfter = WorkAfterTextBox.Text.Trim(),
                Source = SourceTextBox.Text.Trim()
            };
        }

        // === 🔹 ВАЛИДАЦИЯ ДАННЫХ ===
        private bool ValidateFormData(PersonViewModel person)
        {
            // Проверка специальности (обязательное поле)
            if (!person.SpecialtyId.HasValue || person.SpecialtyId.Value <= 0)
            {
                MessageBox.Show("Выберите специальность", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                SpecialtyComboBox.Focus();
                return false;
            }

            // Проверка пола (обязательное поле)
            if (string.IsNullOrEmpty(person.Gender))
            {
                MessageBox.Show("Выберите пол", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                GenderComboBox.Focus();
                return false;
            }

            // Проверка года рождения (если указан)
            if (person.BirthYear.HasValue)
            {
                int currentYear = DateTime.Now.Year;
                if (person.BirthYear.Value < 1900 || person.BirthYear.Value > currentYear)
                {
                    MessageBox.Show($"Год рождения должен быть в диапазоне 1900-{currentYear}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    BirthYearTextBox.Focus();
                    return false;
                }
            }

            // Проверка года выпуска (если указан)
            if (person.GraduationYear.HasValue)
            {
                int currentYear = DateTime.Now.Year;
                if (person.GraduationYear.Value < 1900 || person.GraduationYear.Value > currentYear + 5)
                {
                    MessageBox.Show($"Год выпуска должен быть в диапазоне 1900-{currentYear + 5}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    GraduationYearTextBox.Focus();
                    return false;
                }
            }

            return true;
        }

        // === 🔹 СОХРАНЕНИЕ В БАЗУ ДАННЫХ ===
        private int SavePersonToDatabase(PersonViewModel person)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();

                const string sql = @"
INSERT INTO people (
    full_name, role, specialty_id, group_id, education_id,
    social_origin_id, social_status_id, party_id,
    graduation_year, gender, nationality, birth_year, birth_place,
    address, diploma_date, work_after, source
) VALUES (
    @full_name, @role, @specialty_id, @group_id, @education_id,
    @social_origin_id, @social_status_id, @party_id,
    @graduation_year, @gender, @nationality, @birth_year, @birth_place,
    @address, @diploma_date, @work_after, @source
);
SELECT LAST_INSERT_ID();";

                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@full_name", (object)person.FullName ?? DBNull.Value);
                    command.Parameters.AddWithValue("@role", (object)person.Role ?? DBNull.Value);
                    command.Parameters.AddWithValue("@specialty_id", GetDbValue(person.SpecialtyId));
                    command.Parameters.AddWithValue("@group_id", GetDbValue(person.GroupId));
                    command.Parameters.AddWithValue("@education_id", GetDbValue(person.EducationId));
                    command.Parameters.AddWithValue("@social_origin_id", GetDbValue(person.SocialOriginId));
                    command.Parameters.AddWithValue("@social_status_id", GetDbValue(person.SocialStatusId));
                    command.Parameters.AddWithValue("@party_id", GetDbValue(person.PartyId));
                    command.Parameters.AddWithValue("@graduation_year", GetDbValue(person.GraduationYear));
                    command.Parameters.AddWithValue("@gender", (object)person.Gender ?? DBNull.Value);
                    command.Parameters.AddWithValue("@nationality", (object)person.Nationality ?? DBNull.Value);
                    command.Parameters.AddWithValue("@birth_year", GetDbValue(person.BirthYear));
                    command.Parameters.AddWithValue("@birth_place", (object)person.BirthPlace ?? DBNull.Value);
                    command.Parameters.AddWithValue("@address", (object)person.Address ?? DBNull.Value);
                    command.Parameters.AddWithValue("@diploma_date", (object)person.DiplomaDate ?? DBNull.Value);
                    command.Parameters.AddWithValue("@work_after", (object)person.WorkAfter ?? DBNull.Value);
                    command.Parameters.AddWithValue("@source", (object)person.Source ?? DBNull.Value);

                    // Выполняем запрос и получаем новый ID
                    var result = command.ExecuteScalar();
                    return Convert.ToInt32(result);
                }
            }
        }

        // === 🔹 ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ===

        /// <summary>
        /// Получает ID из ComboBox (возвращает null если выбран пункт "(не выбрано)")
        /// </summary>
        private int? GetSelectedId(ComboBox comboBox)
        {
            if (comboBox.SelectedValue is int id && id > 0)
            {
                return id;
            }
            return null;
        }

        /// <summary>
        /// Преобразует значение для базы данных (null -> DBNull.Value)
        /// </summary>
        private object GetDbValue(int? value)
        {
            return value.HasValue ? (object)value.Value : DBNull.Value;
        }

        /// <summary>
        /// Преобразует значение для базы данных (null -> DBNull.Value)
        /// </summary>
        private object GetDbValue(DateTime? value)
        {
            return value.HasValue ? (object)value.Value : DBNull.Value;
        }

        /// <summary>
        /// Парсит целое число из строки
        /// </summary>
        private int? ParseInt(string text)
        {
            if (int.TryParse(text, out int result))
            {
                return result;
            }
            return null;
        }

        /// <summary>
        /// Очищает форму
        /// </summary>
        private void ClearForm()
        {
            // Текстовые поля
            FullNameTextBox.Clear();
            GraduationYearTextBox.Clear();
            BirthYearTextBox.Clear();
            BirthPlaceTextBox.Clear();
            NationalityTextBox.Clear();
            AddressTextBox.Clear();
            WorkAfterTextBox.Clear();
            SourceTextBox.Clear();

            // ComboBox
            RoleComboBox.SelectedIndex = 0;
            GroupComboBox.SelectedIndex = 0;
            SpecialtyComboBox.SelectedIndex = 0;
            GenderComboBox.SelectedIndex = 0;
            EducationComboBox.SelectedIndex = 0;
            SocialOriginComboBox.SelectedIndex = 0;
            SocialStatusComboBox.SelectedIndex = 0;
            PartyComboBox.SelectedIndex = 0;

            // DatePicker
            DiplomaDatePicker.SelectedDate = null;
        }
    }
}
