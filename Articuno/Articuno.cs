﻿using OpcLabs.EasyOpc.DataAccess;
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

namespace Articuno {
    sealed internal class Articuno {
        /*
         * These are Lists that are used to keep turbines organized on a site level using their Prefixes (ie T001)
         * By default, all turbine prefixes should be in the waitingForPause list, but if the state is not 100
         * Then throw it out of the waiting list and into the conditionsNotMet list until the appriate conditions
         * 
         * Note that all the lists are strings, meaning it works ONLY with Turbine Prefixes
         */

        private static string opcServerName;
        private static int articunoCtrTime;
        private static int ctrCountdown;
        private static bool articunoEnable;
        private static string sitePrefix;

        private static string tempThresholdTag;
        private static string enableArticunoTag;
        private static string articunoCtrTag;
        private static string deltaThresholdTag;
        private static string heartBeatTag;
        static string icePossibleAlarmTag;
        static string numTurbinesPausedTag;

        private static int ONE_MINUTE_POLLING = 60 * 1000;
        private static int HEARTBEAT_POLLING = 5 * 1000;
        private readonly static int NORMAL_UPDATE_RATE = 1000;
        private readonly static int FAST_UPDATE_RATE = 100;
        private static int ACTIVE_NOISE_LEV = 0;
        private static int RUN_STATE = 100;
        private static int DRAFT_STATE = 75;
        private static int PAUSE_STATE = 50;

        private static int MAX_CTR_TIME = 60;
        private static int MIN_CTR_TIME = 1;
        private static int ENABLE = 1;

        private static DatabaseInterface dbi;
        private static MetTowerMediator mm;
        private static TurbineMediator tm;

        //Used a faux proxy for testing until a better stub can  be built
        public Articuno(bool testMode) {
            dbi = DatabaseInterface.Instance;
            mm = MetTowerMediator.Instance;
            tm = TurbineMediator.Instance;

            mm.MaxQueueSize = (HEARTBEAT_POLLING * articunoCtrTime);
            mm.CreateMetTowerObject();
            tm.MaxQueueSize = (HEARTBEAT_POLLING * articunoCtrTime);
            tm.createTurbines(articunoCtrTime);

            SetUpOpcSystemTags();
        }

        public static void Main(object sender, FileSystemEventArgs e) {
            dbi = DatabaseInterface.Instance;
            mm = MetTowerMediator.Instance;
            tm = TurbineMediator.Instance;


            opcServerName = dbi.getOpcServerName();
            sitePrefix = dbi.getSitePrefixValue();
            SetUpOpcSystemTags();

            //Queue size is set up so it is depending on the CTR time and the Heartbeat (in sec)
            mm.MaxQueueSize = ((HEARTBEAT_POLLING / 1000) * articunoCtrTime);
            mm.CreateMetTowerObject();
            tm.MaxQueueSize = ((HEARTBEAT_POLLING / 1000) * articunoCtrTime);
            tm.createTurbines(articunoCtrTime);

            setOpcTagListenerForTurbineAndMetTower();
            StartArticuno();
        }

        public static void StartArticuno() {
            var periodTimeSpan = TimeSpan.FromMilliseconds(ONE_MINUTE_POLLING);
            var heartBeatTimeSpan = TimeSpan.FromMilliseconds(HEARTBEAT_POLLING);

            SetOneMinuteTimer(periodTimeSpan);
            SetHeartBeatTimer(heartBeatTimeSpan);
        }

        public static void SetUpOpcSystemTags() {
            sitePrefix = dbi.getSitePrefixValue();
            opcServerName = dbi.getOpcServerName();

            setSystemInputTags();
            setSystemOutputTags();
        }

        /// <summary>
        /// Method executed from the TurbineMediator class. This should only be used to signal the main program that a turbine is paused
        /// </summary>
        /// <param name="turbineId"></param>
        public static void turbinePausedByArticuno(string turbineId) {
            ArticunoLogger.DataLogger.Debug("ArticunoMain has detected Turbine {0} has paused. ", turbineId);
            updateNumberOfTurbinesPaused();
        }

        /// <summary>
        /// Method executed from the TurbineMediator class. This should only be used to signal the main program that a turbine is cleared
        /// </summary>
        public static void turbineClearedOfIce(string turbineId) {
            ArticunoLogger.DataLogger.Debug("ArticunoMain has detected Turbine {0} has started running from the site.", turbineId);
            ArticunoLogger.DataLogger.Debug("Clearing Articuno Stop Alarm for {0}", turbineId);
            tm.GetTurbine(turbineId).SetPausedByArticunoAlarmValue(false);
            updateNumberOfTurbinesPaused();
        }

        public static bool isAlreadyPaused(string turbineId) {
            var turbinesPausedByArticuno = tm.GetTurbineIdsPausedByArticuno();
            ArticunoLogger.DataLogger.Info("Turbine {0} Paused status: {1} ", turbineId, turbinesPausedByArticuno.Contains(turbineId));
            ArticunoLogger.GeneralLogger.Info("Turbine {0} Paused status: {1} ", turbineId, turbinesPausedByArticuno.Contains(turbineId));
            return turbinesPausedByArticuno.Contains(turbineId);
        }

        public static bool isUccActive() { return OpcServer.isActiveUCC(); }
        private static void SetHeartBeatTimer(TimeSpan heartBeatTimeSpan) {
            System.Timers.Timer heartBeatTimer = new System.Timers.Timer(heartBeatTimeSpan.TotalMilliseconds);
            heartBeatTimer.AutoReset = true;
            heartBeatTimer.Elapsed += new System.Timers.ElapsedEventHandler(updateHeartBeat);
            heartBeatTimer.Start();
        }

        private static void SetOneMinuteTimer(TimeSpan periodTimeSpan) {
            System.Timers.Timer minuteTimer = new System.Timers.Timer(periodTimeSpan.TotalMilliseconds);
            minuteTimer.AutoReset = true;
            minuteTimer.Elapsed += new System.Timers.ElapsedEventHandler(minuteUpdate);
            minuteTimer.Start();
        }

        private static void setSystemInputTags() {
            var systemInputClient = new EasyDAClient();
            systemInputClient.ItemChanged += SystemTagValueChange;

            List<DAItemGroupArguments> systemInputTags = new List<DAItemGroupArguments>();
            tempThresholdTag = sitePrefix + dbi.getTemperatureThresholdTag();
            enableArticunoTag = sitePrefix + dbi.getArticunoEnableTag();
            articunoCtrTag = sitePrefix + dbi.getArticunoCtrTag();
            articunoCtrTime = Convert.ToInt32(OpcServer.readAnalogTag(opcServerName, articunoCtrTag));
            deltaThresholdTag = sitePrefix + dbi.GetDeltaThresholdTag();

            systemInputTags.Add(new DAItemGroupArguments("", opcServerName, tempThresholdTag, NORMAL_UPDATE_RATE, null));
            systemInputTags.Add(new DAItemGroupArguments("", opcServerName, enableArticunoTag, NORMAL_UPDATE_RATE, null));
            systemInputTags.Add(new DAItemGroupArguments("", opcServerName, articunoCtrTag, NORMAL_UPDATE_RATE, null));
            systemInputTags.Add(new DAItemGroupArguments("", opcServerName, deltaThresholdTag, NORMAL_UPDATE_RATE, null));
            systemInputClient.SubscribeMultipleItems(systemInputTags.ToArray());
        }
        private static void setSystemOutputTags() {
            heartBeatTag = sitePrefix + dbi.GetArticunoHeartbeatTag();
            icePossibleAlarmTag = sitePrefix + dbi.GetArticunoIcePossibleOpcTag();
            numTurbinesPausedTag = sitePrefix + dbi.GetArticunoNumbersOfTurbinesPausedTag();
        }

        /// <summary>
        /// Function to set up input tag listeners on both the met tower and turbines
        /// </summary>
        private static void setOpcTagListenerForTurbineAndMetTower() {
            //An OPC client that will respond to Turbine OPC tag value Changes for operating state, partiicipation and NrsMode. Essentually an event listener
            var assetStatusClient = new EasyDAClient();
            assetStatusClient.ItemChanged += MetTowerTurbineOpcTagValueChangedEventListener;
            assetStatusClient.SubscribeMultipleItems(getTurbineTagsForListening().ToArray());
            assetStatusClient.SubscribeMultipleItems(getMetTagsForListening().ToArray());
        }

        private static List<DAItemGroupArguments> getTurbineTagsForListening() {
            List<DAItemGroupArguments> turbineInputTags = new List<DAItemGroupArguments>();
            foreach (string turbinePrefix in tm.getTurbinePrefixList()) {
                try {
                    turbineInputTags.Add(new DAItemGroupArguments("",
                        opcServerName, tm.getOperatingStateTag(turbinePrefix), FAST_UPDATE_RATE, null));
                    turbineInputTags.Add(new DAItemGroupArguments("",
                        opcServerName, tm.getParticipationState(turbinePrefix), FAST_UPDATE_RATE, null));
                    turbineInputTags.Add(new DAItemGroupArguments("",
                        opcServerName, tm.getStartCommandTag(turbinePrefix), FAST_UPDATE_RATE, null));

                    //Only add the Nrs Tag to listener if the config database is configured for it
                    if (!tm.isNrsTagEmpty(turbinePrefix))
                        turbineInputTags.Add(new DAItemGroupArguments("",
                            opcServerName, tm.getNrsStateTag(turbinePrefix), FAST_UPDATE_RATE, null));
                }
                catch (Exception e) {
                    ArticunoLogger.DataLogger.Error(e, "Error when attempting to add to assetInputTags list. {0}", e);
                }
            }
            return turbineInputTags;
        }

        private static List<DAItemGroupArguments> getMetTagsForListening() {
            DataTable reader = dbi.readQuery("SELECT Switch from MetTowerInputTags");
            List<DAItemGroupArguments> metTowerInputTags = new List<DAItemGroupArguments>();
            for (int i = 0; i < reader.Rows.Count; i++) {
                var switchTag = reader.Rows[i]["Switch"].ToString();
                try {
                    metTowerInputTags.Add(new DAItemGroupArguments("", opcServerName, sitePrefix + reader.Rows[i]["Switch"].ToString(), NORMAL_UPDATE_RATE, null));
                }
                catch (Exception e) { ArticunoLogger.DataLogger.Error("Error when attempting to add {0} to assetInputTags list. {1}", switchTag, e); }
            }
            return metTowerInputTags;
        }

        private static void updateHeartBeat(object sender, ElapsedEventArgs e) {
            if (isUccActive()) {
                OpcServer.writeOpcTag(opcServerName, heartBeatTag, !ReadHeartBeatTagValue());
            }
            if (isArticunoEnabled())
                gatherTemperatureAndHumiditySamples();
        }

        private static bool isArticunoEnabled() { return Convert.ToBoolean(OpcServer.readBooleanTag(opcServerName, enableArticunoTag)); }

        private static bool ReadHeartBeatTagValue() { return Convert.ToBoolean(OpcServer.readBooleanTag(opcServerName, heartBeatTag)); }
        private static void gatherTemperatureAndHumiditySamples() {
            //For every heartbeat interval, read the met tower measurements and the turbine temperature measurements
            //This is so more measurements can be gathered to get a more accurate average after every CTR period
            for (int i = 1; i <= MetTowerMediator.GetNumberOfMetTowers(); i++) {
                //This is needed because apparently Met Tower 1 is unnumbered, and so the following strips the '1' essentually. 
                string j = (i == 1) ? "" : Convert.ToString(i);
                //Get all measurements from the met tower. Note that it will get turbine 
                //temperature if the temperature coming from the met tower is bad qualtiy

                double temperature = mm.ReadTemperatureFromMetTower("Met" + j);
                double humidity = mm.readHumidity("Met" + j);

                mm.writeToQueue("Met" + j, temperature, humidity);
            }

            //Call the storeWindSpeed function to store a wind speed average into a turbine queue (for all turbines in the list)
            Parallel.ForEach(tm.getTurbinePrefixList(), p => tm.sampleWindAndRotorSpeeds(p));

        }
        /// <summary>
        /// Function to handle tasks that should be executed every minute (ie get temperature measurements) and every CTR minute (ie check rotor speed, run calculations, etc.) 
        /// </summary>
        private static void minuteUpdate(object sender, ElapsedEventArgs e) {
            ctrCountdown--;
            if (ctrCountdown <= 0)
                calculateMetTowerAverages();

            if (isArticunoEnabled()) {

                tm.decrementTurbineCtrTime();
                tm.UpdateDisplayValuesForAllTurbine();
                mm.GetMetTowerList().ForEach(m => {
                    var h = mm.calculateCtrAvgHumidity(m);
                    var t = mm.calculateCtrAvgTemperature(m);
                    mm.updateDewPoint(m, t, h);
                });
                //Write the MetTower CTR to the tag
                mm.UpdateCtrCountdown(ctrCountdown);
            }

            LogCurrentTurbineStatusesInArticuno();
        }

        private static void calculateMetTowerAverages() {
            ArticunoLogger.DataLogger.Info("CTR countdown reached 0 in ArticunoMain");
            ArticunoLogger.GeneralLogger.Info("CTR countdown reached 0 in ArticunoMain");
            mm.CalculateAverage();
            OpcServer.writeOpcTag(opcServerName, icePossibleAlarmTag, mm.IsAnyMetTowerFrozenAtSite());
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
        private static void MetTowerAndTurbineValueChangeHandler(string opcTag, Object value) {
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

        private static void MetTowerOpcTagValueChangeListener(string prefix, string opcTag) {
            Enum metEnum = mm.FindMetTowerTag(prefix, opcTag);
            switch (metEnum) {
                case MetTowerMediator.MetTowerEnum.Switched:
                    string referencedMet = mm.isMetTowerSwitched(prefix);
                    ArticunoLogger.DataLogger.Info("{0} switched to {1}", prefix, referencedMet);
                    ArticunoLogger.GeneralLogger.Info("{0} switched to {1}", prefix, referencedMet);
                    break;
                default:
                    ArticunoLogger.DataLogger.Debug("Event Changed detected for {0}. However, there is nothing to be done", opcTag);
                    break;
            }
        }

        private static void TurbineOpcTagValueChangeListener(string prefix, string opcTag, object value) {
            //DO NOT FORGET THIS FUNCTION when adding anythign turbine enum related
            //TODO: Refactor this so you are not dependent on the switch statement here
            Enum turbineEnum = tm.findTurbineTag(prefix, opcTag);
            switch (turbineEnum) {
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
                    CheckTurbineParticipationInArticunoEventListener(prefix, value);
                    break;
                default:
                    ArticunoLogger.DataLogger.Debug("Event CHanged detected for {0}. However, there is nothing to be doen", opcTag);
                    break;
            }
        }

        /// <summary>
        /// Checks the NRS upon value change. Should 
        /// If nrs tag doesn't exist, Then it shouldn't do anything. In that case, just write a non-active value
        /// </summary>
        /// <param name="turbineId"></param>
        /// <param name="value"></param>
        private static void CheckTurbineNrsStatus(string turbineId, object value) {
            //Site might not be set up for NRS, so check to see if the tag is empty, if it is, just set the condition to false
            if (tm.getNrsStateTag(turbineId).Equals("")) { tm.setNrsActive(turbineId, false); }
            else {
                if (Convert.ToInt16(value) == ACTIVE_NOISE_LEV)
                    tm.setNrsActive(turbineId, true);
                else
                    tm.setNrsActive(turbineId, false);
            }
        }

        private static void StartTurbineCommandSentFromSiteOrNcc(string turbineId, object startCommand) {
            if (Convert.ToBoolean(startCommand) == true) {
                ArticunoLogger.DataLogger.Info("Turbine {0} has started from NCC or site", turbineId);
                ArticunoLogger.GeneralLogger.Info("Turbine {0} has started from NCC or site", turbineId);
                if (IsTurbinePausedByArticuno(turbineId)) {
                    turbineClearedOfIce(turbineId);
                    LogCurrentTurbineStatusesInArticuno();
                }
            }

        }
        private static void CheckTurbineOperatingState(string turbineId, object value) {
            int turbineOperatinoalState = Convert.ToInt32(tm.readTurbineOperatingStateValue(turbineId));
            ArticunoLogger.DataLogger.Info("{0} Current Operating State: {1} onChangeValue: {2}", turbineId, turbineOperatinoalState, value);
            ArticunoLogger.GeneralLogger.Info("{0} Current Operating State: {1} onChangeValue: {2}", turbineId, turbineOperatinoalState, value);
            bool participationStatus = Convert.ToBoolean(tm.readTurbineParticipationStatus(turbineId));
            //If already paused by Articuno, then there's nothing to do
            if (tm.GetTurbineIdsPausedByArticuno().Contains(turbineId)) { }
            //If not paused by Aritcuno, then you need to check the particpation status and then the operating state of the turbine
            else if (participationStatus) {
                //If turbine isn't in run or draft, then that means it is derated or in emergency, or something else. 
                //If the turbine is in either run or draft, then it meets condition. But make sure it also isn't in the list already
                if ((turbineOperatinoalState == RUN_STATE || turbineOperatinoalState == DRAFT_STATE)) {
                    tm.setOperatingStateCondition(turbineId, true);
                    tm.ResetCtrValueForTurbine(turbineId);
                }
                else {
                    tm.setOperatingStateCondition(turbineId, false);
                }
            }
            else { }
        }
        private static void CheckTurbineParticipationInArticunoEventListener(string turbineId, object value) {
            bool participationStatus = Convert.ToBoolean(tm.readTurbineParticipationStatus(turbineId));
            ArticunoLogger.DataLogger.Info("Turbine {0} Participation in Articuno {1} OnChangeValue {2}", turbineId, participationStatus, value);
            ArticunoLogger.GeneralLogger.Info("Turbine {0} Participation in Articuno {1} OnChangeValue {2}", turbineId, participationStatus, value);
        }

        private static void MetTowerTurbineOpcTagValueChangedEventListener(object sender, EasyDAItemChangedEventArgs e) {
            if (e.Succeeded) {
                string tag = e.Arguments.ItemDescriptor.ItemId;
                MetTowerAndTurbineValueChangeHandler(tag, e.Vtq.Value);
            }
            else { ArticunoLogger.DataLogger.Error("Error occured in MetTowerTurbineOpcTagValueChangedEventListener with {0}. Msg: {1}", e.Arguments.ItemDescriptor.ItemId, e.ErrorMessageBrief); }

        }

        /// <summary>
        /// method that handles system input tag changes such as whether Articuno is enabled or not, Threshold, CTR Period, etc.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void SystemTagValueChange(object sender, EasyDAItemChangedEventArgs e) {
            if (e.Succeeded) {
                string systemInputOpcTag = e.Arguments.ItemDescriptor.ItemId.ToString();
                int value = Convert.ToInt16(e.Vtq.Value);
                if (systemInputOpcTag.Equals(enableArticunoTag)) {
                    articunoEnable = (value == ENABLE) ? true : false;
                    ArticunoLogger.DataLogger.Debug("Articuno is : {0}", articunoEnable ? "Enabled" : "Disabled");
                }
                if (systemInputOpcTag.Equals(articunoCtrTag)) {
                    ctrValueChanged(value);
                }
                if (systemInputOpcTag.Equals(tempThresholdTag)) {
                    mm.UpdateTemperatureThresholdForAllMetTowers(value);
                    ArticunoLogger.DataLogger.Debug("Articuno Temperature Threshold updated to: {0} deg C", value);
                }
                if (systemInputOpcTag.Equals(deltaThresholdTag)) {
                    mm.writeDeltaThreshold(value);
                    ArticunoLogger.DataLogger.Debug("Articuno Temperature Delta updated to: {0} deg C", value);
                }

            }
            else { ArticunoLogger.DataLogger.Error("Error occured in systemInputOnChangeHandler with {0}. Msg: {1}", e.Arguments.ItemDescriptor.ItemId, e.ErrorMessageBrief); }
        }

        /// <summary>
        /// function to handle a change in CTR value. If the CTR value is more than 60 or less than 1, cap it.
        /// </summary>
        /// <param name="value"></param>
        private static void ctrValueChanged(int value) {
            if (value <= MIN_CTR_TIME)
                value = MIN_CTR_TIME;
            else if (value >= MAX_CTR_TIME)
                value = MAX_CTR_TIME;
            articunoCtrTime = value;
            ctrCountdown = value;
            tm.writeCtrTime(value);
            ArticunoLogger.DataLogger.Debug("Articuno CTR updated to: {0} minute", value);

        }

        private static bool IsTurbinePausedByArticuno(string turbineId) => tm.getTurbinePrefixList().Where(p => tm.IsTurbinePausedByArticuno(p)).Contains(turbineId);

        /// <summary>
        /// Logs the current statues of the turbines from Articuno's perspective
        /// </summary>
        private static void LogCurrentTurbineStatusesInArticuno() {


            var turbinesWaitingForPause = tm.GetTurbineIdsWaitingForPause();
            var turbinesPausedByArticuno = tm.GetTurbineIdsPausedByArticuno();

            var turbinesExcludedList = tm.GetTurbineIdsExcludedList();
            var turbinesConditionNotMet = tm.GetTurbineIdsConditionNotMet();

            ArticunoLogger.DataLogger.Debug("Turbines Waiting for Pause: {0}", string.Join(",", turbinesWaitingForPause));
            ArticunoLogger.DataLogger.Debug("Turbines paused by Articuno: {0}", string.Join(",", turbinesPausedByArticuno));
            ArticunoLogger.DataLogger.Debug("Turbines exlucded from Articuno: {0}", string.Join(",", turbinesExcludedList));
            ArticunoLogger.DataLogger.Debug("Turbines awaiting proper condition: {0}", string.Join(",", turbinesConditionNotMet));
        }

        /// <summary>
        /// A private method to help you move turbineId from one list to the next
        /// </summary>
        /// <param name="turbineId">Turbine ID (string) ex: T001</param>
        /// <param name="fromList">The list that turbine Id was in originally</param>
        /// <param name="newList">The list that the turbine Id will be moved to</param>
        private static void updateNumberOfTurbinesPaused() {
            var turbinesPausedByArticuno = tm.GetTurbineIdsPausedByArticuno();
            var numTurbPaused = turbinesPausedByArticuno.Count;
            ArticunoLogger.DataLogger.Debug("Turbines paused by Articuno: {0}", string.Join(",", turbinesPausedByArticuno));
            OpcServer.writeOpcTag(opcServerName, numTurbinesPausedTag, numTurbPaused);
        }

    }
}
