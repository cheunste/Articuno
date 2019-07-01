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
    class DatabaseInterface
    {

        private static readonly string SYSTEM_TABLE = "SystemParameters";
        private static readonly string MET_TOWER_TABLE = "MetTower";
        private static readonly string TURBINE_INPUT_TABLE = "TurbineInputTags";
        private static readonly string PERFORMANCE_FILTER_TABLE = "PerformanceTable";

        static string dataSource = ".\\articuno.db";
        static string ConnectionString = String.Format("Data Source ={0};Version=3;", dataSource);

        //Log
        private static readonly ILog log = LogManager.GetLogger(typeof(ArticunoMain));

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



        //Returns true if the Articuno sqlite database is not found 
        public Boolean databaseNotFound()
        {
            try
            {
            }
            catch (Exception e)
            {
                return true;
            }
            return false;
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

        public List<Turbine> getTurbineList()
        {
            //TODO: Implement
            DataTable result = readCommand("SELECT TurbineId from " + TURBINE_INPUT_TABLE);

            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the name of the OPC Server
        /// </summary>
        /// <returns></returns>
        public string getOpcServer()
        {
            //TODO: Implement
            DataTable result = readCommand("SELECT Description from " + SYSTEM_TABLE);
            throw new NotImplementedException();
            return "";
        }

        public string getMetTower()
        {
            //TODO: check query
            DataTable result = readCommand("SELECT * FROM" + MET_TOWER_TABLE);
            //throw new NotImplementedException();
            return "";
        }
    }
}
