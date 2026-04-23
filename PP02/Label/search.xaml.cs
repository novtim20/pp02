using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PP02.Connect;
using PP02.Classes.Dictionaries;
using PP02.Classes.Specialties;
using PP02.Classes.Person;
using PP02.Label.Dialogs;

namespace PP02.Label
{
    public partial class search : Page
    {
        // 🔹 Строка подключения к вашей БД
        private readonly string _connectionString = Connect.Connect.GetConnectionString();

        // 🔹 Результаты поиска
        private ObservableCollection<PersonViewModel> _searchResults;

        public search()
        {
            InitializeComponent();
            InitializeScrollLogic();
            LoadDictionaries();
            LoadAllPeople(); // 🔹 Загружаем всю БД при открытии
        }

        private void Page_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            // Перезагружаем справочники и данные при загрузке страницы
            LoadDictionaries();
            LoadAllPeople();
        }

        // === 🔹 ЗАГРУЗКА ВСЕЙ БАЗЫ ДАННЫХ ПРИ ОТКРЫТИИ ===
        private void LoadAllPeople()
        {
            try
            {
                var db = new DataProvider();
                db.DataPeople(_connectionString);
                DisplayResults(DataProvider.PeopleVMList);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // === 🔹 ЗАГРУЗКА СПРАВОЧНИКОВ ИЗ БД ===
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
                db.DataGroups(_connectionString); // 🔹 Загружаем группы
                // db.DataSpecialtyMapping(_connectionString); // Опционально, если нужно

                // 🔹 Привязка ComboBox к справочникам + пункт "Все"

                // Образование
                EducationComboBox.ItemsSource = new List<Education>
                    { new Education { Id = -1, Name = "Все" } }
                    .Concat(DataProvider.EducationList)
                    .ToList();
                EducationComboBox.SelectedValuePath = "Id";
                EducationComboBox.DisplayMemberPath = "Name";
                EducationComboBox.SelectedValue = -1;

                // Соц. происхождение
                SocialOriginComboBox.ItemsSource = new List<SocialOrigin>
                    { new SocialOrigin { Id = -1, Name = "Все" } }
                    .Concat(DataProvider.SocialOriginList)
                    .ToList();
                SocialOriginComboBox.SelectedValuePath = "Id";
                SocialOriginComboBox.DisplayMemberPath = "Name";
                SocialOriginComboBox.SelectedValue = -1;

                // Партийность
                PartyComboBox.ItemsSource = new List<Party>
                    { new Party { Id = -1, Name = "Все" } }
                    .Concat(DataProvider.PartyList)
                    .ToList();
                PartyComboBox.SelectedValuePath = "Id";
                PartyComboBox.DisplayMemberPath = "Name";
                PartyComboBox.SelectedValue = -1;

                // Пол (статичный список)
                GenderComboBox.SelectedIndex = 0;

                // 🔹 Группа - выпадающий список
                GroupComboBox.ItemsSource = new List<Group>
                    { new Group { Id = -1, Code = "Все", ShortName = "", Name = "Все", SpecialtyId = -1, IsActive = true, SpecialtyName = "" } }
                    .Concat(DataProvider.GroupList)
                    .ToList();
                GroupComboBox.SelectedIndex = 0;

                // 🔹 Специальность - выпадающий список
                SpecialtyComboBox.ItemsSource = new List<Specialty>
                    { new Specialty { Id = -1, Name = "Все", IsActive = true } }
                    .Concat(DataProvider.SpecialtyList)
                    .ToList();
                SpecialtyComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки справочников: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // === 🔹 ВЫБОР ГРУППЫ ИЗ КОМБОБОКСА ===
        private void GroupComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Обработка выбора группы
        }

        // === 🔹 ВЫБОР СПЕЦИАЛЬНОСТИ ИЗ КОМБОБОКСА ===
        private void SpecialtyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Обработка выбора специальности
        }

        // === 🔹 КНОПКА "НАЙТИ" ===
        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. Получаем критерии поиска
                var criteria = GetSearchCriteria();

                // 2. Фильтруем уже загруженный список
                var filtered = FilterPeople(DataProvider.PeopleVMList, criteria);

                // 3. Отображаем результаты в PersonItem
                DisplayResults(filtered);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка поиска: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // === 🔹 ПОЛУЧЕНИЕ КРИТЕРИЕВ ИЗ ПОЛЕЙ ===
        private SearchCriteria GetSearchCriteria()
        {
            // Получаем выбранную группу из ComboBox
            var selectedGroup = GroupComboBox.SelectedItem as Group;
            int? groupId = (selectedGroup != null && selectedGroup.Id != -1) ? selectedGroup.Id : (int?)null;
            string groupText = (selectedGroup != null && selectedGroup.Id == -1) ? "" : (GroupComboBox.Text ?? "");

            // Получаем выбранную специальность из ComboBox
            var selectedSpecialty = SpecialtyComboBox.SelectedItem as Specialty;
            int? specialtyId = (selectedSpecialty != null && selectedSpecialty.Id != -1) ? selectedSpecialty.Id : (int?)null;
            string specialtyText = (selectedSpecialty != null && selectedSpecialty.Id == -1) ? "" : (SpecialtyComboBox.Text ?? "");

            return new SearchCriteria
            {
                FullName = FullNameTextBox.Text.Trim(),
                IsStudent = IsStudentComboBox.SelectedIndex > 0
                    ? IsStudentComboBox.SelectedIndex == 1
                    : (bool?)null,
                GroupId = groupId,
                Group = groupText,
                GraduationYearStart = GraduationYearDatePicker.SelectedDate?.Year,
                SearchByGraduationPeriod = PeriodCheckBox.IsChecked == true,
                GraduationYearEnd = PeriodCheckBox.IsChecked == true
                    ? EndDatePicker.SelectedDate?.Year
                    : null,
                SpecialtyId = specialtyId,
                Specialty = specialtyText,
                Gender = GenderComboBox.SelectedIndex > 0
                    ? ((ComboBoxItem)GenderComboBox.SelectedItem)?.Content?.ToString()
                    : null,
                Nationality = NationalityTextBox.Text.Trim(),
                BirthYear = BirthYearTextBox.Text.Trim(),
                PartyId = (PartyComboBox.SelectedValue as Party)?.Id,
                BirthPlace = BirthPlaceTextBox.Text.Trim(),
                EducationId = (EducationComboBox.SelectedValue as Education)?.Id,
                SocialPosition = SocialPositionTextBox.Text.Trim(),
                LastWorkplace = LastWorkplaceTextBox.Text.Trim(),
                SocialOriginId = (SocialOriginComboBox.SelectedValue as SocialOrigin)?.Id,
                Address = AddressTextBox.Text.Trim(),
                DiplomaDateStart = DiplomaStartDatePicker.SelectedDate,
                SearchByDiplomaPeriod = DiplomaPeriodCheckBox.IsChecked == true,
                DiplomaDateEnd = DiplomaPeriodCheckBox.IsChecked == true
                    ? DiplomaEndDatePicker.SelectedDate
                    : null,
                Source = SourceTextBox.Text.Trim()
            };
        }

        // === 🔹 ПОИСК ГРУППЫ ПО КОДУ/НАЗВАНИЮ (для фильтрации) ===
        private int? GetGroupIdBySearchText(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return null;

            var group = DataProvider.GroupList
                .FirstOrDefault(g => g.Code.Equals(searchText, StringComparison.OrdinalIgnoreCase));

            return group?.Id;
        }

        // === 🔹 ФИЛЬТРАЦИЯ СПИСКА ===
        private List<PersonViewModel> FilterPeople(List<PersonViewModel> source, SearchCriteria c)
        {
            return source.Where(p =>
                (string.IsNullOrEmpty(c.FullName) || SafeContains(p.FullName, c.FullName)) &&
                (!c.IsStudent.HasValue || (c.IsStudent.Value && p.Role == "Студент") || (!c.IsStudent.Value && p.Role != "Студент")) &&
                // Фильтрация по группе: по ID из ComboBox или по тексту
                (c.GroupId.HasValue ? p.GroupId == c.GroupId.Value :
                    (string.IsNullOrEmpty(c.Group) || SafeContains(p.GroupName, c.Group))) &&
                (!c.GraduationYearStart.HasValue || (p.GraduationYear.HasValue && p.GraduationYear.Value >= c.GraduationYearStart.Value)) &&
                (!c.SearchByGraduationPeriod || !c.GraduationYearEnd.HasValue || (p.GraduationYear.HasValue && p.GraduationYear.Value <= c.GraduationYearEnd.Value)) &&
                // Фильтрация по специальности: по ID из ComboBox или по тексту
                (c.SpecialtyId.HasValue ? p.SpecialtyId == c.SpecialtyId.Value :
                    (string.IsNullOrEmpty(c.Specialty) || SafeContains(p.SpecialtyName, c.Specialty) || SafeContains(p.CurrentSpecialtyName, c.Specialty))) &&
                (string.IsNullOrEmpty(c.Gender) || p.Gender == c.Gender) &&
                (string.IsNullOrEmpty(c.Nationality) || SafeContains(p.Nationality, c.Nationality)) &&
                (string.IsNullOrEmpty(c.BirthYear) || (p.BirthYear.HasValue && p.BirthYear.Value.ToString().Contains(c.BirthYear))) &&
                (c.PartyId == null || c.PartyId == -1 || p.PartyId == c.PartyId) &&
                (string.IsNullOrEmpty(c.BirthPlace) || SafeContains(p.BirthPlace, c.BirthPlace)) &&
                (c.EducationId == null || c.EducationId == -1 || p.EducationId == c.EducationId) &&
                (string.IsNullOrEmpty(c.SocialPosition) || SafeContains(p.SocialStatusName, c.SocialPosition)) &&
                (string.IsNullOrEmpty(c.LastWorkplace) || SafeContains(p.WorkAfter, c.LastWorkplace)) &&
                (c.SocialOriginId == null || c.SocialOriginId == -1 || p.SocialOriginId == c.SocialOriginId) &&
                (string.IsNullOrEmpty(c.Address) || SafeContains(p.Address, c.Address)) &&
                (!c.DiplomaDateStart.HasValue || (p.DiplomaDate.HasValue && p.DiplomaDate.Value >= c.DiplomaDateStart.Value)) &&
                (!c.SearchByDiplomaPeriod || !c.DiplomaDateEnd.HasValue || (p.DiplomaDate.HasValue && p.DiplomaDate.Value <= c.DiplomaDateEnd.Value)) &&
                (string.IsNullOrEmpty(c.Source) || SafeContains(p.Source, c.Source))
            ).ToList();
        }

        // === 🔹 БЕЗОПАСНЫЙ ПОИСК ПОДСТРОКИ (совместимость с C# 7.3) ===
        private bool SafeContains(string source, string value)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(value))
                return true;
            return source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // === 🔹 ОТОБРАЖЕНИЕ РЕЗУЛЬТАТОВ ЧЕРЕЗ PersonItem ===
        private void DisplayResults(List<PersonViewModel> results)
        {
            _searchResults = new ObservableCollection<PersonViewModel>(results);
            ResultsItemsControl.ItemsSource = _searchResults;
            ResultsCountText.Text = $"{results.Count} записей";

            if (results.Count == 0)
            {
                MessageBox.Show("По вашему запросу ничего не найдено",
                    "Результаты", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // === 🔹 КНОПКА "ОЧИСТИТЬ" ===
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            // Очистка текстовых полей
            FullNameTextBox.Clear();
            NationalityTextBox.Clear();
            BirthYearTextBox.Clear();
            BirthPlaceTextBox.Clear();
            SocialPositionTextBox.Clear();
            LastWorkplaceTextBox.Clear();
            AddressTextBox.Clear();
            SourceTextBox.Clear();

            // Сброс ComboBox
            IsStudentComboBox.SelectedIndex = 0;
            GroupComboBox.SelectedIndex = 0;
            SpecialtyComboBox.SelectedIndex = 0;
            GenderComboBox.SelectedIndex = 0;
            PartyComboBox.SelectedIndex = 0;
            EducationComboBox.SelectedIndex = 0;
            SocialOriginComboBox.SelectedIndex = 0;

            // Сброс DatePicker
            GraduationYearDatePicker.SelectedDate = null;
            DiplomaStartDatePicker.SelectedDate = null;

            // Сброс чекбоксов периода
            PeriodCheckBox.IsChecked = false;
            EndDatePicker.IsEnabled = false;
            EndDatePicker.Background = (Brush)new BrushConverter().ConvertFromString("#E8E8E8");

            DiplomaPeriodCheckBox.IsChecked = false;
            DiplomaEndDatePicker.IsEnabled = false;
            DiplomaEndDatePicker.Background = (Brush)new BrushConverter().ConvertFromString("#E8E8E8");

            // Очистка результатов
            ResultsItemsControl.ItemsSource = null;
            ResultsCountText.Text = "0 записей";
        }

        // === 🔹 ОБРАБОТЧИКИ ЧЕКБОКСОВ ПЕРИОДА ===
        private void PeriodCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            bool isEnabled = PeriodCheckBox.IsChecked == true;
            EndDatePicker.IsEnabled = isEnabled;
            EndDatePicker.Background = isEnabled
                ? Brushes.White
                : (Brush)new BrushConverter().ConvertFromString("#E8E8E8");
        }

        private void DiplomaPeriodCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            bool isEnabled = DiplomaPeriodCheckBox.IsChecked == true;
            DiplomaEndDatePicker.IsEnabled = isEnabled;
            DiplomaEndDatePicker.Background = isEnabled
                ? Brushes.White
                : (Brush)new BrushConverter().ConvertFromString("#E8E8E8");
        }

        // === 🔹 КНОПКА "НАВЕРХ" ===
        private void InitializeScrollLogic()
        {
            MainScrollViewer.ScrollChanged += MainScrollViewer_ScrollChanged;
        }

        private void MainScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (MainScrollViewer.VerticalOffset > 300)
                ScrollToTopButton.Visibility = Visibility.Visible;
            else
                ScrollToTopButton.Visibility = Visibility.Collapsed;
        }

        private void ScrollToTopButton_Click(object sender, RoutedEventArgs e)
        {
            MainScrollViewer.ScrollToTop();
        }

        // === 🔹 КНОПКА "ОТЧЁТ" (переход на ReportPage) ===
        private void ReportButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new ReportPage());
        }

        // === 🔹 КНОПКА "ДОБАВИТЬ СТУДЕНТА" (переход на AddStudentPage) ===
        private void AddStudentButton_Click(object sender, RoutedEventArgs e)
        {
            var addStudentPage = new AddStudentPage();
            NavigationService?.Navigate(addStudentPage);
        }

        // === 🔹 КНОПКА "ИМПОРТ ИЗ EXCEL" (переход на ImportExcelPage) ===
        private void ImportExcelButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new ImportExcelPage());
        }

        // === 🔹 КНОПКА "ГРУППЫ СПЕЦИАЛЬНОСТЕЙ" (переход на SpecialtyGroupsPage) ===
        private void SpecialtyGroupsButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new SpecialtyGroupsPage());
        }

        // === 🔹 КНОПКА ДОБАВЛЕНИЯ ГРУППЫ ===
        private void AddGroupButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddSpecialtyGroupDialog
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true && dialog.NewGroupId.HasValue)
            {
                // Перезагружаем справочник групп
                var db = new DataProvider();
                db.DataGroups(_connectionString);

                // Обновляем ComboBox
                GroupComboBox.ItemsSource = new List<Group>
                    { new Group { Id = -1, Code = "Все", ShortName = "", Name = "Все", SpecialtyId = -1, IsActive = true, SpecialtyName = "" } }
                    .Concat(DataProvider.GroupList)
                    .ToList();

                // Если была добавлена группа - выбираем её
                var newGroup = DataProvider.GroupList.FirstOrDefault(g => g.Id == dialog.NewGroupId.Value);
                if (newGroup != null)
                {
                    GroupComboBox.SelectedItem = newGroup;
                }
            }
        }

        // === 🔹 ОБРАБОТЧИК УДАЛЕНИЯ СТУДЕНТА ИЗ PersonItem ===
        private void PersonItem_PersonDeleted(object sender, int personId)
        {
            // Удаляем из ObservableCollection
            var personToRemove = _searchResults?.FirstOrDefault(p => p.Id == personId);
            if (personToRemove != null)
            {
                _searchResults.Remove(personToRemove);
                ResultsCountText.Text = $"{_searchResults.Count} записей";
            }

            // Также удаляем из общего списка DataProvider.PeopleVMList
            var dbPerson = DataProvider.PeopleVMList.FirstOrDefault(p => p.Id == personId);
            if (dbPerson != null)
            {
                DataProvider.PeopleVMList.Remove(dbPerson);
            }

            MessageBox.Show("Список студентов обновлён", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // === 🔹 КНОПКА ДОБАВЛЕНИЯ СПЕЦИАЛЬНОСТИ ===
        private void AddSpecialtyButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddSpecialtyGroupDialog
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true && dialog.NewSpecialtyId.HasValue)
            {
                // Перезагружаем справочник специальностей
                var db = new DataProvider();
                db.DataSpecialties(_connectionString);

                // Обновляем ComboBox
                SpecialtyComboBox.ItemsSource = new List<Specialty>
                    { new Specialty { Id = -1, Name = "Все", IsActive = true } }
                    .Concat(DataProvider.SpecialtyList)
                    .ToList();

                // Если была добавлена специальность - выбираем её
                var newSpecialty = DataProvider.SpecialtyList.FirstOrDefault(s => s.Id == dialog.NewSpecialtyId.Value);
                if (newSpecialty != null)
                {
                    SpecialtyComboBox.SelectedItem = newSpecialty;
                }
            }
        }
    }

    // === 🔹 КРИТЕРИИ ПОИСКА ===
    public class SearchCriteria
    {
        public string FullName { get; set; }
        public bool? IsStudent { get; set; }
        public int? GroupId { get; set; }      // 🔹 ID группы из ComboBox
        public string Group { get; set; }       // Текст для поиска по группе
        public int? GraduationYearStart { get; set; }
        public bool SearchByGraduationPeriod { get; set; }
        public int? GraduationYearEnd { get; set; }
        public int? SpecialtyId { get; set; }   // 🔹 ID специальности из ComboBox
        public string Specialty { get; set; }   // Текст для поиска по специальности
        public string Gender { get; set; }
        public string Nationality { get; set; }
        public string BirthYear { get; set; }
        public int? PartyId { get; set; }
        public string BirthPlace { get; set; }
        public int? EducationId { get; set; }
        public string SocialPosition { get; set; }
        public string LastWorkplace { get; set; }
        public int? SocialOriginId { get; set; }
        public string Address { get; set; }
        public DateTime? DiplomaDateStart { get; set; }
        public bool SearchByDiplomaPeriod { get; set; }
        public DateTime? DiplomaDateEnd { get; set; }
        public string Source { get; set; }
    }
}