using MySql.Data.MySqlClient;
using PP02.Classes.Person;
using PP02.Classes.Specialties;
using PP02.Classes.Dictionaries;
using PP02.Connect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace PP02
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        public static MainWindow init;

        MainWindow Main;
        // Образование
        Classes.Dictionaries.Education Education;
        public List<Classes.Dictionaries.Education> EducationList = new List<Classes.Dictionaries.Education>();

        // Соц. происхождение
        Classes.Dictionaries.SocialOrigin SocialOrigin;
        public List<Classes.Dictionaries.SocialOrigin> SocialOriginList = new List<Classes.Dictionaries.SocialOrigin>();

        // Соц. положение
        Classes.Dictionaries.SocialStatus SocialStatus;
        public List<Classes.Dictionaries.SocialStatus> SocialStatusList = new List<Classes.Dictionaries.SocialStatus>();

        // Партийность
        Classes.Dictionaries.Party Party;
        public List<Classes.Dictionaries.Party> PartyList = new List<Classes.Dictionaries.Party>();

        // Специальности
        Specialty Specialty;
        public List<Specialty> SpecialtyList = new List<Specialty>();

        // Маппинг специальностей (связи старая -> новая)
        SpecialtyMapping SpecialtyMapping;
        public List<SpecialtyMapping> SpecialtyMappingList = new List<SpecialtyMapping>();

        // Человек (основная сущность)
        Person Person;
        public List<Person> PeopleList = new List<Person>();

        // Человек для отображения (с подставленными названиями из справочников)
        PersonViewModel PersonVM;
        public List<PersonViewModel> PeopleVMList = new List<PersonViewModel>();
        public MainWindow()
        {
            InitializeComponent();
            init = this;
            Main = this;
            OpenPages(new Label.authorization(Main));
            ///OpenPages(new Label.search());
        }
        public void OpenPages(Page page)
        {
            frame.Navigate(page);
        }
        // ============================================================================
        // МЕТОДЫ ЗАГРУЗКИ СПРАВОЧНИКОВ
        // ============================================================================

        public void DataEducation(string connect)
        {
            EducationList.Clear();
            MySqlDataReader query = Connect.Connect.ExecuteQuery(connect);
            while (query.Read())
            {
                EducationList.Add(new Classes.Dictionaries.Education
                {
                    Id = query.GetInt32(0),
                    Name = query.GetString(1)
                });
            }
            query.Close();
        }
        ///"SELECT id, name FROM ref_education ORDER BY name"

        public void DataSocialOrigin(string connect)
        {
            SocialOriginList.Clear();
            MySqlDataReader query = Connect.Connect.ExecuteQuery(connect);
            while (query.Read())
            {
                SocialOriginList.Add(new Classes.Dictionaries.SocialOrigin
                {
                    Id = query.GetInt32(0),
                    Name = query.GetString(1)
                });
            }
            query.Close();
        }
        ///"SELECT id, name FROM ref_social_origin ORDER BY name"

        public void DataSocialStatus(string connect)
        {
            SocialStatusList.Clear();
            MySqlDataReader query = Connect.Connect.ExecuteQuery(connect);
            while (query.Read())
            {
                SocialStatusList.Add(new Classes.Dictionaries.SocialStatus
                {
                    Id = query.GetInt32(0),
                    Name = query.GetString(1)
                });
            }
            query.Close();
        }

        public void DataParty(string connect)
        {
            PartyList.Clear();
            MySqlDataReader query = Connect.Connect.ExecuteQuery(connect);
            while (query.Read())
            {
                PartyList.Add(new Classes.Dictionaries.Party
                {
                    Id = query.GetInt32(0),
                    Name = query.GetString(1)
                });
            }
            query.Close();
        }

        // ============================================================================
        // МЕТОДЫ ЗАГРУЗКИ СПЕЦИАЛЬНОСТЕЙ
        // ============================================================================

        public void DataSpecialties(string connect)
        {
            SpecialtyList.Clear();
            MySqlDataReader query = Connect.Connect.ExecuteQuery(connect);
            while (query.Read())
            {
                SpecialtyList.Add(new Specialty
                {
                    Id = query.GetInt32(0),
                    Name = query.GetString(1),
                    IsActive = query.GetBoolean(2)
                });
            }
            query.Close();
        }

        public void DataSpecialtyMapping(string connect)
        {
            SpecialtyMappingList.Clear();
            MySqlDataReader query = Connect.Connect.ExecuteQuery(connect);
            while (query.Read())
            {
                SpecialtyMappingList.Add(new SpecialtyMapping
                {
                    Id = query.GetInt32(0),
                    OldSpecialtyId = query.GetInt32(1),
                    NewSpecialtyId = query.GetInt32(2)
                });
            }
            query.Close();
        }

        // ============================================================================
        // МЕТОД ЗАГРУЗКИ ОСНОВНЫХ ДАННЫХ (ЛЮДИ)
        // ============================================================================

        public void DataPeople(string connect)
        {
            PeopleVMList.Clear();

            // Порядок колонок в запросе (важно не менять его):
            // 0-17: Основные данные человека (ID, ФИО, даты, ссылки)
            // 18-23: Названия из справочников (Education, Origin, Status, Party, Specialty, CurrentSpecialty)
            string sql = @"
        SELECT
            p.id, p.full_name, p.role, p.specialty_id, p.education_id,
            p.social_origin_id, p.social_status_id, p.party_id,
            p.graduation_year, p.group_name, p.gender, p.nationality,
            p.birth_year, p.birth_place, p.address, p.diploma_date,
            p.work_after, p.source,
            edu.name, orig.name, stat.name, party.name,
            spec.name, new_spec.name
        FROM people p
        LEFT JOIN ref_education edu ON p.education_id = edu.id
        LEFT JOIN ref_social_origin orig ON p.social_origin_id = orig.id
        LEFT JOIN ref_social_status stat ON p.social_status_id = stat.id
        LEFT JOIN ref_party party ON p.party_id = party.id
        JOIN specialties spec ON p.specialty_id = spec.id
        LEFT JOIN specialty_mapping map ON spec.id = map.old_specialty_id
        LEFT JOIN specialties new_spec ON map.new_specialty_id = new_spec.id
        ORDER BY p.full_name";

            MySqlDataReader query = Connect.Connect.ExecuteQuery(connect);

            while (query.Read())
            {
                PeopleVMList.Add(new PersonViewModel
                {
                    // --- Блок 1: Основные данные (Индексы 0-17) ---
                    Id = query.GetInt32(0),
                    FullName = query.GetString(1),
                    Role = query.GetString(2),

                    // ID связей (индексы 3-7)
                    SpecialtyId = query.GetInt32(3),
                    EducationId = query.GetInt32(4),
                    SocialOriginId = query.GetInt32(5),
                    SocialStatusId = query.GetInt32(6),
                    PartyId = query.GetInt32(7),

                    // Данные (индексы 8-17)
                    GraduationYear = query.GetInt32(8),
                    GroupName = query.GetString(9),
                    Gender = query.GetString(10),
                    Nationality = query.GetString(11),
                    BirthYear = query.GetInt32(12),
                    BirthPlace = query.GetString(13),
                    Address = query.GetString(14),
                    DiplomaDate = query.GetDateTime(15),
                    WorkAfter = query.GetString(16),
                    Source = query.GetString(17),

                    // --- Блок 2: Названия справочников (Индексы 18-23) ---
                    // Обратите внимание: индексы идут в порядке SELECT, а не в порядке свойств класса

                    EducationName = query.GetString(18),           // edu.name
                    SocialOriginName = query.GetString(19),        // orig.name
                    SocialStatusName = query.GetString(20),        // stat.name
                    PartyName = query.GetString(21),               // party.name
                    SpecialtyName = query.GetString(22),           // spec.name (как в дипломе)

                    // CurrentSpecialtyName: если есть новая (23), берем её, иначе старую (22)
                    CurrentSpecialtyName = query.IsDBNull(23) ? query.GetString(22) : query.GetString(23)
                });
            }
            query.Close();
        }

        // ============================================================================
        // МЕТОД ЗАГРУЗКИ ВСЕХ ДАННЫХ ОДНИМ ВЫЗОВОМ
        // ============================================================================

        public void LoadAllData(string connect)
        {
            DataEducation(connect);
            DataSocialOrigin(connect);
            DataSocialStatus(connect);
            DataParty(connect);
            DataSpecialties(connect);
            DataSpecialtyMapping(connect);
            DataPeople(connect);
        }
    }
}