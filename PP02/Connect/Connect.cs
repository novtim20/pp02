using MySql.Data.MySqlClient;
using MySqlX.XDevAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PP02.Connect
{
    internal class Connect
    {
        public int con1 = 1;
        public string server = "127.0.0.1;";
        public string uid = "root";
        public string pwd = "root";
        public string database = "avtosalon";
        public string port = "3306";
        public static MySqlDataReader Connection(string query)
        {
            //string con = $"server={server};uid={uid};pwd={pwd};database={database};";
            //switch (con1)
            //{
            //    case 1:
            //        con += $"port={port};";
            //        break;
            //    case 2:
            //        con += "port=3306;";
            //        break;
            //    case 3:
            //        con += "port=3306;";
            //        break;
            //}
            MySqlConnection connection = new MySqlConnection("server=127.0.0.1;uid=root;pwd=root;database=vipusk;port=3306;");
            connection.Open();
            MySqlCommand cmd = new MySqlCommand(query, connection);
            return cmd.ExecuteReader();
        }


    }
}