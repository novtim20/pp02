using MySql.Data.MySqlClient;
using System;
using System.Configuration;
using System.Data;

namespace PP02.Connect
{
    internal class Connect
    {
        // Параметры подключения (можно изменить при необходимости)
        public string server = "127.0.0.1";
        public string uid = "root";
        public string pwd = "root";
        public string database = "vipusk";
        public string port = "3306";

        /// <summary>
        /// Получает строку подключения из App.config
        /// </summary>
        public static string GetConnectionString()
        {
            return ConfigurationManager.ConnectionStrings["MySqlConnectionString"]?.ConnectionString
                ?? "server=127.0.0.1;uid=root;pwd=root;database=pp022;port=3306;";
        }

        /// <summary>
        /// Создаёт и возвращает новое соединение с базой данных
        /// </summary>
        public static MySqlConnection CreateConnection()
        {
            string connectionString = GetConnectionString();
            return new MySqlConnection(connectionString);
        }

        /// <summary>
        /// Выполняет запрос и возвращает MySqlDataReader
        /// Внимание: вызывающий код должен закрыть reader и соединение!
        /// </summary>
        public static MySqlDataReader ExecuteQuery(string query)
        {
            MySqlConnection connection = CreateConnection();
            connection.Open();
            MySqlCommand cmd = new MySqlCommand(query, connection);
            // Возвращаем reader с CommandBehavior.CloseConnection, чтобы при закрытии reader закрывалось и соединение
            return cmd.ExecuteReader(CommandBehavior.CloseConnection);
        }
    }
}