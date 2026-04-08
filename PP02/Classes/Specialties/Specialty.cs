using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PP02.Classes.Specialties
{
    public class Specialty
    {
        public int Id { get; set; }
        public string Code { get; set; }         // Актуальный код (напр. 09.02.01)
        public string Name { get; set; }
        public bool IsActive { get; set; }       // is_active из БД
        public DateTime? ValidFrom { get; set; } // Дата введения актуального кода

        // Для отображения в ComboBox
        public override string ToString()
        {
            return $"{Code} - {Name}";
        }
    }

    /// <summary>
    /// Модель группы (таблица groups)
    /// </summary>
    public class Group
    {
        public int Id { get; set; }
        public string Code { get; set; }           // Код группы: РП-01-1
        public string ShortName { get; set; }      // Сокращение: РП
        public string Name { get; set; }           // Отображаемое название
        public int SpecialtyId { get; set; }       // Ссылка на специальность
        public bool IsActive { get; set; }         // Статус активности

        // Для отображения в ComboBox
        public override string ToString()
        {
            return $"{Code} [{ShortName}]";
        }

        // Название специальности (заполняется при загрузке)
        public string SpecialtyName { get; set; }
    }
}