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

            const string sql = "SELECT id, name, active, data, group_id FROM specialties WHERE active = 1 ORDER BY name";

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
                        IsActive = reader.GetBoolean(2),
                        ValidFrom = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3),
                        GroupId = GetIntOrNull(reader, 4)
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
                        Name = GetStringOrNull(reader, 1)
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

            const string sql = "SELECT id, from_specialty_id, to_specialty_id, transition_type, effective_date FROM specialty_transitions";

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

        /// <summary>
        /// Загружает исторические алиасы специальностей (таблица specialty_aliases)
        /// </summary>
        public void DataSpecialtyAliases(string connectionString)
        {
            SpecialtyAliasList.Clear();

            const string sql = @"SELECT id, specialty_id, old_code, old_name, valid_from, valid_to
                                 FROM specialty_aliases
                                 ORDER BY old_code";

            using (var connection = GetConnection(connectionString))
            using (var command = new MySqlCommand(sql, connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    SpecialtyAliasList.Add(new SpecialtyAlias
                    {
                        Id = reader.GetInt32(0),
                        SpecialtyId = reader.GetInt32(1),
                        OldCode = GetStringOrNull(reader, 2),
                        OldName = GetStringOrNull(reader, 3),
                        ValidFrom = GetDateTimeOrNull(reader, 4),
                        ValidTo = GetDateTimeOrNull(reader, 5)
                    });
                }
            }
        }

        /// <summary>
        /// Загружает переходы между специальностями (таблица specialty_transitions)
        /// </summary>
        public void DataSpecialtyTransitions(string connectionString)
        {
            SpecialtyTransitionList.Clear();

            const string sql = @"SELECT id, from_specialty_id, to_specialty_id, transition_type, effective_date
                                 FROM specialty_transitions
                                 ORDER BY effective_date";

            using (var connection = GetConnection(connectionString))
            using (var command = new MySqlCommand(sql, connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    SpecialtyTransitionList.Add(new SpecialtyTransition
                    {
                        Id = reader.GetInt32(0),
                        FromSpecialtyId = reader.GetInt32(1),
                        ToSpecialtyId = reader.GetInt32(2),
                        TransitionType = GetStringOrNull(reader, 3),
                        EffectiveDate = GetDateTimeOrNull(reader, 4)
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
            DataSpecialtyMapping(connectionString);
            DataSpecialtyAliases(connectionString);
            DataSpecialtyTransitions(connectionString);
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