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

namespace PP02.Label
{
    public partial class search : Page
    {
        // 🔹 Строка подключения к вашей БД
        private readonly string _connectionString = "server=127.0.0.1;uid=root;pwd=root;database=pp02;port=3306;";

        // 🔹 Результаты поиска
        private ObservableCollection<PersonViewModel> _searchResults;

        public search()
        {
            InitializeComponent();
            InitializeScrollLogic();
            LoadDictionaries();
            LoadAllPeople(); // 🔹 Загружаем всю БД при открытии
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

                // 🔹 Инициализация поиска по группам (FULLTEXT)
                GroupSearchTextBox.TextChanged += GroupSearchTextBox_TextChanged;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки справочников: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // === 🔹 ПОИСК ГРУПП С ИСПОЛЬЗОВАНИЕМ FULLTEXT ===
        private void GroupSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var searchText = GroupTextBox.Text.Trim();

                // Показываем/скрываем список результатов
                if (string.IsNullOrEmpty(searchText))
                {
                    GroupResultsListBox.Visibility = Visibility.Collapsed;
                    GroupResultsListBox.ItemsSource = null;
                    return;
                }

                // Поиск с использованием FULLTEXT индекса
                var db = new DataProvider();
                var results = db.SearchGroups(_connectionString, searchText);

                if (results.Count > 0)
                {
                    GroupResultsListBox.ItemsSource = results;
                    GroupResultsListBox.Visibility = Visibility.Visible;
                }
                else
                {
                    GroupResultsListBox.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                // Fallback на обычный LIKE поиск при ошибке FULLTEXT
                var filtered = DataProvider.GroupList
                    .Where(g => g.Code.IndexOf(GroupTextBox.Text, StringComparison.OrdinalIgnoreCase) >= 0
                             || (g.ShortName != null && g.ShortName.IndexOf(GroupTextBox.Text, StringComparison.OrdinalIgnoreCase) >= 0)
                             || (g.Name != null && g.Name.IndexOf(GroupTextBox.Text, StringComparison.OrdinalIgnoreCase) >= 0))
                    .ToList();

                if (filtered.Count > 0)
                {
                    GroupResultsListBox.ItemsSource = filtered;
                    GroupResultsListBox.Visibility = Visibility.Visible;
                }
                else
                {
                    GroupResultsListBox.Visibility = Visibility.Collapsed;
                }
            }
        }

        // === 🔹 ВЫБОР ГРУППЫ ИЗ СПИСКА ===
        private void GroupResultsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GroupResultsListBox.SelectedItem is Group selectedGroup)
            {
                GroupTextBox.Text = selectedGroup.Code;
                GroupResultsListBox.Visibility = Visibility.Collapsed;
                GroupResultsListBox.ItemsSource = null; // Скрыть список после выбора
            }
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
            return new SearchCriteria
            {
                FullName = FullNameTextBox.Text.Trim(),
                IsStudent = IsStudentComboBox.SelectedIndex > 0
                    ? IsStudentComboBox.SelectedIndex == 1
                    : (bool?)null,
                Group = GroupTextBox.Text.Trim(),
                GraduationYearStart = GraduationYearDatePicker.SelectedDate?.Year,
                SearchByGraduationPeriod = PeriodCheckBox.IsChecked == true,
                GraduationYearEnd = PeriodCheckBox.IsChecked == true
                    ? EndDatePicker.SelectedDate?.Year
                    : null,
                Specialty = SpecialtyTextBox.Text.Trim(),
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
            // Получаем ID группы по тексту поиска (если указан точный код)
            var groupIdFilter = GetGroupIdBySearchText(c.Group);

            return source.Where(p =>
                (string.IsNullOrEmpty(c.FullName) || SafeContains(p.FullName, c.FullName)) &&
                (!c.IsStudent.HasValue || (c.IsStudent.Value && p.Role == "Студент") || (!c.IsStudent.Value && p.Role != "Студент")) &&
                // Фильтрация по группе: либо точное совпадение ID, либо поиск по подстроке в имени группы
                (string.IsNullOrEmpty(c.Group) ||
                    (groupIdFilter.HasValue ? p.GroupId == groupIdFilter : SafeContains(p.GroupName, c.Group))) &&
                (!c.GraduationYearStart.HasValue || (p.GraduationYear.HasValue && p.GraduationYear.Value >= c.GraduationYearStart.Value)) &&
                (!c.SearchByGraduationPeriod || !c.GraduationYearEnd.HasValue || (p.GraduationYear.HasValue && p.GraduationYear.Value <= c.GraduationYearEnd.Value)) &&
                (string.IsNullOrEmpty(c.Specialty) || SafeContains(p.SpecialtyName, c.Specialty) || SafeContains(p.CurrentSpecialtyName, c.Specialty)) &&
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
            GroupTextBox.Clear();
            SpecialtyTextBox.Clear();
            NationalityTextBox.Clear();
            BirthYearTextBox.Clear();
            BirthPlaceTextBox.Clear();
            SocialPositionTextBox.Clear();
            LastWorkplaceTextBox.Clear();
            AddressTextBox.Clear();
            SourceTextBox.Clear();

            // Сброс ComboBox
            IsStudentComboBox.SelectedIndex = 0;
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

            // Сброс результатов поиска групп
            GroupResultsListBox.ItemsSource = null;
            GroupResultsListBox.Visibility = Visibility.Collapsed;

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
    }

    // === 🔹 КРИТЕРИИ ПОИСКА ===
    public class SearchCriteria
    {
        public string FullName { get; set; }
        public bool? IsStudent { get; set; }
        public string Group { get; set; }
        public int? GraduationYearStart { get; set; }
        public bool SearchByGraduationPeriod { get; set; }
        public int? GraduationYearEnd { get; set; }
        public string Specialty { get; set; }
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