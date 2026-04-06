using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PP02.Classes.Person
{
    public class PersonViewModel
    {
        // ========================================
        // ОСНОВНЫЕ ДАННЫЕ
        // ========================================
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; } // "Студент" или "Преподаватель"

        // ========================================
        // ID СВЯЗЕЙ СО СПРАВОЧНИКАМИ (для редактирования/сохранения)
        // ========================================
        public int? SpecialtyId { get; set; }
        public int? EducationId { get; set; }
        public int? SocialOriginId { get; set; }
        public int? SocialStatusId { get; set; }
        public int? PartyId { get; set; }

        // ========================================
        // НАЗВАНИЯ ИЗ СПРАВОЧНИКОВ (для отображения)
        // ========================================
        public string SpecialtyName { get; set; }          // Как в дипломе
        public string CurrentSpecialtyName { get; set; }   // Актуальное название
        public string EducationName { get; set; }
        public string SocialOriginName { get; set; }
        public string SocialStatusName { get; set; }
        public string PartyName { get; set; }

        // ========================================
        // ДОПОЛНИТЕЛЬНЫЕ ДАННЫЕ
        // ========================================
        public int? GraduationYear { get; set; }
        public string GroupName { get; set; }
        public string Gender { get; set; }
        public string Nationality { get; set; }
        public int? BirthYear { get; set; }
        public string BirthPlace { get; set; }
        public string Address { get; set; }
        public DateTime? DiplomaDate { get; set; }
        public string WorkAfter { get; set; }
        public string Source { get; set; }
        public string PhotoPath { get; set; }
        public int? DiplomaNumber { get; set; }
    }
}
