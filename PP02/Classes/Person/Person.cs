using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PP02.Classes.Person
{
    public class Person
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        // "Студент" или "Преподаватель"

        // ID связей со справочниками
        public int? GroupId { get; set; }         // ID группы из academic_records
        public int? SpecialtyId { get; set; }
        public int? EducationId { get; set; }
        public int? SocialOriginId { get; set; }
        public int? SocialStatusId { get; set; }
        public int? PartyId { get; set; }

        // Основные данные
        public int? GraduationYear { get; set; }
        public string GroupName { get; set; }     // Код группы для отображения
        public string Gender { get; set; }
        public string Nationality { get; set; }
        public int? BirthYear { get; set; }
        public string BirthPlace { get; set; }
        public string Address { get; set; }
        public DateTime? DiplomaDate { get; set; }
        public string WorkAfter { get; set; }
        public string Source { get; set; }

        // Конструктор
        public Person(int id, string fullName, string role, int? groupId, int? specialtyId, int? educationId,
            int? socialOriginId, int? socialStatusId, int? partyId, int? graduationYear,
            string groupName, string gender, string nationality, int? birthYear,
            string birthPlace, string address, DateTime? diplomaDate, string workAfter,
            string source)
        {
            Id = id;
            FullName = fullName;
            Role = role;
            GroupId = groupId;
            SpecialtyId = specialtyId;
            EducationId = educationId;
            SocialOriginId = socialOriginId;
            SocialStatusId = socialStatusId;
            PartyId = partyId;
            GraduationYear = graduationYear;
            GroupName = groupName;
            Gender = gender;
            Nationality = nationality;
            BirthYear = birthYear;
            BirthPlace = birthPlace;
            Address = address;
            DiplomaDate = diplomaDate;
            WorkAfter = workAfter;
            Source = source;
        }

        // Пустой конструктор для инициализации
        public Person()
        {
        }
    }
}