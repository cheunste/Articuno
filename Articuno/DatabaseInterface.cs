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
            return Convert.ToString(result.Rows[0][0]);

        }
        public int getHeartbeatIntervalValue() { return Convert.ToInt32(readQuery("SELECT DefaultValue from SystemInputTags WHERE Description='HeartbeatInterval'").Rows[0][0]); }

        public string getTemperatureThresholdTag() { return readQuery("SELECT OpcTag from SystemInputTags WHERE Description='AmbTempThreshold'").Rows[0][0].ToString(); }

        public string getArticunoEnableTag() { return readQuery("SELECT OpcTag from SystemInputTags WHERE Description='ArticunoEnable'").Rows[0][0].ToString(); }

        public string getArticunoCtrTag() { return readQuery("SELECT OpcTag from SystemInputTags WHERE Description='CTRPeriod'").Rows[0][0].ToString(); }
        public string GetDeltaThresholdTag() { return readQuery("SELECT OpcTag from SystemInputTags WHERE Description='DeltaTmpThreshold'").Rows[0][0].ToString(); }

        public string GetTurbineScalingFactorValue() { return ReadQueryFromSystemInput("ScalingFactor", "DefaultValue"); }
        public string GetTurbineStartupTime() { return ReadQueryFromSystemInput("TurbineStartupTime", "DefaultValue"); }


        public string GetArticunoIcePossibleOpcTag() { return readQuery("SELECT OpcTag from SystemOutputTags WHERE Description='IcePossible';").Rows[0][0].ToString(); }
        public string GetArticunoNumbersOfTurbinesPausedTag() { return readQuery("SELECT OpcTag from SystemOutputTags WHERE Description='NumTurbIced';").Rows[0][0].ToString(); }
        public string GetArticunoHeartbeatTag() { return readQuery("SELECT OpcTag from SystemOutputTags WHERE Description='Heartbeat';").Rows[0][0].ToString(); }

        //MetTower
        public int GetNumberOfMetTowers() { return Convert.ToInt16(readQuery("SELECT Count(*) as num FROM MetTowerInputTags;").Rows[0][0]); }
        public DataTable GetMetId() { return readQuery("SELECT MetId FROM MetTowerInputTags"); }
        public string GetMetTowerPrimTempValueTag(string metId) { return ReadQueryFromMetTowerInputTagsTable("PrimTempValueTag", metId); }
        public string GetMetTowerSecTempValueTag(string metId) { return ReadQueryFromMetTowerInputTagsTable("SecTempValueTag", metId); }
        public string GetMetTowerPrimHumidityTag(string metId) { return ReadQueryFromMetTowerInputTagsTable("PrimHumidityValueTag", metId); }
        public string GetMetTowerSwitchCommandTag(string metId) { return ReadQueryFromMetTowerInputTagsTable("Switch", metId); }
        public string GetMetHumidityOutOfRangeAlarmTag(string metId) { return ReadQueryFromMetTowerOutputTagsTable("HumidityOutOfRangeTag", metId); }
        public string GetMetNoDataAlarmTag(string metId) { return ReadQueryFromMetTowerOutputTagsTable("NoDataAlarmTag", metId); }
        public string GetMetIceIndicationAlarmTag(string metId) { return ReadQueryFromMetTowerOutputTagsTable("IceIndicationTag", metId); }
        public string GetMetHumidityBadQualityAlarmTag(string metId) { return ReadQueryFromMetTowerOutputTagsTable("HumidityBadQualityTag", metId); }
        public string GetMetBadSecondaryTempSensorAlarmTag(string metId) { return ReadQueryFromMetTowerOutputTagsTable("TempSecBadQualityTag", metId); }
        public string GetMetBadPrimaryTempSensorAlarmTag(string metId) { return ReadQueryFromMetTowerOutputTagsTable("TempPrimBadQualityTag", metId); }
        public string GetMetPrimaryTempOutOfRangeAlarmTag(string metId) { return ReadQueryFromMetTowerOutputTagsTable("TempPrimOutOfRangeTag", metId); }
        public string GetMetSecondaryTempOutOfRangeAlarmTag(string metId) { return ReadQueryFromMetTowerOutputTagsTable("TempSecOutOfRangeTag", metId); }
        public string GetMetCtrTemperatureTag(string metId) { return ReadQueryFromMetTowerOutputTagsTable("CtrTemperature", metId); }
        public string GetMetCtrDewTag(string metId) { return ReadQueryFromMetTowerOutputTagsTable("CtrDew", metId); }
        public string GetMetCtrHumidityTag(string metId) { return ReadQueryFromMetTowerOutputTagsTable("CtrHumidity", metId); }

        public string GetBackupTurbineForMet(string metId) { return ReadQueryFromMetTowerInputTagsTable("BackupTurbine", metId); }

        //Turbine
        public DataTable GetAllTurbineId() { return readQuery("SELECT TurbineId FROM TurbineInputTags;"); }
        public DataTable GetTurbineInputColumn(string turbinePrefix) { return readQuery(String.Format("SELECT * from TurbineInputTags WHERE TurbineId='{0}'", turbinePrefix)); }
        public string GetTurbineOperatingStateTag(string turbinePrefix) { return ReadQueryFromTurbineInputTagsTable("OperatingState", turbinePrefix); }
        public string GetTurbineNrsModeTag(string turbinePrefix) { return ReadQueryFromTurbineInputTagsTable("NrsMode", turbinePrefix); }
        public string GetTurbineParticiaptionTag(string turbinePrefix) { return ReadQueryFromTurbineInputTagsTable("Participation", turbinePrefix); }
        public string GetTurbineRotorSpeedTag(string turbinePrefix) { return ReadQueryFromTurbineInputTagsTable("RotorSpeed", turbinePrefix); }
        public string GetTurbineStartCommandTag(string turbinePrefix) { return ReadQueryFromTurbineInputTagsTable("Start", turbinePrefix); }
        public string GetTurbinePauseCommandTag(string turbinePrefix) { return ReadQueryFromTurbineInputTagsTable("Pause", turbinePrefix); }
        public string GetTurbineTemperatureTag(string turbinePrefix) { return ReadQueryFromTurbineInputTagsTable("Temperature", turbinePrefix); }
        public string GetTurbineWindSpeedTag(string turbinePrefix) { return ReadQueryFromTurbineInputTagsTable("WindSpeed", turbinePrefix); }
        public string GetMetTowerReference(string turbinePrefix) { return ReadQueryFromTurbineInputTagsTable("MetReference", turbinePrefix); }
        public string GetTurbineStoppedAlarmTag(string turbinePrefix) { return ReadQueryFromTurbineOutputTagsTable("Alarm", turbinePrefix); }
        public string GetTurbineAgcBlockingTag(string turbinePrefix) { return ReadQueryFromTurbineOutputTagsTable("AGCBlocking", turbinePrefix); }
        public string GetTurbineLowRotorSpeedFlagTag(string turbinePrefix) { return ReadQueryFromTurbineOutputTagsTable("LowRotorSpeedFlag", turbinePrefix); }
        public string GetTurbineCtrCountdownTag(string turbinePrefix) { return ReadQueryFromTurbineOutputTagsTable("CTRCountdown", turbinePrefix); }
        public string GetTurbineAvgRotorSpeedTag(string turbinePrefix) { return ReadQueryFromTurbineOutputTagsTable("AvgRotorSpeedTag", turbinePrefix); }
        public string GetTurbineAvgWindSpeedTag(string turbinePrefix) { return ReadQueryFromTurbineOutputTagsTable("AvgWindSpeedTag", turbinePrefix); }


        private string ReadQueryFromMetTowerInputTagsTable(string command, string metId)
        {
            return readQuery(String.Format("SELECT {0} from MetTowerInputTags where MetId='{1}';", command, metId)).Rows[0][0].ToString();
        }

        private string ReadQueryFromMetTowerOutputTagsTable(string command, string metId)
        {
            return readQuery(String.Format("SELECT {0} from MetTowerOutputTags where MetId='{1}';", command, metId)).Rows[0][0].ToString();
        }

        private string ReadQueryFromTurbineInputTagsTable(string command, string turbinePrefix)
        {
            return readQuery(String.Format("SELECT {0} from TurbineInputTags where TurbineId='{1}'", command, turbinePrefix)).Rows[0][0].ToString();
        }
        private string ReadQueryFromTurbineOutputTagsTable(string command, string turbinePrefix)
        {
            return readQuery(String.Format("SELECT {0} from TurbineOutputTags where TurbineId='{1}'", command, turbinePrefix)).Rows[0][0].ToString();
        }

        private string ReadQueryFromSystemInput(string description, string field)
        {
            return readQuery(String.Format("SELECT {0} from SystemInputTags where Description='{1}';", field, description)).Rows[0][0].ToString();
        }
    }
}
