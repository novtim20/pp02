using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.WordProcessing;
using DocumentFormat.OpenXml.Packaging;
using System.IO;
using PP02.Classes.Person;
using PP02.Connect;
using System.Drawing;

namespace PP02.Label
{
    /// <summary>
    /// Логика взаимодействия для ReportPage.xaml
    /// Страница формирования отчета с фильтрацией и экспортом в Excel/Word
    /// </summary>
    public partial class ReportPage : Page
    {
        private List<PersonViewModel> _allPeople;
        private List<PersonViewModel> _filteredPeople;

        // 🔹 Строка подключения к вашей БД
        private readonly string _connectionString = "server=127.0.0.1;uid=root;pwd=root;database=pp02;port=3306;";

        public ReportPage()
        {
            InitializeComponent();
            LoadData();
        }

        private async void LoadData()
        {
            try
            {
                // Загружаем справочники
                await System.Threading.Tasks.Task.Run(() => 
                {
                    var db = new DataProvider();
                    db.LoadAllDictionaries(_connectionString);
                });
                
                // Загружаем все данные
                _allPeople = await System.Threading.Tasks.Task.Run(() => 
                {
                    var db = new DataProvider();
                    db.DataPeople(_connectionString);
                    return DataProvider.PeopleVMList.ToList();
                });
                
                _filteredPeople = _allPeople.ToList();

                // Заполняем фильтры
                FillFilters();

                // Обновляем предпросмотр
                UpdatePreview();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FillFilters()
        {
            // Специальности
            if (DataProvider.SpecialtyList != null && DataProvider.SpecialtyList.Count > 0)
            {
                SpecialtyFilter.ItemsSource = DataProvider.SpecialtyList;
            }

            // Образование
            if (DataProvider.EducationList != null && DataProvider.EducationList.Count > 0)
            {
                EducationFilter.ItemsSource = DataProvider.EducationList;
            }

            // Социальное происхождение
            if (DataProvider.SocialOriginList != null && DataProvider.SocialOriginList.Count > 0)
            {
                SocialOriginFilter.ItemsSource = DataProvider.SocialOriginList;
            }

            // Годы выпуска - извлекаем уникальные из данных
            var graduationYears = _allPeople
                .Where(p => p.GraduationYear.HasValue)
                .Select(p => p.GraduationYear.Value)
                .Distinct()
                .OrderBy(y => y)
                .ToList();

            foreach (var year in graduationYears)
            {
                GraduationYearFilter.Items.Add(new ComboBoxItem { Content = year.ToString() });
            }

            // Добавляем обработчики изменений
            RoleFilter.SelectionChanged += Filter_SelectionChanged;
            SpecialtyFilter.SelectionChanged += Filter_SelectionChanged;
            GraduationYearFilter.SelectionChanged += Filter_SelectionChanged;
            GenderFilter.SelectionChanged += Filter_SelectionChanged;
            EducationFilter.SelectionChanged += Filter_SelectionChanged;
            SocialOriginFilter.SelectionChanged += Filter_SelectionChanged;
        }

        private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void FullNameSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            _filteredPeople = _allPeople.AsEnumerable();

            // Фильтр по роли
            var selectedRole = (RoleFilter.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (selectedRole != null && selectedRole != "Все")
            {
                _filteredPeople = _filteredPeople.Where(p => p.Role == selectedRole).ToList();
            }

            // Фильтр по специальности
            var selectedSpecialty = SpecialtyFilter.SelectedItem as Classes.Specialties.Specialty;
            if (selectedSpecialty != null)
            {
                _filteredPeople = _filteredPeople.Where(p => p.SpecialtyId == selectedSpecialty.Id).ToList();
            }

            // Фильтр по году выпуска
            var selectedYearText = (GraduationYearFilter.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (selectedYearText != null && selectedYearText != "Все")
            {
                if (int.TryParse(selectedYearText, out int selectedYear))
                {
                    _filteredPeople = _filteredPeople.Where(p => p.GraduationYear == selectedYear).ToList();
                }
            }

            // Фильтр по полу
            var selectedGender = (GenderFilter.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (selectedGender != null && selectedGender != "Все")
            {
                _filteredPeople = _filteredPeople.Where(p => p.Gender == selectedGender).ToList();
            }

            // Фильтр по образованию
            var selectedEducation = EducationFilter.SelectedItem as Classes.Dictionaries.Education;
            if (selectedEducation != null)
            {
                _filteredPeople = _filteredPeople.Where(p => p.EducationId == selectedEducation.Id).ToList();
            }

            // Фильтр по социальному происхождению
            var selectedSocialOrigin = SocialOriginFilter.SelectedItem as Classes.Dictionaries.SocialOrigin;
            if (selectedSocialOrigin != null)
            {
                _filteredPeople = _filteredPeople.Where(p => p.SocialOriginId == selectedSocialOrigin.Id).ToList();
            }

            // Поиск по ФИО
            var searchText = FullNameSearch.Text.Trim().ToLower();
            if (!string.IsNullOrEmpty(searchText))
            {
                _filteredPeople = _filteredPeople.Where(p => 
                    p.FullName.ToLower().Contains(searchText)).ToList();
            }

            UpdatePreview();
        }

        private void UpdatePreview()
        {
            PreviewDataGrid.ItemsSource = null;
            PreviewDataGrid.ItemsSource = _filteredPeople;
            
            StatusMessage.Text = $"Найдено записей: {_filteredPeople.Count}";
        }

        private void ExportToExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_filteredPeople.Count == 0)
                {
                    MessageBox.Show("Нет данных для экспорта", "Предупреждение", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Создаем книгу Excel
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Отчет");

                    // Заголовки
                    var headers = new[] 
                    { 
                        "№", "ФИО", "Роль", "Специальность", "Группа", 
                        "Год выпуска", "Пол", "Национальность", "Год рождения",
                        "Место рождения", "Образование", "Соц. происхождение",
                        "Соц. статус", "Партийность", "Адрес", "Дата диплома",
                        "Работа после", "Источник"
                    };

                    // Стиль заголовков
                    var headerRow = worksheet.Row(1);
                    headerRow.Style.Font.Bold = true;
                    headerRow.Style.Fill.BackgroundColor = XLColor.FromColor(System.Drawing.Color.FromArgb(45, 117, 70));
                    headerRow.Style.Font.FontColor = XLColor.White;

                    for (int i = 0; i < headers.Length; i++)
                    {
                        worksheet.Cell(1, i + 1).Value = headers[i];
                    }

                    // Данные
                    int row = 2;
                    foreach (var person in _filteredPeople)
                    {
                        worksheet.Cell(row, 1).Value = row - 1;
                        worksheet.Cell(row, 2).Value = person.FullName;
                        worksheet.Cell(row, 3).Value = person.Role;
                        worksheet.Cell(row, 4).Value = person.SpecialtyName ?? "";
                        worksheet.Cell(row, 5).Value = person.GroupName ?? "";
                        worksheet.Cell(row, 6).Value = person.GraduationYear?.ToString() ?? "";
                        worksheet.Cell(row, 7).Value = person.Gender ?? "";
                        worksheet.Cell(row, 8).Value = person.Nationality ?? "";
                        worksheet.Cell(row, 9).Value = person.BirthYear?.ToString() ?? "";
                        worksheet.Cell(row, 10).Value = person.BirthPlace ?? "";
                        worksheet.Cell(row, 11).Value = person.EducationName ?? "";
                        worksheet.Cell(row, 12).Value = person.SocialOriginName ?? "";
                        worksheet.Cell(row, 13).Value = person.SocialStatusName ?? "";
                        worksheet.Cell(row, 14).Value = person.PartyName ?? "";
                        worksheet.Cell(row, 15).Value = person.Address ?? "";
                        worksheet.Cell(row, 16).Value = person.DiplomaDate?.ToString("dd.MM.yyyy") ?? "";
                        worksheet.Cell(row, 17).Value = person.WorkAfter ?? "";
                        worksheet.Cell(row, 18).Value = person.Source ?? "";

                        row++;
                    }

                    // Автоширина колонок
                    worksheet.Columns().AdjustToContents();

                    // Сохранение
                    var saveDialog = new Microsoft.Win32.SaveFileDialog
                    {
                        Filter = "Excel файлы (*.xlsx)|*.xlsx",
                        FileName = $"Отчет_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                    };

                    if (saveDialog.ShowDialog() == true)
                    {
                        workbook.Save(saveDialog.FileName);
                        StatusMessage.Text = "Файл Excel успешно сохранен!";
                        MessageBox.Show($"Отчет успешно экспортирован в Excel!\nФайл: {saveDialog.FileName}", 
                            "Экспорт завершен", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта в Excel: {ex.Message}", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportToWord_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_filteredPeople.Count == 0)
                {
                    MessageBox.Show("Нет данных для экспорта", "Предупреждение", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Диалог сохранения
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Word документы (*.docx)|*.docx",
                    FileName = $"Отчет_{DateTime.Now:yyyyMMdd_HHmmss}.docx"
                };

                if (saveDialog.ShowDialog() != true)
                    return;

                // Создание документа Word
                using (var document = WordprocessingDocument.Create(saveDialog.FileName, 
                    WordprocessingDocumentType.Document))
                {
                    var mainPart = document.AddMainDocumentPart();
                    mainPart.Document = new Document();
                    var body = mainPart.Document.AppendChild(new Body());

                    // Заголовок
                    var title = body.AppendChild(new Paragraph());
                    var titleRun = title.AppendChild(new Run());
                    titleRun.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text("Отчет по базе данных"));
                    
                    var titleProperties = new RunProperties();
                    titleProperties.Append(new Bold());
                    titleProperties.Append(new FontSize { Val = "28" });
                    titleRun.RunProperties = titleProperties;

                    // Дата формирования
                    var datePara = body.AppendChild(new Paragraph());
                    var dateRun = datePara.AppendChild(new Run());
                    dateRun.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text(
                        $"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}"));
                    
                    var dateProperties = new RunProperties();
                    dateProperties.Append(new Italic());
                    dateProperties.Append(new FontSize { Val = "14" });
                    dateRun.RunProperties = dateProperties;

                    // Информация о количестве записей
                    var countPara = body.AppendChild(new Paragraph());
                    var countRun = countPara.AppendChild(new Run());
                    countRun.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text(
                        $"Всего записей в отчете: {_filteredPeople.Count}"));
                    
                    body.AppendChild(new Paragraph()); // Пустая строка

                    // Таблица
                    var table = body.AppendChild(new Table());
                    
                    // Стили таблицы
                    var tableProperties = new TableProperties();
                    var tableBorders = new TableBorders();
                    tableBorders.TopBorder = new TopBorder { Val = BorderValues.Single, Size = 4 };
                    tableBorders.BottomBorder = new BottomBorder { Val = BorderValues.Single, Size = 4 };
                    tableBorders.LeftBorder = new LeftBorder { Val = BorderValues.Single, Size = 4 };
                    tableBorders.RightBorder = new RightBorder { Val = BorderValues.Single, Size = 4 };
                    tableBorders.InsideHorizontalBorder = new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 };
                    tableBorders.InsideVerticalBorder = new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 };
                    tableProperties.Append(tableBorders);
                    table.AppendChild(tableProperties);

                    // Заголовки таблицы
                    var headers = new[] 
                    { 
                        "№", "ФИО", "Роль", "Специальность", "Группа", "Год выпуска", "Пол"
                    };

                    var headerRow = new TableRow();
                    foreach (var header in headers)
                    {
                        var cell = new TableCell();
                        var para = new Paragraph();
                        var run = new Run();
                        run.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text(header));
                        
                        var runProperties = new RunProperties();
                        runProperties.Append(new Bold());
                        run.RunProperties = runProperties;
                        
                        para.AppendChild(run);
                        cell.AppendChild(para);
                        headerRow.AppendChild(cell);
                    }
                    table.AppendChild(headerRow);

                    // Данные
                    int rowNumber = 1;
                    foreach (var person in _filteredPeople)
                    {
                        var dataRow = new TableRow();
                        
                        var cells = new[]
                        {
                            rowNumber.ToString(),
                            person.FullName,
                            person.Role,
                            person.SpecialtyName ?? "",
                            person.GroupName ?? "",
                            person.GraduationYear?.ToString() ?? "",
                            person.Gender ?? ""
                        };

                        foreach (var cellData in cells)
                        {
                            var cell = new TableCell();
                            var para = new Paragraph();
                            var run = new Run();
                            run.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text(cellData));
                            para.AppendChild(run);
                            cell.AppendChild(para);
                            dataRow.AppendChild(cell);
                        }

                        table.AppendChild(dataRow);
                        rowNumber++;
                    }

                    // Дополнительная информация
                    body.AppendChild(new Paragraph());
                    var infoPara = body.AppendChild(new Paragraph());
                    var infoRun = infoPara.AppendChild(new Run());
                    infoRun.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text(
                        "Примечание: Это сокращенная версия отчета. Полная информация доступна в системе."));
                    
                    var infoProperties = new RunProperties();
                    infoProperties.Append(new Italic());
                    infoProperties.Append(new FontSize { Val = "10" });
                    infoRun.RunProperties = infoProperties;
                }

                StatusMessage.Text = "Файл Word успешно сохранен!";
                MessageBox.Show($"Отчет успешно экспортирован в Word!\nФайл: {saveDialog.FileName}", 
                    "Экспорт завершен", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта в Word: {ex.Message}", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
