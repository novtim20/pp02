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
        public string Name { get; set; }
        public bool IsActive { get; set; }       // is_active из БД
        public DateTime? ValidFrom { get; set; } // Дата введения/обновления (из column 'data')
        public int? GroupId { get; set; }        // ID группы/категории специальности

        // Для совместимости с элементом "Все" в ComboBox
        public Specialty()
        {
        }

        // Для отображения в ComboBox
        public override string ToString()
        {
            return $"{Name}";
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

        // Название специальности для отображения в списке групп
        public string SpecialtyName { get; set; }

        // Для совместимости с элементом "Все" в ComboBox
        public Group()
        {
            SpecialtyName = "";
        }
    }

    /// <summary>
    /// Модель исторического алиаса специальности (таблица specialty_aliases)
    /// </summary>
    public class SpecialtyAlias
    {
        public int Id { get; set; }
        public int SpecialtyId { get; set; }       // Ссылка на основную специальность
        public string OldCode { get; set; }        // Старый код
        public string OldName { get; set; }        // Старое название
        public DateTime? ValidFrom { get; set; }   // Дата действия
        public DateTime? ValidTo { get; set; }     // Дата окончания действия

        public override string ToString()
        {
            return $"{OldCode} - {OldName} (архив)";
        }
    }

    /// <summary>
    /// Модель перехода между специальностями (таблица specialty_transitions)
    /// </summary>
    public class SpecialtyTransition
    {
        public int Id { get; set; }
        public int FromSpecialtyId { get; set; }   // От какой специальности
        public int ToSpecialtyId { get; set; }     // К какой специальности
        public string TransitionType { get; set; } // "split" или "merge"
        public DateTime? EffectiveDate { get; set; } // Дата перехода

        public override string ToString()
        {
            return $"{TransitionType}: {FromSpecialtyId} -> {ToSpecialtyId}";
        }
    }
}