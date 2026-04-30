using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using PP02.Connect;
using PP02.Classes.Person;
using MySql.Data.MySqlClient;
using System.Windows.Controls;

namespace PP02.Label.Dialogs
{
    public partial class AddEducationDocumentDialog : Window
    {
        private readonly string _connectionString;
        public int? NewDocumentId { get; private set; }
        private List<Classes.Person.Person> _peopleList = new List<Classes.Person.Person>();

        public AddEducationDocumentDialog(string connectionString)
        {
            InitializeComponent();
            _connectionString = connectionString;
            LoadPeople();
        }

        /// <summary>
        /// Загрузка списка людей
        /// </summary>
        private void LoadPeople()
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = new MySqlCommand("SELECT id, full_name, role FROM persons ORDER BY full_name", connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            _peopleList.Add(new Classes.Person.Person
                            {
                                Id = reader.GetInt32(0),
                                FullName = reader.GetString(1),
                                Role = reader.GetString(2)
                            });
                        }
                    }
                }
                CmbPerson.ItemsSource = _peopleList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке списка людей: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Создание нового человека
        /// </summary>
        private void BtnCreateNewPerson_Click(object sender, RoutedEventArgs e)
        {
            // Здесь можно открыть диалог создания нового человека
            MessageBox.Show("Для создания нового человека перейдите на страницу управления людьми.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Отмена
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// Сохранение документа
        /// </summary>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверка обязательных полей
            if (string.IsNullOrWhiteSpace(TxtDocName.Text))
            {
                MessageBox.Show("Введите наименование документа!", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtDocName.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtRecipientLastName.Text) || string.IsNullOrWhiteSpace(TxtRecipientFirstName.Text))
            {
                MessageBox.Show("Введите фамилию и имя получателя!", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtRecipientLastName.Focus();
                return;
            }

            try
            {
                int? personId = CmbPerson.SelectedValue as int?;

                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = new MySqlCommand())
                    {
                        command.Connection = connection;
                        command.CommandText = @"
                            INSERT INTO education_documents (
                                person_id, doc_name, doc_type, doc_status,
                                loss_confirmed, exchange_confirmed, destruction_confirmed,
                                education_level, doc_series, doc_number, issue_date,
                                reg_number, specialty_code, specialty_name, qualification_name,
                                program_name, enrollment_year, graduation_year, study_duration_years,
                                recipient_last_name, recipient_first_name, recipient_middle_name,
                                recipient_birth_date, recipient_gender, snils, citizenship_country_code,
                                study_form, education_form_at_termination, funding_source,
                                has_target_contract, target_contract_number, target_contract_date,
                                contract_org_name, contract_org_ogrn, contract_org_kpp,
                                employer_org_name, employer_org_ogrn, employer_org_kpp,
                                employer_federal_subject,
                                original_doc_name, original_series, original_number,
                                original_reg_number, original_issue_date,
                                original_recipient_last_name, original_recipient_first_name,
                                original_recipient_middle_name
                            ) VALUES (
                                @person_id, @doc_name, @doc_type, @doc_status,
                                @loss_confirmed, @exchange_confirmed, @destruction_confirmed,
                                @education_level, @doc_series, @doc_number, @issue_date,
                                @reg_number, @specialty_code, @specialty_name, @qualification_name,
                                @program_name, @enrollment_year, @graduation_year, @study_duration_years,
                                @recipient_last_name, @recipient_first_name, @recipient_middle_name,
                                @recipient_birth_date, @recipient_gender, @snils, @citizenship_country_code,
                                @study_form, @education_form_at_termination, @funding_source,
                                @has_target_contract, @target_contract_number, @target_contract_date,
                                @contract_org_name, @contract_org_ogrn, @contract_org_kpp,
                                @employer_org_name, @employer_org_ogrn, @employer_org_kpp,
                                @employer_federal_subject,
                                @original_doc_name, @original_series, @original_number,
                                @original_reg_number, @original_issue_date,
                                @original_recipient_last_name, @original_recipient_first_name,
                                @original_recipient_middle_name
                            );
                            SELECT LAST_INSERT_ID();";

                        command.Parameters.AddWithValue("@person_id", (object)personId ?? DBNull.Value);
                        AddParameter(command, "@doc_name", TxtDocName.Text);
                        AddParameter(command, "@doc_type", TxtDocType.Text);
                        AddParameter(command, "@doc_status", TxtDocStatus.Text);
                        AddParameter(command, "@loss_confirmed", ChkLossConfirmed.IsChecked);
                        AddParameter(command, "@exchange_confirmed", ChkExchangeConfirmed.IsChecked);
                        AddParameter(command, "@destruction_confirmed", ChkDestructionConfirmed.IsChecked);
                        AddParameter(command, "@education_level", TxtEducationLevel.Text);
                        AddParameter(command, "@doc_series", TxtDocSeries.Text);
                        AddParameter(command, "@doc_number", TxtDocNumber.Text);
                        AddParameter(command, "@issue_date", DpIssueDate.SelectedDate);
                        AddParameter(command, "@reg_number", TxtRegNumber.Text);
                        AddParameter(command, "@specialty_code", TxtSpecialtyCode.Text);
                        AddParameter(command, "@specialty_name", TxtSpecialtyName.Text);
                        AddParameter(command, "@qualification_name", TxtQualificationName.Text);
                        AddParameter(command, "@program_name", TxtProgramName.Text);
                        AddParameter(command, "@enrollment_year", ParseInt(TxtEnrollmentYear.Text));
                        AddParameter(command, "@graduation_year", ParseInt(TxtGraduationYear.Text));
                        AddParameter(command, "@study_duration_years", ParseDecimal(TxtStudyDurationYears.Text));
                        AddParameter(command, "@recipient_last_name", TxtRecipientLastName.Text);
                        AddParameter(command, "@recipient_first_name", TxtRecipientFirstName.Text);
                        AddParameter(command, "@recipient_middle_name", TxtRecipientMiddleName.Text);
                        AddParameter(command, "@recipient_birth_date", DpRecipientBirthDate.SelectedDate);

                        var genderItem = CmbRecipientGender.SelectedItem as ComboBoxItem;
                        var gender = genderItem?.Content?.ToString() == "Мужской" ? "М" :
                                     genderItem?.Content?.ToString() == "Женский" ? "Ж" : null;
                        AddParameter(command, "@recipient_gender", gender);

                        AddParameter(command, "@snils", TxtSnils.Text);
                        AddParameter(command, "@citizenship_country_code", TxtCitizenshipCountryCode.Text);
                        AddParameter(command, "@study_form", TxtStudyForm.Text);
                        AddParameter(command, "@education_form_at_termination", TxtEducationFormAtTermination.Text);
                        AddParameter(command, "@funding_source", TxtFundingSource.Text);
                        AddParameter(command, "@has_target_contract", ChkHasTargetContract.IsChecked);
                        AddParameter(command, "@target_contract_number", TxtTargetContractNumber.Text);
                        AddParameter(command, "@target_contract_date", DpTargetContractDate.SelectedDate);
                        AddParameter(command, "@contract_org_name", TxtContractOrgName.Text);
                        AddParameter(command, "@contract_org_ogrn", TxtContractOrgOgrn.Text);
                        AddParameter(command, "@contract_org_kpp", TxtContractOrgKpp.Text);
                        AddParameter(command, "@employer_org_name", TxtEmployerOrgName.Text);
                        AddParameter(command, "@employer_org_ogrn", TxtEmployerOrgOgrn.Text);
                        AddParameter(command, "@employer_org_kpp", TxtEmployerOrgKpp.Text);
                        AddParameter(command, "@employer_federal_subject", TxtEmployerFederalSubject.Text);
                        AddParameter(command, "@original_doc_name", TxtOriginalDocName.Text);
                        AddParameter(command, "@original_series", TxtOriginalSeries.Text);
                        AddParameter(command, "@original_number", TxtOriginalNumber.Text);
                        AddParameter(command, "@original_reg_number", TxtOriginalRegNumber.Text);
                        AddParameter(command, "@original_issue_date", DpOriginalIssueDate.SelectedDate);
                        AddParameter(command, "@original_recipient_last_name", TxtOriginalRecipientLastName.Text);
                        AddParameter(command, "@original_recipient_first_name", TxtOriginalRecipientFirstName.Text);
                        AddParameter(command, "@original_recipient_middle_name", TxtOriginalRecipientMiddleName.Text);

                        NewDocumentId = Convert.ToInt32(command.ExecuteScalar());
                    }
                }

                MessageBox.Show("Документ успешно добавлен!", "Сохранение", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddParameter(MySqlCommand command, string name, object value)
        {
            if (value == null)
                command.Parameters.AddWithValue(name, DBNull.Value);
            else
                command.Parameters.AddWithValue(name, value);
        }

        private int? ParseInt(string text)
        {
            if (int.TryParse(text, out var result))
                return result;
            return null;
        }

        private decimal? ParseDecimal(string text)
        {
            if (decimal.TryParse(text, out var result))
                return result;
            return null;
        }
    }
}