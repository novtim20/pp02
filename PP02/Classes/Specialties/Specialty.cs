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
        public bool IsCurrent { get; set; } // True = новая, False = старая
    }
}
