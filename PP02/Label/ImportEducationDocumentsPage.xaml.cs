using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ClosedXML.Excel;
using MySql.Data.MySqlClient;
using PP02.Connect;

namespace PP02.Label
{
    /// <summary>
    /// Модель для настройки маппинга столбцов Excel на поля БД
    /// </summary>
    public class EducationDocColumnMapping : INotifyPropertyChanged
    {
        private string _databaseField;
        private string _excelColumn;
        private bool _useForImport;
        private string _sampleValue;

        public string DatabaseField
        {
            get => _databaseField;
            set { _databaseField = value; OnPropertyChanged(nameof(DatabaseField)); }
        }

        public string ExcelColumn
        {
            get => _excelColumn;
            set { _excelColumn = value; OnPropertyChanged(nameof(ExcelColumn)); }
        }

        public bool UseForImport
        {
            get => _useForImport;
            set { _useForImport = value; OnPropertyChanged(nameof(UseForImport)); }
        }

        public string SampleValue
        {
            get => _sampleValue;
            set { _sampleValue = value; OnPropertyChanged(nameof(SampleValue)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Информация о маппинге для использования в фоновом потоке
    /// </summary>
    public class EducationDocMappingInfo
    {
        public string DatabaseField { get; set; }
        public string ExcelColumn { get; set; }
    }

    /// <summary>
    /// Страница импорта образовательных документов из Excel
    /// </summary>
    public partial class ImportEducationDocumentsPage : Page
    {
        // Строка подключения к БД
        private readonly string _connectionString = Connect.Connect.GetConnectionString();

        // Список всех возможных полей БД для маппинга (education_documents)
        private readonly List<string> _databaseFields = new List<string>
        {
            "person_id", "doc_name", "doc_type", "doc_status", "loss_confirmed",
            "exchange_confirmed", "destruction_confirmed", "education_level",
            "doc_series", "doc_number", "issue_date", "reg_number",
            "specialty_code", "specialty_name", "qualification_name", "program_name",
            "enrollment_year", "graduation_year", "study_duration_years",
            "recipient_last_name", "recipient_first_name", "recipient_middle_name",
            "recipient_birth_date", "recipient_gender", "snils",
            "citizenship_country_code", "study_form", "education_form_at_termination",
            "funding_source", "has_target_contract", "target_contract_number",
            "target_contract_date", "contract_org_name", "contract_org_ogrn",
            "contract_org_kpp", "employer_org_name", "employer_org_ogrn",
            "employer_org_kpp", "employer_federal_subject"
        };

        // Столбцы из Excel файла
        private List<string> _excelColumns = new List<string>();
        public ObservableCollection<string> ExcelColumns { get; } = new ObservableCollection<string>();

        // Данные маппинга
        private ObservableCollection<EducationDocColumnMapping> _mappings = new ObservableCollection<EducationDocColumnMapping>();
        public ObservableCollection<EducationDocColumnMapping> Mappings => _mappings;

        // Загруженные данные из Excel
        private List<Dictionary<string, string>> _excelData = new List<Dictionary<string, string>>();

        // Предпросмотр данных
        private ObservableCollection<EducationDocPreviewItem> _previewItems = new ObservableCollection<EducationDocPreviewItem>();

        public ImportEducationDocumentsPage()
        {
            InitializeComponent();
            InitializeMappings();
            DataContext = this;
        }

        /// <summary>
        /// Инициализация списка маппингов всеми возможными полями
        /// </summary>
        private void InitializeMappings()
        {
            _mappings.Clear();
            foreach (var field in _databaseFields)
            {
                _mappings.Add(new EducationDocColumnMapping
                {
                    DatabaseField = field,
                    ExcelColumn = null,
                    UseForImport = false,
                    SampleValue = "-"
                });
            }
            MappingDataGrid.ItemsSource = _mappings;
        }

        /// <summary>
        /// Выбор файла Excel через диалог
        /// </summary>
        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Excel Files|*.xlsx;*.xls|All Files|*.*",
                Title = "Выберите файл Excel"
            };

            if (dialog.ShowDialog() == true)
            {
                FilePathTextBox.Text = dialog.FileName;
                FileStatusText.Text = $"Файл выбран: {Path.GetFileName(dialog.FileName)}";
            }
        }

        /// <summary>
        /// Загрузка и чтение файла Excel
        /// </summary>
        private async void LoadFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FilePathTextBox.Text))
            {
                MessageBox.Show("Сначала выберите файл Excel", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string filePath = FilePathTextBox.Text;

            try
            {
                LoadFileButton.IsEnabled = false;
                LoadFileButton.Content = "⏳ Загрузка...";

                await Task.Run(() => LoadExcelFile(filePath));

                LoadFileButton.Content = "✅ Файл загружен";
                FileStatusText.Text = $"Загружено строк: {_excelData.Count}";

                UpdateSampleValues();
                ImportButton.IsEnabled = _excelData.Count > 0;

                MessageBox.Show($"Файл успешно загружен!\nНайдено записей: {_excelData.Count}\n\nТеперь настройте соответствие столбцов.",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки файла: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                LoadFileButton.Content = "📋 Загрузить файл";
            }
            finally
            {
                LoadFileButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// Чтение файла Excel
        /// </summary>
        private void LoadExcelFile(string filePath)
        {
            _excelData.Clear();
            _excelColumns.Clear();

            string tempPath = Path.Combine(Path.GetTempPath(), Path.GetFileName(filePath));
            try
            {
                File.Copy(filePath, tempPath, true);

                using (var workbook = new XLWorkbook(tempPath))
                {
                    var worksheet = workbook.Worksheet(1);
                    var firstRow = worksheet.FirstRowUsed();

                    var headers = new List<string>();
                    foreach (var cell in firstRow.Cells())
                    {
                        var header = cell.GetValue<string>()?.Trim();
                        if (!string.IsNullOrEmpty(header))
                        {
                            headers.Add(header);
                        }
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ExcelColumns.Clear();
                        foreach (var header in headers)
                        {
                            ExcelColumns.Add(header);
                            _excelColumns.Add(header);
                        }
                    });

                    var dataRows = worksheet.RowsUsed().Skip(1);
                    foreach (var row in dataRows)
                    {
                        var rowData = new Dictionary<string, string>();
                        for (int i = 0; i < headers.Count; i++)
                        {
                            var cellValue = row.Cell(i + 1).GetValue<string>();
                            rowData[headers[i]] = cellValue ?? "";
                        }
                        _excelData.Add(rowData);
                    }
                }
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); } catch { }
                }
            }
        }

        /// <summary>
        /// Обновление примеров значений для каждого поля
        /// </summary>
        private void UpdateSampleValues()
        {
            if (_excelData.Count == 0) return;

            var firstRow = _excelData[0];
            foreach (var mapping in _mappings)
            {
                if (!string.IsNullOrEmpty(mapping.ExcelColumn) &&
                    firstRow.ContainsKey(mapping.ExcelColumn))
                {
                    var sample = firstRow[mapping.ExcelColumn];
                    mapping.SampleValue = string.IsNullOrEmpty(sample) ? "(пусто)" : sample;
                }
                else
                {
                    mapping.SampleValue = "-";
                }
            }
        }

        /// <summary>
        /// Автоматическая настройка маппинга на основе имен столбцов
        /// </summary>
        private void AutoMapButton_Click(object sender, RoutedEventArgs e)
        {
            if (_excelColumns.Count == 0)
            {
                MessageBox.Show("Сначала загрузите файл Excel", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int mappedCount = 0;
            foreach (var mapping in _mappings)
            {
                var matchedColumn = _excelColumns.FirstOrDefault(col =>
                    col.ToLower().Contains(GetSearchKey(mapping.DatabaseField)) ||
                    GetSearchKey(mapping.DatabaseField).Contains(col.ToLower().Replace(" ", "").Replace("_", "")));

                if (matchedColumn != null)
                {
                    mapping.ExcelColumn = matchedColumn;
                    mapping.UseForImport = true;
                    mappedCount++;

                    if (_excelData.Count > 0 && _excelData[0].ContainsKey(matchedColumn))
                    {
                        mapping.SampleValue = _excelData[0][matchedColumn];
                    }
                }
            }

            MappingStatusText.Text = $"Автоматически сопоставлено полей: {mappedCount}";
            UpdatePreview();
        }

        /// <summary>
        /// Получение ключевого слова для поиска соответствий
        /// </summary>
        private string GetSearchKey(string dbField)
        {
            var keyMap = new Dictionary<string, string>
            {
                { "person_id", "person id лицо id" },
                { "doc_name", "наименование документа название док" },
                { "doc_type", "вид документа тип док" },
                { "doc_status", "статус документа статус док" },
                { "loss_confirmed", "утрата подтверждение потеря" },
                { "exchange_confirmed", "обмен подтверждение" },
                { "destruction_confirmed", "уничтожение подтверждение" },
                { "education_level", "уровень образования образован" },
                { "doc_series", "серия документа серия док" },
                { "doc_number", "номер документа номер док" },
                { "issue_date", "дата выдачи выдача" },
                { "reg_number", "регистрационный номер рег номер" },
                { "specialty_code", "код специальности код проф" },
                { "specialty_name", "наименование специальности специальность" },
                { "qualification_name", "наименование квалификации квалификация" },
                { "program_name", "наименование программы программа" },
                { "enrollment_year", "год поступления поступление" },
                { "graduation_year", "год окончания выпуск год" },
                { "study_duration_years", "срок обучения лет длительность" },
                { "recipient_last_name", "фамилия получателя фамилия" },
                { "recipient_first_name", "имя получателя имя" },
                { "recipient_middle_name", "отчество получателя отчество" },
                { "recipient_birth_date", "дата рождения получателя рождение дата" },
                { "recipient_gender", "пол получателя пол" },
                { "snils", "снилс снилс" },
                { "citizenship_country_code", "гражданство код страны оксм гражданство" },
                { "study_form", "форма обучения форма обуч" },
                { "education_form_at_termination", "форма получения образования форма образован" },
                { "funding_source", "источник финансирования источник финанс" },
                { "has_target_contract", "договор целевом обучении целевой договор" },
                { "target_contract_number", "номер договора целевом номер договор" },
                { "target_contract_date", "дата договора целевом дата договор" },
                { "contract_org_name", "организация договор организация орг" },
                { "contract_org_ogrn", "огрн организации огрн орг" },
                { "contract_org_kpp", "кпп организации кпп орг" },
                { "employer_org_name", "организация работодатель работодатель орг" },
                { "employer_org_ogrn", "огрн работодателя огрн работ" },
                { "employer_org_kpp", "кпп работодателя кпп работ" },
                { "employer_federal_subject", "субъект федерации работодатель субъект регион" }
            };

            return keyMap.ContainsKey(dbField) ? keyMap[dbField] : dbField.Replace("_", " ");
        }

        /// <summary>
        /// Сброс настроек маппинга
        /// </summary>
        private void ResetMappingButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var mapping in _mappings)
            {
                mapping.ExcelColumn = null;
                mapping.UseForImport = false;
                mapping.SampleValue = "-";
            }
            MappingStatusText.Text = "Настройки сброшены";
            PreviewDataGrid.ItemsSource = null;
            _previewItems.Clear();
            PreviewStatusText.Text = "Загрузите файл и настройте маппинг для просмотра";
        }

        /// <summary>
        /// Перемещение строки маппинга вверх
        /// </summary>
        private void MoveUp_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var mapping = button?.DataContext as EducationDocColumnMapping;
            if (mapping == null) return;

            int index = _mappings.IndexOf(mapping);
            if (index > 0)
            {
                _mappings.Move(index, index - 1);
            }
        }

        /// <summary>
        /// Перемещение строки маппинга вниз
        /// </summary>
        private void MoveDown_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var mapping = button?.DataContext as EducationDocColumnMapping;
            if (mapping == null) return;

            int index = _mappings.IndexOf(mapping);
            if (index >= 0 && index < _mappings.Count - 1)
            {
                _mappings.Move(index, index + 1);
            }
        }

        /// <summary>
        /// Обновление предварительного просмотра данных
        /// </summary>
        private void UpdatePreview()
        {
            _previewItems.Clear();

            var previewCount = Math.Min(_excelData.Count, 10);

            for (int i = 0; i < previewCount; i++)
            {
                var rowData = _excelData[i];
                var previewItem = new EducationDocPreviewItem();

                foreach (var mapping in _mappings)
                {
                    if (mapping.UseForImport && !string.IsNullOrEmpty(mapping.ExcelColumn))
                    {
                        var value = rowData.ContainsKey(mapping.ExcelColumn)
                            ? rowData[mapping.ExcelColumn]
                            : "";

                        switch (mapping.DatabaseField)
                        {
                            case "recipient_last_name":
                                previewItem.RecipientLastName = value;
                                break;
                            case "recipient_first_name":
                                previewItem.RecipientFirstName = value;
                                break;
                            case "recipient_middle_name":
                                previewItem.RecipientMiddleName = value;
                                break;
                            case "doc_type":
                                previewItem.DocType = value;
                                break;
                            case "doc_series":
                                previewItem.DocSeries = value;
                                break;
                            case "doc_number":
                                previewItem.DocNumber = value;
                                break;
                            case "issue_date":
                                previewItem.IssueDate = value;
                                break;
                            case "specialty_name":
                                previewItem.SpecialtyName = value;
                                break;
                            case "qualification_name":
                                previewItem.QualificationName = value;
                                break;
                        }
                    }
                }

                _previewItems.Add(previewItem);
            }

            PreviewDataGrid.ItemsSource = null;
            PreviewDataGrid.ItemsSource = _previewItems;
            PreviewStatusText.Text = $"Показано {previewCount} из {_excelData.Count} записей";
        }

        /// <summary>
        /// Импорт данных в базу
        /// </summary>
        private async void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            if (_excelData.Count == 0)
            {
                MessageBox.Show("Нет данных для импорта", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var activeMappings = _mappings.Where(m => m.UseForImport && !string.IsNullOrEmpty(m.ExcelColumn))
                .Select(m => new EducationDocMappingInfo { DatabaseField = m.DatabaseField, ExcelColumn = m.ExcelColumn })
                .ToList();

            if (activeMappings.Count == 0)
            {
                MessageBox.Show("Настройте хотя бы одно поле для импорта", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Начать импорт {_excelData.Count} записей?\n\nБудут использованы поля:\n{string.Join("\n", activeMappings.Select(m => $"• {m.DatabaseField} ← {m.ExcelColumn}"))}",
                "Подтверждение импорта",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            var excelDataCopy = _excelData.ToList();
            var mappingsCopy = activeMappings.ToList();

            bool skipDuplicates = SkipDuplicatesCheckBox.IsChecked == true;
            bool validateData = ValidateDataCheckBox.IsChecked == true;
            bool linkToPerson = LinkToPersonCheckBox.IsChecked == true;

            try
            {
                ImportProgressBar.Visibility = Visibility.Visible;
                ImportProgressText.Visibility = Visibility.Visible;
                ImportButton.IsEnabled = false;

                int importedCount = 0;
                int skippedCount = 0;
                int errorCount = 0;

                await Task.Run(() =>
                {
                    using (var connection = new MySqlConnection(_connectionString))
                    {
                        connection.Open();
                        var transaction = connection.BeginTransaction();

                        try
                        {
                            for (int i = 0; i < excelDataCopy.Count; i++)
                            {
                                var rowData = excelDataCopy[i];

                                if (skipDuplicates)
                                {
                                    var docSeries = GetMappedValueInternal(rowData, mappingsCopy, "doc_series");
                                    var docNumber = GetMappedValueInternal(rowData, mappingsCopy, "doc_number");
                                    if (!string.IsNullOrEmpty(docSeries) && !string.IsNullOrEmpty(docNumber) &&
                                        IsDuplicate(connection, docSeries, docNumber, transaction))
                                    {
                                        skippedCount++;
                                        continue;
                                    }
                                }

                                if (validateData)
                                {
                                    if (!ValidateRowData(rowData, mappingsCopy))
                                    {
                                        errorCount++;
                                        continue;
                                    }
                                }

                                int? personId = null;
                                if (linkToPerson)
                                {
                                    var lastName = GetMappedValueInternal(rowData, mappingsCopy, "recipient_last_name");
                                    var firstName = GetMappedValueInternal(rowData, mappingsCopy, "recipient_first_name");
                                    var middleName = GetMappedValueInternal(rowData, mappingsCopy, "recipient_middle_name");
                                    personId = FindPersonId(connection, lastName, firstName, middleName, transaction);
                                }

                                if (InsertEducationDocument(connection, rowData, transaction, mappingsCopy, personId))
                                {
                                    importedCount++;
                                }
                                else
                                {
                                    errorCount++;
                                }

                                Dispatcher.Invoke(() =>
                                {
                                    ImportProgressBar.Value = (double)(i + 1) / excelDataCopy.Count * 100;
                                    ImportProgressText.Text = $"Обработано: {i + 1} из {excelDataCopy.Count} | Успешно: {importedCount} | Пропущено: {skippedCount} | Ошибки: {errorCount}";
                                });
                            }

                            transaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[ERROR] Transaction failed during import: {ex.Message}");
                            transaction.Rollback();
                            throw;
                        }
                    }
                });

                MessageBox.Show(
                    $"Импорт завершен!\n\n✅ Успешно импортировано: {importedCount}\n⚠️ Пропущено (дубликаты): {skippedCount}\n❌ Ошибки: {errorCount}",
                    "Результат импорта",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                NavigationService?.Navigate(new search());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при импорте: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ImportProgressBar.Visibility = Visibility.Collapsed;
                ImportProgressText.Visibility = Visibility.Collapsed;
                ImportButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// Получение значения из строки данных с использованием копии маппингов или глобальных маппингов
        /// </summary>
        private string GetMappedValueInternal(Dictionary<string, string> rowData, List<EducationDocMappingInfo> mappingsCopy, string dbField)
        {
            if (mappingsCopy != null)
            {
                var mapping = mappingsCopy.FirstOrDefault(m => m.DatabaseField == dbField);
                if (mapping != null && !string.IsNullOrEmpty(mapping.ExcelColumn) && rowData.ContainsKey(mapping.ExcelColumn))
                {
                    return rowData[mapping.ExcelColumn];
                }
            }
            else
            {
                var mapping = _mappings.FirstOrDefault(m => m.DatabaseField == dbField && m.UseForImport);
                if (mapping != null && !string.IsNullOrEmpty(mapping.ExcelColumn) && rowData.ContainsKey(mapping.ExcelColumn))
                {
                    return rowData[mapping.ExcelColumn];
                }
            }
            return null;
        }

        /// <summary>
        /// Проверка на дубликат по серии и номеру документа
        /// </summary>
        private bool IsDuplicate(MySqlConnection connection, string docSeries, string docNumber, MySqlTransaction transaction)
        {
            const string sql = "SELECT COUNT(*) FROM education_documents WHERE doc_series = @series AND doc_number = @number";
            using (var command = new MySqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@series", docSeries);
                command.Parameters.AddWithValue("@number", docNumber);
                var count = Convert.ToInt32(command.ExecuteScalar());
                return count > 0;
            }
        }

        /// <summary>
        /// Поиск ID лица по ФИО
        /// </summary>
        private int? FindPersonId(MySqlConnection connection, string lastName, string firstName, string middleName, MySqlTransaction transaction)
        {
            if (string.IsNullOrEmpty(lastName) && string.IsNullOrEmpty(firstName))
                return null;

            var sql = "SELECT id FROM persons WHERE 1=1";
            if (!string.IsNullOrEmpty(lastName))
                sql += " AND full_name LIKE @lastName";
            if (!string.IsNullOrEmpty(firstName))
                sql += " AND full_name LIKE @firstName";

            using (var command = new MySqlCommand(sql, connection, transaction))
            {
                if (!string.IsNullOrEmpty(lastName))
                    command.Parameters.AddWithValue("@lastName", $"%{lastName}%");
                if (!string.IsNullOrEmpty(firstName))
                    command.Parameters.AddWithValue("@firstName", $"%{firstName}%");

                var result = command.ExecuteScalar();
                return result != null ? (int?)Convert.ToInt32(result) : null;
            }
        }

        /// <summary>
        /// Валидация строки данных
        /// </summary>
        private bool ValidateRowData(Dictionary<string, string> rowData, List<EducationDocMappingInfo> mappingsCopy)
        {
            var docSeries = GetMappedValueInternal(rowData, mappingsCopy, "doc_series");
            var docNumber = GetMappedValueInternal(rowData, mappingsCopy, "doc_number");

            if (string.IsNullOrEmpty(docSeries) || string.IsNullOrEmpty(docNumber))
                return false;

            var issueDateStr = GetMappedValueInternal(rowData, mappingsCopy, "issue_date");
            if (!string.IsNullOrEmpty(issueDateStr))
            {
                if (!DateTime.TryParse(issueDateStr, out _))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Вставка записи об образовательном документе
        /// </summary>
        private bool InsertEducationDocument(MySqlConnection connection, Dictionary<string, string> rowData,
            MySqlTransaction transaction, List<EducationDocMappingInfo> mappingsCopy, int? personId)
        {
            try
            {
                var fields = new List<string>();
                var parameters = new List<string>();

                if (personId.HasValue)
                {
                    fields.Add("person_id");
                    parameters.Add("@person_id");
                }

                foreach (var mapping in mappingsCopy)
                {
                    if (mapping.DatabaseField == "person_id") continue;

                    var value = GetMappedValueInternal(rowData, mappingsCopy, mapping.DatabaseField);
                    if (!string.IsNullOrEmpty(value))
                    {
                        fields.Add(mapping.DatabaseField);
                        parameters.Add("@" + mapping.DatabaseField);
                    }
                }

                if (fields.Count == 0)
                    return false;

                var sql = $"INSERT INTO education_documents ({string.Join(", ", fields)}) VALUES ({string.Join(", ", parameters)})";

                using (var command = new MySqlCommand(sql, connection, transaction))
                {
                    if (personId.HasValue)
                    {
                        command.Parameters.AddWithValue("@person_id", personId.Value);
                    }

                    foreach (var mapping in mappingsCopy)
                    {
                        if (mapping.DatabaseField == "person_id") continue;

                        var value = GetMappedValueInternal(rowData, mappingsCopy, mapping.DatabaseField);
                        if (!string.IsNullOrEmpty(value))
                        {
                            var paramName = "@" + mapping.DatabaseField;

                            if (mapping.DatabaseField.EndsWith("_date") && DateTime.TryParse(value, out DateTime dateValue))
                            {
                                command.Parameters.AddWithValue(paramName, dateValue);
                            }
                            else if (mapping.DatabaseField.EndsWith("_year") && int.TryParse(value, out int yearValue))
                            {
                                command.Parameters.AddWithValue(paramName, yearValue);
                            }
                            else if (mapping.DatabaseField.EndsWith("_confirmed") && int.TryParse(value, out int boolValue))
                            {
                                command.Parameters.AddWithValue(paramName, boolValue == 1);
                            }
                            else if (mapping.DatabaseField == "study_duration_years" && decimal.TryParse(value, out decimal durationValue))
                            {
                                command.Parameters.AddWithValue(paramName, durationValue);
                            }
                            else if (mapping.DatabaseField == "has_target_contract" && int.TryParse(value, out int contractValue))
                            {
                                command.Parameters.AddWithValue(paramName, contractValue == 1);
                            }
                            else
                            {
                                command.Parameters.AddWithValue(paramName, value);
                            }
                        }
                    }

                    command.ExecuteNonQuery();
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] InsertEducationDocument failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Отмена и переход назад
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new search());
        }

        /// <summary>
        /// Переключение на импорт студентов
        /// </summary>
        private void ImportStudentsButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new ImportExcelPage());
        }

        /// <summary>
        /// Переключение на импорт документов
        /// </summary>
        private void ImportDocumentsButton_Click(object sender, RoutedEventArgs e)
        {
            // Уже находимся на странице импорта документов
            MessageBox.Show("Вы уже находитесь на странице импорта документов", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    /// <summary>
    /// Класс для предпросмотра импортируемых данных
    /// </summary>
    public class EducationDocPreviewItem
    {
        public string RecipientLastName { get; set; }
        public string RecipientFirstName { get; set; }
        public string RecipientMiddleName { get; set; }
        public string RecipientFullName => $"{RecipientLastName} {RecipientFirstName} {RecipientMiddleName}".Trim();
        public string DocType { get; set; }
        public string DocSeries { get; set; }
        public string DocNumber { get; set; }
        public string IssueDate { get; set; }
        public string SpecialtyName { get; set; }
        public string QualificationName { get; set; }
    }
}