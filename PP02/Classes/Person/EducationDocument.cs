using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PP02.Classes.Person
{
    /// <summary>
    /// Модель документа об образовании (таблица education_documents)
    /// </summary>
    public class EducationDocument
    {
        // ========================================
        // ОСНОВНЫЕ ДАННЫЕ
        // ========================================
        public int Id { get; set; }
        public int? PersonId { get; set; }

        // ========================================
        // ИНФОРМАЦИЯ О ДОКУМЕНТЕ
        // ========================================
        public string DocName { get; set; }                    // Наименование документа
        public string DocType { get; set; }                    // Вид документа
        public string DocStatus { get; set; }                  // Статус документа
        public bool? LossConfirmed { get; set; }               // Подтверждение утраты
        public bool? ExchangeConfirmed { get; set; }           // Подтверждение обмена
        public bool? DestructionConfirmed { get; set; }        // Подтверждение уничтожения
        public string EducationLevel { get; set; }             // Уровень образования

        // ========================================
        // РЕКВИЗИТЫ ДОКУМЕНТА
        // ========================================
        public string DocSeries { get; set; }                  // Серия документа
        public string DocNumber { get; set; }                  // Номер документа
        public DateTime? IssueDate { get; set; }               // Дата выдачи
        public string RegNumber { get; set; }                  // Регистрационный номер

        // ========================================
        // СПЕЦИАЛЬНОСТЬ И КВАЛИФИКАЦИЯ
        // ========================================
        public string SpecialtyCode { get; set; }              // Код профессии, специальности
        public string SpecialtyName { get; set; }              // Наименование профессии, специальности
        public string QualificationName { get; set; }          // Наименование квалификации
        public string ProgramName { get; set; }                // Наименование образовательной программы

        // ========================================
        // СРОКИ ОБУЧЕНИЯ
        // ========================================
        public int? EnrollmentYear { get; set; }               // Год поступления
        public int? GraduationYear { get; set; }               // Год окончания
        public decimal? StudyDurationYears { get; set; }       // Срок обучения, лет

        // ========================================
        // ДАННЫЕ ПОЛУЧАТЕЛЯ
        // ========================================
        public string RecipientLastName { get; set; }          // Фамилия получателя
        public string RecipientFirstName { get; set; }         // Имя получателя
        public string RecipientMiddleName { get; set; }        // Отчество получателя
        public DateTime? RecipientBirthDate { get; set; }      // Дата рождения получателя
        public string RecipientGender { get; set; }            // Пол получателя
        public string Snils { get; set; }                      // СНИЛС
        public string CitizenshipCountryCode { get; set; }     // Гражданство (код по ОКСМ)

        // ========================================
        // ФОРМА И ФИНАНСИРОВАНИЕ
        // ========================================
        public string StudyForm { get; set; }                  // Форма обучения
        public string EducationFormAtTermination { get; set; } // Форма получения образования на момент прекращения
        public string FundingSource { get; set; }              // Источник финансирования обучения

        // ========================================
        // ЦЕЛЕВОЕ ОБУЧЕНИЕ
        // ========================================
        public bool? HasTargetContract { get; set; }           // Наличие договора о целевом обучении
        public string TargetContractNumber { get; set; }       // Номер договора о целевом обучении
        public DateTime? TargetContractDate { get; set; }      // Дата заключения договора
        public string ContractOrgName { get; set; }            // Наименование организации (договор)
        public string ContractOrgOgrn { get; set; }            // ОГРН организации (договор)
        public string ContractOrgKpp { get; set; }             // КПП организации (договор)

        // ========================================
        // РАБОТОДАТЕЛЬ
        // ========================================
        public string EmployerOrgName { get; set; }            // Наименование организации работодателя
        public string EmployerOrgOgrn { get; set; }            // ОГРН организации работодателя
        public string EmployerOrgKpp { get; set; }             // КПП организации работодателя
        public string EmployerFederalSubject { get; set; }     // Субъект федерации работодателя

        // ========================================
        // ОРИГИНАЛ ДОКУМЕНТА
        // ========================================
        public string OriginalDocName { get; set; }            // Наименование оригинала
        public string OriginalSeries { get; set; }             // Серия оригинала
        public string OriginalNumber { get; set; }             // Номер оригинала
        public string OriginalRegNumber { get; set; }          // Регистрационный N оригинала
        public DateTime? OriginalIssueDate { get; set; }       // Дата выдачи оригинала
        public string OriginalRecipientLastName { get; set; }  // Фамилия получателя (оригинал)
        public string OriginalRecipientFirstName { get; set; } // Имя получателя (оригинал)
        public string OriginalRecipientMiddleName { get; set; }// Отчество получателя (оригинал)

        // ========================================
        // СВЯЗАННЫЕ ДАННЫЕ (для отображения)
        // ========================================
        public string PersonFullName { get; set; }             // ФИО человека (из persons)
    }
}