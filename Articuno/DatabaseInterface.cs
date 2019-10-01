using log4net;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Articuno
{
    sealed internal class DatabaseInterface
    {

        private static readonly string SYSTEM_INPUT_TABLE = "SystemInputTags";

        static string dataSource = ".\\articuno.db";
        static string ConnectionString = String.Format("Data Source ={0};Version=3;", dataSource);

        //Log
        private static readonly ILog log = LogManager.GetLogger(typeof(Articuno));

        private DatabaseInterface()
        {

        }

        public static DatabaseInterface Instance { get { return Nested.instance; } }

        private class Nested
        {
            static Nested()
            {

            }
            internal static readonly DatabaseInterface instance = new DatabaseInterface();
        }

        public DataTable readCommand(string command)
        {
            List<List<object>> content = new List<List<object>>();
            List<object> sublist = new List<object>();
            DataTable dt = new DataTable();
            using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(command, connection))
                {
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        //You do things here
                        dt.Load(reader);
                    }
                }
            }
            return dt;
            //SQLiteCommand cmd = new SQLiteCommand(command, articunoDBConnection);
        }

        /// <summary>
        /// Used for update queries. Doesn't check to see if artiunoDBConnection is null or no
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public int updateCommand(string command)
        {
            using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(command, connection))
                {
                    //return cmd.ExecuteNonQuery();
                    cmd.ExecuteNonQuery();
                }
            }
            return 0;
        }

        /// <summary>
        /// Gets the name of the OPC Server
        /// </summary>
        /// <returns></returns>
        public string getOpcServer()
        {
            DataTable result = readCommand(String.Format("SELECT OpcTag from {0} WHERE Description ='OpcServerName' ", SYSTEM_INPUT_TABLE));
            return Convert.ToString(result.Rows[0]["OpcTag"]);
        }
    }
}
