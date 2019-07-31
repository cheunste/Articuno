using log4net;
using OpcLabs.EasyOpc.DataAccess;
using OpcLabs.EasyOpc.DataAccess.Generic;
using OpcLabs.EasyOpc.DataAccess.OperationModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Articuno
{
    class ArticunoMain
    {
        /*
         * These are Lists that are used to keep turbines organized on a site level using their Prefixes (ie T001)
         * By default, all turbine prefixes should be in the waitingForPause list, but if the state is not 100
         * Then throw it out of the waiting list and into the conditionsNotMet list until the appriate conditions
         * 
         * Note that all the lists are strings, meaning it works ONLY with Turbine Prefixes
         */

        //Turbines that are removed from Articuno by the dispatchers
        public static List<string> turbinesExcludedList;
        //Turbines participating in Articuno and are waiting to be Paused.
        public static List<string> turbinesWaitingForPause;
        //Turbines that are already paused by articuno
        public static List<string> turbinesPausedByArticuno;
        //Turbines that are taken out of the Waiting list due to some other factor (derate, not running, etc.)
        public static List<string> turbinesConditionNotMet;

        private static string opcServerName;
        private static int articunoCtr;
        private static int ctrCountdown;
        private static bool articunoEnable;

        //OpcTag getters and setters
        private static string tempThresholdTag;
        private static string enableArticunoTag;
        private static string articunoCtrTag;
        private static string deltaThresholdTag;
        private static string dewThresholdTag;

        //Constants
        private static int ONE_MINUTE_POLLING = 60 * 1000;
        private static int NOISE_LEV = 5;
        private static int RUN_STATE = 100;
        private static int DRAFT_STATE = 75;

        //Log
        private static readonly ILog log = LogManager.GetLogger(typeof(ArticunoMain));

        //Singleton instance declarations
        private static DatabaseInterface dbi;
        private static MetTowerMediator mm;
        private static TurbineMediator tm;

        //Constructor. This is only used for unit testing purposes
        public ArticunoMain()
        {
            dbi = DatabaseInterface.Instance;
            mm = MetTowerMediator.Instance;
            tm = TurbineMediator.Instance;

            mm.createMetTower();
            tm.createTestTurbines();
            turbinesExcludedList = new List<string>();
            turbinesPausedByArticuno = new List<string>();
            turbinesWaitingForPause = new List<string>();
            turbinesConditionNotMet = new List<string>();
        }

        static void Main(string[] args)
        {
            dbi = DatabaseInterface.Instance;
            mm = MetTowerMediator.Instance;
            tm = TurbineMediator.Instance;

            //Call the create methods
            mm.createMetTower();
            //tm.createTurbines();
            tm.createTestTurbines();

            turbinesExcludedList = new List<string>();
            turbinesPausedByArticuno = new List<string>();
            turbinesWaitingForPause = new List<string>();
            turbinesConditionNotMet = new List<string>();

            setup();
            start();
        }

        public static void start()
        {
            //The following lines starts a threading lambda and executes a function every minute. THis is used for events that require minute polling and CTR polling 
            var startTimeSpan = TimeSpan.Zero;
            var periodTimeSpan = TimeSpan.FromMilliseconds(ONE_MINUTE_POLLING);
            var timer = new System.Threading.Timer((e) => { minuteUpdate(); }, null, startTimeSpan, periodTimeSpan);

            //start of the infinite loop
            while (true)
            {
                //Run only if articuno is enabled. If not, then wait until something occurs
                while (articunoEnable)
                {

                }
            }
        }
        public static void setup()
        {
            //Get the OPC Server name
            DataTable reader = dbi.readCommand("SELECT * from SystemInputTags WHERE Description='OpcServerName'");
            opcServerName = reader.Rows[0]["OpcTag"].ToString();
            string tag;

            //Set up the accesssors for system output tags
            reader = dbi.readCommand("SELECT * from SystemOutputTags WHERE Description='NumTurbIced'");

            //A speicifc client that will respond to System Tag input changes. 
            var systemInputClient = new EasyDAClient();
            systemInputClient.ItemChanged += SystemInputOnChange;
            List<DAItemGroupArguments> systemInputTags = new List<DAItemGroupArguments>();
            reader = dbi.readCommand("SELECT * from SystemInputTags WHERE Description!='SitePrefix' AND Description!='OpcServerName' order by Description ASC");
            for (int i = 0; i < reader.Rows.Count; i++)
            {
                tag = reader.Rows[i]["OpcTag"].ToString();
                //The following switch statement is ambiguious
                //because the query always return the OPC tag column in a certain order, 
                //the switch statement acts as a setter and set the tag from the database into the member variable of this class
                //If this still doesn't make sense, try executing the above SELECT * query in SQLite
                switch (i)
                {
                    case 0: tempThresholdTag = tag; break;
                    case 1: enableArticunoTag = tag; break;
                    case 2: articunoCtrTag = tag; break;
                    case 3: deltaThresholdTag = tag; break;
                    case 4: dewThresholdTag = tag; break;
                    case 5: break;
                    case 6: break;
                }
                systemInputTags.Add(new DAItemGroupArguments("", opcServerName, tag, 1000, null));
            }
            systemInputClient.SubscribeMultipleItems(systemInputTags.ToArray());

            //A  client that will respond to Turbine OPC tag Changes for operating state, partiicipation and NrsMode
            var assetStatusClient = new EasyDAClient();
            assetStatusClient.ItemChanged += assetTagChangeHandler;
            List<DAItemGroupArguments> assetInputTags = new List<DAItemGroupArguments>();
            foreach (string prefix in tm.getTurbinePrefixList())
            {
                string cmd = String.Format("SELECT OperatingState, Participation,NrsMode from TurbineInputTags where TurbineId='{0}'", prefix);
                reader = dbi.readCommand(cmd);
                for (int i = 0; i < reader.Rows.Count; i++)
                {
                    try
                    {
                        assetInputTags.Add(new DAItemGroupArguments("",
                            opcServerName, reader.Rows[i]["OperatingState"].ToString(), 1000, null));
                        assetInputTags.Add(new DAItemGroupArguments("",
                            opcServerName, reader.Rows[i]["Participation"].ToString(), 1000, null));
                        assetInputTags.Add(new DAItemGroupArguments("",
                            opcServerName, reader.Rows[i]["NrsMode"].ToString(), 1000, null));
                    }
                    catch (Exception e) { log.ErrorFormat("Error when attempting to add to assetInputTags list. {0}", e); }
                }
            }


            //Same client will be used to respond to Met Tower OPC tag Changes (only the switching command though)
            reader = dbi.readCommand("SELECT Switch from MetTowerInputTags");
            for (int i = 0; i < reader.Rows.Count; i++)
            {
                var switchTag = reader.Rows[i]["Switch"].ToString();
                try
                {
                    assetInputTags.Add(new DAItemGroupArguments("",
                        opcServerName, reader.Rows[i]["Switch"].ToString(), 1000, null));
                }
                catch (Exception e) { log.ErrorFormat("Error when attempting to add {0} to assetInputTags list. {1}", switchTag, e); }
            }

            assetStatusClient.SubscribeMultipleItems(assetInputTags.ToArray());
        }

        /// <summary>
        /// Function to handle tasks that should be executed every minute (ie get temperature measurements) and every CTR minute (ie check rotor speed, run calculations, etc.) 
        /// </summary>
        private static void minuteUpdate()
        {
            //For every minute, read the met tower measurements and the turbine temperature measurements
            for (int i = 1; i <= MetTowerMediator.getNumMetTower(); i++)
            {
                //Get all measurements from the met tower. Note that it will get turbine 
                //temperature if the temperature coming from the met tower is bad qualtiy
                Tuple<double, double, double, double> metMeasurements = mm.getAllMeasurements("Met" + i);

                double temperature = metMeasurements.Item1;
                double humidity = metMeasurements.Item2;
                mm.writeToQueue("Met" + i, temperature, humidity);
            }

            //Get turbine to update internal wind speed queue
            foreach (string prefix in tm.getTurbinePrefixList())
            {
                //Call the storeWindSpeed function to store a wind speed average into a turbine queue
                tm.storeMinuteAverages(prefix);
            }


            //For every CTR minute, do the other calculation stuff. Better set up a  member variable here
            ctrCountdown--;
            if (ctrCountdown == 0)
            {
                log.InfoFormat("CTR countdown reached 0");
                //Calculate temperature averages from the all the temperature queues
                for (int i = 1; i <= MetTowerMediator.getNumMetTower(); i++)
                {
                    double tempAvg = mm.calculateCtrAvgTemperature("Met" + i);
                    //Send this temperature to the Met Mediator and determine if met tower is freezing or not
                    mm.isFreezing("Met" + i, tempAvg);
                }
                //Call the RotorSPeedCheck function to compare rotor speed for all turbines
                foreach (string prefix in tm.getTurbinePrefixList()) { tm.RotorSpeedCheck(prefix); }

                //Set the CTR back to the original value
                ctrCountdown = articunoCtr;
            }
        }

        /// <summary>
        ///  ONLY USED FROM EVENT HANDLERS. This Method that is used to find enums so Articuno knows what other method should call. 
        /// </summary>
        /// <param name="opcTag"></param>
        /// <summary>
        /// method called upon NRS or turbine Operating Status or participation change
        /// </summary>
        /// <param name="sender"></param>
        /// <param name=""></param>
        /**
         * Event handler executed when turbine or met input tags are changed by the user
         *  Turbine input tags are the following:
         *  - ArticunoParticipation (Participation)
         *  - NrsMode (Noise Level - Not used at all site)
         *  
         *  Met input tags are the following:
         *  - Switch Command (whether met tower is switched or not)
         *  
         *  System input tags are the following:
         *  - Thresholds (Delta, Amb Temp and Dews) 
         *  - CTR 
         *  - Enable
         * 
         * This method is used to monitor the tag change from a turbine's NRS state or a turbine's oeprating state 
         * What happens is that once either state changes, Articuno will remove them from its internal queue
         * until Operating state is back to 100 (Run status) or when NRS is 5. Remove it from the queue otherwise
         * 
         * You might need to get the turbine prefix (via substring) and then get the actual turbine object 
         * 
         * HOWEVER, you need to check to see if the turbine also
         * - participating in Articuno
         * - Not in derate, or excluded or some other weird shit
         * - 
         * 
         */
        private static void assetChange(string opcTag, Object value)
        {
            /*
             * The following will find the met and turbine indicator for any given OPC Tag that changed by finding any words that are four characters long. First three can be alphanumeric,
             * but the last one must be a number
             *
             * Ex: SCRAB.T001.WROT.RotSpdAv will match T001 and SCRAB.MET1.AmbRh1 will match MET1
             */
            string pattern = @"\b\w{3}\d{1}\b";
            string input = opcTag;
            Regex lookup = new Regex(pattern, RegexOptions.Singleline);
            Match matchLookup = lookup.Match(opcTag);
            string prefix = matchLookup.ToString();


            //If it matches the met tower
            //TODO: Implement this. You should only have the me ttower switch. Thresholds are dealt with in SystemInputOnChange()
            if (matchLookup.Value.ToUpper().Contains("MET"))
            {
                Enum metEnum = mm.findMetTowerTag(matchLookup.Value, opcTag);
                switch (metEnum)
                {
                    case MetTowerMediator.MetTowerEnum.Switched:
                        mm.switchMetTower(prefix);
                        break;
                    default:
                        log.DebugFormat("Event CHanged detected for {0}. However, there is nothing to be doen", opcTag);
                        break;
                }

            }
            //Else, assume Turbine or system input. Not like there's anything else given the regex
            else
            {
                Enum turbineEnum = tm.findTurbineTag(matchLookup.Value, opcTag);
                switch (turbineEnum)
                {
                    case TurbineMediator.TurbineEnum.NrsMode:
                        tm.writeNrsStateTag(prefix, value);
                        break;
                    //In the case where the turbine went into a different state. This includes pause by the dispatchers, site, curtailment, maintenance, anything non-Articuno 
                    case TurbineMediator.TurbineEnum.OperatingState:
                        int state = Convert.ToInt16(value);
                        //If already paused by Articuno, then there's nothing to do
                        if (isPausedByArticuno(prefix)) { break; }
                        //If turbine isn't in run or draft, then that means it is derated or in emergency, or something else
                        if ((state != RUN_STATE || state != DRAFT_STATE)) { conditionsNotMet(prefix); }
                        else { conditionsMet(prefix); }
                        break;
                    case TurbineMediator.TurbineEnum.Participation:
                        bool partipationStatus = Convert.ToBoolean(value);
                        if (isPausedByArticuno(prefix)) { break; }
                        if (partipationStatus == false && !turbinesExcludedList.Contains(prefix)) { conditionsNotMet(prefix); }
                        else { conditionsMet(prefix); }
                        break;
                    default:
                        log.DebugFormat("Event CHanged detected for {0}. However, there is nothing to be doen", opcTag);
                        break;
                }
            }
        }

        //This is a method that is triggered upon any value changes for certain OPC Tags
        static void assetTagChangeHandler(object sender, EasyDAItemChangedEventArgs e)
        {
            if (e.Succeeded)
            {
                string tag = e.Arguments.ItemDescriptor.ItemId;
                assetChange(tag, e.Vtq.Value);
            }
            else { log.ErrorFormat("Error occured in onItemChangeHandler with {0}. Msg: {1}", e.Arguments.ItemDescriptor.ItemId, e.ErrorMessageBrief); }

        }

        /// <summary>
        /// method that handles system input tag changes such as whether Articuno is enabled or not, Threshold, CTR Period, etc.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /*
         * Event Handler that is executed whenever  the system input tags changed
         * System input tags are the following:
            - Articuno.TmpThreshold
            - Articuno.CurtailEna (Most important)
            - Articuno.EvalTm  (CTR Period should be updated in both the ArticunoMain and the Turbine classes)
            - Articuno.TmpDelta (requires met tower to perform update)
            - Articuno.TmpDew (requires met tower to perform update)
         * 
         */

        static void SystemInputOnChange(object sender, EasyDAItemChangedEventArgs e)
        {
            if (e.Succeeded)
            {
                string tag = e.Arguments.ItemDescriptor.ItemId.ToString();
                int value = Convert.ToInt16(e.Vtq.Value);
                if (tag.Contains("Enable") || tag.Contains("CurtailEna"))
                {
                    articunoEnable = (value == 1) ? true : false;
                    log.InfoFormat("Articuno is : {0}", articunoEnable ? "Enabled" : "Disabled");
                }
                if (tag.Contains("CTR") || tag.Contains("EvalTm"))
                {
                    articunoCtr = value;
                    ctrCountdown = value;
                    tm.writeCtrTime(value);
                }
                if (tag.Contains("TmpTreshold")) { mm.writeTemperatureThreshold(value); }
                if (tag.Contains("TmpDelta")) { mm.writeDeltaThreshold(value); }
            }
            else { log.ErrorFormat("Error occured in systemInputOnChangeHandler with {0}. Msg: {1}", e.Arguments.ItemDescriptor.ItemId, e.ErrorMessageBrief); }
        }

        private static bool isPausedByArticuno(string turbineId) { return turbinesPausedByArticuno.Contains(turbineId); }
        //method used to update member lists when a turbine isn't ready to be paused by articuno
        private static void conditionsNotMet(string turbineId)
        {

            if (turbinesWaitingForPause.Contains(turbineId))
            {
                turbinesWaitingForPause.Remove(turbineId);
                turbinesConditionNotMet.Add(turbineId);
            }
        }

        //Method used to update member lists  when a turbine is ready to be paused by ARticuno
        private static void conditionsMet(string turbineId)
        {
            //If turbine is waiting to be paused and it isn't already paused by Articuno
            if (!turbinesWaitingForPause.Contains(turbineId) &&
                !tm.isPausedByArticuno(turbineId))
            {
                turbinesWaitingForPause.Add(turbineId);
                turbinesConditionNotMet.Remove(turbineId);
            }
        }

        /// <summary>
        /// Method executed from the TurbineMediator class. This should only be used to signal the main program that a turbine is paused
        /// </summary>
        /// <param name="turbineId"></param>
        public static void turbinePausedByArticuno(string turbineId)
        {
            log.DebugFormat("ArticunoMain has detected Turbine {0} has paused. ", turbineId);
            turbinesWaitingForPause.Remove(turbineId);
            turbinesPausedByArticuno.Add(turbineId);

        }

        /// <summary>
        /// Method executed from the TurbineMediator class. This should only be used to signal the main program that a turbine is cleared
        /// </summary>
        public static void turbineClearedBySite(string turbineId)
        {
            log.DebugFormat("ArticunoMain has detected Turbine {0} has started from the site.", turbineId);
            turbinesWaitingForPause.Add(turbineId);
            turbinesPausedByArticuno.Remove(turbineId);
        }

    }
}
