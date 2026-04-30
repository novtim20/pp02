using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PP02.Connect;
using PP02.Classes.Person;
using MySql.Data.MySqlClient;
using PP02.Label.Dialogs;

namespace PP02.Label
{
    public partial class EducationDocumentsPage : Page
    {
        private readonly string _connectionString = Connect.Connect.GetConnectionString();
        private List<EducationDocument> _allDocuments = new List<EducationDocument>();

        public EducationDocumentsPage()
        {
            InitializeComponent();
            LoadDocuments();
        }

        /// <summary>
        /// Загрузка всех документов из БД
        /// </summary>
        private void LoadDocuments()
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = new MySqlCommand())
                    {
                        command.Connection = connection;
                        command.CommandText = @"
                            SELECT
                                ed.id, ed.person_id, ed.doc_name, ed.doc_type, ed.doc_status,
                                ed.loss_confirmed, ed.exchange_confirmed, ed.destruction_confirmed,
                                ed.education_level, ed.doc_series, ed.doc_number, ed.issue_date,
                                ed.reg_number, ed.specialty_code, ed.specialty_name, ed.qualification_name,
                                ed.program_name, ed.enrollment_year, ed.graduation_year, ed.study_duration_years,
                                ed.recipient_last_name, ed.recipient_first_name, ed.recipient_middle_name,
                                ed.recipient_birth_date, ed.recipient_gender, ed.snils, ed.citizenship_country_code,
                                ed.study_form, ed.education_form_at_termination, ed.funding_source,
                                ed.has_target_contract, ed.target_contract_number, ed.target_contract_date,
                                ed.contract_org_name, ed.contract_org_ogrn, ed.contract_org_kpp,
                                ed.employer_org_name, ed.employer_org_ogrn, ed.employer_org_kpp,
                                ed.employer_federal_subject,
                                ed.original_doc_name, ed.original_series, ed.original_number,
                                ed.original_reg_number, ed.original_issue_date,
                                ed.original_recipient_last_name, ed.original_recipient_first_name,
                                ed.original_recipient_middle_name,
                                p.full_name as person_full_name
                            FROM education_documents ed
                            LEFT JOIN persons p ON ed.person_id = p.id
                            ORDER BY ed.recipient_last_name, ed.recipient_first_name, ed.doc_name";

                        using (var reader = command.ExecuteReader())
                        {
                            _allDocuments.Clear();
                            while (reader.Read())
                            {
                                var doc = new EducationDocument
                                {
                                    Id = reader.GetInt32(0),
                                    PersonId = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1),
                                    DocName = GetStringOrNull(reader, 2),
                                    DocType = GetStringOrNull(reader, 3),
                                    DocStatus = GetStringOrNull(reader, 4),
                                    LossConfirmed = reader.IsDBNull(5) ? (bool?)null : reader.GetBoolean(5),
                                    ExchangeConfirmed = reader.IsDBNull(6) ? (bool?)null : reader.GetBoolean(6),
                                    DestructionConfirmed = reader.IsDBNull(7) ? (bool?)null : reader.GetBoolean(7),
                                    EducationLevel = GetStringOrNull(reader, 8),
                                    DocSeries = GetStringOrNull(reader, 9),
                                    DocNumber = GetStringOrNull(reader, 10),
                                    IssueDate = reader.IsDBNull(11) ? (DateTime?)null : reader.GetDateTime(11),
                                    RegNumber = GetStringOrNull(reader, 12),
                                    SpecialtyCode = GetStringOrNull(reader, 13),
                                    SpecialtyName = GetStringOrNull(reader, 14),
                                    QualificationName = GetStringOrNull(reader, 15),
                                    ProgramName = GetStringOrNull(reader, 16),
                                    EnrollmentYear = reader.IsDBNull(17) ? (int?)null : reader.GetInt32(17),
                                    GraduationYear = reader.IsDBNull(18) ? (int?)null : reader.GetInt32(18),
                                    StudyDurationYears = reader.IsDBNull(19) ? (decimal?)null : reader.GetDecimal(19),
                                    RecipientLastName = GetStringOrNull(reader, 20),
                                    RecipientFirstName = GetStringOrNull(reader, 21),
                                    RecipientMiddleName = GetStringOrNull(reader, 22),
                                    RecipientBirthDate = reader.IsDBNull(23) ? (DateTime?)null : reader.GetDateTime(23),
                                    RecipientGender = GetStringOrNull(reader, 24),
                                    Snils = GetStringOrNull(reader, 25),
                                    CitizenshipCountryCode = GetStringOrNull(reader, 26),
                                    StudyForm = GetStringOrNull(reader, 27),
                                    EducationFormAtTermination = GetStringOrNull(reader, 28),
                                    FundingSource = GetStringOrNull(reader, 29),
                                    HasTargetContract = reader.IsDBNull(30) ? (bool?)null : reader.GetBoolean(30),
                                    TargetContractNumber = GetStringOrNull(reader, 31),
                                    TargetContractDate = reader.IsDBNull(32) ? (DateTime?)null : reader.GetDateTime(32),
                                    ContractOrgName = GetStringOrNull(reader, 33),
                                    ContractOrgOgrn = GetStringOrNull(reader, 34),
                                    ContractOrgKpp = GetStringOrNull(reader, 35),
                                    EmployerOrgName = GetStringOrNull(reader, 36),
                                    EmployerOrgOgrn = GetStringOrNull(reader, 37),
                                    EmployerOrgKpp = GetStringOrNull(reader, 38),
                                    EmployerFederalSubject = GetStringOrNull(reader, 39),
                                    OriginalDocName = GetStringOrNull(reader, 40),
                                    OriginalSeries = GetStringOrNull(reader, 41),
                                    OriginalNumber = GetStringOrNull(reader, 42),
                                    OriginalRegNumber = GetStringOrNull(reader, 43),
                                    OriginalIssueDate = reader.IsDBNull(44) ? (DateTime?)null : reader.GetDateTime(44),
                                    OriginalRecipientLastName = GetStringOrNull(reader, 45),
                                    OriginalRecipientFirstName = GetStringOrNull(reader, 46),
                                    OriginalRecipientMiddleName = GetStringOrNull(reader, 47),
                                    PersonFullName = GetStringOrNull(reader, 48)
                                };
                                _allDocuments.Add(doc);
                            }
                        }
                    }
                }

                FilterAndDisplayDocuments();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке документов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Фильтрация и отображение документов
        /// </summary>
        private void FilterAndDisplayDocuments()
        {
            var searchText = TxtSearch.Text.Trim().ToLower();

            var filtered = string.IsNullOrEmpty(searchText)
                ? _allDocuments
                : _allDocuments.Where(d =>
                    (!string.IsNullOrEmpty(d.RecipientLastName) && d.RecipientLastName.ToLower().Contains(searchText)) ||
                    (!string.IsNullOrEmpty(d.RecipientFirstName) && d.RecipientFirstName.ToLower().Contains(searchText)) ||
                    (!string.IsNullOrEmpty(d.DocSeries) && d.DocSeries.ToLower().Contains(searchText)) ||
                    (!string.IsNullOrEmpty(d.DocNumber) && d.DocNumber.ToLower().Contains(searchText)) ||
                    (!string.IsNullOrEmpty(d.Snils) && d.Snils.ToLower().Contains(searchText)) ||
                    (!string.IsNullOrEmpty(d.DocName) && d.DocName.ToLower().Contains(searchText))
                ).ToList();

            ItemsDocuments.ItemsSource = filtered;
            TxtCount.Text = $"Найдено документов: {filtered.Count}";
            TxtNoData.Visibility = filtered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Безопасное получение строки
        /// </summary>
        private string GetStringOrNull(MySqlDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }

        /// <summary>
        /// Поиск документов
        /// </summary>
        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterAndDisplayDocuments();
        }

        /// <summary>
        /// Обновление списка
        /// </summary>
        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            TxtSearch.Text = string.Empty;
            LoadDocuments();
        }

        /// <summary>
        /// Добавление нового документа
        /// </summary>
        private void BtnAddDocument_Click(object sender, RoutedEventArgs e)
        {
            // Открываем диалог для выбора человека или создания нового документа
            var dialog = new AddEducationDocumentDialog(_connectionString);
            if (dialog.ShowDialog() == true && dialog.NewDocumentId.HasValue)
            {
                LoadDocuments();
            }
        }
    }
}