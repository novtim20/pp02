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
using ClosedXML.Excel; // Для работы с Excel
using QuestPDF.Fluent; // Для работы с PDF
using QuestPDF.Helpers; // Вспомогательные классы для PDF
using QuestPDF.Infrastructure; // Инфраструктура PDF
using WordDocument = DocumentFormat.OpenXml.Wordprocessing.Document; // Алиас для Word Document
using PdfDocument = QuestPDF.Fluent.Document; // Алиас для PDF Document

namespace PP02.Label
{
    public partial class ReportPage : Page
    {
        private List<PersonViewModel> _allPeople;
        private List<PersonViewModel> _filteredPeople;
        private List<EducationDocument> _allEducationDocuments;
        private List<EducationDocument> _filteredEducationDocuments;

        // Списки для фильтров
        private List<string> _roles;
        private List<string> _specialties;
        private List<string> _educations;
        private List<string> _socialOrigins;
        private List<string> _socialStatuses;
        private List<string> _parties;
        private List<string> _groups;
        private List<string> _docTypes;
        private List<string> _educationLevels;

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
                var groupsList = DataProvider.GroupList;

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

                _groups = new List<string> { "Все" };
                _groups.AddRange(groupsList.Select(g => g.Code));

                _roles = new List<string> { "Все", "Студент", "Преподаватель" };

                // Списки для фильтров документов об образовании
                _docTypes = new List<string> { "Все" };
                _educationLevels = new List<string> { "Все" };

                // Привязываем к ComboBox
                CbRole.ItemsSource = _roles;
                CbSpecialty.ItemsSource = _specialties;
                CbEducation.ItemsSource = _educations;
                CbOrigin.ItemsSource = _socialOrigins;
                CbStatus.ItemsSource = _socialStatuses;
                CbParty.ItemsSource = _parties;
                CbGroup.ItemsSource = _groups;

                // Выбираем "Все" по умолчанию
                CbRole.SelectedIndex = 0;
                CbSpecialty.SelectedIndex = 0;
                CbEducation.SelectedIndex = 0;
                CbOrigin.SelectedIndex = 0;
                CbStatus.SelectedIndex = 0;
                CbParty.SelectedIndex = 0;
                CbGroup.SelectedIndex = 0;
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
                _allEducationDocuments = dataProvider.GetEducationDocuments();
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
            var groupFilter = CbGroup.SelectedItem?.ToString() ?? "Все";
            var nameFilter = TbNameSearch.Text.ToLower();

            // Фильтр по временному промежутку (год выпуска)
            int? startYear = DpStartDate.SelectedDate?.Year;
            int? endYear = DpEndDate.SelectedDate?.Year;

            _filteredPeople = _allPeople.Where(p =>
            {
                bool matchRole = roleFilter == "Все" || p.Role == roleFilter;
                bool matchSpecialty = specialtyFilter == "Все" || p.SpecialtyName == specialtyFilter;
                bool matchEducation = educationFilter == "Все" || p.EducationName == educationFilter;
                bool matchOrigin = originFilter == "Все" || p.SocialOriginName == originFilter;
                bool matchStatus = statusFilter == "Все" || p.SocialStatusName == statusFilter;
                bool matchParty = partyFilter == "Все" || p.PartyName == partyFilter;
                bool matchGroup = groupFilter == "Все" || p.GroupName == groupFilter;
                bool matchName = string.IsNullOrEmpty(nameFilter) ||
                                 (!string.IsNullOrEmpty(p.FullName) && p.FullName.ToLower().Contains(nameFilter));
                bool matchDate = (!startYear.HasValue || (p.GraduationYear.HasValue && p.GraduationYear >= startYear.Value)) &&
                                 (!endYear.HasValue || (p.GraduationYear.HasValue && p.GraduationYear <= endYear.Value));

                return matchRole && matchSpecialty && matchEducation && matchOrigin && matchStatus && matchParty && matchGroup && matchName && matchDate;
            }).ToList();

            // Фильтрация документов об образовании
            if (_allEducationDocuments != null)
            {
                _filteredEducationDocuments = _allEducationDocuments.Where(d =>
                {
                    bool matchName = string.IsNullOrEmpty(nameFilter) ||
                                     (!string.IsNullOrEmpty(d.RecipientLastName) && d.RecipientLastName.ToLower().Contains(nameFilter)) ||
                                     (!string.IsNullOrEmpty(d.RecipientFirstName) && d.RecipientFirstName.ToLower().Contains(nameFilter)) ||
                                     (!string.IsNullOrEmpty(d.PersonFullName) && d.PersonFullName.ToLower().Contains(nameFilter));
                    bool matchDocType = true; // Можно добавить фильтр по типу документа
                    bool matchEducationLevel = true; // Можно добавить фильтр по уровню образования
                    bool matchDate = (!startYear.HasValue || (d.GraduationYear.HasValue && d.GraduationYear >= startYear.Value)) &&
                                     (!endYear.HasValue || (d.GraduationYear.HasValue && d.GraduationYear <= endYear.Value));

                    return matchName && matchDocType && matchEducationLevel && matchDate;
                }).ToList();
            }

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

        private void DateFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService != null && NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
            else
            {
                // Если нет возможности вернуться назад, переходим на главную страницу поиска
                NavigationService?.Navigate(new search());
            }
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
                    // Экспорт документов об образовании вместе с людьми, если включен переключатель
                    if (ChkIncludeEducationDocuments.IsChecked == true)
                    {
                        ExportPeopleWithEducationDocumentsToWord(saveFileDialog.FileName, _filteredPeople, _allEducationDocuments);
                    }
                    else
                    {
                        ExportToWord(saveFileDialog.FileName, _filteredPeople);
                    }

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

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Документ Excel (*.xlsx)|*.xlsx",
                Title = "Сохранить отчет в Excel"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Экспорт документов об образовании вместе с людьми, если включен переключатель
                    if (ChkIncludeEducationDocuments.IsChecked == true)
                    {
                        ExportPeopleWithEducationDocumentsToExcel(saveFileDialog.FileName, _filteredPeople, _allEducationDocuments);
                    }
                    else
                    {
                        ExportToExcel(saveFileDialog.FileName, _filteredPeople);
                    }

                    MessageBox.Show("Отчет успешно создан!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при создании Excel файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnExportPdf_Click(object sender, RoutedEventArgs e)
        {
            if (_filteredPeople == null || !_filteredPeople.Any())
            {
                MessageBox.Show("Нет данных для экспорта.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Документ PDF (*.pdf)|*.pdf",
                Title = "Сохранить отчет в PDF"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Экспорт документов об образовании вместе с людьми, если включен переключатель
                    if (ChkIncludeEducationDocuments.IsChecked == true)
                    {
                        ExportPeopleWithEducationDocumentsToPdf(saveFileDialog.FileName, _filteredPeople, _allEducationDocuments);
                    }
                    else
                    {
                        ExportToPdf(saveFileDialog.FileName, _filteredPeople);
                    }

                    MessageBox.Show("Отчет успешно создан!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при создании PDF файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExportToWord(string filePath, List<PersonViewModel> people)
        {
            using (WordprocessingDocument wordDocument = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document))
            {
                MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();
                mainPart.Document = new WordDocument();
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

            // Создаем границы таблицы
            TableBorders borders = new TableBorders(
                new TopBorder() { Val = BorderValues.Single, Size = 6 },
                new BottomBorder() { Val = BorderValues.Single, Size = 6 },
                new LeftBorder() { Val = BorderValues.Single, Size = 6 },
                new RightBorder() { Val = BorderValues.Single, Size = 6 },
                new InsideHorizontalBorder() { Val = BorderValues.Single, Size = 6 },
                new InsideVerticalBorder() { Val = BorderValues.Single, Size = 6 }
            );

            // Создаем свойства таблицы и добавляем границы
            TableProperties tblProps = new TableProperties();
            tblProps.TableBorders = borders;

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

        private void ExportToExcel(string filePath, List<PersonViewModel> people)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Отчет");

                // Заголовок отчета
                worksheet.Cell(1, 1).Value = "Отчет по базе данных людей";
                worksheet.Cell(1, 1).Style.Font.SetBold();
                worksheet.Cell(1, 1).Style.Font.SetFontSize(16);

                worksheet.Cell(2, 1).Value = $"Дата формирования: {DateTime.Now.ToShortDateString()}";
                worksheet.Cell(3, 1).Value = $"Всего записей: {people.Count}";

                // Заголовки таблицы
                int headerRow = 5;
                string[] headers = { "ФИО", "Роль", "Специальность", "Образование", "Год выпуска", "Группа" };

                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = worksheet.Cell(headerRow, i + 1);
                    cell.Value = headers[i];
                    cell.Style.Font.SetBold();
                    cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                    cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                }

                // Данные
                int dataRow = headerRow + 1;
                foreach (var person in people)
                {
                    worksheet.Cell(dataRow, 1).Value = person.FullName ?? "-";
                    worksheet.Cell(dataRow, 2).Value = person.Role ?? "-";
                    worksheet.Cell(dataRow, 3).Value = person.SpecialtyName ?? "-";
                    worksheet.Cell(dataRow, 4).Value = person.EducationName ?? "-";
                    worksheet.Cell(dataRow, 5).Value = person.GraduationYear?.ToString() ?? "-";
                    worksheet.Cell(dataRow, 6).Value = person.GroupName ?? "-";
                    dataRow++;
                }

                // Автоподбор ширины колонок
                worksheet.Columns().AdjustToContents();

                // Границы для всей таблицы
                var range = worksheet.Range(headerRow, 1, dataRow - 1, headers.Length);
                range.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                range.Style.Border.SetInsideBorder(XLBorderStyleValues.Thin);

                workbook.SaveAs(filePath);
            }
        }

        private void ExportToPdf(string filePath, List<PersonViewModel> people)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            PdfDocument.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    // Заголовок
                    page.Header().Column(col =>
                    {
                        col.Item()
                            .Text("Отчет по базе данных людей")
                            .FontSize(16)
                            .Bold()
                            .AlignCenter();

                        col.Item()
                            .PaddingTop(10)
                            .Text($"Дата формирования: {DateTime.Now.ToShortDateString()}")
                            .FontSize(12);

                        col.Item()
                            .PaddingTop(5)
                            .Text($"Всего записей: {people.Count}")
                            .FontSize(12);
                    });

                    // Таблица
                    page.Content()
                        .PaddingVertical(20)
                        .Table(table =>
                        {
                            // Настройка колонок
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2); // ФИО
                                columns.RelativeColumn(1); // Роль
                                columns.RelativeColumn(1.5f); // Специальность
                                columns.RelativeColumn(1); // Образование
                                columns.RelativeColumn(1); // Год выпуска
                                columns.RelativeColumn(1); // Группа
                            });

                            // Заголовки таблицы
                            string[] headers = { "ФИО", "Роль", "Специальность", "Образование", "Год выпуска", "Группа" };
                            foreach (var header in headers)
                            {
                                table.Cell().Element(CellStyle).Text(header);
                            }

                            // Данные
                            foreach (var person in people)
                            {
                                table.Cell().Element(CellStyle).Text(person.FullName ?? "-");
                                table.Cell().Element(CellStyle).Text(person.Role ?? "-");
                                table.Cell().Element(CellStyle).Text(person.SpecialtyName ?? "-");
                                table.Cell().Element(CellStyle).Text(person.EducationName ?? "-");
                                table.Cell().Element(CellStyle).Text(person.GraduationYear?.ToString() ?? "-");
                                table.Cell().Element(CellStyle).Text(person.GroupName ?? "-");
                            }
                        });

                    // Нумерация страниц
                    page.Footer()
                        .AlignCenter()
                        .Text(text =>
                        {
                            text.CurrentPageNumber();
                            text.Span(" / ");
                            text.TotalPages();
                        });
                });
            })
                .GeneratePdf(filePath);
        }

        private static IContainer CellStyle(IContainer container)
        {
            return container
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(5);
        }

        // ============================================================================
        // МЕТОДЫ ЭКСПОРТА ДОКУМЕНТОВ ОБ ОБРАЗОВАНИИ
        // ============================================================================

        private void ExportEducationDocumentsToWord(string filePath, List<EducationDocument> documents)
        {
            if (documents == null || !documents.Any()) return;

            using (WordprocessingDocument wordDocument = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document))
            {
                MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();
                mainPart.Document = new WordDocument();
                Body body = mainPart.Document.AppendChild(new Body());

                // Заголовок
                AddParagraph(body, "Отчет по документам об образовании", true, 16);
                AddParagraph(body, $"Дата формирования: {DateTime.Now.ToShortDateString()}", false, 12);
                AddParagraph(body, $"Всего записей: {documents.Count}", false, 12);
                AddParagraph(body, "", false, 12);

                // Таблица
                Table table = AddTable(body);

                // Заголовки таблицы
                string[] headers = { "ФИО получателя", "Тип документа", "Уровень образования", "Серия", "Номер", "Дата выдачи", "Специальность", "Год окончания" };
                TableRow headerRow = new TableRow();
                foreach (var header in headers)
                {
                    headerRow.Append(CreateTableCell(header, true));
                }
                table.Append(headerRow);

                // Данные
                foreach (var doc in documents)
                {
                    TableRow row = new TableRow();
                    row.Append(CreateTableCell($"{doc.RecipientLastName} {doc.RecipientFirstName} {doc.RecipientMiddleName}"?.Trim() ?? "-"));
                    row.Append(CreateTableCell(doc.DocType ?? "-"));
                    row.Append(CreateTableCell(doc.EducationLevel ?? "-"));
                    row.Append(CreateTableCell(doc.DocSeries ?? "-"));
                    row.Append(CreateTableCell(doc.DocNumber ?? "-"));
                    row.Append(CreateTableCell(doc.IssueDate?.ToShortDateString() ?? "-"));
                    row.Append(CreateTableCell(doc.SpecialtyName ?? "-"));
                    row.Append(CreateTableCell(doc.GraduationYear?.ToString() ?? "-"));
                    table.Append(row);
                }

                body.Append(table);
                mainPart.Document.Save();
            }
        }

        private void ExportEducationDocumentsToExcel(string filePath, List<EducationDocument> documents)
        {
            if (documents == null || !documents.Any()) return;

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Документы об образовании");

                // Заголовок отчета
                worksheet.Cell(1, 1).Value = "Отчет по документам об образовании";
                worksheet.Cell(1, 1).Style.Font.SetBold();
                worksheet.Cell(1, 1).Style.Font.SetFontSize(16);

                worksheet.Cell(2, 1).Value = $"Дата формирования: {DateTime.Now.ToShortDateString()}";
                worksheet.Cell(3, 1).Value = $"Всего записей: {documents.Count}";

                // Заголовки таблицы
                int headerRow = 5;
                string[] headers = { "ФИО получателя", "Тип документа", "Уровень образования", "Серия", "Номер", "Дата выдачи", "Специальность", "Год окончания", "Форма обучения", "Источник финансирования" };

                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = worksheet.Cell(headerRow, i + 1);
                    cell.Value = headers[i];
                    cell.Style.Font.SetBold();
                    cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                    cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                }

                // Данные
                int dataRow = headerRow + 1;
                foreach (var doc in documents)
                {
                    worksheet.Cell(dataRow, 1).Value = $"{doc.RecipientLastName} {doc.RecipientFirstName} {doc.RecipientMiddleName}".Trim();
                    worksheet.Cell(dataRow, 2).Value = doc.DocType ?? "-";
                    worksheet.Cell(dataRow, 3).Value = doc.EducationLevel ?? "-";
                    worksheet.Cell(dataRow, 4).Value = doc.DocSeries ?? "-";
                    worksheet.Cell(dataRow, 5).Value = doc.DocNumber ?? "-";
                    worksheet.Cell(dataRow, 6).Value = doc.IssueDate?.ToShortDateString() ?? "-";
                    worksheet.Cell(dataRow, 7).Value = doc.SpecialtyName ?? "-";
                    worksheet.Cell(dataRow, 8).Value = doc.GraduationYear?.ToString() ?? "-";
                    worksheet.Cell(dataRow, 9).Value = doc.StudyForm ?? "-";
                    worksheet.Cell(dataRow, 10).Value = doc.FundingSource ?? "-";
                    dataRow++;
                }

                // Автоподбор ширины колонок
                worksheet.Columns().AdjustToContents();

                // Границы для всей таблицы
                var range = worksheet.Range(headerRow, 1, dataRow - 1, headers.Length);
                range.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                range.Style.Border.SetInsideBorder(XLBorderStyleValues.Thin);

                workbook.SaveAs(filePath);
            }
        }

        private void ExportEducationDocumentsToPdf(string filePath, List<EducationDocument> documents)
        {
            if (documents == null || !documents.Any()) return;

            QuestPDF.Settings.License = LicenseType.Community;

            PdfDocument.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    // Заголовок
                    page.Header().Column(col =>
                    {
                        col.Item()
                            .Text("Отчет по документам об образовании")
                            .FontSize(16)
                            .Bold()
                            .AlignCenter();

                        col.Item()
                            .PaddingTop(10)
                            .Text($"Дата формирования: {DateTime.Now.ToShortDateString()}")
                            .FontSize(12);

                        col.Item()
                            .PaddingTop(5)
                            .Text($"Всего записей: {documents.Count}")
                            .FontSize(12);
                    });

                    // Таблица
                    page.Content()
                        .PaddingVertical(20)
                        .Table(table =>
                        {
                            // Настройка колонок
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2); // ФИО
                                columns.RelativeColumn(1); // Тип
                                columns.RelativeColumn(1); // Уровень
                                columns.RelativeColumn(0.8f); // Серия
                                columns.RelativeColumn(1); // Номер
                                columns.RelativeColumn(1); // Дата
                                columns.RelativeColumn(1.5f); // Специальность
                                columns.RelativeColumn(0.8f); // Год
                            });

                            // Заголовки таблицы
                            string[] headers = { "ФИО", "Тип", "Уровень", "Серия", "Номер", "Дата", "Специальность", "Год" };
                            foreach (var header in headers)
                            {
                                table.Cell().Element(CellStyle).Text(header);
                            }

                            // Данные
                            foreach (var doc in documents)
                            {
                                table.Cell().Element(CellStyle).Text($"{doc.RecipientLastName} {doc.RecipientFirstName}".Trim());
                                table.Cell().Element(CellStyle).Text(doc.DocType ?? "-");
                                table.Cell().Element(CellStyle).Text(doc.EducationLevel ?? "-");
                                table.Cell().Element(CellStyle).Text(doc.DocSeries ?? "-");
                                table.Cell().Element(CellStyle).Text(doc.DocNumber ?? "-");
                                table.Cell().Element(CellStyle).Text(doc.IssueDate?.ToShortDateString() ?? "-");
                                table.Cell().Element(CellStyle).Text(doc.SpecialtyName ?? "-");
                                table.Cell().Element(CellStyle).Text(doc.GraduationYear?.ToString() ?? "-");
                            }
                        });

                    // Нумерация страниц
                    page.Footer()
                        .AlignCenter()
                        .Text(text =>
                        {
                            text.CurrentPageNumber();
                            text.Span(" / ");
                            text.TotalPages();
                        });
                });
            })
                .GeneratePdf(filePath);
        }

        /// <summary>
        /// Экспорт людей с их документами об образовании в Word
        /// </summary>
        private void ExportPeopleWithEducationDocumentsToWord(string filePath, List<PersonViewModel> people, List<EducationDocument> allDocuments)
        {
            using (WordprocessingDocument wordDocument = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document))
            {
                MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();
                mainPart.Document = new WordDocument();
                Body body = mainPart.Document.AppendChild(new Body());

                // Заголовок
                AddParagraph(body, "Отчет по людям с документами об образовании", true, 16);
                AddParagraph(body, $"Дата формирования: {DateTime.Now.ToShortDateString()}", false, 12);
                AddParagraph(body, $"Всего записей: {people.Count}", false, 12);
                AddParagraph(body, "", false, 12);

                foreach (var person in people)
                {
                    // Информация о человеке
                    AddParagraph(body, $"ФИО: {person.FullName}", true, 14);
                    AddParagraph(body, $"Роль: {person.Role}", false, 12);
                    AddParagraph(body, $"Специальность: {person.SpecialtyName ?? "-"}", false, 12);
                    AddParagraph(body, $"Образование: {person.EducationName ?? "-"}", false, 12);
                    AddParagraph(body, $"Год выпуска: {person.GraduationYear?.ToString() ?? "-"}", false, 12);
                    AddParagraph(body, $"Группа: {person.GroupName ?? "-"}", false, 12);

                    // Документы об образовании этого человека
                    var personDocs = allDocuments?.Where(d => d.PersonId == person.Id).ToList() ?? new List<EducationDocument>();

                    if (personDocs.Any())
                    {
                        AddParagraph(body, $"Документы об образовании ({personDocs.Count}):", true, 13);

                        Table table = AddTable(body);

                        string[] headers = { "Тип документа", "Уровень образования", "Серия", "Номер", "Дата выдачи", "Специальность", "Год окончания" };
                        TableRow headerRow = new TableRow();
                        foreach (var header in headers)
                        {
                            headerRow.Append(CreateTableCell(header, true));
                        }
                        table.Append(headerRow);

                        foreach (var doc in personDocs)
                        {
                            TableRow row = new TableRow();
                            row.Append(CreateTableCell(doc.DocType ?? "-"));
                            row.Append(CreateTableCell(doc.EducationLevel ?? "-"));
                            row.Append(CreateTableCell(doc.DocSeries ?? "-"));
                            row.Append(CreateTableCell(doc.DocNumber ?? "-"));
                            row.Append(CreateTableCell(doc.IssueDate?.ToShortDateString() ?? "-"));
                            row.Append(CreateTableCell(doc.SpecialtyName ?? "-"));
                            row.Append(CreateTableCell(doc.GraduationYear?.ToString() ?? "-"));
                            table.Append(row);
                        }

                        body.Append(table);
                    }
                    else
                    {
                        AddParagraph(body, "Документы об образовании: нет документов", false, 12);
                    }

                    AddParagraph(body, "", false, 12); // Разделитель между людьми
                    AddParagraph(body, "─────────────────────────────────────────────────────", false, 12);
                    AddParagraph(body, "", false, 12);
                }

                mainPart.Document.Save();
            }
        }

        /// <summary>
        /// Экспорт людей с их документами об образовании в Excel
        /// </summary>
        private void ExportPeopleWithEducationDocumentsToExcel(string filePath, List<PersonViewModel> people, List<EducationDocument> allDocuments)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Люди с документами");

                // Заголовок отчета
                worksheet.Cell(1, 1).Value = "Отчет по людям с документами об образовании";
                worksheet.Cell(1, 1).Style.Font.SetBold();
                worksheet.Cell(1, 1).Style.Font.SetFontSize(16);

                worksheet.Cell(2, 1).Value = $"Дата формирования: {DateTime.Now.ToShortDateString()}";
                worksheet.Cell(3, 1).Value = $"Всего записей: {people.Count}";

                // Заголовки таблицы - объединяем данные людей и документов
                int headerRow = 5;
                string[] headers = {
                    "ФИО", "Роль", "Специальность", "Образование", "Год выпуска", "Группа",
                    "Тип документа", "Уровень образования", "Серия", "Номер", "Дата выдачи",
                    "Специальность (док)", "Год окончания (док)", "Форма обучения", "Источник финансирования"
                };

                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = worksheet.Cell(headerRow, i + 1);
                    cell.Value = headers[i];
                    cell.Style.Font.SetBold();
                    cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                    cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                }

                // Данные
                int dataRow = headerRow + 1;
                foreach (var person in people)
                {
                    var personDocs = allDocuments?.Where(d => d.PersonId == person.Id).ToList() ?? new List<EducationDocument>();

                    if (personDocs.Any())
                    {
                        foreach (var doc in personDocs)
                        {
                            worksheet.Cell(dataRow, 1).Value = person.FullName;
                            worksheet.Cell(dataRow, 2).Value = person.Role;
                            worksheet.Cell(dataRow, 3).Value = person.SpecialtyName ?? "-";
                            worksheet.Cell(dataRow, 4).Value = person.EducationName ?? "-";
                            worksheet.Cell(dataRow, 5).Value = person.GraduationYear?.ToString() ?? "-";
                            worksheet.Cell(dataRow, 6).Value = person.GroupName ?? "-";

                            worksheet.Cell(dataRow, 7).Value = doc.DocType ?? "-";
                            worksheet.Cell(dataRow, 8).Value = doc.EducationLevel ?? "-";
                            worksheet.Cell(dataRow, 9).Value = doc.DocSeries ?? "-";
                            worksheet.Cell(dataRow, 10).Value = doc.DocNumber ?? "-";
                            worksheet.Cell(dataRow, 11).Value = doc.IssueDate?.ToShortDateString() ?? "-";
                            worksheet.Cell(dataRow, 12).Value = doc.SpecialtyName ?? "-";
                            worksheet.Cell(dataRow, 13).Value = doc.GraduationYear?.ToString() ?? "-";
                            worksheet.Cell(dataRow, 14).Value = doc.StudyForm ?? "-";
                            worksheet.Cell(dataRow, 15).Value = doc.FundingSource ?? "-";

                            dataRow++;
                        }
                    }
                    else
                    {
                        // Человек без документов - одна строка с прочерками в полях документов
                        worksheet.Cell(dataRow, 1).Value = person.FullName;
                        worksheet.Cell(dataRow, 2).Value = person.Role;
                        worksheet.Cell(dataRow, 3).Value = person.SpecialtyName ?? "-";
                        worksheet.Cell(dataRow, 4).Value = person.EducationName ?? "-";
                        worksheet.Cell(dataRow, 5).Value = person.GraduationYear?.ToString() ?? "-";
                        worksheet.Cell(dataRow, 6).Value = person.GroupName ?? "-";

                        for (int col = 7; col <= headers.Length; col++)
                        {
                            worksheet.Cell(dataRow, col).Value = "-";
                        }

                        dataRow++;
                    }
                }

                // Автоподбор ширины колонок
                worksheet.Columns().AdjustToContents();

                // Границы для всей таблицы
                var range = worksheet.Range(headerRow, 1, dataRow - 1, headers.Length);
                range.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                range.Style.Border.SetInsideBorder(XLBorderStyleValues.Thin);

                workbook.SaveAs(filePath);
            }
        }

        /// <summary>
        /// Экспорт людей с их документами об образовании в PDF
        /// </summary>
        private void ExportPeopleWithEducationDocumentsToPdf(string filePath, List<PersonViewModel> people, List<EducationDocument> allDocuments)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            PdfDocument.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    // Заголовок
                    page.Header().Column(col =>
                    {
                        col.Item()
                            .Text("Отчет по людям с документами об образовании")
                            .FontSize(16)
                            .Bold()
                            .AlignCenter();

                        col.Item()
                            .PaddingTop(10)
                            .Text($"Дата формирования: {DateTime.Now.ToShortDateString()}")
                            .FontSize(12);

                        col.Item()
                            .PaddingTop(5)
                            .Text($"Всего записей: {people.Count}")
                            .FontSize(12);
                    });

                    // Контент
                    page.Content().PaddingVertical(20).Column(column =>
                    {
                        foreach (var person in people)
                        {
                            // Информация о человеке
                            column.Item().PaddingBottom(5).Text(person.FullName).FontSize(12).Bold();
                            column.Item().Text($"Роль: {person.Role}, Специальность: {person.SpecialtyName ?? "-"}, Группа: {person.GroupName ?? "-"}").FontSize(10);

                            // Документы об образовании
                            var personDocs = allDocuments?.Where(d => d.PersonId == person.Id).ToList() ?? new List<EducationDocument>();

                            if (personDocs.Any())
                            {
                                column.Item().PaddingTop(5).Text($"Документы ({personDocs.Count}):").FontSize(11).Bold();

                                column.Item().PaddingTop(5).Table(table =>
                                {
                                    table.ColumnsDefinition(cols =>
                                    {
                                        cols.RelativeColumn(1); // Тип
                                        cols.RelativeColumn(1); // Уровень
                                        cols.RelativeColumn(0.8f); // Серия
                                        cols.RelativeColumn(1); // Номер
                                        cols.RelativeColumn(1); // Дата
                                        cols.RelativeColumn(1.5f); // Специальность
                                        cols.RelativeColumn(0.8f); // Год
                                    });

                                    string[] headers = { "Тип", "Уровень", "Серия", "Номер", "Дата", "Специальность", "Год" };
                                    foreach (var header in headers)
                                    {
                                        table.Cell().Element(CellStyle).Text(header);
                                    }

                                    foreach (var doc in personDocs)
                                    {
                                        table.Cell().Element(CellStyle).Text(doc.DocType ?? "-");
                                        table.Cell().Element(CellStyle).Text(doc.EducationLevel ?? "-");
                                        table.Cell().Element(CellStyle).Text(doc.DocSeries ?? "-");
                                        table.Cell().Element(CellStyle).Text(doc.DocNumber ?? "-");
                                        table.Cell().Element(CellStyle).Text(doc.IssueDate?.ToShortDateString() ?? "-");
                                        table.Cell().Element(CellStyle).Text(doc.SpecialtyName ?? "-");
                                        table.Cell().Element(CellStyle).Text(doc.GraduationYear?.ToString() ?? "-");
                                    }
                                });
                            }
                            else
                            {
                                column.Item().PaddingTop(5).Text("Нет документов об образовании").FontSize(10).Italic().FontColor(Colors.Grey.Medium);
                            }

                            column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        }
                    });

                    // Нумерация страниц
                    page.Footer()
                        .AlignCenter()
                        .Text(text =>
                        {
                            text.CurrentPageNumber();
                            text.Span(" / ");
                            text.TotalPages();
                        });
                });
            })
                .GeneratePdf(filePath);
        }

    }
}