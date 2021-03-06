﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpcLabs.EasyOpc.DataAccess;
using OpcLabs.EasyOpc.OperationModel;

namespace Articuno {
    /// <summary>
    /// The turbine class exposes OPC tags that are relevant to the program.
    /// You can set the tags via the setXXXXXTag method and get the value of said tag via the getXXXXXXValue method
    /// </summary>

    /*
     * This is the turbine class. It represents a turbine object in Articuno 
     * It probably should be an interface...but having an interface to create just one type of turbine seems kinda redundant. That's why I have a factory class
     */
    sealed internal class Turbine {
        //Instance of OPC server
        string OpcServerName;

        //Member variables for algorithm
        private bool temperatureConditionMet;
        private bool operatingStateConditionMet;
        private bool turbinePerformanceConditionMet;
        private static TurbineMediator tm;

        //CTR Time Remaining. This is used to count down to zero. Don't 
        private int ctrRemaining;

        //Queues
        //private Queue<Double> windSpeedQueue;
        //private Queue<Double> rotorSpeedQueue;

        private CircularQueue<Double> windSpeedQueue;
        private CircularQueue<Double> rotorSpeedQueue;

        //Other fields
        private string RELEASE = "Release";
        private string CURTAIL = "Curtail";

        //Constants
        private readonly double AGC_BLOCK_COMMAND = 0.00;
        private readonly double AGC_UNBLOCK_COMMAND = 1.00;
        public static readonly int NRS_NOT_ACTIVE = 5;
        public static readonly int NRS_ACTIVE = 0;

        //Constructors
        public Turbine(string prefix, String OpcServerName) {
            this.TurbinePrefix = prefix;
            this.OpcServerName = OpcServerName;
            //windSpeedQueue = new Queue<double>();
            //rotorSpeedQueue = new Queue<double>();

            tm = TurbineMediator.Instance;
        }

        public void initializeQueue() {
            windSpeedQueue = new CircularQueue<double>(tm.MaxQueueSize);
            rotorSpeedQueue = new CircularQueue<double>(tm.MaxQueueSize);
        }

        //Methods to read the value for the wind speed, rotor speed, etc. value from the OPC Server
        public Object readTurbineWindSpeedValue() { return OpcServer.readAnalogTag(OpcServerName, WindSpeedTag); }
        public Object readTurbineRotorSpeedValue() { return OpcServer.readAnalogTag(OpcServerName, RotorSpeedTag); }
        public Object readTurbineOperatingStateValue() { return OpcServer.readAnalogTag(OpcServerName, OperatingStateTag); }


        public Object readTurbineTemperatureValue() { return OpcServer.readAnalogTag(OpcServerName, TemperatureTag); }

        public Object readTurbineScalingFactorValue() { return ScalingFactorValue; }
        public Boolean isTurbineParticipating() { return Convert.ToBoolean(OpcServer.readBooleanTag(OpcServerName, ParticipationTag)); }
        public Object readStoppedByArticunoAlarmValue() { return OpcServer.readAnalogTag(OpcServerName, StoppedAlarmTag); }
        public Object readTurbineCtrTimeRemaining() => Convert.ToInt32(OpcServer.readAnalogTag(OpcServerName, CtrCountdownTag));
        public Boolean readTurbineLowRotorSpeedFlagValue() { return Convert.ToBoolean(OpcServer.readBooleanTag(OpcServerName, LowRotorSpeedFlagTag)); }
        public Object readAgcBlockValue() { return OpcServer.readBooleanTag(OpcServerName, AgcBlockingTag); }

        //public Accessors (Getters and Setters)  to set the member variables to the  OPC tag
        public string WindSpeedTag { set; get; }
        public string RotorSpeedTag { set; get; }
        public string OperatingStateTag { set; get; }
        public string NrsStateTag { set; get; }
        public string StartCommandTag { internal set; get; }
        public string TurbineDefaultCtr { set; get; }
        public string TemperatureTag { set; get; }
        public string TurbineHumidityTag { set; get; }
        public string ParticipationTag { set; get; }
        public string StoppedAlarmTag { set; get; }
        public string TurbinePrefix { set; get; }
        public string DeRate { set; get; }
        public string StartupTime { get; set; }

        //THese four tags are meant for Articuno to write to.
        public string LoadShutdownTag { set; get; }
        public string AgcBlockingTag { set; get; }
        public string LowRotorSpeedFlagTag { get; set; }
        public string CtrCountdownTag { get; set; }
        public string ScalingFactorValue { get; set; }
        public string AvgRotorSpeedTag { get; set; }
        public string AvgWindSpeedTag { get; set; }

        public void writeTurbineCtrValue(int articunoCtrValue) {
            TurbineDefaultCtr = articunoCtrValue.ToString();
            ctrRemaining = articunoCtrValue;
            OpcServer.writeOpcTag(OpcServerName, CtrCountdownTag, articunoCtrValue);
        }

        public double writeTurbineLoadShutdownCommand() {
            ArticunoLogger.DataLogger.Debug("Shutdown command for {0} has been sent", this.TurbinePrefix);
            try {
                OpcServer.writeOpcTag(OpcServerName, this.LoadShutdownTag, 1.00);
                return 1.0;
            }
            catch (OpcException opcException) {
                ArticunoLogger.DataLogger.Error("Error stropping {0}: {1}", this.TurbinePrefix, opcException.GetBaseException().Message);
                return -1.0;
            }
        }
        public void writeTurbineNoiseLevel(Object value) {
            if (NrsStateTag.Equals("")) { }
            else
                OpcServer.writeOpcTag(OpcServerName, NrsStateTag, Convert.ToDouble(value));
            //Reset CTR on noise level change. This is mandatory
            resetTurbineCtrTime();
        }
        public void writeOperatingState(Object value) { OpcServer.writeOpcTag(OpcServerName, OperatingStateTag, Convert.ToDouble(value)); }
        public void decrementTurbineCtrTime() {
            ctrRemaining--;
            ArticunoLogger.DataLogger.Info("{0} Current CTR: {1}", GetTurbinePrefixValue(), ctrRemaining);
            ArticunoLogger.GeneralLogger.Info("{0} Current CTR: {1}", GetTurbinePrefixValue(), ctrRemaining);
            OpcServer.writeOpcTag(OpcServerName, CtrCountdownTag, ctrRemaining);

            // When  a turbine is started, additional startup time is needed. The time after startup will be greather than the TurbineCtr time.
            // During this startup period, you should not store the data in the queues as they're unreliable
            if (ctrRemaining > Convert.ToInt32(TurbineDefaultCtr))
                emptyQueue();
            if (ctrRemaining <= 0) {
                ArticunoLogger.DataLogger.Info("CTR period for Turbine {0} reached Zero.", GetTurbinePrefixValue());
                ArticunoLogger.GeneralLogger.Info("CTR period for Turbine {0} reached Zero.", GetTurbinePrefixValue());
                resetTurbineCtrTime();
                tm.RotorSpeedCheck(GetTurbinePrefixValue());
                if (tm.isUccActive())
                    CheckArticunoPausingConditions();
            }
        }
        public bool IsTurbineNrsInActiveMode() {
            int state = Convert.ToInt32(readNrsStateValue());
            if (state == NRS_ACTIVE)
                return true;
            else
                return false;
        }

        //The following five fucntions are set by the main Articuno class. They show if each of the four/five algorithms are true
        public void SetTemperatureCondition(bool state) { this.temperatureConditionMet = state; }
        public void SetOperatingStateCondition(bool state) { this.operatingStateConditionMet = state; }

        /// <summary>
        /// Method called after a NRS mode has been changed by the site. Changing the NRS condition also resets the CTR and clears all stored queues
        /// </summary>
        public void TurbineNrsModeChanged(bool state) {
            resetTurbineCtrTime();
            emptyQueue();
        }
        public void SetTurbineUnderPerformanceCondition(bool state) {
            //Do not set the turbine underperformance flag if turbine isn't participating in Articuno
            //as this will trigger an unwanted alarm by multiple parties
            if (!isTurbineParticipating())
                state = false;
            turbinePerformanceConditionMet = state;
            OpcServer.writeOpcTag(OpcServerName, this.LowRotorSpeedFlagTag, state);
        }

        public string MainMetTowerReference { set; get; }

        public void CheckArticunoPausingConditions() {

            bool frozenCondition = isTurbineParticipating() && temperatureConditionMet && operatingStateConditionMet && turbinePerformanceConditionMet;
            ArticunoLogger.DataLogger.Debug("Checking ice condition for {5}.  Paused by Articuno: {0}\nTurbine Participation?: {1} " +
                "Icy Temp Condition?: {2}, OperatingState: {3}, Low TurbinePerf Condition?: {4}", frozenCondition,
                isTurbineParticipating(), temperatureConditionMet, operatingStateConditionMet, turbinePerformanceConditionMet, GetTurbinePrefixValue());

            if (frozenCondition) {
                ArticunoLogger.DataLogger.Debug("Icing conditions satisfied for {0}", GetTurbinePrefixValue());
                pauseByArticuno(true);
            }
            else {
                ArticunoLogger.DataLogger.Debug("No ice detected for turbine {0}", GetTurbinePrefixValue());
                pauseByArticuno(false);
            }
        }

        //For Wind speed and Rotor Speed queues. 
        public void addWindSpeedToQueue(double windSpeed) { windSpeedQueue.Enqueue(windSpeed); }
        public void addRotorSpeedToQueue(double rotorSpeed) { rotorSpeedQueue.Enqueue(rotorSpeed); }
        public Queue<double> getWindSpeedQueue() => windSpeedQueue;
        public Queue<double> getRotorSpeedQueue() => rotorSpeedQueue;

        public void UpdateDisplayValue() {
            try {
                var rotorSpeed = GetAvgRotorSpeed();
                var windSpeed = GetAvgWindSpeed();
                ArticunoLogger.DataLogger.Debug("{0} avg wind speed: {1}, rotor speed: {2}", GetTurbinePrefixValue(), windSpeed, rotorSpeed);
            }
            catch (Exception e) {
                ArticunoLogger.DataLogger.Error(" No data stored in  {0}'s wind speed queue (size: {1}) or rotor speed queue (size: {2})",
                    GetTurbinePrefixValue(), windSpeedQueue.Count, rotorSpeedQueue.Count);
            }
        }
        /// <summary>
        /// Method call to trim all content of a turbine's wind speed queue and rotor speed queue
        /// </summary>
        public void emptyQueue() {
            windSpeedQueue.Clear();
            rotorSpeedQueue.Clear();
        }

        /// <summary>
        /// Start the turbine. This function clears its alarm, reset its CTRCount and empty its queue
        /// </summary>
        public void startTurbine() {
            //Unblock Turbine from AGC
            BlockTurbineFromAGC(false);

            LogTurbineCurtailmentRecord(TurbinePrefix, RELEASE);
            ArticunoLogger.DataLogger.Debug("Start Command Received for Turbine {0}", GetTurbinePrefixValue());
            //Give the turbine some time to start 
            int startupTime = Convert.ToInt32(StartupTime);
            ArticunoLogger.DataLogger.Debug("Giving additional {0} minutes to allow turbine to start up....", startupTime);
            SetPausedByArticunoAlarmValue(false);
            emptyQueue();
            ArticunoLogger.DataLogger.Info("Turbine {0} has started", GetTurbinePrefixValue());
            ArticunoLogger.GeneralLogger.Info("Turbine {0} has started", GetTurbinePrefixValue());
            ArticunoLogger.DataLogger.Info("Turbine {0} CTR Value reset to: {1}", GetTurbinePrefixValue(), (Convert.ToInt32(TurbineDefaultCtr)) + startupTime);
            ArticunoLogger.GeneralLogger.Info("Turbine {0} CTR Value reset to: {1}", GetTurbinePrefixValue(), (Convert.ToInt32(TurbineDefaultCtr)) + startupTime);
            resetTurbineCtrTime(startupTime);
        }

        public static double CalculateAverageRotorSpeed(Queue<double> rsq) => rsq.Count > 0 ? Math.Round(rsq.Average(), 3) : 0.0;
        public static double CalculateAverageWindSpeed(Queue<double> wsq) => wsq.Count > 0 ? Math.Round(wsq.Average(), 3) : 0.0;

        public void resetTurbineCtrTime(int startupTime = 0) {
            double currentCtr = Convert.ToInt32(readTurbineCtrTimeRemaining());
            double turbineDefaultCtrTime = Convert.ToInt32(TurbineDefaultCtr);
            //If the current CTR is larger than the default TurbineCtr, then don't reset it as that means turbine was recently start
            if (turbineDefaultCtrTime >= currentCtr) {
                ctrRemaining = Convert.ToInt32(TurbineDefaultCtr) + startupTime;
                OpcServer.writeOpcTag(OpcServerName, CtrCountdownTag, ctrRemaining);
            }
        }

        private Object readNrsStateValue() {
            if (NrsStateTag.Equals("")) { return NRS_NOT_ACTIVE; }
            else { return OpcServer.readAnalogTag(OpcServerName, NrsStateTag); }
        }

        //Misc functions
        public string GetTurbinePrefixValue() { return this.TurbinePrefix; }

        //Write Articuno Alarm
        public void SetPausedByArticunoAlarmValue(Object value) { OpcServer.writeOpcTag(OpcServerName, StoppedAlarmTag, Convert.ToBoolean(value)); }

        /// <summary>
        /// Method used to trigger a pausing condition due to ice.
        /// </summary>
        /// <param name="state"></param>
        private void pauseByArticuno(bool state) {
            if (state) {
                if (!tm.IsTurbinePausedByArticuno(TurbinePrefix)) {
                    LogTurbineCurtailmentRecord(TurbinePrefix, CURTAIL);
                    ArticunoLogger.CurtailmentLogger.Debug("Turbine {0} is getting paused by Articuno", TurbinePrefix);
                    //Block Turbine in AGC
                    BlockTurbineFromAGC(true);
                    ArticunoLogger.DataLogger.Debug("Sending pause commmand for {0}", GetTurbinePrefixValue());
                    writeTurbineLoadShutdownCommand();
                    ArticunoLogger.DataLogger.Debug("Writing alarm for {0}", GetTurbinePrefixValue());
                    SetPausedByArticunoAlarmValue(true);
                    tm.UpdateArticunoMain(TurbineMediator.TurbineEnum.PausedByArticuno, TurbinePrefix);
                    ArticunoLogger.DataLogger.Info("Turbine {0} is now paused", GetTurbinePrefixValue());
                    ArticunoLogger.GeneralLogger.Info("Turbine {0} is now paused", GetTurbinePrefixValue());
                }
            }
        }

        /// <summary>
        /// function to block the turbine from AGC.
        /// </summary>
        /// <param name="state"></param>
        private void BlockTurbineFromAGC(bool state) {
            if (state)
                OpcServer.writeOpcTag(OpcServerName, AgcBlockingTag, Convert.ToDouble(AGC_BLOCK_COMMAND));
            else
                OpcServer.writeOpcTag(OpcServerName, AgcBlockingTag, Convert.ToDouble(AGC_UNBLOCK_COMMAND));
        }


        public double GetAvgRotorSpeed() {
            double avgRotorSpeed = 0.0;
            try {
                if (rotorSpeedQueue.Count > 0) {
                    avgRotorSpeed = CalculateAverageRotorSpeed(rotorSpeedQueue);
                    OpcServer.writeOpcTag(OpcServerName, AvgRotorSpeedTag, avgRotorSpeed);
                }
                else
                    ArticunoLogger.DataLogger.Debug("No rotor speed samples for {0}. Returning zero",GetTurbinePrefixValue());
                return avgRotorSpeed;
            }
            catch (Exception e) {
                ArticunoLogger.DataLogger.Error("There is no data stored in  {0}'s rotor speed queue size: {1}", GetTurbinePrefixValue(), rotorSpeedQueue.Count);
                throw e;
            }
        }

        public double GetAvgWindSpeed() {
            double avgWindSpeed = 0.0;
            try {
                if (rotorSpeedQueue.Count > 0) {
                    avgWindSpeed = CalculateAverageWindSpeed(windSpeedQueue);
                    OpcServer.writeOpcTag(OpcServerName, AvgWindSpeedTag, avgWindSpeed);
                }
                else
                    ArticunoLogger.DataLogger.Debug("No wind speed samples for {0}. Returning zero",GetTurbinePrefixValue());

                return avgWindSpeed;
            }
            catch (Exception e) {
                ArticunoLogger.DataLogger.Error("There is no data stored in  {0}'s rotor speed queue size: {1}", GetTurbinePrefixValue(), windSpeedQueue.Count);
                throw e;
            }
        }

        private void LogTurbineCurtailmentRecord(string turbinePrefix, string activity) {
            ArticunoLogger.CurtailmentLogger.WithProperty("prop", new {
                activity = activity,
                tempCondition = temperatureConditionMet,
                operatingStateCondition = operatingStateConditionMet,
                turbPerfCondition = turbinePerformanceConditionMet,
                wsQueue = string.Join(",", windSpeedQueue.ToArray()),
                rsQueue = string.Join(",", rotorSpeedQueue.ToArray())
            }).Debug("Turbine {0} is ", turbinePrefix,
            activity.ToUpper().Equals(RELEASE) ? "being released" : "curtailed due to ice");
        }
    }
}
