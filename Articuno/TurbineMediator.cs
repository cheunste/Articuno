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
    class TurbineMediator
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

        //Rotor Speed
        private RotorSpeedFilter filterTable;

        //Tag-Enum Dictionary
        Dictionary<TurbineEnum, string> tagEnum = new Dictionary<TurbineEnum, string>();

        /// <summary>
        /// constructor for the TurbineMediator class. 
        /// </summary>
        private TurbineMediator()
        {
            turbinePrefixList = new List<string>();
            turbineList = new List<Turbine>();
            tempList = new List<string>();
            tempObjectList = new List<Object>();
            this.opcServerName = getOpcServerName();

            //For RotorSpeed Filter Table. There should only be one instance of this. 
            filterTable = new RotorSpeedFilter();
        }

        private string getOpcServerName() { return DatabaseInterface.Instance.getOpcServer(); }

        public void createPrefixList()
        {
            DataTable reader = DatabaseInterface.Instance.readCommand("SELECT TurbineId FROM TurbineInputTags;");
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
            foreach (string turbinePrefix in turbinePrefixList)
            {
                Turbine turbine = new Turbine(turbinePrefix, opcServerName);

                //For Turbine tags from the  TurbineInputTags Table
                string cmd =
                    String.Format("SELECT * " +
                    "from TurbineInputTags WHERE TurbineId='{0}'", turbinePrefix);
                DataTable reader = dbi.readCommand(cmd);

                //Note that NRS can be empty, so that's why there is a try/catch here. If it is empty, just set it to an empty string
                //Or it can be an empty string in the database
                try { turbine.NrsStateTag = reader.Rows[0]["NrsMode"].ToString(); }
                catch (NullReferenceException e) { turbine.NrsStateTag = ""; }

                turbine.OperatingStateTag = reader.Rows[0]["OperatingState"].ToString();
                turbine.RotorSpeedTag = reader.Rows[0]["RotorSpeed"].ToString();
                turbine.TemperatureTag = reader.Rows[0]["Temperature"].ToString();
                turbine.WindSpeedTag = reader.Rows[0]["WindSpeed"].ToString();
                turbine.ParticipationTag = reader.Rows[0]["Participation"].ToString();
                turbine.ScalingFactorTag = reader.Rows[0]["ScalingFactor"].ToString();
                turbine.LoadShutdownTag = reader.Rows[0]["Pause"].ToString();
                turbine.StartCommandTag = reader.Rows[0]["Start"].ToString();

                string primMetTower = reader.Rows[0]["MetReference"].ToString();
                //turbine.setMetTower(primMetTower);
                turbine.MetTowerPrefix = primMetTower;

                try
                {
                    //If the RedundancyForMet is not empty, then that means
                    //The met tower is noted to be used as a temperature measurement source
                    //In case a met tower fails
                    if (reader.Rows[0]["RedundancyForMet"].ToString() != null)
                    {
                        string backupMetTower = reader.Rows[0]["RedundancyForMet"].ToString();
                        MetTowerMediator.Instance.setTurbineBackup(backupMetTower, turbine);
                    }
                }
                //no operation. Reaching here implies this met tower isn't set up for redundancy 
                catch (Exception e) { }

                //For Turbine tags from the TurbineOutputTags Table There might be duplicates
                cmd = String.Format("SELECT * " +
                    "from TurbineOutputTags WHERE TurbineId='{0}'", turbinePrefix);
                reader = dbi.readCommand(cmd);

                turbine.AlarmTag = reader.Rows[0]["Alarm"].ToString();
                //turbine.LoadShutdownTag = reader.Rows[0]["Pause"].ToString();
                //turbine.StartCommandTag = reader.Rows[0]["Start"].ToString();

                //Add turbine to the turbine list
                turbineList.Add(turbine);
            }
        }

        public bool isPausedByArticuno(String turbineId) { return Convert.ToBoolean(getTurbine(turbineId).readAlarmValue()); }
        /// <summary>
        /// Command to pause a turbine given a Turbine prefix. Also known as loadshutdown
        /// </summary>
        /// <param name="turbine"></param>
        public void pauseTurbine(string turbinePrefix)
        {
            foreach (Turbine turbineInList in turbineList)
            {
                if (turbineInList.getTurbinePrefixValue().Equals(turbinePrefix))
                {
                    log.DebugFormat("Attempting to pause turbine {0} from TurbineMediator", turbineInList.getTurbinePrefixValue());
                    turbineInList.writeLoadShutdownCmd();
                    updateMain(TurbineEnum.PausedByArticuno, turbinePrefix);
                }
            }
        }

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
            foreach (Turbine turbine in turbineList) { if (turbine.getTurbinePrefixValue().Equals(prefix)) { return turbine; } }
            return null;
        }

        /// <summary>
        /// Returns the list of turbines for the site
        /// </summary>
        /// <returns></returns>
        public List<Turbine> getTurbineList() { return turbineList; }


        //Get methods to get the OPC Tag given a turbine Id
        public string getTurbineWindSpeedTag(string turbineId) { return getTurbine(turbineId).WindSpeedTag; }
        public string getRotorSpeedTag(string turbineId) { return getTurbine(turbineId).RotorSpeedTag; }
        public string getOperatingStateTag(string turbineId) { return getTurbine(turbineId).OperatingStateTag; }
        public string getNrsStateTag(string turbineId) { return getTurbine(turbineId).NrsStateTag; }
        public string getTemperatureTag(string turbineId) { return getTurbine(turbineId).TemperatureTag; }
        public string getLoadShutdownTag(string turbineId) { return getTurbine(turbineId).LoadShutdownTag; }

        public int getTurbineCtrTime(string turbineId) { return Convert.ToInt32(getTurbine(turbineId).TurbineCtr); }
        public string getHumidityTag(string turbineId) { return getTurbine(turbineId).TurbineHumidityTag; }

        //For reading OPC value using turbineId
        /// <summary>
        /// Deprecated in favor of design change. This is now in storeMinuteAverages
        /// </summary>
        /// <param name="turbineId"></param>
        /// <returns></returns>
        public Object readWindSpeedValue(string turbineId) { return OpcServer.readOpcTag( opcServerName, getTurbineWindSpeedTag(turbineId)); }
        /// <summary>
        /// Deprecated in favor of design change. This is now in storeMinuteAverages
        /// </summary>
        /// <param name="turbineId"></param>
        /// <returns></returns>
        public Object readRotorSpeedValue(string turbineId) { return OpcServer.readOpcTag( opcServerName, getRotorSpeedTag(turbineId)); }
        public Object readOperatingStateValue(string turbineId) { return OpcServer.readOpcTag( opcServerName, getOperatingStateTag(turbineId)); }
        public Object readNrsStateValue(string turbineId) { return OpcServer.readOpcTag( opcServerName, getNrsStateTag(turbineId)); }
        public Object readTemperatureValue(string turbineId) { return OpcServer.readOpcTag( opcServerName, getTemperatureTag(turbineId)); }
        public Object readHumidityValue(string turbineId) { return OpcServer.readOpcTag( opcServerName, getHumidityTag(turbineId)); }

        //For writing (using turbineId). Note that the mediator really shouldn't be writing to all the availble turbine tags. If you need to test something, you need to create a turbine object 
        public void writeNrsStateTag(string turbineId, object value) { getTurbine(turbineId).writeNoiseLevel(value); }
        public void writeTurbineCtrTag(string turbineId, int value) { getTurbine(turbineId).writeTurbineCtrValue(value); }
        public void writeLoadShutDownCmd(string turbineId) { getTurbine(turbineId).writeLoadShutdownCmd(); }

        /// <summary>
        /// sets the CTR time for this turbine
        /// </summary>
        /// <param name="value"></param>
        public void setCtrTime(string turbineId, int ctrValue) { getTurbine(turbineId).writeTurbineCtrValue(ctrValue); }
        public int getCtrCountdown(string turbineId) { return getTurbine(turbineId).readCtrCurrentValue(); }

        /// <summary>
        /// Used for testing only. This creates a testing scenario that uses only T001 
        /// </summary>
        public void createTestTurbines()
        {
            turbinePrefixList.Clear();
            //turbinePrefixList.Add("T001");
            for (int i = 1; i <= 5; i++)
            {
                turbinePrefixList.Add("T00"+i.ToString());
            }
            getOpcServerName();
            createTurbines();
        }

        //These are functions called by the main Articuno class to set an icing protocol condition given a turbine. Remember, the turbine should pause automatically independently of each other
        public void setTemperatureCondition(string turbineId, bool state) { log.InfoFormat("Temperature condition for {0} {1}", turbineId, state ? "met" : "not met"); getTurbine(turbineId).setTemperatureCondition(state); }
        public void setOperatingStateCondition(string turbineId, bool state) { log.InfoFormat("Operating status condition for {0} {1}", turbineId, state ? "met" : "not met"); getTurbine(turbineId).setOperatingStateCondition(state); }
        public void setNrscondition(string turbineId, bool state) { log.InfoFormat("NRS Condition for {0} {1}", turbineId, state ? "met" : "not met"); getTurbine(turbineId).setNrsCondition(state); }
        public void setTurbinePerformanceCondition(string turbineId, bool state) { log.InfoFormat("Turbine Performance condition for {0} {1}", turbineId, state ? "met" : "not met"); getTurbine(turbineId).setTurbinePerformanceCondition(state); }
        public void setDeRateCondition(string turbineId, bool state) { log.InfoFormat("De Rate condition for {0} {1}", turbineId, state ? "met" : "not met"); getTurbine(turbineId).setDeRateCondition(state); }

        /// <summary>
        /// force a check Ice condition given a turbine Id. Should only be used in testing only
        /// </summary>
        /// <param name="turbineId"></param>
        public void checkIcingConditions(string turbineId)
        {
            Turbine turbine = getTurbine(turbineId);
            turbine.checkIcingConditions();
        }

        /*
         * I'm going to implement this really lazily as I stopped caring about optimization.
         * What this does is that given a tag, it should return a TurbineEnum to the main Articuno tag.
         */

        /// <summary>
        /// This method takes a turbine id and a tag name and then returns a TurbineEnum object Only used by the main Articuno class and nothing else 
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

            //bool nrsMode = Convert.ToBoolean(turbine.readNrsStateValue());
            bool nrsMode = Convert.ToInt16(turbine.readNrsStateValue()) >= 5 ? true : false;

            var windSpeedQueueCount = windSpeedQueue.Count;
            var windSpeedAverage = 0.0;
            var rotorSpeedQueueCount = rotorSpeedQueue.Count;
            var rotorSpeedAverage = 0.0;

            //Loop through the queue until the queue is empty and then divide it by the queue count stored earlier
            while (windSpeedQueue.Count != 0) { windSpeedAverage += windSpeedQueue.Dequeue(); }
            while (rotorSpeedQueue.Count != 0) { rotorSpeedAverage += rotorSpeedQueue.Dequeue(); }

            var filterTuple = filterTable.search(windSpeedAverage / windSpeedQueueCount, nrsMode);

            var referenceRotorSpeed = filterTuple.Item1;
            var referenceStdDev = filterTuple.Item2;

            var currentScalingFactor = Convert.ToDouble(turbine.readTurbineScalingFactorValue());

            //Set under performance condition to be true. Else, clear it
            if ((rotorSpeedAverage / rotorSpeedQueueCount) < referenceRotorSpeed - (currentScalingFactor * referenceStdDev)) { turbine.setTurbinePerformanceCondition(true); }
            else { turbine.setTurbinePerformanceCondition(false); }

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
                bool isMetFrozen = MetTowerMediator.Instance.isMetFrozen(metPrefix);

                if (metId.Equals(metPrefix) && isMetFrozen)
                    setTemperatureCondition(turbinePrefix, true);
                else
                    setTemperatureCondition(turbinePrefix, false);
            }

        }

        /// <summary>
        /// Method to signal the Articuno Main method that the turbines have paused by the program or cleared by the site
        /// </summary>
        public void updateMain(TurbineEnum status, string turbineId)
        {
            if (status.Equals(TurbineEnum.PausedByArticuno))
                ArticunoMain.turbinePausedByArticuno(turbineId);
            else
                ArticunoMain.turbineClearedOfIce(turbineId);
        }
    }
}
