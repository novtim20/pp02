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
        /// Загружает справочник "Специальности"
        /// </summary>
        public void DataSpecialties(string connectionString)
        {
            SpecialtyList.Clear();

            const string sql = "SELECT id, name, is_current FROM specialties ORDER BY name";

            using (var connection = GetConnection(connectionString))
            using (var command = new MySqlCommand(sql, connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    SpecialtyList.Add(new Specialty
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        IsCurrent = reader.GetBoolean(2)
                    });
                }
            }
        }

        /// <summary>
        /// Загружает таблицу маппинга специальностей
        /// </summary>
        public void DataSpecialtyMapping(string connectionString)
        {
            SpecialtyMappingList.Clear();

            const string sql = "SELECT id, old_specialty_id, new_specialty_id FROM specialty_mapping";

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

            // 🔹 Порядок колонок в запросе КРИТИЧЕСКИ ВАЖЕН — соответствует индексам в reader
            const string sql = @"
SELECT
    -- Блок 1: Основные данные человека (индексы 0-17)
    p.id,                      -- 0
    p.full_name,               -- 1
    p.role,                    -- 2
    p.specialty_id,            -- 3
    p.education_id,            -- 4
    p.social_origin_id,        -- 5
    p.social_status_id,        -- 6
    p.party_id,                -- 7
    p.graduation_year,         -- 8
    p.group_name,              -- 9
    p.gender,                  -- 10
    p.nationality,             -- 11
    p.birth_year,              -- 12
    p.birth_place,             -- 13
    p.address,                 -- 14
    p.diploma_date,            -- 15
    p.work_after,              -- 16
    p.source,                  -- 17

    -- Блок 2: Названия из справочников (индексы 18-23)
    edu.name,                  -- 18: EducationName
    orig.name,                 -- 19: SocialOriginName
    stat.name,                 -- 20: SocialStatusName
    party.name,                -- 21: PartyName
    spec.name,                 -- 22: SpecialtyName (как в дипломе)
    new_spec.name              -- 23: CurrentSpecialtyName (актуальная)
FROM people p
LEFT JOIN ref_education edu ON p.education_id = edu.id
LEFT JOIN ref_social_origin orig ON p.social_origin_id = orig.id
LEFT JOIN ref_social_status stat ON p.social_status_id = stat.id
LEFT JOIN ref_party party ON p.party_id = party.id
JOIN specialties spec ON p.specialty_id = spec.id
LEFT JOIN specialty_mapping map ON spec.id = map.old_specialty_id
LEFT JOIN specialties new_spec ON map.new_specialty_id = new_spec.id
ORDER BY p.full_name";

            using (var connection = GetConnection(connectionString))
            using (var command = new MySqlCommand(sql, connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    PeopleVMList.Add(new PersonViewModel
                    {
                        // === Блок 1: Основные данные (индексы 0-17) ===
                        Id = reader.GetInt32(0),
                        FullName = reader.GetString(1),
                        Role = GetStringOrNull(reader, 2),

                        // ID связей
                        SpecialtyId = reader.GetInt32(3),
                        EducationId = GetIntOrNull(reader, 4),
                        SocialOriginId = GetIntOrNull(reader, 5),
                        SocialStatusId = GetIntOrNull(reader, 6),
                        PartyId = GetIntOrNull(reader, 7),

                        // Текстовые и числовые данные
                        GraduationYear = GetIntOrNull(reader, 8),
                        GroupName = GetStringOrNull(reader, 9),
                        Gender = GetStringOrNull(reader, 10),
                        Nationality = GetStringOrNull(reader, 11),
                        BirthYear = GetIntOrNull(reader, 12),
                        BirthPlace = GetStringOrNull(reader, 13),
                        Address = GetStringOrNull(reader, 14),
                        DiplomaDate = GetDateTimeOrNull(reader, 15),
                        WorkAfter = GetStringOrNull(reader, 16),
                        Source = GetStringOrNull(reader, 17),

                        // === Блок 2: Названия справочников (индексы 18-23) ===
                        EducationName = GetStringOrNull(reader, 18),
                        SocialOriginName = GetStringOrNull(reader, 19),
                        SocialStatusName = GetStringOrNull(reader, 20),
                        PartyName = GetStringOrNull(reader, 21),
                        SpecialtyName = GetStringOrNull(reader, 22),

                        // Актуальная специальность: если есть новая — берём её, иначе старую
                        CurrentSpecialtyName = reader.IsDBNull(23)
                            ? GetStringOrNull(reader, 22)
                            : GetStringOrNull(reader, 23)
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
            SpecialtyMappingList.Clear();
            PeopleVMList.Clear();
        }
    }
}