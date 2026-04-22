using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PP02.Classes.Specialties
{
    /// <summary>
    /// Модель группы специальностей (таблица specialty_groups)
    /// </summary>
    public class SpecialtyGroup
    {
        public int Id { get; set; }
        public string Name { get; set; }           // Название группы специальностей
        public string Description { get; set; }    // Описание группы
        public DateTime? CreatedAt { get; set; }   // Дата создания группы
        public bool IsActive { get; set; }         // Статус активности

        // Список специальностей, принадлежащих этой группе
        public List<Specialty> Specialties { get; set; } = new List<Specialty>();

        // Для отображения в ComboBox
        public override string ToString()
        {
            return Name;
        }
    }
}