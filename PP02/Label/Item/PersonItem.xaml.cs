using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PP02.Connect;
using PP02.Classes.Dictionaries;
using PP02.Classes.Specialties;
using PP02.Classes.Person;
using System.Windows.Media;
using PP02.Label.Dialogs;
using System.IO;
using Microsoft.Win32;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;

namespace PP02.Label.Item
{
    public partial class PersonItem : UserControl
    {
        // 🔹 Строка подключения (замените на вашу или возьмите из конфигурации)
        private readonly string _connectionString = Connect.Connect.GetConnectionString();

        // 🔹 Текущая запись для редактирования
        private PersonViewModel _currentPerson;

        // 🔹 Флаг: были ли изменены данные
        private bool _isDirty = false;

        // 🔹 Событие удаления студента (для уведомления родителя)
        public event EventHandler<int> PersonDeleted;

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
            CmbGroup.SelectedValuePath = "Id";
            CmbGroup.IsEditable = true;
            CmbGroup.StaysOpenOnEdit = true;

            // Специальности - основные + исторические алиасы
            var specialties = DataProvider.SpecialtyList.ToList();
            CmbSpecialty.ItemsSource = specialties;
            CmbSpecialty.SelectedValuePath = "Id";
            CmbSpecialty.IsEditable = true;
            CmbSpecialty.StaysOpenOnEdit = true;

            // Исторические алиасы (для выпадающего списка при вводе старого кода)
            // Можно добавить отдельный ComboBox или использовать автодополнение

            // Подписка на изменения группы для авто-определения специальности
            CmbGroup.SelectionChanged += CmbGroup_SelectionChanged;
            //CmbGroup.TextChanged += CmbGroup_TextChanged;

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

            // При загрузке данных поля должны быть заблокированы (режим просмотра)
            SetFieldsEnabled(false);
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

        // Кнопка "➕" — Добавить группу
        private void AddGroupButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddSpecialtyGroupDialog();

            // Переключаем на вкладку группы
            foreach (var item in dialog.MainTabControl.Items)
            {
                if (item is System.Windows.Controls.TabItem tab &&
                    tab.Header?.ToString() == "👥 Группа")
                {
                    dialog.MainTabControl.SelectedItem = tab;
                    break;
                }
            }

            if (dialog.ShowDialog() == true && dialog.NewGroupId.HasValue)
            {
                // Обновляем список групп
                CmbGroup.ItemsSource = null;
                CmbGroup.ItemsSource = DataProvider.GroupList.ToList();

                // Выбираем newly созданную группу
                CmbGroup.SelectedValue = dialog.NewGroupId.Value;
                _isDirty = true;
            }
        }

        // Кнопка "➕" — Добавить специальность
        private void AddSpecialtyButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddSpecialtyGroupDialog();

            // Переключаем на вкладку специальности
            foreach (var item in dialog.MainTabControl.Items)
            {
                if (item is System.Windows.Controls.TabItem tab &&
                    tab.Header?.ToString() == "📋 Специальность")
                {
                    dialog.MainTabControl.SelectedItem = tab;
                    break;
                }
            }

            if (dialog.ShowDialog() == true && dialog.NewSpecialtyId.HasValue)
            {
                // Обновляем список специальностей
                CmbSpecialty.ItemsSource = null;
                CmbSpecialty.ItemsSource = DataProvider.SpecialtyList.ToList();

                // Выбираем newly созданную специальность
                CmbSpecialty.SelectedValue = dialog.NewSpecialtyId.Value;
                _isDirty = true;
            }
        }

        // Кнопка "▼" — раскрытие подробной информации
        private void BtnExpand_Click(object sender, RoutedEventArgs e)
        {
            if (ExpDetails != null)
            {
                ExpDetails.Visibility = Visibility.Visible;
                ExpDetails.IsExpanded = true;
            }
            if (BtnExpand != null) BtnExpand.Visibility = Visibility.Collapsed;
            // Показываем кнопку "Изменить" и кнопку "Отменить" после раскрытия
            if (BtnEdit != null) BtnEdit.Visibility = Visibility.Visible;
            if (BtnCancel != null) BtnCancel.Visibility = Visibility.Visible;
            if (BtnSave != null) BtnSave.Visibility = Visibility.Collapsed;

            // Поля остаются заблокированными (режим просмотра)
            SetFieldsEnabled(false);
        }

        // Кнопка "✏️ Изменить" — включение режима редактирования
        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            SetEditMode();
        }

        // === 🔹 ПЕРЕКЛЮЧЕНИЕ В РЕЖИМ РЕДАКТИРОВАНИЯ ===
        private void SetEditMode()
        {
            // Показываем кнопку сохранения и крестик отмены рядом, скрываем кнопку изменения
            if (BtnEdit != null) BtnEdit.Visibility = Visibility.Collapsed;
            if (BtnSave != null) BtnSave.Visibility = Visibility.Visible;
            if (BtnCancel != null) BtnCancel.Visibility = Visibility.Visible;

            // Разблокируем все поля для редактирования
            SetFieldsEnabled(true);
        }

        // === 🔹 ПЕРЕКЛЮЧЕНИЕ В РЕЖИМ ПРОСМОТРА ===
        private void SetViewMode()
        {
            // Не закрываем ExpDetails, просто переключаем кнопки на "Изменить" + "Отменить"
            // Скрываем кнопку "Сохранить", показываем "Изменить" и "Отменить"
            if (BtnEdit != null) BtnEdit.Visibility = Visibility.Visible;
            if (BtnSave != null) BtnSave.Visibility = Visibility.Collapsed;
            // Кнопка "Отменить" остается видимой после раскрытия стрелочки
            if (BtnCancel != null) BtnCancel.Visibility = Visibility.Visible;

            // Блокируем все поля для просмотра
            SetFieldsEnabled(false);
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
                if (BtnExpand != null) BtnExpand.Visibility = Visibility.Visible;
                if (ExpDetails != null)
                {
                    ExpDetails.IsExpanded = false;
                    ExpDetails.Visibility = Visibility.Collapsed;
                }

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

        // Кнопка "✕ Отмена" - отменяет изменения или закрывает карточку
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (_isDirty)
            {
                var result = MessageBox.Show("Есть несохранённые изменения. Отменить?",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes) return;
            }

            // Проверяем, в каком режиме мы находимся (редактирование или просмотр)
            bool isEditMode = BtnSave != null && BtnSave.Visibility == Visibility.Visible;

            if (isEditMode)
            {
                // Если в режиме редактирования - отменяем изменения и возвращаемся в режим просмотра
                LoadDataToUI();
                SetViewMode();
                // Кнопки "Изменить" и "Отменить" остаются видимыми
            }
            else
            {
                // Если в режиме просмотра - закрываем карточку
                // Скрываем кнопку "Отменить" и "Изменить"
                if (BtnCancel != null) BtnCancel.Visibility = Visibility.Collapsed;
                if (BtnEdit != null) BtnEdit.Visibility = Visibility.Collapsed;
                // Показываем стрелочку для повторного раскрытия
                if (BtnExpand != null) BtnExpand.Visibility = Visibility.Visible;
                // Сворачиваем ExpDetails
                if (ExpDetails != null)
                {
                    ExpDetails.IsExpanded = false;
                    ExpDetails.Visibility = Visibility.Collapsed;
                }
            }
        }

        // === 🔹 УДАЛЕНИЕ СТУДЕНТА ИЗ БАЗЫ ДАННЫХ ===

        // Кнопка "🗑 Удалить"
        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPerson == null) return;

            var result = MessageBox.Show(
                $"Вы действительно хотите удалить студента {_currentPerson.FullName}?\n\nЭто действие нельзя отменить.",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                int deletedPersonId = _currentPerson.Id;
                DeletePersonFromDatabase(_currentPerson.Id);

                MessageBox.Show($"Студент {_currentPerson.FullName} успешно удалён", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // Уведомляем родительский контрол об удалении
                PersonDeleted?.Invoke(this, deletedPersonId);

                // После удаления переключаем в режим просмотра и показываем стрелочку
                SetViewMode();
                if (BtnExpand != null) BtnExpand.Visibility = Visibility.Visible;
                if (ExpDetails != null)
                {
                    ExpDetails.IsExpanded = false;
                    ExpDetails.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // === 🔹 УДАЛЕНИЕ ДАННЫХ ИЗ БАЗЫ (реальный SQL DELETE) ===
        private void DeletePersonFromDatabase(int personId)
        {
            using (var connection = new MySql.Data.MySqlClient.MySqlConnection(_connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // 1. Удаляем записи из зависимых таблиц (каскадное удаление через FOREIGN KEY может быть настроено)

                        // Удаляем из career_records
                        const string sqlCareer = @"DELETE FROM career_records WHERE person_id = @person_id";
                        using (var command = new MySql.Data.MySqlClient.MySqlCommand(sqlCareer, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@person_id", personId);
                            command.ExecuteNonQuery();
                        }

                        // Удаляем из social_profiles
                        const string sqlSocial = @"DELETE FROM social_profiles WHERE person_id = @person_id";
                        using (var command = new MySql.Data.MySqlClient.MySqlCommand(sqlSocial, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@person_id", personId);
                            command.ExecuteNonQuery();
                        }

                        // Удаляем из academic_records
                        const string sqlAcademic = @"DELETE FROM academic_records WHERE person_id = @person_id";
                        using (var command = new MySql.Data.MySqlClient.MySqlCommand(sqlAcademic, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@person_id", personId);
                            command.ExecuteNonQuery();
                        }

                        // 2. Удаляем основную запись из persons
                        const string sqlPersons = @"DELETE FROM persons WHERE id = @id";
                        using (var command = new MySql.Data.MySqlClient.MySqlCommand(sqlPersons, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@id", personId);
                            command.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        // === 🔹 ВКЛЮЧЕНИЕ/ВЫКЛЮЧЕНИЕ ПОЛЕЙ РЕДАКТИРОВАНИЯ ===
        private void SetFieldsEnabled(bool enabled)
        {
            CmbRole.IsEnabled = enabled;
            TxtEditGradYear.IsEnabled = enabled;
            CmbGroup.IsEnabled = enabled;
            CmbSpecialty.IsEnabled = enabled;
            CmbGender.IsEnabled = enabled;
            TxtEditBirthYear.IsEnabled = enabled;
            TxtEditBirthPlace.IsEnabled = enabled;
            TxtEditNationality.IsEnabled = enabled;
            CmbEducation.IsEnabled = enabled;
            CmbSocialOrigin.IsEnabled = enabled;
            CmbSocialStatus.IsEnabled = enabled;
            CmbParty.IsEnabled = enabled;
            TxtEditAddress.IsEnabled = enabled;
            TxtEditWorkAfter.IsEnabled = enabled;
            TxtEditSource.IsEnabled = enabled;
        }

        // === 🔹 ЭКСПОРТ В WORD (ОТЧЕТ О ВЫПУСКНИКЕ) ===

        // Кнопка "📄 Экспорт в отчет"
        private void BtnExportReport_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPerson == null)
            {
                MessageBox.Show("Нет данных для экспорта", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Диалог сохранения файла
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Документ Word (*.docx)|*.docx",
                    FileName = $"Отчет_{_currentPerson.FullName.Replace(" ", "_")}.docx",
                    Title = "Сохранение отчета о выпускнике"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    GenerateGraduateReport(_currentPerson, saveFileDialog.FileName);
                    MessageBox.Show($"Отчет успешно сохранен:\n{saveFileDialog.FileName}", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    // После экспорта переключаем в режим просмотра и показываем стрелочку
                    SetViewMode();
                    if (BtnExpand != null) BtnExpand.Visibility = Visibility.Visible;
                    if (ExpDetails != null)
                    {
                        ExpDetails.IsExpanded = false;
                        ExpDetails.Visibility = Visibility.Collapsed;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Метод генерации отчета в формате Word
        private void GenerateGraduateReport(PersonViewModel person, string filePath)
        {
            // Получаем данные из справочников
            var educationName = DataProvider.EducationList.FirstOrDefault(e => e.Id == person.EducationId)?.Name ?? "Не указано";
            var socialOriginName = DataProvider.SocialOriginList.FirstOrDefault(s => s.Id == person.SocialOriginId)?.Name ?? "Не указано";
            var socialStatusName = DataProvider.SocialStatusList.FirstOrDefault(s => s.Id == person.SocialStatusId)?.Name ?? "Не указано";
            var partyName = DataProvider.PartyList.FirstOrDefault(p => p.Id == person.PartyId)?.Name ?? "Не указано";
            var groupName = person.GroupName ?? "Не указана";
            var specialtyName = person.CurrentSpecialtyName ?? person.SpecialtyName ?? "Не указана";

            using (var document = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document))
            {
                // Добавляем основную часть документа
                MainDocumentPart mainPart = document.AddMainDocumentPart();
                mainPart.Document = new Document();
                Body body = mainPart.Document.AppendChild(new Body());

                // Заголовок отчета
                body.AppendChild(new Paragraph(
                    new Run(new Text("ОТЧЕТ О ВЫПУСКНИКЕ"))
                    {
                        RunProperties = new RunProperties(new Bold())
                    })
                {
                    ParagraphProperties = new ParagraphProperties(new SpacingBetweenLines { After = "200" })
                });

                // Учебное заведение
                body.AppendChild(new Paragraph(
                    new Run(new Text("Учебное заведение: Пермский авиационный техникум")))
                {
                    ParagraphProperties = new ParagraphProperties(new SpacingBetweenLines { After = "100" })
                });

                // Документ
                body.AppendChild(new Paragraph(
                    new Run(new Text("Документ: Анкета выпускника")))
                {
                    ParagraphProperties = new ParagraphProperties(new SpacingBetweenLines { After = "100" })
                });

                // Дата формирования
                body.AppendChild(new Paragraph(
                    new Run(new Text($"Дата формирования отчета: {DateTime.Now:dd.MM.yyyy}")))
                {
                    ParagraphProperties = new ParagraphProperties(new SpacingBetweenLines { After = "200" })
                });

                // Раздел 1: Персональные данные
                AddSectionHeader(body, "1. Персональные данные");
                AddParameterRow(body, "Фамилия, Имя, Отчество", person.FullName);
                AddParameterRow(body, "Дата рождения", person.BirthYear?.ToString() ?? "Не указана");
                AddParameterRow(body, "Год выпуска", person.GraduationYear?.ToString() ?? "Не указан");

                // Раздел 2: Образование и Квалификация
                AddSectionHeader(body, "2. Образование и Квалификация");
                AddParameterRow(body, "Специальность", specialtyName);
                AddParameterRow(body, "Группа", groupName);
                AddParameterRow(body, "Образование до техникума", educationName);
                AddParameterRow(body, "Пол", person.Gender == "М" ? "Мужской" : person.Gender == "Ж" ? "Женский" : "Не указан");

                // Раздел 3: Социальные данные
                AddSectionHeader(body, "3. Социальные данные");
                AddParameterRow(body, "Национальность", person.Nationality ?? "Не указана");
                AddParameterRow(body, "Место рождения", person.BirthPlace ?? "Не указано");
                AddParameterRow(body, "Соц. происхождение", socialOriginName);
                AddParameterRow(body, "Соц. положение", socialStatusName);
                AddParameterRow(body, "Партийность", partyName);
                AddParameterRow(body, "Домашний адрес", person.Address ?? "Не указан");

                // Раздел 4: Трудовая деятельность
                AddSectionHeader(body, "4. Трудовая деятельность");
                AddParameterRow(body, "Где работал после техникума", person.WorkAfter ?? "Не указано");

                // Раздел 5: Дополнительная информация
                AddSectionHeader(body, "5. Дополнительная информация");
                AddParameterRow(body, "Источник информации", person.Source ?? "Не указан");
                AddParameterRow(body, "Роль в базе", person.Role ?? "Не указана");

                // Подвал с подписями
                body.AppendChild(new Paragraph(
                    new Run(new Text("\n\nОтчет сформирован автоматически на основе данных архива.")))
                {
                    ParagraphProperties = new ParagraphProperties(new SpacingBetweenLines { After = "100" })
                });

                body.AppendChild(new Paragraph(
                    new Run(new Text($"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}")))
                {
                    ParagraphProperties = new ParagraphProperties(new SpacingBetweenLines { After = "200" })
                });

                mainPart.Document.Save();
            }
        }

        // Вспомогательный метод: добавление заголовка раздела
        private void AddSectionHeader(Body body, string text)
        {
            body.AppendChild(new Paragraph(
                new Run(new Text(text))
                {
                    RunProperties = new RunProperties(new Bold())
                })
            {
                ParagraphProperties = new ParagraphProperties(new SpacingBetweenLines { Before = "200", After = "100" })
            });
        }

        // Вспомогательный метод: добавление строки параметра (таблица из 2 ячеек)
        private void AddParameterRow(Body body, string parameter, string value)
        {
            var table = new Table();

            // Настройки таблицы
            var tblBorders = new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 4 },
                new BottomBorder { Val = BorderValues.Single, Size = 4 },
                new LeftBorder { Val = BorderValues.Single, Size = 4 },
                new RightBorder { Val = BorderValues.Single, Size = 4 },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 }
            );
            table.AppendChild(tblBorders);

            // Ширина таблицы
            table.AppendChild(new TableWidth { Type = TableWidthUnitValues.Pct, Width = "100%" });

            // Строка таблицы
            var tr = new TableRow();

            // Ячейка с названием параметра
            var tc1 = new TableCell(
                new Paragraph(new Run(new Text(parameter))
                {
                    RunProperties = new RunProperties(new Bold())
                })
            );
            tc1.AppendChild(new TableCellWidth { Type = TableWidthUnitValues.Pct, Width = "40%" });

            // Ячейка со значением
            var tc2 = new TableCell(
                new Paragraph(new Run(new Text(value)))
            );
            tc2.AppendChild(new TableCellWidth { Type = TableWidthUnitValues.Pct, Width = "60%" });

            tr.AppendChild(tc1);
            tr.AppendChild(tc2);
            table.AppendChild(tr);

            body.AppendChild(table);

            // Отступ после таблицы
            body.AppendChild(new Paragraph(new Run(new Text(""))));
        }

        // === 🔹 ОБНОВЛЕНИЕ ДАННЫХ В БАЗЕ (реальный SQL UPDATE) ===
        private void UpdatePersonInDatabase(PersonViewModel person)
        {
            using (var connection = new MySql.Data.MySqlClient.MySqlConnection(_connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // 1. Обновление основной таблицы persons
                        const string sqlPersons = @"
UPDATE persons SET
    role = @role,
    gender = @gender,
    nationality = @nationality,
    birth_year = @birth_year,
    birth_place = @birth_place,
    address = @address,
    source = @source
WHERE id = @id";

                        using (var command = new MySql.Data.MySqlClient.MySqlCommand(sqlPersons, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@id", person.Id);
                            command.Parameters.AddWithValue("@role", (object)person.Role ?? DBNull.Value);
                            command.Parameters.AddWithValue("@gender", (object)person.Gender ?? DBNull.Value);
                            command.Parameters.AddWithValue("@nationality", (object)person.Nationality ?? DBNull.Value);
                            command.Parameters.AddWithValue("@birth_year", (object)person.BirthYear ?? DBNull.Value);
                            command.Parameters.AddWithValue("@birth_place", (object)person.BirthPlace ?? DBNull.Value);
                            command.Parameters.AddWithValue("@address", (object)person.Address ?? DBNull.Value);
                            command.Parameters.AddWithValue("@source", (object)person.Source ?? DBNull.Value);
                            command.ExecuteNonQuery();
                        }

                        // 2. Обновление academic_records (образование, группа, специальность, год выпуска)
                        const string sqlAcademic = @"
INSERT INTO academic_records (person_id, group_id, specialty_id, education_id, graduation_year, diploma_date)
VALUES (@person_id, @group_id, @specialty_id, @education_id, @graduation_year, @diploma_date)
ON DUPLICATE KEY UPDATE
    group_id = VALUES(group_id),
    specialty_id = VALUES(specialty_id),
    education_id = VALUES(education_id),
    graduation_year = VALUES(graduation_year),
    diploma_date = VALUES(diploma_date)";

                        // Определяем specialty_id на основе группы
                        int? specialtyId = null;
                        if (person.GroupId.HasValue)
                        {
                            var group = DataProvider.GroupList.FirstOrDefault(g => g.Id == person.GroupId.Value);
                            if (group != null)
                            {
                                specialtyId = group.SpecialtyId;
                            }
                        }
                        else if (person.SpecialtyId.HasValue)
                        {
                            specialtyId = person.SpecialtyId;
                        }

                        using (var command = new MySql.Data.MySqlClient.MySqlCommand(sqlAcademic, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@person_id", person.Id);
                            command.Parameters.AddWithValue("@group_id", (object)person.GroupId ?? DBNull.Value);
                            command.Parameters.AddWithValue("@specialty_id", (object)specialtyId ?? DBNull.Value);
                            command.Parameters.AddWithValue("@education_id", (object)person.EducationId ?? DBNull.Value);
                            command.Parameters.AddWithValue("@graduation_year", (object)person.GraduationYear ?? DBNull.Value);
                            command.Parameters.AddWithValue("@diploma_date", DBNull.Value); // Можно добавить поле DiplomaDate в модель
                            command.ExecuteNonQuery();
                        }

                        // 3. Обновление social_profiles (соц. происхождение, статус, партийность)
                        const string sqlSocial = @"
INSERT INTO social_profiles (person_id, social_origin_id, social_status_id, party_id)
VALUES (@person_id, @social_origin_id, @social_status_id, @party_id)
ON DUPLICATE KEY UPDATE
    social_origin_id = VALUES(social_origin_id),
    social_status_id = VALUES(social_status_id),
    party_id = VALUES(party_id)";

                        using (var command = new MySql.Data.MySqlClient.MySqlCommand(sqlSocial, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@person_id", person.Id);
                            command.Parameters.AddWithValue("@social_origin_id", (object)person.SocialOriginId ?? DBNull.Value);
                            command.Parameters.AddWithValue("@social_status_id", (object)person.SocialStatusId ?? DBNull.Value);
                            command.Parameters.AddWithValue("@party_id", (object)person.PartyId ?? DBNull.Value);
                            command.ExecuteNonQuery();
                        }

                        // 4. Обновление career_records (работа после)
                        const string sqlCareer = @"
INSERT INTO career_records (person_id, work_after)
VALUES (@person_id, @work_after)
ON DUPLICATE KEY UPDATE
    work_after = VALUES(work_after)";

                        using (var command = new MySql.Data.MySqlClient.MySqlCommand(sqlCareer, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@person_id", person.Id);
                            command.Parameters.AddWithValue("@work_after", (object)person.WorkAfter ?? DBNull.Value);
                            command.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }
    }
}