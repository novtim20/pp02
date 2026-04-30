using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;
using PP02.Classes.Dictionaries;
using PP02.Classes.Specialties;
using PP02.Classes.Person; // 🔹 Проверьте, что этот namespace соответствует вашим моделям

namespace PP02.Connect
{
    /// <summary>
    /// Класс для работы с базой данных: загрузка справочников и основных данных
    /// </summary>
    public class DataProvider
    {
        // ============================================================================
        // СТАТИЧЕСКИЕ СПИСКИ ДЛЯ ХРАНЕНИЯ ДАННЫХ
        // ============================================================================

        #region Справочники

        public static List<Education> EducationList { get; } = new List<Education>();
        public static List<SocialOrigin> SocialOriginList { get; } = new List<SocialOrigin>();
        public static List<SocialStatus> SocialStatusList { get; } = new List<SocialStatus>();
        public static List<Party> PartyList { get; } = new List<Party>();
        public static List<Specialty> SpecialtyList { get; } = new List<Specialty>();
        public static List<Group> GroupList { get; } = new List<Group>();
        public static List<SpecialtyGroup> SpecialtyGroupList { get; } = new List<SpecialtyGroup>();
        public static List<SpecialtyMapping> SpecialtyMappingList { get; } = new List<SpecialtyMapping>();
        public static List<SpecialtyAlias> SpecialtyAliasList { get; } = new List<SpecialtyAlias>();
        public static List<SpecialtyTransition> SpecialtyTransitionList { get; } = new List<SpecialtyTransition>();

        #endregion

        #region Основные данные

        public static List<PersonViewModel> PeopleVMList { get; } = new List<PersonViewModel>();
        public static List<EducationDocument> EducationDocumentsList { get; } = new List<EducationDocument>();

        #endregion

        // ============================================================================
        // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
        // ============================================================================

        /// <summary>
        /// Создаёт и открывает соединение с БД
        /// </summary>
        private static MySqlConnection GetConnection(string connectionString)
        {
            var connection = new MySqlConnection(connectionString);
            if (connection.State != ConnectionState.Open)
                connection.Open();
            return connection;
        }

        /// <summary>
        /// Безопасное получение строки с проверкой на NULL
        /// </summary>
        private static string GetStringOrNull(MySqlDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }

        /// <summary>
        /// Безопасное получение int? с проверкой на NULL
        /// </summary>
        private static int? GetIntOrNull(MySqlDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? (int?)null : reader.GetInt32(ordinal);
        }

        /// <summary>
        /// Безопасное получение DateTime? с проверкой на NULL
        /// </summary>
        private static DateTime? GetDateTimeOrNull(MySqlDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? (DateTime?)null : reader.GetDateTime(ordinal);
        }

        // ============================================================================
        // МЕТОДЫ ЗАГРУЗКИ СПРАВОЧНИКОВ
        // ============================================================================

        /// <summary>
        /// Загружает справочник "Образование"
        /// </summary>
        public void DataEducation(string connectionString)
        {
            EducationList.Clear();

            const string sql = "SELECT id, name FROM ref_education ORDER BY name";

            using (var connection = GetConnection(connectionString))
            using (var command = new MySqlCommand(sql, connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    EducationList.Add(new Education
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1)
                    });
                }
            }
        }

        /// <summary>
        /// Загружает справочник "Социальное происхождение"
        /// </summary>
        public void DataSocialOrigin(string connectionString)
        {
            SocialOriginList.Clear();

            const string sql = "SELECT id, name FROM ref_social_origin ORDER BY name";

            using (var connection = GetConnection(connectionString))
            using (var command = new MySqlCommand(sql, connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    SocialOriginList.Add(new SocialOrigin
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1)
                    });
                }
            }
        }

        /// <summary>
        /// Загружает справочник "Социальное положение"
        /// </summary>
        public void DataSocialStatus(string connectionString)
        {
            SocialStatusList.Clear();

            const string sql = "SELECT id, name FROM ref_social_status ORDER BY name";

            using (var connection = GetConnection(connectionString))
            using (var command = new MySqlCommand(sql, connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    SocialStatusList.Add(new SocialStatus
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1)
                    });
                }
            }
        }

        /// <summary>
        /// Загружает справочник "Партийность"
        /// </summary>
        public void DataParty(string connectionString)
        {
            PartyList.Clear();

            const string sql = "SELECT id, name FROM ref_party ORDER BY name";

            using (var connection = GetConnection(connectionString))
            using (var command = new MySqlCommand(sql, connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    PartyList.Add(new Party
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1)
                    });
                }
            }
        }

        /// <summary>
        /// Загружает справочник "Специальности" с поддержкой FULLTEXT поиска
        /// </summary>
        public void DataSpecialties(string connectionString)
        {
            SpecialtyList.Clear();

            const string sql = "SELECT id, name, short_name, active, data, group_id FROM specialties WHERE active = 1 ORDER BY name";

            using (var connection = GetConnection(connectionString))
            using (var command = new MySqlCommand(sql, connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    SpecialtyList.Add(new Specialty
                    {
                        Id = reader.GetInt32(0),
                        Name = GetStringOrNull(reader, 1),
                        ShortName = GetStringOrNull(reader, 2),
                        IsActive = reader.GetBoolean(3),
                        ValidFrom = reader.IsDBNull(4) ? (DateTime?)null : reader.GetDateTime(4),
                        GroupId = GetIntOrNull(reader, 5)
                    });
                }
            }
        }

        /// <summary>
        /// Загружает группы специальностей (таблица specialty_groups)
        /// Сортировка по названию
        /// </summary>
        public void DataSpecialtyGroups(string connectionString)
        {
            SpecialtyGroupList.Clear();

            const string sql = @"SELECT id, name
                                 FROM specialty_groups
                                 ORDER BY name";

            using (var connection = GetConnection(connectionString))
            using (var command = new MySqlCommand(sql, connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var group = new SpecialtyGroup
                    {
                        Id = reader.GetInt32(0),
                        Name = GetStringOrNull(reader, 1),
                        ShortName = null
                    };

                    // Загружаем специальности для этой группы
                    LoadSpecialtiesForGroup(group, connectionString);

                    SpecialtyGroupList.Add(group);
                }
            }
        }

        /// <summary>
        /// Загружает специальности для конкретной группы специальностей
        /// </summary>
        private void LoadSpecialtiesForGroup(SpecialtyGroup group, string connectionString)
        {
            const string sql = "SELECT id, name, active, data, group_id FROM specialties WHERE group_id = @groupId ORDER BY name";

            using (var connection = GetConnection(connectionString))
            using (var command = new MySqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@groupId", group.Id);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        group.Specialties.Add(new Specialty
                        {
                            Id = reader.GetInt32(0),
                            Name = GetStringOrNull(reader, 1),
                            ShortName = null,
                            IsActive = reader.GetBoolean(2),
                            ValidFrom = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3),
                            GroupId = GetIntOrNull(reader, 4)
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Загружает список групп с поддержкой FULLTEXT поиска по code, short_name, name
        /// </summary>
        public void DataGroups(string connectionString)
        {
            GroupList.Clear();

            const string sql = @"SELECT g.id, g.code, g.short_name, g.name, g.specialty_id, g.is_active, s.name as specialty_name
                                 FROM `groups` g
                                 LEFT JOIN specialties s ON g.specialty_id = s.id
                                 ORDER BY g.code";

            using (var connection = GetConnection(connectionString))
            using (var command = new MySqlCommand(sql, connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    GroupList.Add(new Group
                    {
                        Id = reader.GetInt32(0),
                        Code = reader.GetString(1),
                        ShortName = GetStringOrNull(reader, 2),
                        Name = GetStringOrNull(reader, 3),
                        SpecialtyId = reader.GetInt32(4),
                        IsActive = reader.GetBoolean(5),
                        SpecialtyName = GetStringOrNull(reader, 6)
                    });
                }
            }
        }

        /// <summary>
        /// Поиск групп по строке с использованием LIKE (FULLTEXT удалён, т.к. не поддерживается в вашей БД)
        /// </summary>
        public List<Group> SearchGroups(string connectionString, string searchText)
        {
            var results = new List<Group>();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                return new List<Group>(GroupList);
            }

            // Используем LIKE поиск по таблице groups
            const string sql = @"SELECT g.id, g.code, g.short_name, g.name, g.specialty_id, g.is_active, s.name as specialty_name
                                 FROM `groups` g
                                 LEFT JOIN specialties s ON g.specialty_id = s.id
                                 WHERE g.code LIKE @search OR g.short_name LIKE @search OR g.name LIKE @search
                                 ORDER BY g.code";

            using (var connection = GetConnection(connectionString))
            using (var command = new MySqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@search", "%" + searchText + "%");
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(new Group
                        {
                            Id = reader.GetInt32(0),
                            Code = reader.GetString(1),
                            ShortName = GetStringOrNull(reader, 2),
                            Name = GetStringOrNull(reader, 3),
                            SpecialtyId = reader.GetInt32(4),
                            IsActive = reader.GetBoolean(5),
                            SpecialtyName = GetStringOrNull(reader, 6)
                        });
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Загружает таблицу маппинга специальностей (переходы между специальностями)
        /// Метод заглушка, т.к. таблица specialty_transitions отсутствует в вашей БД
        /// </summary>
        public void DataSpecialtyMapping(string connectionString)
        {
            SpecialtyMappingList.Clear();
            // Таблица specialty_transitions отсутствует в БД
        }

        /// <summary>
        /// Загружает исторические алиасы специальностей (таблица specialty_aliases)
        /// Метод заглушка, т.к. таблица specialty_aliases отсутствует в вашей БД
        /// </summary>
        public void DataSpecialtyAliases(string connectionString)
        {
            SpecialtyAliasList.Clear();
            // Таблица specialty_aliases отсутствует в БД
        }

        /// <summary>
        /// Загружает переходы между специальностями (таблица specialty_transitions)
        /// Метод заглушка, т.к. таблица specialty_transitions отсутствует в вашей БД
        /// </summary>
        public void DataSpecialtyTransitions(string connectionString)
        {
            SpecialtyTransitionList.Clear();
            // Таблица specialty_transitions отсутствует в БД
        }

        // ============================================================================
        // МЕТОД ЗАГРУЗКИ ОСНОВНЫХ ДАННЫХ (ЛЮДИ)
        // ============================================================================

        /// <summary>
        /// Загружает список людей с подключением справочников
        /// </summary>
        public void DataPeople(string connectionString)
        {
            PeopleVMList.Clear();

            const string sql = @"
SELECT
    p.id, p.full_name, p.role, p.gender, p.nationality, p.birth_year, p.birth_place, p.address, p.source,
    ar.group_id, ar.specialty_id, ar.education_id, ar.graduation_year, ar.diploma_date,
    sp.social_origin_id, sp.social_status_id, sp.party_id,
    cr.work_after,
    g.code as group_code,
    s.name as specialty_name,
    edu.name as education_name,
    orig.name as social_origin_name,
    stat.name as social_status_name,
    party.name as party_name
FROM persons p
LEFT JOIN academic_records ar ON p.id = ar.person_id
LEFT JOIN social_profiles sp ON p.id = sp.person_id
LEFT JOIN career_records cr ON p.id = cr.person_id
LEFT JOIN `groups` g ON ar.group_id = g.id
LEFT JOIN specialties s ON ar.specialty_id = s.id
LEFT JOIN ref_education edu ON ar.education_id = edu.id
LEFT JOIN ref_social_origin orig ON sp.social_origin_id = orig.id
LEFT JOIN ref_social_status stat ON sp.social_status_id = stat.id
LEFT JOIN ref_party party ON sp.party_id = party.id
ORDER BY p.full_name";

            using (var connection = GetConnection(connectionString))
            using (var command = new MySqlCommand(sql, connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    PeopleVMList.Add(new PersonViewModel
                    {
                        Id = reader.GetInt32(0),
                        FullName = reader.GetString(1),
                        Role = GetStringOrNull(reader, 2),
                        Gender = GetStringOrNull(reader, 3),
                        Nationality = GetStringOrNull(reader, 4),
                        BirthYear = GetIntOrNull(reader, 5),
                        BirthPlace = GetStringOrNull(reader, 6),
                        Address = GetStringOrNull(reader, 7),
                        Source = GetStringOrNull(reader, 8),

                        GroupId = GetIntOrNull(reader, 9),
                        SpecialtyId = GetIntOrNull(reader, 10),
                        EducationId = GetIntOrNull(reader, 11),
                        GraduationYear = GetIntOrNull(reader, 12),
                        DiplomaDate = GetDateTimeOrNull(reader, 13),

                        SocialOriginId = GetIntOrNull(reader, 14),
                        SocialStatusId = GetIntOrNull(reader, 15),
                        PartyId = GetIntOrNull(reader, 16),

                        WorkAfter = GetStringOrNull(reader, 17),

                        GroupName = GetStringOrNull(reader, 18),
                        SpecialtyName = GetStringOrNull(reader, 19),
                        EducationName = GetStringOrNull(reader, 20),
                        SocialOriginName = GetStringOrNull(reader, 21),
                        SocialStatusName = GetStringOrNull(reader, 22),
                        PartyName = GetStringOrNull(reader, 23)
                    });
                }
            }
        }

        // ============================================================================
        // ДОПОЛНИТЕЛЬНЫЕ ПОЛЕЗНЫЕ МЕТОДЫ
        // ============================================================================

        /// <summary>
        /// Загружает все справочники одним вызовом
        /// </summary>
        public void LoadAllDictionaries(string connectionString)
        {
            DataEducation(connectionString);
            DataSocialOrigin(connectionString);
            DataSocialStatus(connectionString);
            DataParty(connectionString);
            DataSpecialties(connectionString);
            DataGroups(connectionString);
            DataSpecialtyGroups(connectionString);
            // Методы-заглушки для отсутствующих таблиц:
            // DataSpecialtyMapping(connectionString);
            // DataSpecialtyAliases(connectionString);
            // DataSpecialtyTransitions(connectionString);
        }

        /// <summary>
        /// Получает название образования по ID
        /// </summary>
        public static string GetEducationNameById(int? id)
        {
            if (!id.HasValue) return null;
            var item = EducationList.Find(e => e.Id == id.Value);
            return item?.Name;
        }

        /// <summary>
        /// Получает название партийности по ID
        /// </summary>
        public static string GetPartyNameById(int? id)
        {
            if (!id.HasValue) return null;
            var item = PartyList.Find(p => p.Id == id.Value);
            return item?.Name;
        }

        /// <summary>
        /// Получает название специальности по ID
        /// </summary>
        public static string GetSpecialtyNameById(int? id)
        {
            if (!id.HasValue) return null;
            var item = SpecialtyList.Find(s => s.Id == id.Value);
            return item?.Name;
        }

        /// <summary>
        /// Возвращает список специальностей
        /// </summary>
        public List<Specialty> GetSpecialties()
        {
            return SpecialtyList;
        }

        /// <summary>
        /// Возвращает список образований
        /// </summary>
        public List<Education> GetEducations()
        {
            return EducationList;
        }

        /// <summary>
        /// Возвращает список социальных происхождений
        /// </summary>
        public List<SocialOrigin> GetSocialOrigins()
        {
            return SocialOriginList;
        }

        /// <summary>
        /// Возвращает список социальных статусов
        /// </summary>
        public List<SocialStatus> GetSocialStatuses()
        {
            return SocialStatusList;
        }

        /// <summary>
        /// Возвращает список партийностей
        /// </summary>
        public List<Party> GetParties()
        {
            return PartyList;
        }

        /// <summary>
        /// Возвращает список людей
        /// </summary>
        public List<PersonViewModel> GetPeople()
        {
            return PeopleVMList;
        }

        /// <summary>
        /// Возвращает список документов об образовании
        /// </summary>
        public List<EducationDocument> GetEducationDocuments()
        {
            return EducationDocumentsList;
        }

        /// <summary>
        /// Загружает документы об образовании из таблицы education_documents
        /// </summary>
        public void DataEducationDocuments(string connectionString)
        {
            EducationDocumentsList.Clear();

            const string sql = @"
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

            using (var connection = GetConnection(connectionString))
            using (var command = new MySqlCommand(sql, connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    EducationDocumentsList.Add(new EducationDocument
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
                    });
                }
            }
        }

        /// <summary>
        /// Очищает все загруженные данные (для перезагрузки)
        /// </summary>
        public static void ClearAll()
        {
            EducationList.Clear();
            SocialOriginList.Clear();
            SocialStatusList.Clear();
            PartyList.Clear();
            SpecialtyList.Clear();
            GroupList.Clear();
            SpecialtyMappingList.Clear();
            PeopleVMList.Clear();
            EducationDocumentsList.Clear();
        }
    }
}