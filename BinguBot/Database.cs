using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;

namespace BinguBot
{
    class Database
    {
        public static Database Instance = new Database();
        public Database()
        {
            string connectionString;
            SqlConnection conn;

            connectionString = @"Data Source=DESKTOP-K5TIM4N;Initial Catalog=BinguBot;User ID=samcr;Password=Fatesept9=";

            conn = new SqlConnection(connectionString);

            conn.Open();
            Console.WriteLine("");
        }

    }
}
