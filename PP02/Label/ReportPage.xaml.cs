using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32; // Для SaveFileDialog
using DocumentFormat.OpenXml; // Основные типы OpenXML
using DocumentFormat.OpenXml.Packaging; // WordprocessingDocument
using DocumentFormat.OpenXml.Wordprocessing; // Классы для Word (Paragraph, Table и т.д.)
using PP02.Classes.Person;
using PP02.Connect;
using PP02.Classes.Dictionaries;
using PP02.Classes.Specialties;

namespace PP02.Label
{
    public partial class ReportPage : Page
    {
        private List<PersonViewModel> _allPeople;
        private List<PersonViewModel> _filteredPeople;

        // Списки для фильтров
        private List<string> _roles;
        private List<string> _specialties;
        private List<string> _educations;
        private List<string> _socialOrigins;
        private List<string> _socialStatuses;
        private List<string> _parties;

        public ReportPage()
        {
            InitializeComponent();
            LoadDataForFilters();
            LoadAllData();
        }

        private void LoadDataForFilters()
        {
            try
            {
                var dataProvider = new DataProvider();

                // Загружаем справочники
                var specialtiesList = dataProvider.GetSpecialties();
                var educationsList = dataProvider.GetEducations();
                var originsList = dataProvider.GetSocialOrigins();
                var statusesList = dataProvider.GetSocialStatuses();
                var partiesList = dataProvider.GetParties();

                // Формируем списки для ComboBox (добавляем пункт "Все")
                _specialties = new List<string> { "Все" };
                _specialties.AddRange(specialtiesList.Select(s => s.Name));

                _educations = new List<string> { "Все" };
                _educations.AddRange(educationsList.Select(e => e.Name));

                _socialOrigins = new List<string> { "Все" };
                _socialOrigins.AddRange(originsList.Select(o => o.Name));

                _socialStatuses = new List<string> { "Все" };
                _socialStatuses.AddRange(statusesList.Select(s => s.Name));

                _parties = new List<string> { "Все" };
                _parties.AddRange(partiesList.Select(p => p.Name));

                _roles = new List<string> { "Все", "Студент", "Преподаватель" };

                // Привязываем к ComboBox
                CbRole.ItemsSource = _roles;
                CbSpecialty.ItemsSource = _specialties;
                CbEducation.ItemsSource = _educations;
                CbOrigin.ItemsSource = _socialOrigins;
                CbStatus.ItemsSource = _socialStatuses;
                CbParty.ItemsSource = _parties;

                // Выбираем "Все" по умолчанию
                CbRole.SelectedIndex = 0;
                CbSpecialty.SelectedIndex = 0;
                CbEducation.SelectedIndex = 0;
                CbOrigin.SelectedIndex = 0;
                CbStatus.SelectedIndex = 0;
                CbParty.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки фильтров: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadAllData()
        {
            try
            {
                var dataProvider = new DataProvider();
                _allPeople = dataProvider.GetPeople(); // Предполагается, что этот метод есть в DataProvider
                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilters()
        {
            if (_allPeople == null) return;

            var roleFilter = CbRole.SelectedItem?.ToString() ?? "Все";
            var specialtyFilter = CbSpecialty.SelectedItem?.ToString() ?? "Все";
            var educationFilter = CbEducation.SelectedItem?.ToString() ?? "Все";
            var originFilter = CbOrigin.SelectedItem?.ToString() ?? "Все";
            var statusFilter = CbStatus.SelectedItem?.ToString() ?? "Все";
            var partyFilter = CbParty.SelectedItem?.ToString() ?? "Все";
            var nameFilter = TbNameSearch.Text.ToLower();

            _filteredPeople = _allPeople.Where(p =>
            {
                bool matchRole = roleFilter == "Все" || p.Role == roleFilter;
                bool matchSpecialty = specialtyFilter == "Все" || p.SpecialtyName == specialtyFilter;
                bool matchEducation = educationFilter == "Все" || p.EducationName == educationFilter;
                bool matchOrigin = originFilter == "Все" || p.SocialOriginName == originFilter;
                bool matchStatus = statusFilter == "Все" || p.SocialStatusName == statusFilter;
                bool matchParty = partyFilter == "Все" || p.PartyName == partyFilter;
                bool matchName = string.IsNullOrEmpty(nameFilter) ||
                                 (!string.IsNullOrEmpty(p.FullName) && p.FullName.ToLower().Contains(nameFilter));

                return matchRole && matchSpecialty && matchEducation && matchOrigin && matchStatus && matchParty && matchName;
            }).ToList();

            ResultsItemsControl.ItemsSource = _filteredPeople;
            LbCount.Text = $"Найдено записей: {_filteredPeople.Count}";
        }

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private void TbNameSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void BtnExportWord_Click(object sender, RoutedEventArgs e)
        {
            if (_filteredPeople == null || !_filteredPeople.Any())
            {
                MessageBox.Show("Нет данных для экспорта.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Документ Word (*.docx)|*.docx",
                Title = "Сохранить отчет в Word"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    ExportToWord(saveFileDialog.FileName, _filteredPeople);
                    MessageBox.Show("Отчет успешно создан!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при создании Word файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            if (_filteredPeople == null || !_filteredPeople.Any())
            {
                MessageBox.Show("Нет данных для экспорта.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show("Функция экспорта в Excel будет добавлена позже. Используйте Word.", "Инфо", MessageBoxButton.OK, MessageBoxImage.Information);
            // Здесь можно реализовать экспорт в CSV или через EPPlus, если нужно
        }

        private void ExportToWord(string filePath, List<PersonViewModel> people)
        {
            using (WordprocessingDocument wordDocument = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document))
            {
                MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();
                mainPart.Document = new Document();
                Body body = mainPart.Document.AppendChild(new Body());

                // Заголовок
                AddParagraph(body, "Отчет по базе данных людей", true, 16);

                AddParagraph(body, $"Дата формирования: {DateTime.Now.ToShortDateString()}", false, 12);
                AddParagraph(body, $"Всего записей: {people.Count}", false, 12);

                AddParagraph(body, "", false, 12); // Пустая строка

                // Таблица
                Table table = AddTable(body);

                // Заголовки таблицы
                string[] headers = { "ФИО", "Роль", "Специальность", "Образование", "Год выпуска", "Группа" };
                TableRow headerRow = new TableRow();
                foreach (var header in headers)
                {
                    headerRow.Append(CreateTableCell(header, true));
                }
                table.Append(headerRow);

                // Данные
                foreach (var person in people)
                {
                    TableRow row = new TableRow();
                    row.Append(CreateTableCell(person.FullName));
                    row.Append(CreateTableCell(person.Role));
                    row.Append(CreateTableCell(person.SpecialtyName ?? "-"));
                    row.Append(CreateTableCell(person.EducationName ?? "-"));
                    row.Append(CreateTableCell(person.GraduationYear?.ToString() ?? "-"));
                    row.Append(CreateTableCell(person.GroupName ?? "-"));
                    table.Append(row);
                }

                body.Append(table);
                mainPart.Document.Save();
            }
        }

        private void AddParagraph(Body body, string text, bool isBold, int fontSizePt)
        {
            Paragraph para = new Paragraph();
            Run run = new Run();
            RunProperties rPr = new RunProperties();

            if (isBold)
                rPr.Append(new Bold());

            rPr.Append(new FontSize { Val = new StringValue((fontSizePt * 2).ToString()) }); // Размер в полупунктах

            run.Append(rPr);
            run.Append(new DocumentFormat.OpenXml.Wordprocessing.Text(text));
            para.Append(run);
            body.Append(para);
        }

        private Table AddTable(Body body)
        {
            Table table = new Table();

            TableProperties tblProps = new TableProperties(
                new TableBorders(
                    new TopBorder { Val = BorderValues.Single, Size = 6 },
                    new BottomBorder { Val = BorderValues.Single, Size = 6 },
                    new LeftBorder { Val = BorderValues.Single, Size = 6 },
                    new RightBorder { Val = BorderValues.Single, Size = 6 },
                    new InsideHorizontalBorder { Val = BorderValues.Single, Size = 6 },
                    new InsideVerticalBorder { Val = BorderValues.Single, Size = 6 }
                )
            );

            table.Append(tblProps);
            // Добавим стиль таблицы для красоты (опционально)
            table.Append(new TableStyle { Val = "TableGrid" });

            body.Append(table);
            return table;
        }

        private TableCell CreateTableCell(string text, bool isHeader = false)
        {
            TableCell cell = new TableCell();
            Paragraph para = new Paragraph();
            Run run = new Run();
            RunProperties rPr = new RunProperties();

            if (isHeader)
            {
                rPr.Append(new Bold());
                rPr.Append(new Shading { Fill = "D3D3D3" }); // Серый фон для шапки
            }

            rPr.Append(new FontSize { Val = new StringValue("12") }); // 12pt

            run.Append(rPr);
            run.Append(new DocumentFormat.OpenXml.Wordprocessing.Text(text));
            para.Append(run);
            cell.Append(para);

            // Ширина ячеек (опционально)
            if (!isHeader)
            {
                // Можно настроить ширину колонок через TableCellProperties
            }

            return cell;
        }
    }
}