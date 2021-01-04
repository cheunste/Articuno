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

namespace Articuno {
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
    sealed internal class TurbineMediator {
        private static List<Turbine> turbineList;
        private List<string> turbinePrefixList;

        private string opcServerName;
        private string sitePrefix;
        private double MAX_ROTOR_SPEED = 20.00;
        private double MIN_ROTOR_SPEED = 0.00;
        private double MAX_WIND_SPEED = 40.00;
        private double MIN_WIND_SPEED = 0.00;

        private RotorSpeedFilter filterTable;

        private DatabaseInterface dbi;

        /// <summary>
        /// Create the turbines from the database.
        /// </summary>
        public void createTurbines() {
            ArticunoLogger.DataLogger.Info("Creating turbine lists");
            ArticunoLogger.GeneralLogger.Info("Creating turbine lists");
            turbineList = new List<Turbine>();
            turbineList.Clear();
            dbi = DatabaseInterface.Instance;
            createPrefixList();

            foreach (string turbinePrefix in turbinePrefixList) { CreateTurbineInstance(turbinePrefix); }
        }

        public void createPrefixList() {
            DataTable reader = dbi.GetAllTurbineId();
            foreach (DataRow item in reader.Rows) { turbinePrefixList.Add(item["TurbineId"].ToString()); }
        }

        public static TurbineMediator Instance { get { return Nested.instance; } }

        /// <summary>
        /// Returns a bool to see if a Turbine is paused by Articuno or not.
        /// </summary>
        /// <param name="turbineId"></param>
        /// <returns></returns>
        public bool isTurbinePausedByArticuno(String turbineId) { return Convert.ToBoolean(GetTurbine(turbineId).readStoppedByArticunoAlarmValue()); }

        /// <summary>
        /// Command to start a turbine given a turbineId 
        /// </summary>
        /// <param name="turbineId">A turbine prefix</param>
        public void startTurbineFromTurbineMediator(string turbineId) {
            ArticunoLogger.DataLogger.Debug("Attempting to start turbine {0} from TurbineMeidator", turbineId);
            GetTurbine(turbineId).startTurbine();
        }

        /// <summary>
        /// Returns the list of turbines for the site
        /// </summary>
        /// <returns></returns>
        public List<Turbine> GetAllTurbineList() => turbineList;


        //Get methods to get the OPC Tag given a turbine Id. Mainly used for test methods
        public string getTurbineWindSpeedTag(string turbineId) { return GetTurbine(turbineId).WindSpeedTag; }
        public string getRotorSpeedTag(string turbineId) { return GetTurbine(turbineId).RotorSpeedTag; }
        public string getOperatingStateTag(string turbineId) { return GetTurbine(turbineId).OperatingStateTag; }
        public string getNrsStateTag(string turbineId) { return GetTurbine(turbineId).NrsStateTag; }
        public string getTemperatureTag(string turbineId) { return GetTurbine(turbineId).TemperatureTag; }
        public string getLoadShutdownTag(string turbineId) { return GetTurbine(turbineId).LoadShutdownTag; }
        public string getStartCommandTag(string turbineId) { return GetTurbine(turbineId).StartCommandTag; }
        public string getParticipationState(string turbineId) { return GetTurbine(turbineId).ParticipationTag; }
        public string getLowRotorSpeedFlag(string turbineId) { return GetTurbine(turbineId).LowRotorSpeedFlagTag; }
        public string getCtrRemaining(string turbineId) { return GetTurbine(turbineId).CtrCountdownTag; }

        public int getTurbineCtrTime(string turbineId) { return Convert.ToInt32(GetTurbine(turbineId).TurbineDefaultCtr); }

        /// <summary>
        /// Deprecated in favor of design change. This is now in storeMinuteAverages
        /// </summary>
        /// <param name="turbineId"></param>
        /// <returns></returns>
        public Object readTurbineWindSpeedValue(string turbineId) { return GetTurbine(turbineId).readTurbineWindSpeedValue(); }
        /// <summary>
        /// Deprecated in favor of design change. This is now in storeMinuteAverages
        /// </summary>
        /// <param name="turbineId"></param>
        /// <returns></returns>
        public Object readTurbineRotorSpeedValue(string turbineId) { return GetTurbine(turbineId).readTurbineRotorSpeedValue(); }
        public Object readTurbineOperatingStateValue(string turbineId) { return GetTurbine(turbineId).readTurbineOperatingStateValue(); }
        public Object readTurbineTemperatureValue(string turbineId) { return GetTurbine(turbineId).readTurbineTemperatureValue(); }
        public Object readTurbineParticipationStatus(string turbineId) { return GetTurbine(turbineId).isTurbineParticipating(); }

        //For writing (using turbineId). Note that the mediator really shouldn't be writing to all the availble turbine tags. If you need to test something, you need to create a turbine object 
        public void writeToTurbineNrsStateTag(string turbineId, object value) { GetTurbine(turbineId).writeTurbineNoiseLevel(value); }
        public void writeToTurbineCtrTag(string turbineId, int value) { GetTurbine(turbineId).writeTurbineCtrValue(value); }
        public void writeTurbineLoadShutDownCmd(string turbineId) { GetTurbine(turbineId).writeTurbineLoadShutdownCommand(); }

        /// <summary>
        /// sets the CTR time for this turbine
        /// </summary>
        /// <param name="value"></param>
        public void setTurbineCtrTime(string turbineId, int ctrValue) { GetTurbine(turbineId).writeTurbineCtrValue(ctrValue); }
        public int getTurbineCtrTimeRemaining(string turbineId) { return Convert.ToInt32(GetTurbine(turbineId).readTurbineCtrTimeRemaining()); }

        //The following four functions are called by the main Articuno class to set an icing protocol condition given a turbine Id. Remember, the turbine should pause automatically independently of each other
        public void setTemperatureCondition(string turbineId, bool state) { ArticunoLogger.DataLogger.Debug("Temperature condition for {0} {1}", turbineId, state ? "met" : "not met"); GetTurbine(turbineId).SetTemperatureCondition(state); }
        public void setOperatingStateCondition(string turbineId, bool state) { ArticunoLogger.DataLogger.Debug("Operating status condition for {0} {1}", turbineId, state ? "met" : "not met"); GetTurbine(turbineId).SetOperatingStateCondition(state); }
        public void setNrsActive(string turbineId, bool state) { ArticunoLogger.DataLogger.Debug("NRS Condition for {0} {1}", turbineId, state ? "active" : "not active"); GetTurbine(turbineId).TurbineNrsModeChanged(state); }
        public void setTurbinePerformanceCondition(string turbineId, bool state) { ArticunoLogger.DataLogger.Debug("Turbine Performance condition for {0} {1}", turbineId, state ? "met" : "not met"); GetTurbine(turbineId).SetTurbineUnderPerformanceCondition(state); }

        /// <summary>
        /// force a check Ice condition given a turbine Id. Should only be used in testing only
        /// </summary>
        /// <param name="turbineId"></param>
        public void checkIcingConditions(string turbineId) {
            Turbine turbine = GetTurbine(turbineId);
            turbine.CheckArticunoPausingConditions();
        }

        /// <summary>
        /// This method takes a turbine id and a tag name and then returns a TurbineEnum object Only used by the main Articuno class and nothing else. Returns a NULL if a tag is not found
        /// </summary>
        /// <param name="turbineId"></param>
        /// <param name="tag"></param>
        /// <returns>A Turbine enum if a match is found. Null otherwise. </returns>
        public Enum findTurbineTag(string turbineId, string tag) {
            Turbine tempTurbine = GetTurbine(turbineId);
            if (tag.ToUpper().Equals(tempTurbine.NrsStateTag.ToUpper())) { return TurbineEnum.NrsMode; }
            else if (tag.ToUpper().Equals(tempTurbine.OperatingStateTag.ToUpper())) { return TurbineEnum.OperatingState; }
            else if (tag.ToUpper().Equals(tempTurbine.RotorSpeedTag.ToUpper())) { return TurbineEnum.RotorSpeed; }
            else if (tag.ToUpper().Equals(tempTurbine.TemperatureTag.ToUpper())) { return TurbineEnum.Temperature; }
            else if (tag.ToUpper().Equals(tempTurbine.WindSpeedTag.ToUpper())) { return TurbineEnum.WindSpeed; }
            else if (tag.ToUpper().Equals(tempTurbine.ParticipationTag.ToUpper())) { return TurbineEnum.Participation; }
            else if (tag.ToUpper().Equals(tempTurbine.StartCommandTag.ToUpper())) { return TurbineEnum.TurbineStarted; }
            else return null;
        }

        public bool isNrsTagEmpty(string turbineId) {
            string nrsTag = dbi.GetTurbineNrsModeTag(turbineId);
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
        public void RotorSpeedCheck(string turbineId) {
            Turbine turbine = GetTurbine(turbineId);
            Queue<double> windSpeedQueue = turbine.getWindSpeedQueue();
            Queue<double> rotorSpeedQueue = turbine.getRotorSpeedQueue();

            bool nrsMode = turbine.IsTurbineNrsInActiveMode();

            //Loop through the queue until the queue is empty and then divide it by the queue count stored earlier
            var rotorSpeedAverage = Turbine.CalculateAverageRotorSpeed(rotorSpeedQueue);
            var windSpeedAverage = Turbine.CalculateAverageWindSpeed(windSpeedQueue);

            var filterTuple = filterTable.FindRotorSpeedAndStdDev(windSpeedAverage , nrsMode);

            var referenceRotorSpeed = filterTuple.Item1;
            var referenceStdDev = filterTuple.Item2;

            var currentScalingFactor = Convert.ToDouble(turbine.readTurbineScalingFactorValue());
            var calculatedRotorThreshold = referenceRotorSpeed - (currentScalingFactor * referenceStdDev);
            ArticunoLogger.DataLogger.Debug("{0} calculated wind speed average: {1}\n using referenced rotor speed: {2}" +
                            "\n using referenced stdDev {3}",
                turbineId, windSpeedAverage, referenceRotorSpeed, referenceStdDev);

            ArticunoLogger.DataLogger.Debug("{0} rotor speed threshold: {1} rotor speed average:{2} ",
                turbineId, calculatedRotorThreshold, rotorSpeedAverage);

            //Set under performance condition to be true. Else, clear it
            if (rotorSpeedAverage < calculatedRotorThreshold) {
                ArticunoLogger.DataLogger.Debug("{0} rotor performance low condition: {1}", turbineId, true);
                turbine.SetTurbineUnderPerformanceCondition(true);
            }
            else {
                ArticunoLogger.DataLogger.Debug("{0} rotor performance low condition: {1}", turbineId, false);
                turbine.SetTurbineUnderPerformanceCondition(false);
            }

            turbine.emptyQueue();
        }

        /// <summary>
        /// Function to tell the turbines to write current temperature and rotor speed values into their queues 
        /// </summary>
        /// <param name="turbineId"></param>
        public void storeMinuteAverages(string turbineId) {
            Turbine turbine = GetTurbine(turbineId);
            double windSpeedAvg = WindSpeedAverageCheck(Convert.ToDouble(turbine.readTurbineWindSpeedValue()));
            double rotorSpeedAvg = RotorSpeedQualityCheck(Convert.ToDouble(turbine.readTurbineRotorSpeedValue()));

            turbine.addWindSpeedToQueue(windSpeedAvg);
            turbine.addRotorSpeedToQueue(rotorSpeedAvg);
        }


        /// <summary>
        /// Write CTR Time for all the turbines. 
        /// </summary>
        /// <param name="articunoCtrTime"></param>
        public void writeCtrTime(int articunoCtrTime) => Parallel.ForEach(GetAllTurbineList(), t => { t.writeTurbineCtrValue(articunoCtrTime); });
        public void decrementTurbineCtrTime() => Parallel.ForEach(getTurbinePrefixList(),p => GetTurbine(p).decrementTurbineCtrTime());
        public void UpdateRotorSpeedDisplayForAllTurbine() => Parallel.ForEach(getTurbinePrefixList(), p => GetTurbine(p).updateRotorSpeedDisplay());

        /// <summary>
        /// This function is used to inform all Turbines to check if their mapped met tower is frozen up
        /// </summary>
        /// <param name="metId">A met tower prefix</param>
        public void checkMetTowerFrozen(MetTower met) {
            GetAllTurbineList().Where(t => MetTowerMediator.Instance.isMetTowerSwitched(t.MainMetTowerReference) == met.MetId)
                .ToList().ForEach(t => {
                    bool isMetFrozen = MetTowerMediator.Instance.IsMetTowerFrozen(met);
                    t.SetTemperatureCondition(isMetFrozen);
                });
        }


        public List<string> getTurbinePrefixList() => GetAllTurbineList().Select(t => t.TurbinePrefix).ToList();

        /// <summary>
        /// Method to signal the Articuno Main method that the turbines have paused by the program or cleared by the site
        /// </summary>
        public void UpdateArticunoMain(TurbineEnum status, string turbineId) {
            if (status.Equals(TurbineEnum.PausedByArticuno))
                Articuno.turbinePausedByArticuno(turbineId);
            else
                Articuno.turbineClearedOfIce(turbineId);
        }

        public void ResetCtrValueForTurbine(string turbineId) {
            GetTurbine(turbineId).resetTurbineCtrTime();
            GetTurbine(turbineId).emptyQueue();
        }

        public bool IsTurbinePausedByArticuno(string turbinePrefix) { return Articuno.isAlreadyPaused(turbinePrefix); }

        public bool isUccActive() { return Articuno.isUccActive(); }

        /// <summary>
        /// Method to get a turbine object given a turbine prefix (ie T001)
        /// </summary>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public Turbine GetTurbine(string prefix) => GetAllTurbineList().Where(t => t.TurbinePrefix.Equals(prefix)).FirstOrDefault();

        public enum TurbineEnum {
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
        private double RotorSpeedQualityCheck(double rotorSpeedValue) {
            if (rotorSpeedValue <= MIN_ROTOR_SPEED) { return MIN_ROTOR_SPEED; }
            else if (rotorSpeedValue >= MAX_ROTOR_SPEED) { return MAX_ROTOR_SPEED; }
            else { return rotorSpeedValue; }
        }

        private double WindSpeedAverageCheck(double windSpeedValue) {
            if (windSpeedValue <= MIN_WIND_SPEED) { return MIN_WIND_SPEED; }
            else if (windSpeedValue >= MAX_WIND_SPEED) { return MAX_WIND_SPEED; }
            else { return windSpeedValue; }
        }

        /// <summary>
        /// Creates a turbine instance, and iniitializes all tags from the database
        /// </summary>
        /// <param name="turbinePrefix"></param>
        private void CreateTurbineInstance(string turbinePrefix) {
            Turbine turbine = new Turbine(turbinePrefix, opcServerName);
            //For Turbine tags from the  TurbineInputTags Table

            turbine.ScalingFactorValue = dbi.GetTurbineScalingFactorValue();
            turbine.StartupTime = dbi.GetTurbineStartupTime();

            turbine.OperatingStateTag = sitePrefix + dbi.GetTurbineOperatingStateTag(turbinePrefix);
            turbine.RotorSpeedTag = sitePrefix + dbi.GetTurbineRotorSpeedTag(turbinePrefix);
            turbine.TemperatureTag = sitePrefix + dbi.GetTurbineTemperatureTag(turbinePrefix);
            turbine.WindSpeedTag = sitePrefix + dbi.GetTurbineWindSpeedTag(turbinePrefix);
            turbine.ParticipationTag = sitePrefix + dbi.GetTurbineParticiaptionTag(turbinePrefix);
            turbine.LoadShutdownTag = sitePrefix + dbi.GetTurbinePauseCommandTag(turbinePrefix);
            turbine.StartCommandTag = sitePrefix + dbi.GetTurbineStartCommandTag(turbinePrefix);
            turbine.AvgRotorSpeedTag = sitePrefix + dbi.GetTurbineAvgRotorSpeedTag(turbinePrefix);
            //Do not include the site prefix for this column. 
            turbine.MainMetTowerReference = dbi.GetMetTowerReference(turbinePrefix);

            SetTurbineNrsTag(turbine);
            InitializeTurbineOutputTags(turbine);

            turbineList.Add(turbine);
        }
        private void SetTurbineNrsTag(Turbine turbine) {
            //The NRS in the config DB can be empty or null, so that's why there is a try/catch here. If it is empty or null, just set it to an empty string
            string noiseLevelTag = "";
            try {
                noiseLevelTag = sitePrefix + dbi.GetTurbineNrsModeTag(turbine.GetTurbinePrefixValue());
                if (noiseLevelTag.Equals(sitePrefix + "")) {
                    turbine.NrsStateTag = "";
                    //turbine.TurbineNrsModeChanged(false);
                }
                else
                    turbine.NrsStateTag = noiseLevelTag;
            }
            catch (NullReferenceException) {
                turbine.NrsStateTag = "";
                //turbine.TurbineNrsModeChanged(false);
            }
        }

        private void InitializeTurbineOutputTags(Turbine turbine) {
            var turbinePrefix = turbine.GetTurbinePrefixValue();
            turbine.StoppedAlarmTag = sitePrefix + dbi.GetTurbineStoppedAlarmTag(turbinePrefix);
            turbine.AgcBlockingTag = sitePrefix + dbi.GetTurbineAgcBlockingTag(turbinePrefix);
            turbine.LowRotorSpeedFlagTag = sitePrefix + dbi.GetTurbineLowRotorSpeedFlagTag(turbinePrefix);
            turbine.CtrCountdownTag = sitePrefix + dbi.GetTurbineCtrCountdownTag(turbinePrefix);
        }
        /// <summary>
        /// constructor for the TurbineMediator class. 
        /// </summary>
        private TurbineMediator() {
            turbinePrefixList = new List<string>();
            turbineList = new List<Turbine>();

            this.opcServerName = DatabaseInterface.Instance.getOpcServerName();
            this.sitePrefix = DatabaseInterface.Instance.getSitePrefixValue();

            filterTable = new RotorSpeedFilter();
        }

        private class Nested {
            static Nested() { }
            internal static readonly TurbineMediator instance = new TurbineMediator();
        }
    }
}
