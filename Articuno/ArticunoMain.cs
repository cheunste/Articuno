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

        private static bool articunoEnable;
        private Queue<double> temperatureQueue;

        private static string opcServerName;
        private static int articunoCtr;

        //OpcTag getters and setters
        private string tempThresholdTag;
        private string enableArticunoTag;
        private string articunoCtrTag;
        private string deltaThresholdTag;
        private string dewThresholdTag;

        private List<Turbine> turbineList;

        //Constants
        private int ONE_MINUTE_POLLING = 60 * 1000;
        private static int NOISE_LEV = 5;
        private static int RUN_STATE = 100;
        private static int DRAFT_STATE = 75;

        //Log
        private static readonly ILog log = LogManager.GetLogger(typeof(ArticunoMain));

        public String opcServer { get; set; }

        public ArticunoMain(string opcServer, string metTower, List<Turbine> list)
        {
            this.turbineList = list;
            turbinesExcludedList = new List<string>();
            turbinesPausedByArticuno = new List<string>();
            turbinesWaitingForPause = new List<string>();

            temperatureQueue = new Queue<double>();
        }

        public void start()
        {


            //The following lines starts a threading lambda and executes a function every minute. THis is used for events that require minute polling and CTR polling 
            var startTimeSpan = TimeSpan.Zero;
            var periodTimeSpan = TimeSpan.FromMilliseconds(ONE_MINUTE_POLLING);
            var timer = new System.Threading.Timer((e) => { minuteUpdate(); }, null, startTimeSpan, periodTimeSpan);

            //start of the infinite loop
            while (true)
            {

            }

        }
        public void setup()
        {
            //Get the OPC Server name
            DataTable reader = DatabaseInterface.Instance.readCommand("Select * from SystemInputTags WHERE Description='OpcServerName'");
            opcServerName = reader.Rows[0]["OpcTag"].ToString();
            string tag;

            //Call the create methods
            MetTowerMediator.Instance.createMetTower();
            TurbineMediator.Instance.createTurbines();

            //Initializes Lists and Queues
            turbinesExcludedList = new List<string>();
            turbinesPausedByArticuno = new List<string>();
            turbinesWaitingForPause = new List<string>();
            temperatureQueue = new Queue<double>();


            //A speicifc client that will respond to System Tag input changes. This can be hard coded
            var systemInputClient = new EasyDAClient();
            systemInputClient.ItemChanged += SystemInputOnChange;
            List<DAItemGroupArguments> systemInputTags = new List<DAItemGroupArguments>();
            reader = DatabaseInterface.Instance.readCommand("Select * from SystemInputTags WHERE Description!='SitePrefix' AND Description!='OpcServerName'");
            for (int i = 0; i < reader.Rows.Count; i++)
            {
                tag = reader.Rows[i]["OpcTag"].ToString();
                //The following switch statement is ambiguious
                //because the query always return the OPC tag column in a certain order, 
                //the switch statement acts as a setter and set the tag from the database into the member variable of this class
                //If this still doesn't make sense, try executing the above Select * query in SQLite
                switch (i)
                {
                    case 0: tempThresholdTag = tag; break;
                    case 1: enableArticunoTag = tag; break;
                    case 2: articunoCtrTag = tag; break;
                    case 3: deltaThresholdTag = tag; break;
                    case 4: dewThresholdTag = tag; break;
                    case 5:  break;
                    case 6:  break;
                }
                systemInputTags.Add(new DAItemGroupArguments("", opcServerName, tag, 1000, null));
            }
            systemInputClient.SubscribeMultipleItems(systemInputTags.ToArray());


            //A  client that will respond to Turbine OPC tag Changes for operating state, partiicipation and NrsMode
            var assetStatusClient = new EasyDAClient();
            assetStatusClient.ItemChanged += assetTagChangeHandler;
            List<DAItemGroupArguments> assetInputTags = new List<DAItemGroupArguments>();
            reader = DatabaseInterface.Instance.readCommand("Select OperatingState, Participation,NrsMode from TurbineInputTags");
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
                catch(Exception e)
                {

                }
                //tag = reader.Rows[i]["OpcTag"].ToString();
                //assetInputTags.Add(new DAItemGroupArguments("", opcServerName, tag, 1000, null));
            }

            assetStatusClient.SubscribeMultipleItems(assetInputTags.ToArray());

        }

        /// <summary>
        /// Function to handle tasks that should be executed every minute (ie get temperature measurements) and every CTR minute (ie check rotor speed, run calculations, etc.) 
        /// </summary>
        private void minuteUpdate()
        {
            //For every minute, read the met tower measurements and the turbine temperature measurements
            //TODO: Fill this shit out
            for (int i = 1; i <= MetTowerMediator.getNumMetTower(); i++)
            {
                //Get all measurements from the met tower
                Tuple<double, double, double, double> metMeasurements = MetTowerMediator.Instance.getAllMeasurements("Met" + i);
                //Get the temperature value of the nearest turbine from the met tower as well
                MetTowerMediator.Instance.getMetTower("Met" + i).getNearestTurbine().readTemperatureValue();
            }

            //For every CTR minute, do the other calculation stuff. Better set up a  member variable here
            //TODO: Implement
            articunoCtr--;
            if (articunoCtr == 0)
            {

                //Get turbine to update rotor speed and other calculations
                //

                //Set the CTR back to the original value
                articunoCtr = readCtrValue();
            }
        }

        /*
        This method is special. It adds the current temperature from either the met tower or the turbine (only if met tower is down) to a temperature queue. 
        This is used for calculating CTR minute temperature averages based on averaging one minute averages, which CORE may or may not be providing
        The way it works is as follows
         - you collect one minute average temperatures values every minute (This will be determined by your timer class)
         - After CTR minute has passed (this could be 10, or 15, or 1, it is dependent on user), then 
         - calculate the average temperature of all the one min temperature values in the entire queue
         - perform a dequeue to remove the first most value
        */
        public void addToTemperatureQueue(double temperature) { throw new NotImplementedException(); }

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
            //TODO: Implement this. You should only have the me ttower switch and the thresholds 
            if (matchLookup.Value.ToUpper().Contains("MET")) { throw new NotImplementedException(); }
            //Else, assume Turbine or system input. Not like there's anything else given the regex
            else
            {
                Enum turbineEnum = TurbineMediator.Instance.findTurbineTag(matchLookup.Value, opcTag);
                switch (turbineEnum)
                {
                    case TurbineMediator.TurbineEnum.NrsMode:
                        TurbineMediator.Instance.writeNrsStateTag(prefix, value);
                        break;
                    case TurbineMediator.TurbineEnum.OperatingState:
                        int state = Convert.ToInt16(value);
                        if (state != RUN_STATE || state != DRAFT_STATE)
                        {
                            turbinesWaitingForPause.Remove(prefix);
                            turbinesConditionNotMet.Add(prefix);
                        }
                        else
                        {
                            turbinesWaitingForPause.Add(prefix);
                            turbinesConditionNotMet.Remove(prefix);
                        }
                        break;
                    case TurbineMediator.TurbineEnum.RotorSpeed:
                        throw new NotImplementedException();
                        break;
                    case TurbineMediator.TurbineEnum.Temperature:
                        throw new NotImplementedException();
                        break;
                    case TurbineMediator.TurbineEnum.WindSpeed:
                        throw new NotImplementedException();
                        break;
                    case TurbineMediator.TurbineEnum.Participation:
                        bool partipationStatus = Convert.ToBoolean(value);

                        if (partipationStatus == false)
                        {
                            turbinesWaitingForPause.Remove(prefix);
                            turbinesExcludedList.Add(prefix);
                        }
                        else
                        {
                            turbinesWaitingForPause.Add(prefix);
                            turbinesExcludedList.Remove(prefix);
                        }
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
        /// <param name="sneder"></param>
        /// <param name="e"></param>
        /*
         * There are only four items in the System InputTags table that really matter. Thre two thresholds (temp and humidity), the CTR period and the Enable tag.
         * You can hard code this
         * 
         * Event Handler that is executed whenever  the system input tags changed
         * System input tags are the following:
         *  - ICE.TmpThreshold
         *  - ICE.CurtailEna
         *  - ICE.EvalTm
         *  - ICE.TmpDelta
         *  - ICE.TmpDew (Kinda optional?)
         *  
         *  CurtailEna is the most important one, which enables Articuno. 
         *  Thresholds requires the Met Tower class to do an update 
         *  CTR Period should be updated in both the ArticunoMain and the Turbine classes
         * 
         */

        static void SystemInputOnChange(object sneder, EasyDAItemChangedEventArgs e)
        {
            if (e.Succeeded)
            {
                string tag = e.Arguments.ItemDescriptor.ItemId.ToString();
                int value = Convert.ToInt16(e.Vtq.Value);
                if (tag.Contains("Enable") || tag.Contains("CurtailEna")) { articunoEnable = (value == 1) ? true : false; }
                if (tag.Contains("CTR") || tag.Contains("EvalTm"))
                {
                    articunoCtr = value;
                    foreach (string turbinePrefix in TurbineMediator.Instance.getTurbinePrefixList()) { TurbineMediator.Instance.writeTurbineCtrTag(turbinePrefix, value); }
                }
                if (tag.Contains("TmpTreshold")) { MetTowerMediator.Instance.writeTemperatureThreshold(value); }
                if (tag.Contains("TmpDelta")) { MetTowerMediator.Instance.writeDeltaThreshold(value); }
            }
            else { log.ErrorFormat("Error occured in systemInputOnChangeHandler with {0}. Msg: {1}", e.Arguments.ItemDescriptor.ItemId, e.ErrorMessageBrief); }
        }

        public static string getOpcServerName() { return opcServerName; }

        public int readCtrValue()
        {
            using (var client = new EasyDAClient())
            {

            }
            return 0;

        }
    }
}
