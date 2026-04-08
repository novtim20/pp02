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
        public static List<SpecialtyMapping> SpecialtyMappingList { get; } = new List<SpecialtyMapping>();

        #endregion

        #region Основные данные

        public static List<PersonViewModel> PeopleVMList { get; } = new List<PersonViewModel>();

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

            const string sql = @"SELECT id, code, name, is_active, valid_from
                                 FROM specialties
                                 WHERE is_active = 1
                                 ORDER BY name";

            using (var connection = GetConnection(connectionString))
            using (var command = new MySqlCommand(sql, connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    SpecialtyList.Add(new Specialty
                    {
                        Id = reader.GetInt32(0),
                        Code = GetStringOrNull(reader, 1),
                        Name = reader.GetString(2),
                        IsActive = reader.GetBoolean(3),
                        ValidFrom = reader.IsDBNull(4) ? (DateTime?)null : reader.GetDateTime(4)
                    });
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
                                 WHERE g.is_active = 1
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
        /// Поиск групп по строке с использованием FULLTEXT индекса
        /// </summary>
        public List<Group> SearchGroups(string connectionString, string searchText)
        {
            var results = new List<Group>();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                return new List<Group>(GroupList);
            }

            // Используем FULLTEXT поиск по таблице groups
            const string sql = @"SELECT g.id, g.code, g.short_name, g.name, g.specialty_id, g.is_active, s.name as specialty_name
                                 FROM `groups` g
                                 LEFT JOIN specialties s ON g.specialty_id = s.id
                                 WHERE MATCH(g.code, g.short_name, g.name) AGAINST(@search IN BOOLEAN MODE)
                                 ORDER BY g.code";

            using (var connection = GetConnection(connectionString))
            using (var command = new MySqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@search", searchText);
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
        /// </summary>
        public void DataSpecialtyMapping(string connectionString)
        {
            SpecialtyMappingList.Clear();

            const string sql = "SELECT id, from_specialty_id, to_specialty_id FROM specialty_transitions";

            using (var connection = GetConnection(connectionString))
            using (var command = new MySqlCommand(sql, connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    SpecialtyMappingList.Add(new SpecialtyMapping
                    {
                        Id = reader.GetInt32(0),
                        OldSpecialtyId = reader.GetInt32(1),
                        NewSpecialtyId = reader.GetInt32(2)
                    });
                }
            }
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

            // 🔹 Используем представление v_student_full_info для упрощения запроса
            // Порядок колонок в запросе КРИТИЧЕСКИ ВАЖЕН — соответствует индексам в reader
            const string sql = @"
SELECT
    -- Блок 1: Основные данные человека (индексы 0-19)
    p.id,                      -- 0
    p.full_name,               -- 1
    p.role,                    -- 2
    p.specialty_id,            -- 3
    p.group_id,                -- 4: group_id (добавлено)
    p.historical_alias_id,     -- 5: historical_alias_id (добавлено)
    p.education_id,            -- 6
    p.social_origin_id,        -- 7
    p.social_status_id,        -- 8
    p.party_id,                -- 9
    p.graduation_year,         -- 10
    g.code,                    -- 11: group_code
    g.short_name,              -- 12: group_short_name (добавлено)
    p.gender,                  -- 13
    p.nationality,             -- 14
    p.birth_year,              -- 15
    p.birth_place,             -- 16
    p.address,                 -- 17
    p.diploma_date,            -- 18
    p.work_after,              -- 19
    p.source,                  -- 20

    -- Блок 2: Названия из справочников (индексы 21-27)
    edu.name,                  -- 21: EducationName
    orig.name,                 -- 22: SocialOriginName
    stat.name,                 -- 23: SocialStatusName
    party.name,                -- 24: PartyName
    COALESCE(sa.old_name, s_curr.name) AS specialty_name,  -- 25: SpecialtyName (как в дипломе)
    s_curr.name,               -- 26: CurrentSpecialtyName (актуальная)
    sa.old_code,               -- 27: HistoricalCode (исторический код)
    sa.old_name                -- 28: HistoricalName (историческое название)
FROM people p
LEFT JOIN `groups` g ON p.group_id = g.id
LEFT JOIN ref_education edu ON p.education_id = edu.id
LEFT JOIN ref_social_origin orig ON p.social_origin_id = orig.id
LEFT JOIN ref_social_status stat ON p.social_status_id = stat.id
LEFT JOIN ref_party party ON p.party_id = party.id
LEFT JOIN specialties s_curr ON p.specialty_id = s_curr.id
LEFT JOIN specialty_aliases sa ON p.historical_alias_id = sa.id
ORDER BY p.full_name";

            using (var connection = GetConnection(connectionString))
            using (var command = new MySqlCommand(sql, connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    PeopleVMList.Add(new PersonViewModel
                    {
                        // === Блок 1: Основные данные (индексы 0-20) ===
                        Id = reader.GetInt32(0),
                        FullName = reader.GetString(1),
                        Role = GetStringOrNull(reader, 2),

                        // ID связей
                        SpecialtyId = reader.GetInt32(3),
                        GroupId = GetIntOrNull(reader, 4),          // 🔹 group_id
                        HistoricalAliasId = GetIntOrNull(reader, 5), // 🔹 historical_alias_id
                        EducationId = GetIntOrNull(reader, 6),
                        SocialOriginId = GetIntOrNull(reader, 7),
                        SocialStatusId = GetIntOrNull(reader, 8),
                        PartyId = GetIntOrNull(reader, 9),

                        // Текстовые и числовые данные
                        GraduationYear = GetIntOrNull(reader, 10),
                        GroupName = GetStringOrNull(reader, 11),      // Код группы из таблицы groups
                        GroupShortName = GetStringOrNull(reader, 12), // 🔹 Краткое название группы
                        Gender = GetStringOrNull(reader, 13),
                        Nationality = GetStringOrNull(reader, 14),
                        BirthYear = GetIntOrNull(reader, 15),
                        BirthPlace = GetStringOrNull(reader, 16),
                        Address = GetStringOrNull(reader, 17),
                        DiplomaDate = GetDateTimeOrNull(reader, 18),
                        WorkAfter = GetStringOrNull(reader, 19),
                        Source = GetStringOrNull(reader, 20),

                        // === Блок 2: Названия справочников (индексы 21-28) ===
                        EducationName = GetStringOrNull(reader, 21),
                        SocialOriginName = GetStringOrNull(reader, 22),
                        SocialStatusName = GetStringOrNull(reader, 23),
                        PartyName = GetStringOrNull(reader, 24),
                        SpecialtyName = GetStringOrNull(reader, 25),

                        // Актуальная специальность: если есть новая — берём её, иначе старую
                        CurrentSpecialtyName = reader.IsDBNull(26)
                            ? GetStringOrNull(reader, 25)
                            : GetStringOrNull(reader, 26),

                        // 🔹 Исторические данные о специальности
                        HistoricalCode = GetStringOrNull(reader, 27),
                        HistoricalName = GetStringOrNull(reader, 28)
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
            DataSpecialtyMapping(connectionString);
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
        }
    }
}