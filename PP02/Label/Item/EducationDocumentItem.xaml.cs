using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PP02.Connect;
using PP02.Classes.Person;
using System.Windows.Media;
using MySql.Data.MySqlClient;

namespace PP02.Label.Item
{
    public partial class EducationDocumentItem : UserControl
    {
        private readonly string _connectionString = Connect.Connect.GetConnectionString();
        private EducationDocument _currentDocument;
        private bool _isDirty = false;

        public event EventHandler<int> DocumentDeleted;

        public EducationDocumentItem()
        {
            InitializeComponent();
        }

        // === 🔹 ЗАВИСИМОСТЬ: Данные документа для привязки ===
        public static readonly DependencyProperty DocumentDataProperty =
            DependencyProperty.Register(
                nameof(DocumentData),
                typeof(EducationDocument),
                typeof(EducationDocumentItem),
                new PropertyMetadata(null, OnDocumentDataChanged));

        public EducationDocument DocumentData
        {
            get => (EducationDocument)GetValue(DocumentDataProperty);
            set => SetValue(DocumentDataProperty, value);
        }

        // === 🔹 ОБРАБОТЧИК ИЗМЕНЕНИЯ ДАННЫХ ===
        private static void OnDocumentDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is EducationDocumentItem control && e.NewValue is EducationDocument doc)
            {
                control._currentDocument = doc;
                control.LoadDataToUI();
            }
        }

        // === 🔹 ЗАГРУЗКА ДАННЫХ ИЗ МОДЕЛИ В ИНТЕРФЕЙС ===
        private void LoadDataToUI()
        {
            if (_currentDocument == null) return;

            // Краткий вид
            TxtDocName.Text = _currentDocument.DocName ?? "Без названия";
            TxtDocType.Text = $"Вид: {_currentDocument.DocType ?? "---"}";
            TxtDocSeriesNumber.Text = $"Серия: {_currentDocument.DocSeries ?? "---"} №{_currentDocument.DocNumber ?? "---"}";
            TxtIssueDate.Text = $"Дата: {(_currentDocument.IssueDate?.ToString("dd.MM.yyyy") ?? "---")}";
            TxtRecipientName.Text = $"Получатель: {_currentDocument.RecipientLastName ?? "---"} {_currentDocument.RecipientFirstName ?? ""}";
            TxtEducationLevel.Text = $"Уровень: {_currentDocument.EducationLevel ?? "---"}";

            // Подробный вид - Блок 1: Информация о документе
            TxtEditDocName.Text = _currentDocument.DocName;
            TxtEditDocType.Text = _currentDocument.DocType;
            TxtEditDocStatus.Text = _currentDocument.DocStatus;
            TxtEditEducationLevel.Text = _currentDocument.EducationLevel;
            TxtEditDocSeries.Text = _currentDocument.DocSeries;
            TxtEditDocNumber.Text = _currentDocument.DocNumber;
            DpEditIssueDate.SelectedDate = _currentDocument.IssueDate;
            TxtEditRegNumber.Text = _currentDocument.RegNumber;
            ChkLossConfirmed.IsChecked = _currentDocument.LossConfirmed;
            ChkExchangeConfirmed.IsChecked = _currentDocument.ExchangeConfirmed;
            ChkDestructionConfirmed.IsChecked = _currentDocument.DestructionConfirmed;

            // Блок 2: Специальность и квалификация
            TxtEditSpecialtyCode.Text = _currentDocument.SpecialtyCode;
            TxtEditSpecialtyName.Text = _currentDocument.SpecialtyName;
            TxtEditQualificationName.Text = _currentDocument.QualificationName;
            TxtEditProgramName.Text = _currentDocument.ProgramName;

            // Блок 3: Сроки обучения
            TxtEditEnrollmentYear.Text = _currentDocument.EnrollmentYear?.ToString();
            TxtEditGraduationYear.Text = _currentDocument.GraduationYear?.ToString();
            TxtEditStudyDurationYears.Text = _currentDocument.StudyDurationYears?.ToString();

            // Блок 4: Данные получателя
            TxtEditRecipientLastName.Text = _currentDocument.RecipientLastName;
            TxtEditRecipientFirstName.Text = _currentDocument.RecipientFirstName;
            TxtEditRecipientMiddleName.Text = _currentDocument.RecipientMiddleName;
            DpEditRecipientBirthDate.SelectedDate = _currentDocument.RecipientBirthDate;

            var genderItem = CmbEditRecipientGender.Items.Cast<ComboBoxItem>()
                .FirstOrDefault(i =>
                    (_currentDocument.RecipientGender == "М" && i.Content.ToString() == "Мужской") ||
                    (_currentDocument.RecipientGender == "Ж" && i.Content.ToString() == "Женский"));
            if (genderItem != null)
                CmbEditRecipientGender.SelectedItem = genderItem;

            TxtEditSnils.Text = _currentDocument.Snils;
            TxtEditCitizenshipCountryCode.Text = _currentDocument.CitizenshipCountryCode;

            // Блок 5: Форма и финансирование
            TxtEditStudyForm.Text = _currentDocument.StudyForm;
            TxtEditEducationFormAtTermination.Text = _currentDocument.EducationFormAtTermination;
            TxtEditFundingSource.Text = _currentDocument.FundingSource;

            // Блок 6: Целевое обучение
            ChkEditHasTargetContract.IsChecked = _currentDocument.HasTargetContract;
            TxtEditTargetContractNumber.Text = _currentDocument.TargetContractNumber;
            DpEditTargetContractDate.SelectedDate = _currentDocument.TargetContractDate;
            TxtEditContractOrgName.Text = _currentDocument.ContractOrgName;
            TxtEditContractOrgOgrn.Text = _currentDocument.ContractOrgOgrn;
            TxtEditContractOrgKpp.Text = _currentDocument.ContractOrgKpp;

            // Блок 7: Работодатель
            TxtEditEmployerOrgName.Text = _currentDocument.EmployerOrgName;
            TxtEditEmployerOrgOgrn.Text = _currentDocument.EmployerOrgOgrn;
            TxtEditEmployerOrgKpp.Text = _currentDocument.EmployerOrgKpp;
            TxtEditEmployerFederalSubject.Text = _currentDocument.EmployerFederalSubject;

            // Блок 8: Оригинал документа
            TxtEditOriginalDocName.Text = _currentDocument.OriginalDocName;
            TxtEditOriginalSeries.Text = _currentDocument.OriginalSeries;
            TxtEditOriginalNumber.Text = _currentDocument.OriginalNumber;
            TxtEditOriginalRegNumber.Text = _currentDocument.OriginalRegNumber;
            DpEditOriginalIssueDate.SelectedDate = _currentDocument.OriginalIssueDate;
            TxtEditOriginalRecipientLastName.Text = _currentDocument.OriginalRecipientLastName;
            TxtEditOriginalRecipientFirstName.Text = _currentDocument.OriginalRecipientFirstName;
            TxtEditOriginalRecipientMiddleName.Text = _currentDocument.OriginalRecipientMiddleName;

            _isDirty = false;
            SetFieldsEnabled(false);
        }

        // === 🔹 СБОР ДАННЫХ ИЗ ИНТЕРФЕЙСА В МОДЕЛЬ ===
        private void SaveDataFromUI()
        {
            if (_currentDocument == null) return;

            _currentDocument.DocName = TxtEditDocName.Text;
            _currentDocument.DocType = TxtEditDocType.Text;
            _currentDocument.DocStatus = TxtEditDocStatus.Text;
            _currentDocument.EducationLevel = TxtEditEducationLevel.Text;
            _currentDocument.DocSeries = TxtEditDocSeries.Text;
            _currentDocument.DocNumber = TxtEditDocNumber.Text;
            _currentDocument.IssueDate = DpEditIssueDate.SelectedDate;
            _currentDocument.RegNumber = TxtEditRegNumber.Text;
            _currentDocument.LossConfirmed = ChkLossConfirmed.IsChecked;
            _currentDocument.ExchangeConfirmed = ChkExchangeConfirmed.IsChecked;
            _currentDocument.DestructionConfirmed = ChkDestructionConfirmed.IsChecked;

            _currentDocument.SpecialtyCode = TxtEditSpecialtyCode.Text;
            _currentDocument.SpecialtyName = TxtEditSpecialtyName.Text;
            _currentDocument.QualificationName = TxtEditQualificationName.Text;
            _currentDocument.ProgramName = TxtEditProgramName.Text;

            _currentDocument.EnrollmentYear = int.TryParse(TxtEditEnrollmentYear.Text, out var enrollment) ? enrollment : (int?)null;
            _currentDocument.GraduationYear = int.TryParse(TxtEditGraduationYear.Text, out var graduation) ? graduation : (int?)null;
            _currentDocument.StudyDurationYears = decimal.TryParse(TxtEditStudyDurationYears.Text, out var duration) ? duration : (decimal?)null;

            _currentDocument.RecipientLastName = TxtEditRecipientLastName.Text;
            _currentDocument.RecipientFirstName = TxtEditRecipientFirstName.Text;
            _currentDocument.RecipientMiddleName = TxtEditRecipientMiddleName.Text;
            _currentDocument.RecipientBirthDate = DpEditRecipientBirthDate.SelectedDate;

            var genderItem = CmbEditRecipientGender.SelectedItem as ComboBoxItem;
            _currentDocument.RecipientGender = genderItem?.Content?.ToString() == "Мужской" ? "М" :
                                               genderItem?.Content?.ToString() == "Женский" ? "Ж" : null;

            _currentDocument.Snils = TxtEditSnils.Text;
            _currentDocument.CitizenshipCountryCode = TxtEditCitizenshipCountryCode.Text;

            _currentDocument.StudyForm = TxtEditStudyForm.Text;
            _currentDocument.EducationFormAtTermination = TxtEditEducationFormAtTermination.Text;
            _currentDocument.FundingSource = TxtEditFundingSource.Text;

            _currentDocument.HasTargetContract = ChkEditHasTargetContract.IsChecked;
            _currentDocument.TargetContractNumber = TxtEditTargetContractNumber.Text;
            _currentDocument.TargetContractDate = DpEditTargetContractDate.SelectedDate;
            _currentDocument.ContractOrgName = TxtEditContractOrgName.Text;
            _currentDocument.ContractOrgOgrn = TxtEditContractOrgOgrn.Text;
            _currentDocument.ContractOrgKpp = TxtEditContractOrgKpp.Text;

            _currentDocument.EmployerOrgName = TxtEditEmployerOrgName.Text;
            _currentDocument.EmployerOrgOgrn = TxtEditEmployerOrgOgrn.Text;
            _currentDocument.EmployerOrgKpp = TxtEditEmployerOrgKpp.Text;
            _currentDocument.EmployerFederalSubject = TxtEditEmployerFederalSubject.Text;

            _currentDocument.OriginalDocName = TxtEditOriginalDocName.Text;
            _currentDocument.OriginalSeries = TxtEditOriginalSeries.Text;
            _currentDocument.OriginalNumber = TxtEditOriginalNumber.Text;
            _currentDocument.OriginalRegNumber = TxtEditOriginalRegNumber.Text;
            _currentDocument.OriginalIssueDate = DpEditOriginalIssueDate.SelectedDate;
            _currentDocument.OriginalRecipientLastName = TxtEditOriginalRecipientLastName.Text;
            _currentDocument.OriginalRecipientFirstName = TxtEditOriginalRecipientFirstName.Text;
            _currentDocument.OriginalRecipientMiddleName = TxtEditOriginalRecipientMiddleName.Text;
        }

        // === 🔹 УПРАВЛЕНИЕ ПОЛЯМИ ===
        private void SetFieldsEnabled(bool enabled)
        {
            foreach (var tb in FindVisualChildren<TextBox>(this))
            {
                tb.IsEnabled = enabled;
            }
            foreach (var dp in FindVisualChildren<DatePicker>(this))
            {
                dp.IsEnabled = enabled;
            }
            foreach (var cb in FindVisualChildren<ComboBox>(this))
            {
                cb.IsEnabled = enabled;
            }
            foreach (var chk in FindVisualChildren<CheckBox>(this))
            {
                chk.IsEnabled = enabled;
            }
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) yield break;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                if (child is T t) yield return t;
                foreach (T childOfChild in FindVisualChildren<T>(child)) yield return childOfChild;
            }
        }

        // === 🔹 ОБРАБОТЧИКИ КНОПОК ===
        private void BtnExpand_Click(object sender, RoutedEventArgs e)
        {
            if (ExpDetails != null)
            {
                ExpDetails.Visibility = Visibility.Visible;
                ExpDetails.IsExpanded = true;
            }
            if (BtnExpand != null) BtnExpand.Visibility = Visibility.Collapsed;
            if (BtnEdit != null) BtnEdit.Visibility = Visibility.Visible;
            if (BtnCancel != null) BtnCancel.Visibility = Visibility.Visible;
            if (BtnSave != null) BtnSave.Visibility = Visibility.Collapsed;
            SetFieldsEnabled(false);
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            SetEditMode();
        }

        private void SetEditMode()
        {
            if (BtnEdit != null) BtnEdit.Visibility = Visibility.Collapsed;
            if (BtnSave != null) BtnSave.Visibility = Visibility.Visible;
            if (BtnCancel != null) BtnCancel.Visibility = Visibility.Visible;
            SetFieldsEnabled(true);
        }

        private void SetViewMode()
        {
            if (BtnEdit != null) BtnEdit.Visibility = Visibility.Visible;
            if (BtnSave != null) BtnSave.Visibility = Visibility.Collapsed;
            if (BtnCancel != null) BtnCancel.Visibility = Visibility.Visible;
            SetFieldsEnabled(false);
        }

        private void ExpDetails_Expanded(object sender, RoutedEventArgs e)
        {
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (_currentDocument == null) return;

            try
            {
                SaveDataFromUI();

                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = new MySqlCommand())
                    {
                        command.Connection = connection;
                        command.CommandText = @"
                            UPDATE education_documents SET
                                doc_name = @doc_name, doc_type = @doc_type, doc_status = @doc_status,
                                loss_confirmed = @loss_confirmed, exchange_confirmed = @exchange_confirmed,
                                destruction_confirmed = @destruction_confirmed, education_level = @education_level,
                                doc_series = @doc_series, doc_number = @doc_number, issue_date = @issue_date,
                                reg_number = @reg_number, specialty_code = @specialty_code,
                                specialty_name = @specialty_name, qualification_name = @qualification_name,
                                program_name = @program_name, enrollment_year = @enrollment_year,
                                graduation_year = @graduation_year, study_duration_years = @study_duration_years,
                                recipient_last_name = @recipient_last_name, recipient_first_name = @recipient_first_name,
                                recipient_middle_name = @recipient_middle_name, recipient_birth_date = @recipient_birth_date,
                                recipient_gender = @recipient_gender, snils = @snils,
                                citizenship_country_code = @citizenship_country_code,
                                study_form = @study_form, education_form_at_termination = @education_form_at_termination,
                                funding_source = @funding_source, has_target_contract = @has_target_contract,
                                target_contract_number = @target_contract_number, target_contract_date = @target_contract_date,
                                contract_org_name = @contract_org_name, contract_org_ogrn = @contract_org_ogrn,
                                contract_org_kpp = @contract_org_kpp, employer_org_name = @employer_org_name,
                                employer_org_ogrn = @employer_org_ogrn, employer_org_kpp = @employer_org_kpp,
                                employer_federal_subject = @employer_federal_subject,
                                original_doc_name = @original_doc_name, original_series = @original_series,
                                original_number = @original_number, original_reg_number = @original_reg_number,
                                original_issue_date = @original_issue_date,
                                original_recipient_last_name = @original_recipient_last_name,
                                original_recipient_first_name = @original_recipient_first_name,
                                original_recipient_middle_name = @original_recipient_middle_name
                            WHERE id = @id";

                        command.Parameters.AddWithValue("@id", _currentDocument.Id);
                        AddParameter(command, "@doc_name", _currentDocument.DocName);
                        AddParameter(command, "@doc_type", _currentDocument.DocType);
                        AddParameter(command, "@doc_status", _currentDocument.DocStatus);
                        AddParameter(command, "@loss_confirmed", _currentDocument.LossConfirmed);
                        AddParameter(command, "@exchange_confirmed", _currentDocument.ExchangeConfirmed);
                        AddParameter(command, "@destruction_confirmed", _currentDocument.DestructionConfirmed);
                        AddParameter(command, "@education_level", _currentDocument.EducationLevel);
                        AddParameter(command, "@doc_series", _currentDocument.DocSeries);
                        AddParameter(command, "@doc_number", _currentDocument.DocNumber);
                        AddParameter(command, "@issue_date", _currentDocument.IssueDate);
                        AddParameter(command, "@reg_number", _currentDocument.RegNumber);
                        AddParameter(command, "@specialty_code", _currentDocument.SpecialtyCode);
                        AddParameter(command, "@specialty_name", _currentDocument.SpecialtyName);
                        AddParameter(command, "@qualification_name", _currentDocument.QualificationName);
                        AddParameter(command, "@program_name", _currentDocument.ProgramName);
                        AddParameter(command, "@enrollment_year", _currentDocument.EnrollmentYear);
                        AddParameter(command, "@graduation_year", _currentDocument.GraduationYear);
                        AddParameter(command, "@study_duration_years", _currentDocument.StudyDurationYears);
                        AddParameter(command, "@recipient_last_name", _currentDocument.RecipientLastName);
                        AddParameter(command, "@recipient_first_name", _currentDocument.RecipientFirstName);
                        AddParameter(command, "@recipient_middle_name", _currentDocument.RecipientMiddleName);
                        AddParameter(command, "@recipient_birth_date", _currentDocument.RecipientBirthDate);
                        AddParameter(command, "@recipient_gender", _currentDocument.RecipientGender);
                        AddParameter(command, "@snils", _currentDocument.Snils);
                        AddParameter(command, "@citizenship_country_code", _currentDocument.CitizenshipCountryCode);
                        AddParameter(command, "@study_form", _currentDocument.StudyForm);
                        AddParameter(command, "@education_form_at_termination", _currentDocument.EducationFormAtTermination);
                        AddParameter(command, "@funding_source", _currentDocument.FundingSource);
                        AddParameter(command, "@has_target_contract", _currentDocument.HasTargetContract);
                        AddParameter(command, "@target_contract_number", _currentDocument.TargetContractNumber);
                        AddParameter(command, "@target_contract_date", _currentDocument.TargetContractDate);
                        AddParameter(command, "@contract_org_name", _currentDocument.ContractOrgName);
                        AddParameter(command, "@contract_org_ogrn", _currentDocument.ContractOrgOgrn);
                        AddParameter(command, "@contract_org_kpp", _currentDocument.ContractOrgKpp);
                        AddParameter(command, "@employer_org_name", _currentDocument.EmployerOrgName);
                        AddParameter(command, "@employer_org_ogrn", _currentDocument.EmployerOrgOgrn);
                        AddParameter(command, "@employer_org_kpp", _currentDocument.EmployerOrgKpp);
                        AddParameter(command, "@employer_federal_subject", _currentDocument.EmployerFederalSubject);
                        AddParameter(command, "@original_doc_name", _currentDocument.OriginalDocName);
                        AddParameter(command, "@original_series", _currentDocument.OriginalSeries);
                        AddParameter(command, "@original_number", _currentDocument.OriginalNumber);
                        AddParameter(command, "@original_reg_number", _currentDocument.OriginalRegNumber);
                        AddParameter(command, "@original_issue_date", _currentDocument.OriginalIssueDate);
                        AddParameter(command, "@original_recipient_last_name", _currentDocument.OriginalRecipientLastName);
                        AddParameter(command, "@original_recipient_first_name", _currentDocument.OriginalRecipientFirstName);
                        AddParameter(command, "@original_recipient_middle_name", _currentDocument.OriginalRecipientMiddleName);

                        command.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Документ успешно сохранён!", "Сохранение", MessageBoxButton.OK, MessageBoxImage.Information);
                _isDirty = false;
                SetViewMode();
                // Уведомляем родительскую страницу об изменении для обновления списка
                DocumentDeleted?.Invoke(this, _currentDocument.Id);
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

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (_isDirty)
            {
                var result = MessageBox.Show("Есть несохранённые изменения. Закрыть без сохранения?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes) return;
            }
            LoadDataToUI();
            SetViewMode();
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_currentDocument == null) return;

            var result = MessageBox.Show($"Вы действительно хотите удалить документ \"{_currentDocument.DocName}\"?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = new MySqlCommand("DELETE FROM education_documents WHERE id = @id", connection))
                    {
                        command.Parameters.AddWithValue("@id", _currentDocument.Id);
                        command.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Документ удалён!", "Удаление", MessageBoxButton.OK, MessageBoxImage.Information);
                DocumentDeleted?.Invoke(this, _currentDocument.Id);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}