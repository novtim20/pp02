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
using PP02.Label.Dialogs;

namespace PP02.Label
{
    public partial class AddStudentPage : Page
    {
        // 🔹 Строка подключения к БД
        private readonly string _connectionString = "server=127.0.0.1;uid=root;pwd=root;database=pp022;port=3306;";

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
                GroupComboBox.SelectedValuePath = "Id";
                GroupComboBox.SelectedIndex = 0;

                // Специальность - выпадающий список
                SpecialtyComboBox.ItemsSource = new List<Specialty>
                    { new Specialty { Id = 0, Name = "(не выбрано)", IsActive = true } }
                    .Concat(DataProvider.SpecialtyList)
                    .ToList();
                SpecialtyComboBox.SelectedValuePath = "Id";
                SpecialtyComboBox.DisplayMemberPath = "Name";
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

        // === 🔹 КНОПКА ДОБАВЛЕНИЯ ГРУППЫ ===
        private void AddGroupButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddSpecialtyGroupDialog
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                // Перезагружаем справочник групп
                var db = new DataProvider();
                db.DataGroups(_connectionString);

                // Обновляем ComboBox
                GroupComboBox.ItemsSource = new List<PP02.Classes.Specialties.Group>
                    { new PP02.Classes.Specialties.Group { Id = 0, Code = "", ShortName = "", Name = "(не выбрано)", SpecialtyId = 0, IsActive = true, SpecialtyName = "" } }
                    .Concat(DataProvider.GroupList)
                    .ToList();

                // Если была добавлена группа - выбираем её
                if (dialog.NewGroupId.HasValue)
                {
                    GroupComboBox.SelectedValue = dialog.NewGroupId.Value;
                }
            }
        }

        // === 🔹 КНОПКА ДОБАВЛЕНИЯ СПЕЦИАЛЬНОСТИ ===
        private void AddSpecialtyButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddSpecialtyGroupDialog
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                // Перезагружаем справочник специальностей
                var db = new DataProvider();
                db.DataSpecialties(_connectionString);

                // Обновляем ComboBox
                SpecialtyComboBox.ItemsSource = new List<Specialty>
                    { new Specialty { Id = 0, Name = "(не выбрано)", IsActive = true } }
                    .Concat(DataProvider.SpecialtyList)
                    .ToList();

                // Если была добавлена специальность - выбираем её
                if (dialog.NewSpecialtyId.HasValue)
                {
                    SpecialtyComboBox.SelectedValue = dialog.NewSpecialtyId.Value;
                }
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
                    SpecialtyComboBox.SelectedValue = specialty.Id;
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
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // 1. Вставка в таблицу persons
                        const string sqlPerson = @"
                    INSERT INTO persons (full_name, role, gender, nationality, birth_year, birth_place, address, source)
                    VALUES (@full_name, @role, @gender, @nationality, @birth_year, @birth_place, @address, @source);
                    SELECT LAST_INSERT_ID();";

                        int newId;
                        using (var cmd = new MySqlCommand(sqlPerson, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@full_name", person.FullName);
                            cmd.Parameters.AddWithValue("@role", person.Role);
                            cmd.Parameters.AddWithValue("@gender", (object)person.Gender ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@nationality", (object)person.Nationality ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@birth_year", GetDbValue(person.BirthYear));
                            cmd.Parameters.AddWithValue("@birth_place", (object)person.BirthPlace ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@address", (object)person.Address ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@source", (object)person.Source ?? DBNull.Value);
                            newId = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        // 2. Вставка в academic_records
                        const string sqlAcademic = @"
                    INSERT INTO academic_records (person_id, group_id, specialty_id, education_id, graduation_year, diploma_date)
                    VALUES (@pid, @gid, @sid, @eid, @gy, @dd);";
                        using (var cmd = new MySqlCommand(sqlAcademic, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@pid", newId);
                            cmd.Parameters.AddWithValue("@gid", GetDbValue(person.GroupId));
                            cmd.Parameters.AddWithValue("@sid", GetDbValue(person.SpecialtyId));
                            cmd.Parameters.AddWithValue("@eid", GetDbValue(person.EducationId));
                            cmd.Parameters.AddWithValue("@gy", GetDbValue(person.GraduationYear));
                            cmd.Parameters.AddWithValue("@dd", (object)person.DiplomaDate ?? DBNull.Value);
                            cmd.ExecuteNonQuery();
                        }

                        // 3. Вставка в social_profiles
                        const string sqlSocial = @"
                    INSERT INTO social_profiles (person_id, social_origin_id, social_status_id, party_id)
                    VALUES (@pid, @soid, @ssid, @paid);";
                        using (var cmd = new MySqlCommand(sqlSocial, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@pid", newId);
                            cmd.Parameters.AddWithValue("@soid", GetDbValue(person.SocialOriginId));
                            cmd.Parameters.AddWithValue("@ssid", GetDbValue(person.SocialStatusId));
                            cmd.Parameters.AddWithValue("@paid", GetDbValue(person.PartyId));
                            cmd.ExecuteNonQuery();
                        }

                        // 4. Вставка в career_records
                        if (!string.IsNullOrEmpty(person.WorkAfter))
                        {
                            const string sqlCareer = "INSERT INTO career_records (person_id, work_after) VALUES (@pid, @wa);";
                            using (var cmd = new MySqlCommand(sqlCareer, connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@pid", newId);
                                cmd.Parameters.AddWithValue("@wa", person.WorkAfter);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                        return newId;
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;
                    }
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