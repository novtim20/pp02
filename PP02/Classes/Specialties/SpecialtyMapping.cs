using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PP02.Classes.Specialties
{
    public class SpecialtyMapping
    {
        public int Id { get; set; }
        public int OldSpecialtyId { get; set; }
        public int NewSpecialtyId { get; set; }

        // Навигационные свойства (для удобства)
        public Specialty OldSpecialty { get; set; }
        public Specialty NewSpecialty { get; set; }
    }
}
