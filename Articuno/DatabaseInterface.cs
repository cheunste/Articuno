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
        private static readonly string SYSTEM_OUTPUT_TABLE = "SystemOutputTags";

        static string dataSource = ".\\articuno.db";
        static string ConnectionString = String.Format("Data Source ={0};Version=3;", dataSource);

        //Log
        private static readonly ILog log = LogManager.GetLogger(typeof(DatabaseInterface));

        private DatabaseInterface() { }

        public static DatabaseInterface Instance { get { return Nested.instance; } }

        private class Nested
        {
            static Nested() { }
            internal static readonly DatabaseInterface instance = new DatabaseInterface();
        }

        public DataTable readQuery(string query)
        {
            List<List<object>> content = new List<List<object>>();
            List<object> sublist = new List<object>();
            DataTable dt = new DataTable();
            using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(query, connection))
                {
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        //You do things here
                        dt.Load(reader);
                    }
                }
            }
            return dt;
        }

        /// <summary>
        /// Used for update queries. Doesn't check to see if artiunoDBConnection is null or no
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public int updateDatabaseWithQuery(string query)
        {
            using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(query, connection))
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
        public string getOpcServerName()
        {
            DataTable result = readQuery(String.Format("SELECT DefaultValue from {0} WHERE Description ='OpcServerName' ", SYSTEM_INPUT_TABLE));
            return Convert.ToString(result.Rows[0]["DefaultValue"]);
        }

        public string getSitePrefixValue()
        {
            DataTable result = readQuery(String.Format("SELECT DefaultValue from {0} WHERE Description ='SitePrefix' ", SYSTEM_INPUT_TABLE));
            return Convert.ToString(result.Rows[0]["DefaultValue"]) + ".";
        }

        public string getActiveUccOpcTag()
        {
            DataTable result = readQuery(String.Format("SELECT OpcTag from {0} WHERE Description ='ActiveUCC' ", SYSTEM_INPUT_TABLE));
            return Convert.ToString(result.Rows[0]["OpcTag"]);
        }

        public int getSampleCountForStaleData()
        {
            DataTable result = readQuery(String.Format("SELECT DefaultValue from {0} WHERE Description ='FlatlineSamples' ", SYSTEM_INPUT_TABLE));
            return Convert.ToInt32(result.Rows[0]["DefaultValue"]);
        }

        public string getMetTowerCtrCountdownTag()
        {
            DataTable result = readQuery(String.Format("SELECT OpcTag from {0} WHERE Description ='MetTowerCtrCountdown' ", SYSTEM_OUTPUT_TABLE));
            return Convert.ToString(result.Rows[0]["OpcTag"]);

        }
        public int getHeartbeatIntervalValue() { return Convert.ToInt32(readQuery("SELECT DefaultValue from SystemInputTags WHERE Description='HeartbeatInterval'").Rows[0]["DefaultValue"]); }

        public string getTemperatureThresholdTag() { return readQuery("SELECT OpcTag from SystemInputTags WHERE Description='AmbTempThreshold'").Rows[0]["OpcTag"].ToString(); }

        public string getArticunoEnableTag() { return readQuery("SELECT OpcTag from SystemInputTags WHERE Description='ArticunoEnable'").Rows[0]["OpcTag"].ToString(); }

        public string getArticunoCtrTag() { return readQuery("SELECT OpcTag from SystemInputTags WHERE Description='CTRPeriod'").Rows[0]["OpcTag"].ToString();}
        public string GetDeltaThresholdTag() { return readQuery("SELECT OpcTag from SystemInputTags WHERE Description='DeltaTmpThreshold'").Rows[0]["OpcTag"].ToString();}

        public string GetTurbineScalingFactor() { return readQuery("SELECT * from SystemInputTags where Description='ScalingFactor';").Rows[0]["DefaultValue"].ToString(); }
        public int GetTurbineStartupTime() { return Convert.ToInt32(readQuery("SELECT * from SystemInputTags where Description='TurbineStartupTime';").Rows[0]["DefaultValue"]); }


        public string GetArticunoIcePossibleOpcTag() {  return readQuery("SELECT OpcTag from SystemOutputTags WHERE Description='IcePossible';").Rows[0]["OpcTag"].ToString(); }
        public string GetArticunoNumbersOfTurbinesPausedTag() {  return readQuery("SELECT OpcTag from SystemOutputTags WHERE Description='NumTurbIced';").Rows[0]["OpcTag"].ToString(); }
        public string GetArticunoHeartbeatTag() {  return readQuery("SELECT OpcTag from SystemOutputTags WHERE Description='Heartbeat';").Rows[0]["OpcTag"].ToString(); }
        //MetTower
        public string GetMetTowerCtrTag() { return readQuery("SELECT * from SystemInputTags where Description='MetTowerCtrCountdown';").Rows[0]["OpcTag"].ToString(); }

    }
}
