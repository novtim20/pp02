using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ClosedXML.Excel;
using MySql.Data.MySqlClient;
using PP02.Connect;
using PP02.Classes.Dictionaries;
using PP02.Classes.Specialties;
using PP02.Classes.Person;

namespace PP02.Label
{
    /// <summary>
    /// Модель для настройки маппинга столбцов Excel на поля БД
    /// </summary>
    public class ColumnMapping : INotifyPropertyChanged
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
    public class ColumnMappingInfo
    {
        public string DatabaseField { get; set; }
        public string ExcelColumn { get; set; }
    }

    /// <summary>
    /// Страница импорта данных студентов из Excel с гибкой настройкой маппинга
    /// </summary>
    public partial class ImportExcelPage : Page
    {
        // Строка подключения к БД
        private readonly string _connectionString = Connect.Connect.GetConnectionString();

        // Список всех возможных полей БД для маппинга
        private readonly List<string> _databaseFields = new List<string>
        {
            "full_name", "role", "group_code", "specialty_name", "graduation_year",
            "gender", "birth_year", "birth_place", "nationality", "address",
            "diploma_date", "work_after", "source", "education_name",
            "social_origin_name", "social_status_name", "party_name"
        };

        // Столбцы из Excel файла
        private List<string> _excelColumns = new List<string>();
        public ObservableCollection<string> ExcelColumns { get; } = new ObservableCollection<string>();

        // Данные маппинга
        private ObservableCollection<ColumnMapping> _mappings = new ObservableCollection<ColumnMapping>();
        public ObservableCollection<ColumnMapping> Mappings => _mappings;

        // Загруженные данные из Excel
        private List<Dictionary<string, string>> _excelData = new List<Dictionary<string, string>>();

        // Предпросмотр данных
        private ObservableCollection<ImportPreviewItem> _previewItems = new ObservableCollection<ImportPreviewItem>();

        public ImportExcelPage()
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
                _mappings.Add(new ColumnMapping
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

            // Сохраняем путь к файлу в локальную переменную до запуска задачи
            string filePath = FilePathTextBox.Text;

            try
            {
                LoadFileButton.IsEnabled = false;
                LoadFileButton.Content = "⏳ Загрузка...";

                await Task.Run(() => LoadExcelFile(filePath));

                LoadFileButton.Content = "✅ Файл загружен";
                FileStatusText.Text = $"Загружено строк: {_excelData.Count}";

                // Обновляем пример значений в маппинге
                UpdateSampleValues();

                // Включаем кнопку импорта
                ImportButton.IsEnabled = _excelData.Count > 0;

                MessageBox.Show($"Файл успешно загружен!\nНайдено записей: {_excelData.Count}\n\nТеперь настройте соответствие столбцов.",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                string errorMessage = $"Ошибка загрузки файла: {ex.Message}\n\n" +
                                     $"Тип ошибки: {ex.GetType().Name}\n" +
                                     $"Путь к файлу: {FilePathTextBox.Text}\n\n" +
                                     $"Детали (Stack Trace):\n{ex.StackTrace}";

                MessageBox.Show(errorMessage, "Ошибка",
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

            // Копируем файл во временную папку, чтобы избежать блокировок
            string tempPath = Path.Combine(Path.GetTempPath(), Path.GetFileName(filePath));
            try
            {
                File.Copy(filePath, tempPath, true);

                using (var workbook = new XLWorkbook(tempPath))
                {
                    var worksheet = workbook.Worksheet(1); // Первый лист
                    var firstRow = worksheet.FirstRowUsed();

                    // Чтение заголовков - выполняем в потоке UI
                    var headers = new List<string>();
                    foreach (var cell in firstRow.Cells())
                    {
                        var header = cell.GetValue<string>()?.Trim();
                        if (!string.IsNullOrEmpty(header))
                        {
                            headers.Add(header);
                        }
                    }

                    // Обновляем коллекцию в потоке UI
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ExcelColumns.Clear();
                        foreach (var header in headers)
                        {
                            ExcelColumns.Add(header);
                            _excelColumns.Add(header);
                        }
                    });

                    // Чтение данных
                    var dataRows = worksheet.RowsUsed().Skip(1); // Пропускаем заголовок
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
                // Удаляем временный файл
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
                // Пытаемся найти совпадение по имени
                var matchedColumn = _excelColumns.FirstOrDefault(col =>
                    col.ToLower().Contains(GetSearchKey(mapping.DatabaseField)) ||
                    GetSearchKey(mapping.DatabaseField).Contains(col.ToLower().Replace(" ", "").Replace("_", "")));

                if (matchedColumn != null)
                {
                    mapping.ExcelColumn = matchedColumn;
                    mapping.UseForImport = true;
                    mappedCount++;

                    // Обновляем пример
                    if (_excelData.Count > 0 && _excelData[0].ContainsKey(matchedColumn))
                    {
                        mapping.SampleValue = _excelData[0][matchedColumn];
                    }
                }
            }

            MappingStatusText.Text = $"Автоматически сопоставлено полей: {mappedCount}";

            // Обновляем предпросмотр
            UpdatePreview();
        }

        /// <summary>
        /// Получение ключевого слова для поиска соответствий
        /// </summary>
        private string GetSearchKey(string dbField)
        {
            var keyMap = new Dictionary<string, string>
            {
                { "full_name", "фио имя фамилия отчество" },
                { "role", "роль студент преподаватель" },
                { "group_code", "группа код группы" },
                { "specialty_name", "специальность спец направление" },
                { "graduation_year", "год выпуска выпуск" },
                { "gender", "пол муж жен" },
                { "birth_year", "год рождения рождение" },
                { "birth_place", "место рождения место рожд" },
                { "nationality", "национальность нац" },
                { "address", "адрес адрес проживания прописка" },
                { "diploma_date", "дата диплома диплом" },
                { "work_after", "работа после трудоустройство" },
                { "source", "источник источник информации" },
                { "education_name", "образование образован" },
                { "social_origin_name", "происхождение соц происх" },
                { "social_status_name", "положение соц полож статус" },
                { "party_name", "партийность партия" }
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
            var mapping = button?.DataContext as ColumnMapping;
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
            var mapping = button?.DataContext as ColumnMapping;
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

            // Берем первые 10 записей для предпросмотра
            var previewCount = Math.Min(_excelData.Count, 10);

            for (int i = 0; i < previewCount; i++)
            {
                var rowData = _excelData[i];
                var previewItem = new ImportPreviewItem();

                foreach (var mapping in _mappings)
                {
                    if (mapping.UseForImport && !string.IsNullOrEmpty(mapping.ExcelColumn))
                    {
                        var value = rowData.ContainsKey(mapping.ExcelColumn)
                            ? rowData[mapping.ExcelColumn]
                            : "";

                        switch (mapping.DatabaseField)
                        {
                            case "full_name":
                                previewItem.FullName = value;
                                break;
                            case "role":
                                previewItem.Role = value;
                                break;
                            case "group_code":
                                previewItem.GroupName = value;
                                break;
                            case "specialty_name":
                                previewItem.SpecialtyName = value;
                                break;
                            case "graduation_year":
                                previewItem.GraduationYear = value;
                                break;
                            case "gender":
                                previewItem.Gender = value;
                                break;
                            case "birth_year":
                                previewItem.BirthYear = value;
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

            // Создаем локальную копию маппингов в потоке UI перед фоновой операцией
            var activeMappings = _mappings.Where(m => m.UseForImport && !string.IsNullOrEmpty(m.ExcelColumn))
                .Select(m => new ColumnMappingInfo { DatabaseField = m.DatabaseField, ExcelColumn = m.ExcelColumn })
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

            // Создаем локальную копию данных для использования в фоновом потоке
            var excelDataCopy = _excelData.ToList();
            var mappingsCopy = activeMappings.ToList();

            // Сохраняем значения чекбоксов в локальные переменные до фонового потока
            bool skipDuplicates = SkipDuplicatesCheckBox.IsChecked == true;
            bool validateData = ValidateDataCheckBox.IsChecked == true;

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

                                // Проверка на дубликаты
                                if (skipDuplicates)
                                {
                                    var fullName = GetMappedValueInternal(rowData, mappingsCopy, "full_name");
                                    if (!string.IsNullOrEmpty(fullName) && IsDuplicate(connection, fullName, transaction))
                                    {
                                        skippedCount++;
                                        continue;
                                    }
                                }

                                // Валидация данных
                                if (validateData)
                                {
                                    if (!ValidateRowData(rowData, mappingsCopy))
                                    {
                                        errorCount++;
                                        continue;
                                    }
                                }

                                // Вставка записи
                                if (InsertPerson(connection, rowData, transaction, mappingsCopy))
                                {
                                    importedCount++;
                                }
                                else
                                {
                                    errorCount++;
                                }

                                // Обновление прогресса
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
                            Console.WriteLine($"[ERROR] Transaction failed during import:");
                            Console.WriteLine($"[ERROR] Exception type: {ex.GetType().Name}");
                            Console.WriteLine($"[ERROR] Message: {ex.Message}");
                            Console.WriteLine($"[ERROR] StackTrace: {ex.StackTrace}");
                            if (ex.InnerException != null)
                            {
                                Console.WriteLine($"[ERROR] InnerException: {ex.InnerException.Message}");
                            }
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

                // Переход на страницу поиска
                NavigationService?.Navigate(new search());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Import failed:");
                Console.WriteLine($"[ERROR] Exception type: {ex.GetType().Name}");
                Console.WriteLine($"[ERROR] Message: {ex.Message}");
                Console.WriteLine($"[ERROR] StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[ERROR] InnerException: {ex.InnerException.Message}");
                }
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
        /// Получение значения из строки данных по имени поля БД
        /// </summary>
        private string GetMappedValue(Dictionary<string, string> rowData, string dbField)
        {
            var mapping = _mappings.FirstOrDefault(m => m.DatabaseField == dbField && m.UseForImport);
            if (mapping != null && !string.IsNullOrEmpty(mapping.ExcelColumn) && rowData.ContainsKey(mapping.ExcelColumn))
            {
                return rowData[mapping.ExcelColumn];
            }
            return null;
        }



        /// <summary>
        /// Проверка на дубликат по ФИО
        /// </summary>
        private bool IsDuplicate(MySqlConnection connection, string fullName, MySqlTransaction transaction)
        {
            const string sql = "SELECT COUNT(*) FROM persons WHERE full_name = @full_name";
            using (var command = new MySqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@full_name", fullName);
                var count = Convert.ToInt32(command.ExecuteScalar());
                return count > 0;
            }
        }

        /// <summary>
        /// Валидация строки данных (использует глобальные маппинги)
        /// </summary>
        private bool ValidateRowData(Dictionary<string, string> rowData)
        {
            return ValidateRowDataInternal(rowData, null);
        }

        /// <summary>
        /// Валидация строки данных с использованием копии маппингов (для фонового потока)
        /// </summary>
        private bool ValidateRowData(Dictionary<string, string> rowData, List<ColumnMappingInfo> mappingsCopy)
        {
            return ValidateRowDataInternal(rowData, mappingsCopy);
        }

        /// <summary>
        /// Внутренний метод валидации строки данных
        /// </summary>
        private bool ValidateRowDataInternal(Dictionary<string, string> rowData, List<ColumnMappingInfo> mappingsCopy)
        {
            // Обязательное поле - ФИО
            var fullName = GetMappedValueInternal(rowData, mappingsCopy, "full_name");
            if (string.IsNullOrWhiteSpace(fullName))
                return false;

            // Проверка года рождения
            var birthYearStr = GetMappedValueInternal(rowData, mappingsCopy, "birth_year");
            if (!string.IsNullOrEmpty(birthYearStr))
            {
                if (!int.TryParse(birthYearStr, out int birthYear) || birthYear < 1900 || birthYear > DateTime.Now.Year)
                    return false;
            }

            // Проверка года выпуска
            var graduationYearStr = GetMappedValueInternal(rowData, mappingsCopy, "graduation_year");
            if (!string.IsNullOrEmpty(graduationYearStr))
            {
                if (!int.TryParse(graduationYearStr, out int graduationYear) || graduationYear < 1900 || graduationYear > DateTime.Now.Year + 5)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Вставка записи о человеке в базу (использует глобальные маппинги из UI потока)
        /// </summary>
        private bool InsertPerson(MySqlConnection connection, Dictionary<string, string> rowData, MySqlTransaction transaction)
        {
            return InsertPersonInternal(connection, rowData, transaction, null);
        }

        /// <summary>
        /// Вставка записи о человеке в базу с использованием копии маппингов (для фонового потока)
        /// </summary>
        private bool InsertPerson(MySqlConnection connection, Dictionary<string, string> rowData, MySqlTransaction transaction, List<ColumnMappingInfo> mappingsCopy)
        {
            return InsertPersonInternal(connection, rowData, transaction, mappingsCopy);
        }

        /// <summary>
        /// Внутренний метод вставки записи
        /// </summary>
        private bool InsertPersonInternal(MySqlConnection connection, Dictionary<string, string> rowData, MySqlTransaction transaction, List<ColumnMappingInfo> mappingsCopy)
        {
            try
            {
                // Получаем ФИО и группу из данных
                string fullName = GetMappedValueInternal(rowData, mappingsCopy, "full_name");
                string groupCodeFromExcel = GetMappedValueInternal(rowData, mappingsCopy, "group_code");

                // Если группа не найдена в отдельной колонке, пытаемся извлечь её из ФИО
                if (string.IsNullOrEmpty(groupCodeFromExcel) && !string.IsNullOrEmpty(fullName))
                {
                    // Пытаемся найти паттерн "ФИО Группа" (разделено табуляцией или пробелами)
                    var parts = ParseFullNameAndGroup(fullName);
                    fullName = parts.FullName;
                    groupCodeFromExcel = parts.GroupCode;
                }

                // Получаем ID для справочников - используем копию маппингов если предоставлена
                var educationId = GetDictionaryIdFromCopy(connection, "ref_education", "name", GetMappedValueInternal(rowData, mappingsCopy, "education_name"), transaction);
                var socialOriginId = GetDictionaryIdFromCopy(connection, "ref_social_origin", "name", GetMappedValueInternal(rowData, mappingsCopy, "social_origin_name"), transaction);
                var socialStatusId = GetDictionaryIdFromCopy(connection, "ref_social_status", "name", GetMappedValueInternal(rowData, mappingsCopy, "social_status_name"), transaction);
                var partyId = GetDictionaryIdFromCopy(connection, "ref_party", "name", GetMappedValueInternal(rowData, mappingsCopy, "party_name"), transaction);
                var specialtyId = GetSpecialtyIdFromCopy(connection, GetMappedValueInternal(rowData, mappingsCopy, "specialty_name"), transaction);

                // Получаем или создаем группу
                int? groupId = null;
                if (!string.IsNullOrEmpty(groupCodeFromExcel))
                {
                    groupId = GetOrCreateGroupId(connection, groupCodeFromExcel, specialtyId, transaction);
                }

                // Сначала вставляем запись в таблицу persons
                const string personSql = @"
INSERT INTO persons (
    full_name, role, gender, nationality, birth_year, birth_place, address, source
) VALUES (
    @full_name, @role, @gender, @nationality, @birth_year, @birth_place, @address, @source
)";

                int personId;
                using (var command = new MySqlCommand(personSql, connection, transaction))
                {
                    command.Parameters.AddWithValue("@full_name", (object)fullName ?? DBNull.Value);
                    command.Parameters.AddWithValue("@role", (object)GetMappedValueInternal(rowData, mappingsCopy, "role") ?? "Студент");
                    command.Parameters.AddWithValue("@gender", (object)GetMappedValueInternal(rowData, mappingsCopy, "gender") ?? DBNull.Value);
                    command.Parameters.AddWithValue("@nationality", (object)GetMappedValueInternal(rowData, mappingsCopy, "nationality") ?? DBNull.Value);

                    var birthYear = GetMappedValueInternal(rowData, mappingsCopy, "birth_year");
                    command.Parameters.AddWithValue("@birth_year", !string.IsNullOrEmpty(birthYear) && int.TryParse(birthYear, out int by) ? (object)by : DBNull.Value);

                    command.Parameters.AddWithValue("@birth_place", (object)GetMappedValueInternal(rowData, mappingsCopy, "birth_place") ?? DBNull.Value);
                    command.Parameters.AddWithValue("@address", (object)GetMappedValueInternal(rowData, mappingsCopy, "address") ?? DBNull.Value);
                    command.Parameters.AddWithValue("@source", (object)GetMappedValueInternal(rowData, mappingsCopy, "source") ?? DBNull.Value);

                    command.ExecuteNonQuery();

                    // Получаем ID newly inserted person
                    command.CommandText = "SELECT LAST_INSERT_ID()";
                    command.Parameters.Clear();
                    personId = Convert.ToInt32(command.ExecuteScalar());
                }

                // Вставляем запись в academic_records
                var gradYear = GetMappedValueInternal(rowData, mappingsCopy, "graduation_year");
                var diplomaDate = GetMappedValueInternal(rowData, mappingsCopy, "diploma_date");

                const string academicSql = @"
INSERT INTO academic_records (
    person_id, group_id, specialty_id, education_id, graduation_year, diploma_date
) VALUES (
    @person_id, @group_id, @specialty_id, @education_id, @graduation_year, @diploma_date
)";

                using (var command = new MySqlCommand(academicSql, connection, transaction))
                {
                    command.Parameters.AddWithValue("@person_id", personId);
                    command.Parameters.AddWithValue("@group_id", GetDbValue(groupId));
                    command.Parameters.AddWithValue("@specialty_id", GetDbValue(specialtyId));
                    command.Parameters.AddWithValue("@education_id", GetDbValue(educationId));
                    command.Parameters.AddWithValue("@graduation_year", !string.IsNullOrEmpty(gradYear) && int.TryParse(gradYear, out int gy) ? (object)gy : DBNull.Value);
                    command.Parameters.AddWithValue("@diploma_date", !string.IsNullOrEmpty(diplomaDate) && DateTime.TryParse(diplomaDate, out DateTime dd) ? (object)dd : DBNull.Value);

                    command.ExecuteNonQuery();
                }

                // Вставляем запись в career_records (если есть данные о работе)
                var workAfter = GetMappedValueInternal(rowData, mappingsCopy, "work_after");
                if (!string.IsNullOrEmpty(workAfter))
                {
                    const string careerSql = @"
INSERT INTO career_records (
    person_id, work_after
) VALUES (
    @person_id, @work_after
)";

                    using (var command = new MySqlCommand(careerSql, connection, transaction))
                    {
                        command.Parameters.AddWithValue("@person_id", personId);
                        command.Parameters.AddWithValue("@work_after", (object)workAfter ?? DBNull.Value);
                        command.ExecuteNonQuery();
                    }
                }

                // Вставляем запись в social_profiles (если есть социальные данные)
                if (socialOriginId.HasValue || socialStatusId.HasValue || partyId.HasValue)
                {
                    const string socialSql = @"
INSERT INTO social_profiles (
    person_id, social_origin_id, social_status_id, party_id
) VALUES (
    @person_id, @social_origin_id, @social_status_id, @party_id
)";

                    using (var command = new MySqlCommand(socialSql, connection, transaction))
                    {
                        command.Parameters.AddWithValue("@person_id", personId);
                        command.Parameters.AddWithValue("@social_origin_id", GetDbValue(socialOriginId));
                        command.Parameters.AddWithValue("@social_status_id", GetDbValue(socialStatusId));
                        command.Parameters.AddWithValue("@party_id", GetDbValue(partyId));
                        command.ExecuteNonQuery();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] InsertPerson failed for row: {string.Join(", ", rowData.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");
                Console.WriteLine($"[ERROR] Exception type: {ex.GetType().Name}");
                Console.WriteLine($"[ERROR] Message: {ex.Message}");
                Console.WriteLine($"[ERROR] StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[ERROR] InnerException: {ex.InnerException.Message}");
                }
                return false;
            }
        }

        /// <summary>
        /// Получение значения из строки данных с использованием копии маппингов или глобальных маппингов
        /// </summary>
        private string GetMappedValueInternal(Dictionary<string, string> rowData, List<ColumnMappingInfo> mappingsCopy, string dbField)
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
        /// Получение ID из справочника по названию
        /// </summary>
        private int? GetDictionaryId(MySqlConnection connection, string tableName, string columnName, string name, MySqlTransaction transaction)
        {
            return GetDictionaryIdInternal(connection, tableName, columnName, name, transaction);
        }

        /// <summary>
        /// Получение ID из справочника по названию (для фонового потока с копией маппингов)
        /// </summary>
        private int? GetDictionaryIdFromCopy(MySqlConnection connection, string tableName, string columnName, string name, MySqlTransaction transaction)
        {
            return GetDictionaryIdInternal(connection, tableName, columnName, name, transaction);
        }

        /// <summary>
        /// Внутренний метод получения ID из справочника
        /// </summary>
        private int? GetDictionaryIdInternal(MySqlConnection connection, string tableName, string columnName, string name, MySqlTransaction transaction)
        {
            if (string.IsNullOrEmpty(name)) return null;

            try
            {
                var sql = $"SELECT id FROM {tableName} WHERE name = @name LIMIT 1";
                using (var command = new MySqlCommand(sql, connection, transaction))
                {
                    command.Parameters.AddWithValue("@name", name);
                    Console.WriteLine($"[DEBUG] Looking up {tableName}.name='{name}'");
                    var result = command.ExecuteScalar();
                    var id = result != null ? (int?)Convert.ToInt32(result) : null;
                    Console.WriteLine($"[DEBUG] Found {tableName}.id={id} for name='{name}'");
                    return id;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] GetDictionaryIdInternal failed for table '{tableName}', name='{name}':");
                Console.WriteLine($"[ERROR] Exception type: {ex.GetType().Name}");
                Console.WriteLine($"[ERROR] Message: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[ERROR] InnerException: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        /// <summary>
        /// Получение ID специальности по названию
        /// </summary>
        private int? GetSpecialtyId(MySqlConnection connection, string specialtyName, MySqlTransaction transaction)
        {
            return GetSpecialtyIdInternal(connection, specialtyName, transaction);
        }

        /// <summary>
        /// Получение ID специальности по названию (для фонового потока)
        /// </summary>
        private int? GetSpecialtyIdFromCopy(MySqlConnection connection, string specialtyName, MySqlTransaction transaction)
        {
            return GetSpecialtyIdInternal(connection, specialtyName, transaction);
        }

        /// <summary>
        /// Внутренний метод получения ID специальности
        /// </summary>
        private int? GetSpecialtyIdInternal(MySqlConnection connection, string specialtyName, MySqlTransaction transaction)
        {
            if (string.IsNullOrEmpty(specialtyName)) return null;

            try
            {
                const string sql = "SELECT id FROM specialties WHERE name = @name OR short_name = @name LIMIT 1";
                using (var command = new MySqlCommand(sql, connection, transaction))
                {
                    command.Parameters.AddWithValue("@name", specialtyName);
                    Console.WriteLine($"[DEBUG] Looking up specialties for '{specialtyName}'");
                    var result = command.ExecuteScalar();
                    var id = result != null ? (int?)Convert.ToInt32(result) : null;
                    Console.WriteLine($"[DEBUG] Found specialties.id={id} for name='{specialtyName}'");
                    return id;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] GetSpecialtyIdInternal failed for specialty '{specialtyName}':");
                Console.WriteLine($"[ERROR] Exception type: {ex.GetType().Name}");
                Console.WriteLine($"[ERROR] Message: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[ERROR] InnerException: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        /// <summary>
        /// Получение ID группы по коду
        /// </summary>
        private int? GetGroupId(MySqlConnection connection, string groupCode, MySqlTransaction transaction)
        {
            return GetGroupIdInternal(connection, groupCode, transaction);
        }

        /// <summary>
        /// Получение ID группы по коду (для фонового потока)
        /// </summary>
        private int? GetGroupIdFromCopy(MySqlConnection connection, string groupCode, MySqlTransaction transaction)
        {
            return GetGroupIdInternal(connection, groupCode, transaction);
        }

        /// <summary>
        /// Внутренний метод получения ID группы
        /// </summary>
        private int? GetGroupIdInternal(MySqlConnection connection, string groupCode, MySqlTransaction transaction)
        {
            if (string.IsNullOrEmpty(groupCode)) return null;

            try
            {
                const string sql = "SELECT id FROM `groups` WHERE code = @code LIMIT 1";
                using (var command = new MySqlCommand(sql, connection, transaction))
                {
                    command.Parameters.AddWithValue("@code", groupCode);
                    Console.WriteLine($"[DEBUG] Looking up groups.code='{groupCode}'");
                    var result = command.ExecuteScalar();
                    var id = result != null ? (int?)Convert.ToInt32(result) : null;
                    Console.WriteLine($"[DEBUG] Found groups.id={id} for code='{groupCode}'");
                    return id;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] GetGroupIdInternal failed for group '{groupCode}':");
                Console.WriteLine($"[ERROR] Exception type: {ex.GetType().Name}");
                Console.WriteLine($"[ERROR] Message: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[ERROR] InnerException: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        /// <summary>
        /// Преобразование значения для БД
        /// </summary>
        private object GetDbValue(int? value)
        {
            return value.HasValue ? (object)value.Value : DBNull.Value;
        }

        /// <summary>
        /// Разбор строки "ФИО [табуляция] Группа" на отдельные части
        /// </summary>
        private (string FullName, string GroupCode) ParseFullNameAndGroup(string input)
        {
            if (string.IsNullOrEmpty(input))
                return (input, null);

            // Проверяем наличие табуляции
            var tabIndex = input.IndexOf('\t');
            if (tabIndex >= 0)
            {
                var fullName = input.Substring(0, tabIndex).Trim();
                var groupCode = input.Substring(tabIndex + 1).Trim();
                return (fullName, groupCode);
            }

            // Проверяем наличие нескольких пробелов подряд (возможно разделение)
            // Или паттерн где после фамилии идет код группы типа "Х-Ш 36"
            var parts = input.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                // Проверяем, похожа ли последняя часть на код группы (содержит дефис и цифры)
                var lastPart = parts[parts.Length - 1];
                var secondLastPart = parts.Length > 1 ? parts[parts.Length - 2] : "";

                // Паттерн группы: что-то вроде "Х-Ш 36" или "М-Ш 40"
                if (System.Text.RegularExpressions.Regex.IsMatch(lastPart, @"^\d+$") &&
                    System.Text.RegularExpressions.Regex.IsMatch(secondLastPart, @"^[А-ЯA-Z]-[А-ЯA-Z]$"))
                {
                    var groupCode = secondLastPart + " " + lastPart;
                    var fullName = string.Join(" ", parts.Take(parts.Length - 2));
                    return (fullName, groupCode);
                }
            }

            // Если не нашли паттерн группы, возвращаем как есть
            return (input, null);
        }

        /// <summary>
        /// Получение ID группы по коду или создание новой группы
        /// </summary>
        private int? GetOrCreateGroupId(MySqlConnection connection, string groupCode, int? specialtyId, MySqlTransaction transaction)
        {
            if (string.IsNullOrEmpty(groupCode))
                return null;

            try
            {
                // Сначала пытаемся найти существующую группу
                var existingId = GetGroupIdInternal(connection, groupCode, transaction);
                if (existingId.HasValue)
                {
                    Console.WriteLine($"[INFO] Found existing group '{groupCode}' with ID {existingId.Value}");
                    return existingId.Value;
                }

                // Группы не существует, создаем новую
                Console.WriteLine($"[INFO] Creating new group '{groupCode}'...");

                // Извлекаем short_name из кода группы (например, "ИТ" из "101-ИТ")
                string shortName = groupCode;
                var spaceIndex = groupCode.IndexOf(' ');
                if (spaceIndex > 0)
                {
                    shortName = groupCode.Substring(0, spaceIndex);
                }
                else
                {
                    // Если нет пробела, пытаемся извлечь часть после дефиса (например, "ИТ" из "101-ИТ")
                    var dashIndex = groupCode.IndexOf('-');
                    if (dashIndex > 0 && dashIndex < groupCode.Length - 1)
                    {
                        shortName = groupCode.Substring(dashIndex + 1);
                    }
                }

                // Если specialtyId не указан, пытаемся определить его по коду группы
                int specId = specialtyId ?? 1; // Значение по умолчанию
                Console.WriteLine($"[INFO] Using specialty_id={specId} for group '{groupCode}'");

                const string insertSql = @"
INSERT INTO `groups` (code, short_name, name, specialty_id, is_active)
VALUES (@code, @short_name, @name, @specialty_id, 1)";

                using (var command = new MySqlCommand(insertSql, connection, transaction))
                {
                    command.Parameters.AddWithValue("@code", groupCode);
                    command.Parameters.AddWithValue("@short_name", (object)shortName ?? DBNull.Value);
                    command.Parameters.AddWithValue("@name", (object)groupCode ?? DBNull.Value);
                    command.Parameters.AddWithValue("@specialty_id", specId);

                    Console.WriteLine($"[DEBUG] Executing INSERT: code={groupCode}, short_name={shortName}, name={groupCode}, specialty_id={specId}");

                    command.ExecuteNonQuery();

                    // Получаем новый ID
                    command.CommandText = "SELECT LAST_INSERT_ID()";
                    command.Parameters.Clear();
                    var newId = command.ExecuteScalar();
                    var result = newId != null ? (int?)Convert.ToInt32(newId) : null;
                    Console.WriteLine($"[INFO] Created group '{groupCode}' with ID {result}");
                    return result;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] GetOrCreateGroupId failed for group '{groupCode}':");
                Console.WriteLine($"[ERROR] Exception type: {ex.GetType().Name}");
                Console.WriteLine($"[ERROR] Message: {ex.Message}");
                Console.WriteLine($"[ERROR] StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[ERROR] InnerException: {ex.InnerException.Message}");
                }
                throw;
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
            // Уже находимся на странице импорта студентов
            MessageBox.Show("Вы уже находитесь на странице импорта студентов", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Переключение на импорт документов
        /// </summary>
        private void ImportDocumentsButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new ImportEducationDocumentsPage());
        }
    }

    /// <summary>
    /// Класс для предпросмотра импортируемых данных
    /// </summary>
    public class ImportPreviewItem
    {
        public string FullName { get; set; }
        public string Role { get; set; }
        public string GroupName { get; set; }
        public string SpecialtyName { get; set; }
        public string GraduationYear { get; set; }
        public string Gender { get; set; }
        public string BirthYear { get; set; }
    }
}