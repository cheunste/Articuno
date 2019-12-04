using log4net;
using OpcLabs.EasyOpc.DataAccess;
using OpcLabs.EasyOpc.DataAccess.Generic;
using OpcLabs.EasyOpc.DataAccess.OperationModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using Topshelf;

namespace Articuno
{
    sealed internal class Articuno
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
        private static int articunoCtrTime;
        private static int ctrCountdown;
        private static bool articunoEnable;
        private static string sitePrefix;
        private static string uccActiveTag;

        //OpcTag getters and setters
        private static string tempThresholdTag;
        private static string enableArticunoTag;
        private static string articunoCtrTag;
        private static string deltaThresholdTag;
        private static string heartBeatTag;
        static string icePossibleAlarmTag;
        static string numTurbinesPausedTag;

        //Constants
        private static int ONE_MINUTE_POLLING = 60 * 1000;
        private static int HEARTBEAT_POLLING = 15 * 1000;
        private readonly static int NORMAL_UPDATE_RATE = 1000;
        private readonly static int FAST_UPDATE_RATE = 100;
        private static int ACTIVE_NOISE_LEV = 0;
        private static int RUN_STATE = 100;
        private static int DRAFT_STATE = 75;

        private static int MAX_CTR_TIME = 60;
        private static int MIN_CTR_TIME = 1;
        private static int ENABLE = 1;

        //Log
        private static readonly ILog log = LogManager.GetLogger(typeof(Articuno));

        //Singleton instance declarations
        private static DatabaseInterface dbi;
        private static MetTowerMediator mm;
        private static TurbineMediator tm;

        //Constructor. This is only used for unit testing purposes
        public Articuno(bool testMode)
        {
            dbi = DatabaseInterface.Instance;
            mm = MetTowerMediator.Instance;
            tm = TurbineMediator.Instance;

            mm.CreateMetTowerObject();
            if (testMode)
                tm.createTestTurbines();
            else
                tm.createTurbines();
            turbinesExcludedList = new List<string>();
            turbinesPausedByArticuno = new List<string>();
            turbinesWaitingForPause = new List<string>();
            turbinesConditionNotMet = new List<string>();

            setup();
        }

        public static void Main(object sender, FileSystemEventArgs e)
        {
            DataTable reader;
            dbi = DatabaseInterface.Instance;
            mm = MetTowerMediator.Instance;
            tm = TurbineMediator.Instance;

            //Get the OPC Server name
            opcServerName = dbi.getOpcServer();

            //Get the Site Prefix
            sitePrefix = dbi.getSitePrefix();

            //Get the tag to see if UCC is active
            uccActiveTag = dbi.getActiveUCCTag();

            //Call the create methods
            mm.CreateMetTowerObject();
            tm.createTurbines();

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
            var periodTimeSpan = TimeSpan.FromMilliseconds(ONE_MINUTE_POLLING);
            var heartBeatTimeSpan = TimeSpan.FromMilliseconds(HEARTBEAT_POLLING);

            System.Timers.Timer minuteTimer = new System.Timers.Timer(periodTimeSpan.TotalMilliseconds);
            minuteTimer.AutoReset = true;
            minuteTimer.Elapsed += new System.Timers.ElapsedEventHandler(minuteUpdate);
            minuteTimer.Start();

            System.Timers.Timer heartBeatTimer = new System.Timers.Timer(heartBeatTimeSpan.TotalMilliseconds);
            heartBeatTimer.AutoReset = true;
            heartBeatTimer.Elapsed += new System.Timers.ElapsedEventHandler(updateHeartBeat);
            heartBeatTimer.Start();
        }
        public static void setup()
        {
            sitePrefix = dbi.getSitePrefix();
            opcServerName = dbi.getOpcServer();

            setSystemInputTags();
            //Set up the accesssors for system output tags
            //Note that there are no event listeners for the SystemOutputTags
            setSystemOutputTags();
            setOpcTagListenerForTurbineAndMetTower();
        }

        /// <summary>
        /// Method executed from the TurbineMediator class. This should only be used to signal the main program that a turbine is paused
        /// </summary>
        /// <param name="turbineId"></param>
        public static void turbinePausedByArticuno(string turbineId)
        {
            log.DebugFormat("ArticunoMain has detected Turbine {0} has paused. ", turbineId);
            MoveTurbineIdToAnotherList(turbineId, turbinesWaitingForPause, turbinesPausedByArticuno);
            OpcServer.writeOpcTag(opcServerName, numTurbinesPausedTag, turbinesPausedByArticuno.Count);
            LogCurrentTurbineStatusesInArticuno();
        }

        /// <summary>
        /// Method executed from the TurbineMediator class. This should only be used to signal the main program that a turbine is cleared
        /// </summary>
        public static void turbineClearedOfIce(string turbineId)
        {
            log.DebugFormat("ArticunoMain has detected Turbine {0} has started running from the site.", turbineId);
            MoveTurbineIdToAnotherList(turbineId, turbinesPausedByArticuno, turbinesWaitingForPause);
            //Update the num turb paused
            OpcServer.writeOpcTag(opcServerName, numTurbinesPausedTag, turbinesPausedByArticuno.Count);
            LogCurrentTurbineStatusesInArticuno();
        }

        //Check to see if a turbine is already pasued or not
        public static bool isAlreadyPaused(string turbineId)
        {
            log.DebugFormat("Turbine {0} is {1} ", turbineId, turbinesPausedByArticuno.Contains(turbineId));
            LogCurrentTurbineStatusesInArticuno();
            return turbinesPausedByArticuno.Contains(turbineId);
        }

        private static void setSystemInputTags()
        {
            var systemInputClient = new EasyDAClient();
            systemInputClient.ItemChanged += SystemTagValueChange;

            List<DAItemGroupArguments> systemInputTags = new List<DAItemGroupArguments>();
            tempThresholdTag = dbi.readCommand("SELECT OpcTag from SystemInputTags WHERE Description='AmbTempThreshold'").Rows[0]["OpcTag"].ToString();
            enableArticunoTag = dbi.readCommand("SELECT OpcTag from SystemInputTags WHERE Description='ArticunoEnable'").Rows[0]["OpcTag"].ToString();
            articunoCtrTag = dbi.readCommand("SELECT OpcTag from SystemInputTags WHERE Description='CTRPeriod'").Rows[0]["OpcTag"].ToString();
            deltaThresholdTag = dbi.readCommand("SELECT OpcTag from SystemInputTags WHERE Description='DeltaTmpThreshold'").Rows[0]["OpcTag"].ToString();

            systemInputTags.Add(new DAItemGroupArguments("", opcServerName, tempThresholdTag, NORMAL_UPDATE_RATE, null));
            systemInputTags.Add(new DAItemGroupArguments("", opcServerName, enableArticunoTag, NORMAL_UPDATE_RATE, null));
            systemInputTags.Add(new DAItemGroupArguments("", opcServerName, articunoCtrTag, NORMAL_UPDATE_RATE, null));
            systemInputTags.Add(new DAItemGroupArguments("", opcServerName, deltaThresholdTag, NORMAL_UPDATE_RATE, null));
            systemInputClient.SubscribeMultipleItems(systemInputTags.ToArray());
        }
        private static void setSystemOutputTags()
        {
            string tag;
            DataTable reader = dbi.readCommand("SELECT * from SystemOutputTags order by Description ASC");
            for (int i = 0; i < reader.Rows.Count; i++)
            {
                tag = sitePrefix + reader.Rows[i]["OpcTag"].ToString();
                switch (i)
                {
                    case 0: heartBeatTag = tag; break;
                    case 1: icePossibleAlarmTag = tag; break;
                    case 2: numTurbinesPausedTag = tag; break;
                }
            }
        }

        /// <summary>
        /// Function to set up input tag listeners on both the met tower and turbines
        /// </summary>
        private static void setOpcTagListenerForTurbineAndMetTower()
        {
            //An OPC client that will respond to Turbine OPC tag value Changes for operating state, partiicipation and NrsMode. Essentually an event listener
            var assetStatusClient = new EasyDAClient();
            assetStatusClient.ItemChanged += MetTowerTurbineOpcTagValueChanged;
            assetStatusClient.SubscribeMultipleItems(getTurbineTagsForListening().ToArray());
            assetStatusClient.SubscribeMultipleItems(getMetTagsForListening().ToArray());
        }

        private static List<DAItemGroupArguments> getTurbineTagsForListening()
        {
            DataTable reader;
            List<DAItemGroupArguments> turbineInputTags = new List<DAItemGroupArguments>();
            foreach (string prefix in tm.getTurbinePrefixList())
            {
                string cmd = String.Format("SELECT OperatingState, Participation,NrsMode,Start from TurbineInputTags where TurbineId='{0}'", prefix);
                reader = dbi.readCommand(cmd);
                for (int i = 0; i < reader.Rows.Count; i++)
                {
                    try
                    {
                        turbineInputTags.Add(new DAItemGroupArguments("",
                            opcServerName, sitePrefix + reader.Rows[i]["OperatingState"].ToString(),FAST_UPDATE_RATE , null));
                        turbineInputTags.Add(new DAItemGroupArguments("",
                            opcServerName, sitePrefix + reader.Rows[i]["Participation"].ToString(), FAST_UPDATE_RATE , null));
                        turbineInputTags.Add(new DAItemGroupArguments("",
                            opcServerName, sitePrefix + reader.Rows[i]["NrsMode"].ToString(), FAST_UPDATE_RATE , null));
                        turbineInputTags.Add(new DAItemGroupArguments("",
                            opcServerName, sitePrefix + reader.Rows[i]["Start"].ToString(), FAST_UPDATE_RATE , null));
                    }
                    catch (Exception e) { log.ErrorFormat("Error when attempting to add to assetInputTags list. {0}", e); }
                }
            }
            return turbineInputTags;
        }

        private static List<DAItemGroupArguments> getMetTagsForListening()
        {
            DataTable reader = dbi.readCommand("SELECT Switch from MetTowerInputTags");
            List<DAItemGroupArguments> metTowerInputTags = new List<DAItemGroupArguments>();
            for (int i = 0; i < reader.Rows.Count; i++)
            {
                var switchTag = reader.Rows[i]["Switch"].ToString();
                try
                {
                    metTowerInputTags.Add(new DAItemGroupArguments("", opcServerName, sitePrefix + reader.Rows[i]["Switch"].ToString(), NORMAL_UPDATE_RATE, null));
                }
                catch (Exception e) { log.ErrorFormat("Error when attempting to add {0} to assetInputTags list. {1}", switchTag, e); }
            }
            return metTowerInputTags;
        }


        //Function to update an heartbeat
        private static void updateHeartBeat(object sender, ElapsedEventArgs e)
        {
            //Only update heartbeat if the UCC is active
            OpcServer.writeOpcTag(opcServerName, heartBeatTag,
                !Convert.ToBoolean(OpcServer.readBooleanTag(opcServerName, heartBeatTag))
                );
            if (articunoEnable)
                gatherTemperatureAndHumiditySamples();
        }
        private static void gatherTemperatureAndHumiditySamples()
        {
            //For every heartbeat interval, read the met tower measurements and the turbine temperature measurements
            //This is so more measurements can be gathered to get a more accurate average after every CTR period
            for (int i = 1; i <= MetTowerMediator.GetNumberOfMetTowers(); i++)
            {
                //This is needed because apparently Met Tower 1 is unnumbered, and so the following strips the '1' essentually. 
                string j = (i == 1) ? "" : Convert.ToString(i);
                //Get all measurements from the met tower. Note that it will get turbine 
                //temperature if the temperature coming from the met tower is bad qualtiy

                double temperature = mm.ReadTemperatureFromMetTower("Met" + j);
                double humidity = mm.readHumidity("Met" + j);

                mm.writeToQueue("Met" + j, temperature, humidity);
            }

            //Call the storeWindSpeed function to store a wind speed average into a turbine queue (for all turbines in the list)
            foreach (string prefix in tm.getTurbinePrefixList()) { tm.storeMinuteAverages(prefix); }

        }
        /// <summary>
        /// Function to handle tasks that should be executed every minute (ie get temperature measurements) and every CTR minute (ie check rotor speed, run calculations, etc.) 
        /// </summary>
        private static void minuteUpdate(object sender, ElapsedEventArgs e)
        {
            //For every CTR minute, do the other calculation stuff. Better set up a  member variable here
            ctrCountdown--;
            if (ctrCountdown == 0)
                calculateMetTowerAverages();

            //Write the MetTower CTR to the tag
            OpcServer.writeOpcTag(opcServerName, dbi.getMetCountdownTag(), ctrCountdown);

            if (articunoEnable)
                tm.decrementTurbineCtrTime();
            LogCurrentTurbineStatusesInArticuno();
        }

        private static void calculateMetTowerAverages()
        {
            log.InfoFormat("CTR countdown reached 0 in ArticunoMain");
            //Calculate temperature averages from the all the temperature queues
            for (int i = 1; i <= MetTowerMediator.GetNumberOfMetTowers(); i++)
            {
                //There is no such thing as "Met1", just "Meh" hence the following
                string j = (i == 1) ? "" : Convert.ToString(i);

                double tempAvg = mm.calculateCtrAvgTemperature("Met" + j);
                double humidityAvg = mm.calculateCtrAvgHumidity("Met" + j);

                log.DebugFormat("CTR avg temp: {0}, avg Humidity: {1}", tempAvg, humidityAvg);

                //Send this temperature to the Met Mediator and determine if met tower is freezing or not
                bool metFrozen = mm.IsMetTowerFrozen("Met" + j, tempAvg, humidityAvg);

                //Update the Dew Point calculation. This value will show up on the faceplate
                mm.updateDewPoint("Met" + j, tempAvg, humidityAvg);
                tm.checkMetTowerFrozen("Met" + j);

                OpcServer.writeOpcTag(opcServerName, icePossibleAlarmTag, mm.IsAnyMetTowerFrozenAtSite());
            }
            //Set the CTR back to the original value
            ctrCountdown = articunoCtrTime;
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
         * Function that is  executed when turbine input OPC tag or met input OPC tag value changes.
         * See the database documentation for more information
         */
        private static void MetTowerAndTurbineValueChangeHandler(string opcTag, Object value)
        {
            /*
             * The following regex will find the met and turbine indicator for any given OPC Tag that changed by finding any words that are four characters long. First three can be alphanumeric,
             * but the last one must be a number
             * Ex: SCRAB.T001.WROT.RotSpdAv will match T001 and SCRAB.MET.AmbRh1 will match MET
             */
            string pattern = @"\b\w{3}(\d{1})*\b";
            Regex lookup = new Regex(pattern, RegexOptions.Singleline);
            Match matchLookup = lookup.Match(opcTag);
            string prefix = matchLookup.ToString();

            if (matchLookup.Value.ToUpper().Contains("MET"))
                MetTowerOpcTagValueChangeListener(prefix, opcTag);
            else
                TurbineOpcTagValueChangeListener(prefix, opcTag, value);
        }

        private static void MetTowerOpcTagValueChangeListener(string prefix, string opcTag)
        {
            Enum metEnum = mm.FindMetTowerTag(prefix, opcTag);
            switch (metEnum)
            {
                case MetTowerMediator.MetTowerEnum.Switched:
                    string referencedMet = mm.isMetTowerSwitched(prefix);
                    log.InfoFormat("{0} switched to {1}", prefix, referencedMet);
                    break;
                default:
                    log.DebugFormat("Event Changed detected for {0}. However, there is nothing to be done", opcTag);
                    break;
            }
        }

        private static void TurbineOpcTagValueChangeListener(string prefix, string opcTag, object value)
        {
            //DO NOT FORGET THIS FUNCTION when adding anythign turbine enum related
            //TODO: Refactor this so you are not dependent on the switch statement here
            Enum turbineEnum = tm.findTurbineTag(prefix, opcTag);
            switch (turbineEnum)
            {
                case TurbineMediator.TurbineEnum.NrsMode:
                    CheckTurbineNrsStatus(prefix, value);
                    break;
                case TurbineMediator.TurbineEnum.TurbineStarted:
                    StartTurbineCommandSentFromSiteOrNcc(prefix, value);
                    break;
                case TurbineMediator.TurbineEnum.OperatingState:
                    CheckTurbineOperatingState(prefix, value);
                    break;
                case TurbineMediator.TurbineEnum.Participation:
                    CheckTurbineParticipationInArticuno(prefix, value);
                    break;
                default:
                    log.DebugFormat("Event CHanged detected for {0}. However, there is nothing to be doen", opcTag);
                    break;
            }
        }

        /// <summary>
        /// Checks the NRS upon value change. Should 
        /// If nrs tag doesn't exist, Then it shouldn't do anything. In that case, just write a non-active value
        /// </summary>
        /// <param name="turbineId"></param>
        /// <param name="value"></param>
        private static void CheckTurbineNrsStatus(string turbineId, object value)
        {
            //Site might not be set up for NRS, so check to see if the tag is empty, if it is, just set the condition to false
            if (tm.getNrsStateTag(turbineId).Equals("")) { tm.setNrsActive(turbineId, false); }
            else
            {
                if (Convert.ToInt16(value) == ACTIVE_NOISE_LEV)
                    tm.setNrsActive(turbineId, true);
                else
                    tm.setNrsActive(turbineId, false);
            }
        }

        private static void StartTurbineCommandSentFromSiteOrNcc(string turbineId, object startCommand)
        {
            if (Convert.ToBoolean(startCommand) == true)
            {
                log.InfoFormat("Turbine {0} has started from NCC or site", turbineId);
                if (IsTurbinePausedByArticuno(turbineId))
                {
                    turbineClearedOfIce(turbineId);
                    TurbineReadyToBePausedByArticuno(turbineId);
                }
            }

        }
        private static void CheckTurbineOperatingState(string turbineId, object value)
        {
            int turbineOperatinoalState = Convert.ToInt32(tm.readTurbineOperatingStateValue(turbineId));
            log.InfoFormat("{0} Current Operating State: {1} onChangeValue: {2}", turbineId, turbineOperatinoalState, value);
            bool participationStatus = Convert.ToBoolean(tm.readTurbineParticipationStatus(turbineId));
            //If already paused by Articuno, then there's nothing to do
            if (IsTurbinePausedByArticuno(turbineId)) { }
            //If not paused by Aritcuno, then you need to check the operating state of the turbine...but NOT for turbines that have arleady been excluded
            else if (participationStatus)
            {
                //If turbine isn't in run or draft, then that means it is derated or in emergency, or something else. 
                //If the turbine is in either run or draft, then it meets condition. But make sure it also isn't in the list already
                if ((turbineOperatinoalState == RUN_STATE || turbineOperatinoalState == DRAFT_STATE))
                {
                    TurbineReadyToBePausedByArticuno(turbineId);
                    tm.setOperatingStateCondition(turbineId, true);
                    tm.ResetCtrValueForTurbine(turbineId);
                }
                else
                {
                    TurbineWaitingForProperCondition(turbineId);
                    tm.setOperatingStateCondition(turbineId, false);
                }
            }
            else { }
        }
        //Method that is executed when user checks/unchecks a turbine from participating in Articuno
        private static void CheckTurbineParticipationInArticuno(string turbineId, object value)
        {
            bool participationStatus = Convert.ToBoolean(tm.readTurbineParticipationStatus(turbineId));
            log.InfoFormat("Turbine {0} Participation in Articuno {1} OnChangeValue {2}", turbineId, participationStatus, value);
            //do nothing if turbine is already in paused by Articuno
            if (IsTurbinePausedByArticuno(turbineId)) { }
            //If turbine not paused by Articuno, then you check for participation status
            else
            {
                if (!participationStatus)
                {
                    MoveTurbineIdToAnotherList(turbineId, turbinesWaitingForPause, turbinesExcludedList);
                    MoveTurbineIdToAnotherList(turbineId, turbinesConditionNotMet, turbinesExcludedList);
                }
                else if (participationStatus)
                    MoveTurbineIdToAnotherList(turbineId, turbinesExcludedList, turbinesWaitingForPause);
            }
        }
        //This is a method that is triggered upon any value changes for certain OPC Tags
        private static void MetTowerTurbineOpcTagValueChanged(object sender, EasyDAItemChangedEventArgs e)
        {
            if (e.Succeeded)
            {
                string tag = e.Arguments.ItemDescriptor.ItemId;
                MetTowerAndTurbineValueChangeHandler(tag, e.Vtq.Value);
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
            - Articuno.TmpThreshold (Temperature Threshold)
            - Articuno.CurtailEna (Most important)
            - Articuno.EvalTm  (CTR Period should be updated in both the ArticunoMain and the Turbine classes)
            - Articuno.TmpDew (requires met tower to perform update)
         */
        private static void SystemTagValueChange(object sender, EasyDAItemChangedEventArgs e)
        {
            if (e.Succeeded)
            {
                string systemInputOpcTag = e.Arguments.ItemDescriptor.ItemId.ToString();
                int value = Convert.ToInt16(e.Vtq.Value);
                if (systemInputOpcTag.Equals(enableArticunoTag))
                {
                    articunoEnable = (value == ENABLE) ? true : false;
                    log.DebugFormat("Articuno is : {0}", articunoEnable ? "Enabled" : "Disabled");
                }
                if (systemInputOpcTag.Equals(articunoCtrTag))
                    ctrValueChanged(value);
                if (systemInputOpcTag.Equals(tempThresholdTag))
                {
                    mm.UpdateTemperatureThresholdForAllMetTowers(value);
                    log.DebugFormat("Articuno Temperature Threshold updated to: {0} deg C", value);
                }
                if (systemInputOpcTag.Equals(deltaThresholdTag))
                {
                    mm.writeDeltaThreshold(value);
                    log.DebugFormat("Articuno Temperature Delta updated to: {0} deg C", value);
                }

            }
            else { log.ErrorFormat("Error occured in systemInputOnChangeHandler with {0}. Msg: {1}", e.Arguments.ItemDescriptor.ItemId, e.ErrorMessageBrief); }
        }

        /// <summary>
        /// function to handle a change in CTR value. If the CTR value is more than 60 or less than 1, cap it.
        /// </summary>
        /// <param name="value"></param>
        private static void ctrValueChanged(int value)
        {
            if (value <= MIN_CTR_TIME)
                value = MIN_CTR_TIME;
            else if (value >= MAX_CTR_TIME)
                value = MAX_CTR_TIME;
            articunoCtrTime = value;
            ctrCountdown = value;
            tm.writeCtrTime(value);
            log.DebugFormat("Articuno CTR updated to: {0} minute", value);

        }

        private static bool IsTurbinePausedByArticuno(string turbineId) { return turbinesPausedByArticuno.Contains(turbineId); }

        private static void TurbineWaitingForProperCondition(string turbineId)
        {
            MoveTurbineIdToAnotherList(turbineId, turbinesWaitingForPause, turbinesConditionNotMet);
            LogCurrentTurbineStatusesInArticuno();
        }

        private static void TurbineReadyToBePausedByArticuno(string turbineId)
        {
            //If turbine is waiting to be paused and it isn't already paused by Articuno
            //If turbine is already paused by Articuno, don't do anything
            if (turbinesPausedByArticuno.Contains(turbineId)) { }
            //If turbine wasn't waiting for pause before nor was it in the excluded list, put it into the pause list
            else if (!turbinesWaitingForPause.Contains(turbineId))
            {
                MoveTurbineIdToAnotherList(turbineId, turbinesConditionNotMet, turbinesWaitingForPause);
            }
            //Log the Current status of the lists
            LogCurrentTurbineStatusesInArticuno();
        }

        /// <summary>
        /// Logs the content of the current internal lists in articuno.
        /// </summary>
        private static void LogCurrentTurbineStatusesInArticuno()
        {
            turbinesWaitingForPause.Sort();
            turbinesPausedByArticuno.Sort();
            turbinesExcludedList.Sort();
            turbinesConditionNotMet.Sort();
            log.InfoFormat("Turbines Waiting for Pause: {0}", string.Join(",", turbinesWaitingForPause.ToArray()));
            log.InfoFormat("Turbines paused by Articuno: {0}", string.Join(",", turbinesPausedByArticuno.ToArray()));
            log.InfoFormat("Turbines exlucded from Articuno: {0}", string.Join(",", turbinesExcludedList.ToArray()));
            log.InfoFormat("Turbines awaiting proper condition: {0}", string.Join(",", turbinesConditionNotMet.ToArray()));
        }

        /// <summary>
        /// A private method to help you move turbineId from one list to the next
        /// </summary>
        /// <param name="turbineId">Turbine ID (string) ex: T001</param>
        /// <param name="fromList">The list that turbine Id was in originally</param>
        /// <param name="newList">The list that the turbine Id will be moved to</param>
        private static void MoveTurbineIdToAnotherList(string turbineId, List<string> fromList, List<string> newList)
        {
            fromList.RemoveAll(x => x.Equals(turbineId));
            if (!newList.Contains(turbineId))
                newList.Add(turbineId);
        }
    }
}
