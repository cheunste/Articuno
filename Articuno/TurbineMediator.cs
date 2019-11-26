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

    //Delegates
    delegate void IcingDelegates(string turbineId, bool state);

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
        //Log
        private static readonly ILog log = LogManager.GetLogger(typeof(TurbineMediator));

        //List of turbine related lists
        private static List<Turbine> turbineList;
        private List<string> turbinePrefixList;

        //These are temp lists that are used for read and get functions in the class 
        private List<Object> tempObjectList;

        //This is just an unused List that is used temporary. Used so that I don't have to instantiate an object everytime I need a list
        private List<string> tempList;

        //Instance of OpcServer. Might not be needed
        private string opcServerName;
        private string sitePrefix;

        //Rotor Speed
        private RotorSpeedFilter filterTable;

        //Tag-Enum Dictionary
        Dictionary<TurbineEnum, string> tagEnum = new Dictionary<TurbineEnum, string>();

        //SQL statement constants
        private readonly string TURBINE_FIND_TURBINEID = "SELECT TurbineId FROM TurbineInputTags;";
        private readonly string TURBINE_INPUT_COLUMN_QUERY = "SELECT * from TurbineInputTags WHERE TurbineId='{0}'";
        private readonly string TURBINE_OUTPUT_COLUMN_QUERY = "SELECT * from TurbineOutputTags WHERE TurbineId='{0}'";
        private readonly string SCALING_FACTOR_QUERY = "SELECT * from SystemInputTags where Description='ScalingFactor';";
        private readonly string TURBINE_STARTUP_TIME = "SELECT * from SystemInputTags where Description='TurbineStartupTime';";

        //Other member variables
        private static string uccActiveTag;

        /// <summary>
        /// constructor for the TurbineMediator class. 
        /// </summary>
        private TurbineMediator()
        {
            turbinePrefixList = new List<string>();
            turbineList = new List<Turbine>();
            tempList = new List<string>();
            tempObjectList = new List<Object>();
            //UCC Active Tag
            uccActiveTag = DatabaseInterface.Instance.getActiveUCCTag();
            this.opcServerName = DatabaseInterface.Instance.getOpcServer();
            this.sitePrefix = DatabaseInterface.Instance.getSitePrefix();

            //For RotorSpeed Filter Table. There should only be one instance of this. 
            filterTable = new RotorSpeedFilter();
        }


        public void createPrefixList()
        {
            DataTable reader = DatabaseInterface.Instance.readCommand(TURBINE_FIND_TURBINEID);
            foreach (DataRow item in reader.Rows) { turbinePrefixList.Add(item["TurbineId"].ToString()); }
        }

        public List<string> getTurbinePrefixList() { return this.turbinePrefixList; }

        //Lines for singleton usage
        public static TurbineMediator Instance { get { return Nested.instance; } }


        private class Nested
        {
            static Nested() { }
            internal static readonly TurbineMediator instance = new TurbineMediator();
        }


        /// <summary>
        /// Create the turbines from the database.
        /// </summary>
        public void createTurbines()
        {
            log.Info("Creating turbine lists");
            turbineList = new List<Turbine>();
            turbineList.Clear();
            DatabaseInterface dbi = DatabaseInterface.Instance;
            createPrefixList();

            //Get the Scaling Factor from the system input tag
            string cmd = String.Format(SCALING_FACTOR_QUERY);
            DataTable reader = dbi.readCommand(cmd);
            reader =dbi.readCommand(cmd);
            string scalingFactor = reader.Rows[0]["DefaultValue"].ToString();
            //Turbine startup time
            cmd = String.Format(TURBINE_STARTUP_TIME);
            reader =dbi.readCommand(cmd);
            string startupTime= reader.Rows[0]["DefaultValue"].ToString();

            foreach (string turbinePrefix in turbinePrefixList)
            {
                Turbine turbine = new Turbine(turbinePrefix, opcServerName);

                //For Turbine tags from the  TurbineInputTags Table
                cmd =
                    String.Format(TURBINE_INPUT_COLUMN_QUERY, turbinePrefix);
                reader = dbi.readCommand(cmd);

                //Note that NRS can be empty, so that's why there is a try/catch here. If it is empty, just set it to an empty string
                //Or it can be an empty string in the database
                try { turbine.NrsStateTag = sitePrefix+reader.Rows[0]["NrsMode"].ToString(); }
                catch (NullReferenceException e) {
                    turbine.NrsStateTag = "";
                    ///Because the turbine doesn't use NRS, just set this to true
                    turbine.setNrsMode(false);
                }

                turbine.OperatingStateTag = sitePrefix+reader.Rows[0]["OperatingState"].ToString();
                turbine.RotorSpeedTag = sitePrefix+reader.Rows[0]["RotorSpeed"].ToString();
                turbine.TemperatureTag = sitePrefix+reader.Rows[0]["Temperature"].ToString();
                turbine.WindSpeedTag = sitePrefix+reader.Rows[0]["WindSpeed"].ToString();
                turbine.ParticipationTag = sitePrefix+reader.Rows[0]["Participation"].ToString();
                turbine.LoadShutdownTag = sitePrefix+reader.Rows[0]["Pause"].ToString();
                turbine.StartCommandTag = sitePrefix+reader.Rows[0]["Start"].ToString();
                //For scaling factor. This does not require a prefix as a systeminput value
                turbine.ScalingFactorValue = scalingFactor;
                //For Start up time. This does not require a prefix as a systeminput value
                turbine.StartupTime = startupTime;

                //Do not include the site prefix for this column. 
                string primMetTower = reader.Rows[0]["MetReference"].ToString();
                turbine.MetTowerPrefix = primMetTower;

                try
                {
                    //If the RedundancyForMet is not empty, then that means
                    //The met tower is noted to be used as a temperature measurement source
                    //In case a met tower fails
                    if (reader.Rows[0]["RedundancyForMet"].ToString() != null)
                    {
                        //Do not include the site prefix for this column
                        string backupMetTower = reader.Rows[0]["RedundancyForMet"].ToString();
                        MetTowerMediator.Instance.SetTurbineBackupForMet(backupMetTower, turbine);
                    }
                }
                //no operation. Reaching here implies this met tower isn't set up for redundancy 
                catch (Exception e) { }

                //For Turbine tags from the TurbineOutputTags Table There might be duplicates
                cmd = String.Format(TURBINE_OUTPUT_COLUMN_QUERY, turbinePrefix);
                reader = dbi.readCommand(cmd);

                turbine.StoppedAlarmTag = sitePrefix+reader.Rows[0]["Alarm"].ToString();
                turbine.AgcBlockingTag = sitePrefix+reader.Rows[0]["AGCBlocking"].ToString();
                turbine.LowRotorSpeedFlagTag = sitePrefix+reader.Rows[0]["LowRotorSpeedFlag"].ToString();
                turbine.CtrCountdownTag = sitePrefix+reader.Rows[0]["CTRCountdown"].ToString();
                //Add turbine to the turbine list
                turbineList.Add(turbine);
            }
        }

        /// <summary>
        /// Returns a bool to see if a Turbine is paused by Articuno or not.
        /// </summary>
        /// <param name="turbineId"></param>
        /// <returns></returns>
        public bool isPausedByArticuno(String turbineId) { return Convert.ToBoolean(getTurbine(turbineId).readAlarmValue()); }
       
        /// <summary>
        /// Command to start a turbine given a turbineId 
        /// </summary>
        /// <param name="turbineId">A turbine prefix</param>
        public void startTurbine(string turbineId)
        {
            log.DebugFormat("Attempting to start turbine {0} from TurbineMeidator", turbineId);
            getTurbine(turbineId).startTurbine();
        }

        /// <summary>
        /// Method to get a turbine object given a turbine prefix (ie T001)
        /// </summary>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public static Turbine getTurbine(string prefix)
        {
            foreach (Turbine turbine in turbineList) { if (turbine.GetTurbinePrefixValue().Equals(prefix)) { return turbine; } }
            return null;
        }

        /// <summary>
        /// Returns the list of turbines for the site
        /// </summary>
        /// <returns></returns>
        public List<Turbine> getTurbineList() { return turbineList; }


        //Get methods to get the OPC Tag given a turbine Id. Mainly used for test methods
        public string getTurbineWindSpeedTag(string turbineId) { return getTurbine(turbineId).WindSpeedTag; }
        public string getRotorSpeedTag(string turbineId) { return getTurbine(turbineId).RotorSpeedTag; }
        public string getOperatingStateTag(string turbineId) { return getTurbine(turbineId).OperatingStateTag; }
        public string getNrsStateTag(string turbineId) { return getTurbine(turbineId).NrsStateTag; }
        public string getTemperatureTag(string turbineId) { return getTurbine(turbineId).TemperatureTag; }
        public string getLoadShutdownTag(string turbineId) { return getTurbine(turbineId).LoadShutdownTag; }
        public string getParticipationState(string turbineId) { return getTurbine(turbineId).ParticipationTag; }
        public string getLowRotorSpeedFlag(string turbineId) { return getTurbine(turbineId).LowRotorSpeedFlagTag; }
        public string getCtrRemaining(string turbineId) { return getTurbine(turbineId).CtrCountdownTag; }

        public int getTurbineCtrTime(string turbineId) { return Convert.ToInt32(getTurbine(turbineId).TurbineCtr); }

        //For reading OPC value using turbineId
        /// <summary>
        /// Deprecated in favor of design change. This is now in storeMinuteAverages
        /// </summary>
        /// <param name="turbineId"></param>
        /// <returns></returns>
        public Object readWindSpeedValue(string turbineId) { return getTurbine(turbineId).readWindSpeedValue(); }
        /// <summary>
        /// Deprecated in favor of design change. This is now in storeMinuteAverages
        /// </summary>
        /// <param name="turbineId"></param>
        /// <returns></returns>
        public Object readRotorSpeedValue(string turbineId) { return getTurbine(turbineId).readRotorSpeedValue(); }
        public Object readOperatingStateValue(string turbineId) { return getTurbine(turbineId).readOperatingStateValue(); }
        public Object readTemperatureValue(string turbineId) { return getTurbine(turbineId).readTemperatureValue(); }
        public Object readParticipationValue(string turbineId) { return getTurbine(turbineId).readParticipationValue(); }

        //For writing (using turbineId). Note that the mediator really shouldn't be writing to all the availble turbine tags. If you need to test something, you need to create a turbine object 
        public void writeNrsStateTag(string turbineId, object value) { getTurbine(turbineId).writeNoiseLevel(value); }
        public void writeTurbineCtrTag(string turbineId, int value) { getTurbine(turbineId).writeTurbineCtrValue(value); }
        public void writeLoadShutDownCmd(string turbineId) { getTurbine(turbineId).writeLoadShutdownCmd(); }

        /// <summary>
        /// sets the CTR time for this turbine
        /// </summary>
        /// <param name="value"></param>
        public void setCtrTime(string turbineId, int ctrValue) { getTurbine(turbineId).writeTurbineCtrValue(ctrValue); }
        public int getCtrCountdown(string turbineId) { return Convert.ToInt32(getTurbine(turbineId).readCtrCurrentValue()); }

        /// <summary>
        /// Used for testing only. This creates a testing scenario that uses only T001 
        /// </summary>
        public void createTestTurbines()
        {
            turbinePrefixList.Clear();
            turbinePrefixList.Add("T001");
            this.opcServerName = DatabaseInterface.Instance.getOpcServer();
            this.sitePrefix = DatabaseInterface.Instance.getSitePrefix();
            createTurbines();
        }

        //The following four functions are called by the main Articuno class to set an icing protocol condition given a turbine Id. Remember, the turbine should pause automatically independently of each other
        public void setTemperatureCondition(string turbineId, bool state) { log.DebugFormat("Temperature condition for {0} {1}", turbineId, state ? "met" : "not met"); getTurbine(turbineId).SetTemperatureCondition(state); }
        public void setOperatingStateCondition(string turbineId, bool state) { log.DebugFormat("Operating status condition for {0} {1}", turbineId, state ? "met" : "not met"); getTurbine(turbineId).SetOperatingStateCondition(state); }
        public void setNrsActive(string turbineId, bool state) { log.DebugFormat("NRS Condition for {0} {1}", turbineId, state ? "active" : "not active"); getTurbine(turbineId).setNrsMode(state); }
        public void setTurbinePerformanceCondition(string turbineId, bool state) { log.DebugFormat("Turbine Performance condition for {0} {1}", turbineId, state ? "met" : "not met"); getTurbine(turbineId).SetTurbineUnderPerformanceCondition(state); }

        /// <summary>
        /// force a check Ice condition given a turbine Id. Should only be used in testing only
        /// </summary>
        /// <param name="turbineId"></param>
        public void checkIcingConditions(string turbineId)
        {
            Turbine turbine = getTurbine(turbineId);
            turbine.CheckArticunoPausingConditions();
        }

        /*
         * I'm going to implement this really lazily as I can't think of a better way to do this (for the time being)
         * What this does is that given a tag, it should return a TurbineEnum that represent the OPC tag back to the main Articuno class.
         */

        /// <summary>
        /// This method takes a turbine id and a tag name and then returns a TurbineEnum object Only used by the main Articuno class and nothing else. Returns a NULL if a tag is not found
        /// </summary>
        /// <param name="turbineId"></param>
        /// <param name="tag"></param>
        /// <returns>A Turbine enum if a match is found. Null otherwise. </returns>
        public Enum findTurbineTag(string turbineId, string tag)
        {
            Turbine tempTurbine = getTurbine(turbineId);
            if (tag.ToUpper().Equals(tempTurbine.NrsStateTag.ToUpper())) { return TurbineEnum.NrsMode; }
            else if (tag.ToUpper().Equals(tempTurbine.OperatingStateTag.ToUpper())) { return TurbineEnum.OperatingState; }
            else if (tag.ToUpper().Equals(tempTurbine.RotorSpeedTag.ToUpper())) { return TurbineEnum.RotorSpeed; }
            else if (tag.ToUpper().Equals(tempTurbine.TemperatureTag.ToUpper())) { return TurbineEnum.Temperature; }
            else if (tag.ToUpper().Equals(tempTurbine.WindSpeedTag.ToUpper())) { return TurbineEnum.WindSpeed; }
            else if (tag.ToUpper().Equals(tempTurbine.ParticipationTag.ToUpper())) { return TurbineEnum.Participation; }
            else if (tag.ToUpper().Equals(tempTurbine.StartCommandTag.ToUpper())) { return TurbineEnum.TurbineStarted; }
            //If it reaches here, I have no freaking clue what's going on, but whatever is calling it needs to handle it 
            else return null;
        }


        //TurbineEnum. This is used to interact with Articuno 
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

        //FUnction to determine whether or not a turbine is underperforming due to ice
        /// <summary>
        /// Calculates the average CTR wind speed and then searches the filter table using CTR average. Paues the turbine if conditions were met. 
        /// Doesn't pause otherwise
        /// </summary>
        /// <param name="turbineId">turbine prefix in string</param>
        //Note all vars are doubles here
        public void RotorSpeedCheck(string turbineId)
        {
            Turbine turbine = getTurbine(turbineId);
            Queue<double> windSpeedQueue = turbine.getWindSpeedQueue();
            Queue<double> rotorSpeedQueue = turbine.getRotorSpeedQueue();

            bool nrsMode = turbine.isNrsActive();

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

            //For sanity check, make sure the windSPeedQueue is empty 
            turbine.emptyQueue();
        }

        /// <summary>
        /// Function to tell the turbines to write current temperature and rotor speed values into their queues 
        /// </summary>
        /// <param name="turbineId"></param>
        public void storeMinuteAverages(string turbineId)
        {
            Turbine turbine = getTurbine(turbineId);
            double windSpeedAvg = Convert.ToDouble(turbine.readWindSpeedValue());
            double rotorSpeedAvg = Convert.ToDouble(turbine.readRotorSpeedValue());
            turbine.addWindSpeedToQueue(windSpeedAvg);
            turbine.addRotorSpeedToQueue(rotorSpeedAvg);
        }

        /// <summary>
        /// Write CTR Time for all the turbines. 
        /// </summary>
        /// <param name="articunoCtrTime"></param>
        public void writeCtrTime(int articunoCtrTime)
        {
            foreach (string turbinePrefix in getTurbinePrefixList()) { writeTurbineCtrTag(turbinePrefix, articunoCtrTime); }
        }

        public void decrementTurbineCtrTime()
        {
            foreach (string turbinePrefix in getTurbinePrefixList()) { getTurbine(turbinePrefix).decrementCtrTime(); }
        }


        /// <summary>
        /// This function is used to inform all Turbines to check if their mapped met tower is frozen up
        /// </summary>
        /// <param name="metId">A met tower prefix</param>
        //Note that met tower can be switched
        public void checkMetTowerFrozen(string metId)
        {
            foreach (string turbinePrefix in getTurbinePrefixList())
            {
                string temp = getTurbine(turbinePrefix).MetTowerPrefix;
                string metPrefix = MetTowerMediator.Instance.isMetTowerSwitched(temp);
                bool isMetFrozen = MetTowerMediator.Instance.IsMetTowerFrozen(metPrefix);

                if (metId.Equals(metPrefix))
                    setTemperatureCondition(turbinePrefix, isMetFrozen);
            }

        }

        /// <summary>
        /// Method to signal the Articuno Main method that the turbines have paused by the program or cleared by the site
        /// </summary>
        public void updateMain(TurbineEnum status, string turbineId)
        {
            if (status.Equals(TurbineEnum.PausedByArticuno))
                Articuno.turbinePausedByArticuno(turbineId);
            else
                Articuno.turbineClearedOfIce(turbineId);
        }

        public void resetCtr(string turbineId)
        {
            getTurbine(turbineId).resetCtrTime();
            getTurbine(turbineId).emptyQueue();
        }

        public bool isTurbinePaused(string turbinePrefix) { return Articuno.isAlreadyPaused(turbinePrefix); }

        public bool isUCCActive()
        {
            return Convert.ToBoolean(OpcServer.isActiveUCC(opcServerName, uccActiveTag));
        }
    }
}
