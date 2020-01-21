using log4net;
using OpcLabs.EasyOpc.DataAccess;
using OpcLabs.EasyOpc.DataAccess.OperationModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;

namespace Articuno
{
    /// <summary>
    /// The turbine Mediator is a singleton class that acts also acts as a mediator and it is not only responsible 
    /// for creating the numerous turbines at the site, but also acts as a middle man between the turbine class and the rest of hte program
    /// to provide turbine information (temperature, wind speed, rotor speed, operating state, etc.)
    /// It takes in a list of turbine prefixes and then creates a turbine class based  on the prefix
    /// </summary>

    /*
     * Turbine Mediator class is used to create turbines and provides a layer of abstraction and provide turbine related information upon request.
     * 
     * The reason why this doesn't use an interface is that there is only one type of turbine, but multiple
     * of the same turbine is needed to be created.
     * 
     * Furthermore, this class also provides the list of turbines it creates along
     * with providing several methods to get the turbine's OPC tags (via getXXX functions) and values (via readXXX functions).
     * Both the above returns a list
     * 
     */
    sealed internal class TurbineMediator
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(TurbineMediator));

        private static List<Turbine> turbineList;
        private List<string> turbinePrefixList;

        private string opcServerName;
        private string sitePrefix;

        private RotorSpeedFilter filterTable;

        private readonly string TURBINE_FIND_TURBINEID = "SELECT TurbineId FROM TurbineInputTags;";
        private readonly string TURBINE_INPUT_COLUMN_QUERY = "SELECT * from TurbineInputTags WHERE TurbineId='{0}'";
        private readonly string TURBINE_OUTPUT_COLUMN_QUERY = "SELECT * from TurbineOutputTags WHERE TurbineId='{0}'";
        private readonly string SCALING_FACTOR_QUERY = "SELECT * from SystemInputTags where Description='ScalingFactor';";
        private readonly string TURBINE_STARTUP_TIME = "SELECT * from SystemInputTags where Description='TurbineStartupTime';";

        private static string uccActiveTag;
        private DatabaseInterface dbi;

        /// <summary>
        /// Create the turbines from the database.
        /// </summary>
        public void createTurbines()
        {
            log.Info("Creating turbine lists");
            turbineList = new List<Turbine>();
            turbineList.Clear();
            dbi = DatabaseInterface.Instance;
            createPrefixList();

            foreach (string turbinePrefix in turbinePrefixList) { CreateTurbineInstance(turbinePrefix); }
        }

        public void createPrefixList()
        {
            DataTable reader = DatabaseInterface.Instance.readQuery(TURBINE_FIND_TURBINEID);
            foreach (DataRow item in reader.Rows) { turbinePrefixList.Add(item["TurbineId"].ToString()); }
        }

        public static TurbineMediator Instance { get { return Nested.instance; } }

        /// <summary>
        /// Returns a bool to see if a Turbine is paused by Articuno or not.
        /// </summary>
        /// <param name="turbineId"></param>
        /// <returns></returns>
        public bool isTurbinePausedByArticuno(String turbineId) { return Convert.ToBoolean(GetTurbinePrefixFromMediator(turbineId).readStoppedByArticunoAlarmValue()); }

        /// <summary>
        /// Command to start a turbine given a turbineId 
        /// </summary>
        /// <param name="turbineId">A turbine prefix</param>
        public void startTurbineFromTurbineMediator(string turbineId)
        {
            log.DebugFormat("Attempting to start turbine {0} from TurbineMeidator", turbineId);
            GetTurbinePrefixFromMediator(turbineId).startTurbine();
        }

        /// <summary>
        /// Returns the list of turbines for the site
        /// </summary>
        /// <returns></returns>
        public List<Turbine> getTurbineList() { return turbineList; }


        //Get methods to get the OPC Tag given a turbine Id. Mainly used for test methods
        public string getTurbineWindSpeedTag(string turbineId) { return GetTurbinePrefixFromMediator(turbineId).WindSpeedTag; }
        public string getRotorSpeedTag(string turbineId) { return GetTurbinePrefixFromMediator(turbineId).RotorSpeedTag; }
        public string getOperatingStateTag(string turbineId) { return GetTurbinePrefixFromMediator(turbineId).OperatingStateTag; }
        public string getNrsStateTag(string turbineId) { return GetTurbinePrefixFromMediator(turbineId).NrsStateTag; }
        public string getTemperatureTag(string turbineId) { return GetTurbinePrefixFromMediator(turbineId).TemperatureTag; }
        public string getLoadShutdownTag(string turbineId) { return GetTurbinePrefixFromMediator(turbineId).LoadShutdownTag; }
        public string getStartCommandTag (string turbineId) { return GetTurbinePrefixFromMediator(turbineId).StartCommandTag; }
        public string getParticipationState(string turbineId) { return GetTurbinePrefixFromMediator(turbineId).ParticipationTag; }
        public string getLowRotorSpeedFlag(string turbineId) { return GetTurbinePrefixFromMediator(turbineId).LowRotorSpeedFlagTag; }
        public string getCtrRemaining(string turbineId) { return GetTurbinePrefixFromMediator(turbineId).CtrCountdownTag; }

        public int getTurbineCtrTime(string turbineId) { return Convert.ToInt32(GetTurbinePrefixFromMediator(turbineId).TurbineDefaultCtr); }

        /// <summary>
        /// Deprecated in favor of design change. This is now in storeMinuteAverages
        /// </summary>
        /// <param name="turbineId"></param>
        /// <returns></returns>
        public Object readTurbineWindSpeedValue(string turbineId) { return GetTurbinePrefixFromMediator(turbineId).readTurbineWindSpeedValue(); }
        /// <summary>
        /// Deprecated in favor of design change. This is now in storeMinuteAverages
        /// </summary>
        /// <param name="turbineId"></param>
        /// <returns></returns>
        public Object readTurbineRotorSpeedValue(string turbineId) { return GetTurbinePrefixFromMediator(turbineId).readTurbineRotorSpeedValue(); }
        public Object readTurbineOperatingStateValue(string turbineId) { return GetTurbinePrefixFromMediator(turbineId).readTurbineOperatingStateValue(); }
        public Object readTurbineTemperatureValue(string turbineId) { return GetTurbinePrefixFromMediator(turbineId).readTurbineTemperatureValue(); }
        public Object readTurbineParticipationStatus(string turbineId) { return GetTurbinePrefixFromMediator(turbineId).isTurbineParticipating(); }

        //For writing (using turbineId). Note that the mediator really shouldn't be writing to all the availble turbine tags. If you need to test something, you need to create a turbine object 
        public void writeToTurbineNrsStateTag(string turbineId, object value) { GetTurbinePrefixFromMediator(turbineId).writeTurbineNoiseLevel(value); }
        public void writeToTurbineCtrTag(string turbineId, int value) { GetTurbinePrefixFromMediator(turbineId).writeTurbineCtrValue(value); }
        public void writeTurbineLoadShutDownCmd(string turbineId) { GetTurbinePrefixFromMediator(turbineId).writeTurbineLoadShutdownCommand(); }

        /// <summary>
        /// sets the CTR time for this turbine
        /// </summary>
        /// <param name="value"></param>
        public void setTurbineCtrTime(string turbineId, int ctrValue) { GetTurbinePrefixFromMediator(turbineId).writeTurbineCtrValue(ctrValue); }
        public int getTurbineCtrTimeRemaining(string turbineId) { return Convert.ToInt32(GetTurbinePrefixFromMediator(turbineId).readTurbineCtrTimeRemaining()); }

        /// <summary>
        /// Used for testing only. This creates a testing scenario that uses only T001 
        /// </summary>
        public void createTestTurbines()
        {
            turbinePrefixList.Clear();
            turbinePrefixList.Add("T001");
            this.opcServerName = DatabaseInterface.Instance.getOpcServerName();
            this.sitePrefix = DatabaseInterface.Instance.getSitePrefixValue();
            createTurbines();
        }

        //The following four functions are called by the main Articuno class to set an icing protocol condition given a turbine Id. Remember, the turbine should pause automatically independently of each other
        public void setTemperatureCondition(string turbineId, bool state) { log.DebugFormat("Temperature condition for {0} {1}", turbineId, state ? "met" : "not met"); GetTurbinePrefixFromMediator(turbineId).SetTemperatureCondition(state); }
        public void setOperatingStateCondition(string turbineId, bool state) { log.DebugFormat("Operating status condition for {0} {1}", turbineId, state ? "met" : "not met"); GetTurbinePrefixFromMediator(turbineId).SetOperatingStateCondition(state); }
        public void setNrsActive(string turbineId, bool state) { log.DebugFormat("NRS Condition for {0} {1}", turbineId, state ? "active" : "not active"); GetTurbinePrefixFromMediator(turbineId).TurbineNrsModeChanged(state); }
        public void setTurbinePerformanceCondition(string turbineId, bool state) { log.DebugFormat("Turbine Performance condition for {0} {1}", turbineId, state ? "met" : "not met"); GetTurbinePrefixFromMediator(turbineId).SetTurbineUnderPerformanceCondition(state); }

        /// <summary>
        /// force a check Ice condition given a turbine Id. Should only be used in testing only
        /// </summary>
        /// <param name="turbineId"></param>
        public void checkIcingConditions(string turbineId)
        {
            Turbine turbine = GetTurbinePrefixFromMediator(turbineId);
            turbine.CheckArticunoPausingConditions();
        }

        /// <summary>
        /// This method takes a turbine id and a tag name and then returns a TurbineEnum object Only used by the main Articuno class and nothing else. Returns a NULL if a tag is not found
        /// </summary>
        /// <param name="turbineId"></param>
        /// <param name="tag"></param>
        /// <returns>A Turbine enum if a match is found. Null otherwise. </returns>
        public Enum findTurbineTag(string turbineId, string tag)
        {
            Turbine tempTurbine = GetTurbinePrefixFromMediator(turbineId);
            if (tag.ToUpper().Equals(tempTurbine.NrsStateTag.ToUpper())) { return TurbineEnum.NrsMode; }
            else if (tag.ToUpper().Equals(tempTurbine.OperatingStateTag.ToUpper())) { return TurbineEnum.OperatingState; }
            else if (tag.ToUpper().Equals(tempTurbine.RotorSpeedTag.ToUpper())) { return TurbineEnum.RotorSpeed; }
            else if (tag.ToUpper().Equals(tempTurbine.TemperatureTag.ToUpper())) { return TurbineEnum.Temperature; }
            else if (tag.ToUpper().Equals(tempTurbine.WindSpeedTag.ToUpper())) { return TurbineEnum.WindSpeed; }
            else if (tag.ToUpper().Equals(tempTurbine.ParticipationTag.ToUpper())) { return TurbineEnum.Participation; }
            else if (tag.ToUpper().Equals(tempTurbine.StartCommandTag.ToUpper())) { return TurbineEnum.TurbineStarted; }
            else return null;
        }

        public  bool isNrsTagEmpty(string turbineId)
        {
            string nrsTag= dbi.getTurbineNrsModeTag(turbineId);
            if (nrsTag == null || nrsTag.Equals(""))
                return true;
            else
                return false;
        }

        /// <summary>
        /// Calculates the average CTR wind speed and then searches the filter table using CTR average. Paues the turbine if conditions were met. 
        /// Doesn't pause otherwise
        /// </summary>
        /// <param name="turbineId">turbine prefix in string</param>
        public void RotorSpeedCheck(string turbineId)
        {
            Turbine turbine = GetTurbinePrefixFromMediator(turbineId);
            Queue<double> windSpeedQueue = turbine.getWindSpeedQueue();
            Queue<double> rotorSpeedQueue = turbine.getRotorSpeedQueue();

            bool nrsMode = turbine.IsTurbineNrsInActiveMode();

            var windSpeedQueueCount = windSpeedQueue.Count;
            var windSpeedAverage = 0.0;
            var rotorSpeedQueueCount = rotorSpeedQueue.Count;
            var rotorSpeedAverage = 0.0;

            //Loop through the queue until the queue is empty and then divide it by the queue count stored earlier
            while (windSpeedQueue.Count != 0) { windSpeedAverage += windSpeedQueue.Dequeue(); }
            while (rotorSpeedQueue.Count != 0) { rotorSpeedAverage += rotorSpeedQueue.Dequeue(); }

            var filterTuple = filterTable.FindRotorSpeedAndStdDev(windSpeedAverage / windSpeedQueueCount, nrsMode);

            var referenceRotorSpeed = filterTuple.Item1;
            var referenceStdDev = filterTuple.Item2;

            var currentScalingFactor = Convert.ToDouble(turbine.readTurbineScalingFactorValue());

            //Set under performance condition to be true. Else, clear it
            if ((rotorSpeedAverage / rotorSpeedQueueCount) < referenceRotorSpeed - (currentScalingFactor * referenceStdDev)) { turbine.SetTurbineUnderPerformanceCondition(true); }
            else { turbine.SetTurbineUnderPerformanceCondition(false); }

            turbine.emptyQueue();
        }

        /// <summary>
        /// Function to tell the turbines to write current temperature and rotor speed values into their queues 
        /// </summary>
        /// <param name="turbineId"></param>
        public void storeMinuteAverages(string turbineId)
        {
            Turbine turbine = GetTurbinePrefixFromMediator(turbineId);
            double windSpeedAvg = Convert.ToDouble(turbine.readTurbineWindSpeedValue());
            double rotorSpeedAvg = Convert.ToDouble(turbine.readTurbineRotorSpeedValue());
            turbine.addWindSpeedToQueue(windSpeedAvg);
            turbine.addRotorSpeedToQueue(rotorSpeedAvg);
        }

        /// <summary>
        /// Write CTR Time for all the turbines. 
        /// </summary>
        /// <param name="articunoCtrTime"></param>
        public void writeCtrTime(int articunoCtrTime)
        {
            foreach (string turbinePrefix in getTurbinePrefixList()) { writeToTurbineCtrTag(turbinePrefix, articunoCtrTime); }
        }

        public void decrementTurbineCtrTime()
        {
            foreach (string turbinePrefix in getTurbinePrefixList()) { GetTurbinePrefixFromMediator(turbinePrefix).decrementTurbineCtrTime(); }
        }


        /// <summary>
        /// This function is used to inform all Turbines to check if their mapped met tower is frozen up
        /// </summary>
        /// <param name="metId">A met tower prefix</param>
        public void checkMetTowerFrozen(string metId)
        {
            foreach (string turbinePrefix in getTurbinePrefixList())
            {
                string temp = GetTurbinePrefixFromMediator(turbinePrefix).MainMetTowerReference;
                string metPrefix = MetTowerMediator.Instance.isMetTowerSwitched(temp);
                bool isMetFrozen = MetTowerMediator.Instance.IsMetTowerFrozen(metPrefix);

                if (metId.Equals(metPrefix))
                    setTemperatureCondition(turbinePrefix, isMetFrozen);
            }
        }

        public List<string> getTurbinePrefixList() { return this.turbinePrefixList; }

        /// <summary>
        /// Method to signal the Articuno Main method that the turbines have paused by the program or cleared by the site
        /// </summary>
        public void UpdateArticunoMain(TurbineEnum status, string turbineId)
        {
            if (status.Equals(TurbineEnum.PausedByArticuno))
                Articuno.turbinePausedByArticuno(turbineId);
            else
                Articuno.turbineClearedOfIce(turbineId);
        }

        public void ResetCtrValueForTurbine(string turbineId)
        {
            GetTurbinePrefixFromMediator(turbineId).resetTurbineCtrTime();
            GetTurbinePrefixFromMediator(turbineId).emptyQueue();
        }

        public bool IsTurbinePausedByArticuno(string turbinePrefix) { return Articuno.isAlreadyPaused(turbinePrefix); }

        public bool IsUCCActive() { return Convert.ToBoolean(OpcServer.isActiveUCC(opcServerName, uccActiveTag)); }

        /// <summary>
        /// Method to get a turbine object given a turbine prefix (ie T001)
        /// </summary>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public static Turbine GetTurbinePrefixFromMediator(string prefix)
        {
            foreach (Turbine turbine in turbineList) { if (turbine.GetTurbinePrefixValue().Equals(prefix)) { return turbine; } }
            return null;
        }

        public enum TurbineEnum
        {
            NrsMode,
            OperatingState,
            RotorSpeed,
            Temperature,
            WindSpeed,
            Participation,
            PausedByArticuno,
            TurbineStarted,
            ClearBySite
        }
        /// <summary>
        /// Creates a turbine instance, and iniitializes all tags from the database
        /// </summary>
        /// <param name="turbinePrefix"></param>
        private void CreateTurbineInstance(string turbinePrefix)
        {
            Turbine turbine = new Turbine(turbinePrefix, opcServerName);
            //For Turbine tags from the  TurbineInputTags Table
            string cmd = String.Format(TURBINE_INPUT_COLUMN_QUERY, turbinePrefix);
            DataTable reader = dbi.readQuery(cmd);
            turbine.ScalingFactorValue = GetTurbineScalingFactor();
            turbine.StartupTime = GetTurbineStartUpTime();

            SetTurbineNrsTag(turbine, reader);
            turbine.OperatingStateTag = sitePrefix + reader.Rows[0]["OperatingState"].ToString();
            turbine.RotorSpeedTag = sitePrefix + reader.Rows[0]["RotorSpeed"].ToString();
            turbine.TemperatureTag = sitePrefix + reader.Rows[0]["Temperature"].ToString();
            turbine.WindSpeedTag = sitePrefix + reader.Rows[0]["WindSpeed"].ToString();
            turbine.ParticipationTag = sitePrefix + reader.Rows[0]["Participation"].ToString();
            turbine.LoadShutdownTag = sitePrefix + reader.Rows[0]["Pause"].ToString();
            turbine.StartCommandTag = sitePrefix + reader.Rows[0]["Start"].ToString();
            //Do not include the site prefix for this column. 
            turbine.MainMetTowerReference = reader.Rows[0]["MetReference"].ToString();

            SetMetTowerRedundancyForTurbine(turbine, reader);
            InitializeTurbineOutputTags(turbine);

            turbineList.Add(turbine);
        }

        private string GetTurbineScalingFactor()
        {
            //Get the Scaling Factor from the system input tag table
            string cmd = String.Format(SCALING_FACTOR_QUERY);
            DataTable reader = dbi.readQuery(cmd);
            return reader.Rows[0]["DefaultValue"].ToString();
        }

        private string GetTurbineStartUpTime()
        {
            string cmd = String.Format(TURBINE_STARTUP_TIME);
            DataTable reader = dbi.readQuery(cmd);
            return reader.Rows[0]["DefaultValue"].ToString();
        }

        private void SetTurbineNrsTag(Turbine turbine, DataTable reader)
        {
            //Note that NRS can be empty, so that's why there is a try/catch here. If it is empty or null, just set it to an empty string
            string noiseLevelTag = "";
            try
            {
                noiseLevelTag = sitePrefix + reader.Rows[0]["NrsMode"].ToString();
                if (noiseLevelTag.Equals(""))
                {
                    turbine.NrsStateTag = "";
                    turbine.TurbineNrsModeChanged(false);
                }
                else
                    turbine.NrsStateTag = noiseLevelTag;
            }
            catch (NullReferenceException)
            {
                turbine.NrsStateTag = "";
                turbine.TurbineNrsModeChanged(false);
            }
        }

        private bool DoesTurbineNrsTagExist(string nrsTag,DataTable reader)
        {
            string noiseLevelTag = sitePrefix + reader.Rows[0]["NrsMode"].ToString();
            return noiseLevelTag.Equals("") ? false : true;
        }
        private void SetMetTowerRedundancyForTurbine(Turbine turbine, DataTable reader)
        {
            //If the RedundancyForMet field is not empty, that means the turbine's  temperature sensor is used as a backup in case a met tower fails. 
            //No operation if field doesn't exist
            try
            {
                if (reader.Rows[0]["RedundancyForMet"].ToString() != null)
                {
                    //Do not include the site prefix for this column
                    string backupMetTower = reader.Rows[0]["RedundancyForMet"].ToString();
                    MetTowerMediator.Instance.SetTurbineBackupForMet(backupMetTower, turbine);
                }
            }
            catch (Exception) { }
        }

        private void InitializeTurbineOutputTags(Turbine turbine)
        {
            //For Turbine tags from the TurbineOutputTags Table There might be duplicates
            string cmd = String.Format(TURBINE_OUTPUT_COLUMN_QUERY, turbine.GetTurbinePrefixValue());
            DataTable reader = dbi.readQuery(cmd);
            turbine.StoppedAlarmTag = sitePrefix + reader.Rows[0]["Alarm"].ToString();
            turbine.AgcBlockingTag = sitePrefix + reader.Rows[0]["AGCBlocking"].ToString();
            turbine.LowRotorSpeedFlagTag = sitePrefix + reader.Rows[0]["LowRotorSpeedFlag"].ToString();
            turbine.CtrCountdownTag = sitePrefix + reader.Rows[0]["CTRCountdown"].ToString();
        }
        /// <summary>
        /// constructor for the TurbineMediator class. 
        /// </summary>
        private TurbineMediator()
        {
            turbinePrefixList = new List<string>();
            turbineList = new List<Turbine>();

            uccActiveTag = DatabaseInterface.Instance.getActiveUccOpcTag();
            this.opcServerName = DatabaseInterface.Instance.getOpcServerName();
            this.sitePrefix = DatabaseInterface.Instance.getSitePrefixValue();

            filterTable = new RotorSpeedFilter();
        }

        private class Nested
        {
            static Nested() { }
            internal static readonly TurbineMediator instance = new TurbineMediator();
        }
    }
}
