using log4net;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpcLabs.EasyOpc.DataAccess;
using OpcLabs.EasyOpc.OperationModel;

namespace Articuno
{
    /// <summary>
    /// The turbine class exposes OPC tags that are relevant to the program.
    /// You can set the tags via the setXXXXXTag method and get the value of said tag via the getXXXXXXValue method
    /// </summary>

    /*
     * This is the turbine class. It represents a turbine object in Articuno 
     * It probably should be an interface...but having an interface to create just one type of turbine seems kinda redundant. That's why I have a factory class
     */
    sealed internal class Turbine
    {
        //Instance of OPC server
        string OpcServerName;

        //Member variables for algorithm
        private bool temperatureConditionMet;
        private bool operatingStateConditionMet;
        private bool turbinePerformanceConditionMet;
        private static TurbineMediator tm;

        //CTR Time. This is used to count down to zero. NOT set it.
        private int ctrCountDown;

        //Queues
        private Queue<Double> windSpeedQueue;
        private Queue<Double> rotorSpeedQueue;

        //Other fields
        //this determines if the turbine is participating in Articuno or not. This must be a 'high priority check'  
        private bool articunoParicipation;

        //Constants
        //Startup buffer
        private readonly double AGC_BLOCK_COMMAND = 0.00;
        private readonly double AGC_UNBLOCK_COMMAND = 1.00;
        public static readonly int NRS_NOT_ACTIVE = 5;
        public static readonly int NRS_ACTIVE = 0;


        //Log
        private static readonly ILog log = LogManager.GetLogger(typeof(Turbine));

        //Constructors
        public Turbine(string prefix, String OpcServerName)
        {
            this.TurbinePrefix = prefix;
            this.OpcServerName = OpcServerName;
            windSpeedQueue = new Queue<double>();
            rotorSpeedQueue = new Queue<double>();
            tm = TurbineMediator.Instance;

        }

        //Methods to read the value for the wind speed, rotor speed, etc. value from the OPC Server
        public Object readWindSpeedValue() { return OpcServer.readAnalogTag(OpcServerName, WindSpeedTag); }
        public Object readRotorSpeedValue() { return OpcServer.readAnalogTag(OpcServerName, RotorSpeedTag); }
        public Object readOperatingStateValue() { return OpcServer.readAnalogTag(OpcServerName, OperatingStateTag); }
        private Object readNrsStateValue()
        {
            if (NrsStateTag.Equals("")) { return NRS_NOT_ACTIVE; }
            else { return OpcServer.readAnalogTag(OpcServerName, NrsStateTag); }
        }

        public bool isNrsActive()
        {
            int state = Convert.ToInt32(readNrsStateValue());
            if (state == NRS_ACTIVE)
                return true;
            else
                return false;

        }
        public Object readTemperatureValue() { return OpcServer.readAnalogTag(OpcServerName, TemperatureTag); }

        public Object readTurbineScalingFactorValue()
        {
            return ScalingFactorValue;
        }
        public Object readParticipationValue() { return OpcServer.readAnalogTag(OpcServerName, ParticipationTag); }
        public Object readAlarmValue() { return OpcServer.readAnalogTag(OpcServerName, StoppedAlarmTag); }
        public Object readCtrCurrentValue() { return OpcServer.readAnalogTag(OpcServerName, CtrCountdownTag); }
        public Object readLowRotorSpeedFlagValue() { return OpcServer.readAnalogTag(OpcServerName, LowRotorSpeedFlagTag); }
        public Object readAgcBlockValue() { return OpcServer.readBooleanTag(OpcServerName, AgcBlockingTag); }

        //public Accessors (Getters and Setters)  to set the member variables to the  OPC tag
        // Not entirely sure if these should be public or not, but it does make reading code easier
        public string WindSpeedTag { set; get; }
        public string RotorSpeedTag { set; get; }
        public string OperatingStateTag { set; get; }
        public string NrsStateTag { set; get; }
        public string StartCommandTag { internal set; get; }
        public string TurbineCtr { set; get; }
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

        //Theses are used to write to the OP Tag Values.  There shouldn't be too many of these
        public void writeTurbineCtrValue(int articunoCtrValue)
        {
            TurbineCtr = articunoCtrValue.ToString();
            ctrCountDown = articunoCtrValue;
            OpcServer.writeOpcTag(OpcServerName, CtrCountdownTag, articunoCtrValue);
        }

        //Load shutdown function. Probably the most important function
        public double writeLoadShutdownCmd()
        {
            log.DebugFormat("Shutdown command for {0} has been sent", this.TurbinePrefix);
            try
            {
                OpcServer.writeOpcTag(OpcServerName, this.LoadShutdownTag, 1.00);
                return 1.0;
            }
            catch (OpcException opcException)
            {
                log.ErrorFormat("Error stropping {0}: {1}", this.TurbinePrefix, opcException.GetBaseException().Message);
                return -1.0;
            }
        }
        //Write Articuno Alarm
        public void writeAlarmTagValue(Object value) { OpcServer.writeOpcTag(OpcServerName, StoppedAlarmTag, Convert.ToBoolean(value)); }
        public void writeNoiseLevel(Object value)
        {
            //Don't write anything if tag doesn't exist
            if (NrsStateTag.Equals("")) { }
            else
            {
                //Write the new state of the CTR
                OpcServer.writeOpcTag(OpcServerName, NrsStateTag, Convert.ToDouble(value));
            }
            //Reset CTR on noise level change
            resetCtrTime();

        }
        public void writeOperatingState(Object value) { OpcServer.writeOpcTag(OpcServerName, OperatingStateTag, Convert.ToDouble(value)); }
        public void decrementCtrTime()
        {
            ctrCountDown--;
            log.InfoFormat("{0} Current CTR: {1}", getTurbinePrefixValue(), ctrCountDown);
            OpcServer.writeOpcTag(OpcServerName, CtrCountdownTag, ctrCountDown);

            // When the NCC starts a turbine, additional startup time is needed. The time after startup will be greather than the TurbineCtr time.
            // During this startup period, you should not store the data in the queues as they're unreliable
            if (ctrCountDown > Convert.ToInt32(TurbineCtr))
                emptyQueue();
            // Once the CTR countdown reaches zero, do typical calculation
            if (ctrCountDown <= 0)
            {
                log.InfoFormat("CTR period for Turbine {0} reached Zero.", getTurbinePrefixValue());
                resetCtrTime();
                //Call the RotorSPeedCheck function to compare rotor speed for all turbines
                tm.RotorSpeedCheck(getTurbinePrefixValue());

                //Does Check the rest of the icing conditions
                //Do NOT call the check Ice function if the UCC is not active
                if (tm.isUCCActive())
                    checkIcingConditions();
            }
        }

        //Function to restart the Ctr Time. 
        public void resetCtrTime(int startupTime = 0)
        {
            //Read Current CTR
            double currCtr = Convert.ToInt32(readCtrCurrentValue());

            //Reset CTR countdown
            //If the current CTR is larger than the TurbineCtr, then don't do anything. 
            //This means it was recently started and the turbine state hasn't changed (because a turbine takes a while to get started)
            //However, if the currCtr is less than TuirbineCtr, that means it recently went to 0 and needs a restart
            if (Convert.ToInt32(TurbineCtr) >= currCtr)
            {
                ctrCountDown = Convert.ToInt32(TurbineCtr) + startupTime;
                OpcServer.writeOpcTag(OpcServerName, CtrCountdownTag, ctrCountDown);
            }
        }

        //Misc functions
        public string getTurbinePrefixValue() { return this.TurbinePrefix; }

        //The following five fucntions are set by the main Articuno class. They show if each of the four/five 
        //algorithms are true
        public void setTemperatureCondition(bool state) { this.temperatureConditionMet = state; }
        public void setOperatingStateCondition(bool state) { this.operatingStateConditionMet = state; }

        /// <summary>
        /// Method to set NRS mode from the turbine. false means NRS not active. true means NRS is active. 
        /// Changing the NRS condition also resets the CTR and clears all stored queues
        /// </summary>
        /// <param name="state">A boolean</param>
        public void setNrsMode(bool state)
        {
            //Reset CTR in this condition and empty queue. Essentually, start from scratch
            //This is because a turbine must remain in its NRS without level change the ENTIRE CTR period.
            resetCtrTime();
            emptyQueue();
        }
        public void setTurbinePerformanceCondition(bool state)
        {
            turbinePerformanceConditionMet = state;
            OpcServer.writeOpcTag(OpcServerName, this.LowRotorSpeedFlagTag, state);
        }

        /*
         * Met Tower accessor. Note that it only takes a prefix (ie Met1, Met2)
         */
        public string MetTowerPrefix { set; get; }

        //The actual method that checks all conditions and throws a load shutdown command if needed
        public void checkIcingConditions()
        {

            bool frozenCondition = Convert.ToBoolean(readParticipationValue()) && temperatureConditionMet && operatingStateConditionMet && turbinePerformanceConditionMet;
            log.DebugFormat("Checking ice condition for {5}.  Paused by Articuno: {0}\nTurbine Participation?: {1} " +
                "Icy Temp Condition?: {2}, OperatingState: {3}, Low TurbinePerf Condition?: {4}", frozenCondition,
                Convert.ToBoolean(readParticipationValue()), temperatureConditionMet, operatingStateConditionMet, turbinePerformanceConditionMet, getTurbinePrefixValue());

            if (frozenCondition)
            {
                log.DebugFormat("Icing conditions satisfied for {0}", getTurbinePrefixValue());
                pauseByArticuno(true);
            }
            else
            {
                log.DebugFormat("No ice detected for turbine {0}", getTurbinePrefixValue());
                pauseByArticuno(false);
            }
        }

        //For Wind speed and Rotor Speed queues. 
        public void addWindSpeedToQueue(double windSpeed) { windSpeedQueue.Enqueue(windSpeed); }
        public void addRotorSpeedToQueue(double rotorSpeed) { rotorSpeedQueue.Enqueue(rotorSpeed); }
        public Queue<double> getWindSpeedQueue() { return windSpeedQueue; }
        public Queue<double> getRotorSpeedQueue() { return rotorSpeedQueue; }
        /// <summary>
        /// Method call to clear all content of a turbine's wind speed queue and rotor speed queue
        /// </summary>
        public void emptyQueue() { windSpeedQueue.Clear(); rotorSpeedQueue.Clear(); }

        /// <summary>
        /// Method used to trigger a pausing condition due to ice.
        /// </summary>
        /// <param name="pause"></param>
        /*
         * This method is needed because not only are you sending a pause command to the turbine
         * but you also have to do loggign, raising alarm, etc.
         * 
         */
        private void pauseByArticuno(bool pause)
        {
            if (pause)
            {
                if (!tm.isTurbinePaused(TurbinePrefix))
                {
                    //Block Turbine in AGC
                    blockTurbine(true);
                    log.DebugFormat("Sending pause commmand for {0}", getTurbinePrefixValue());
                    writeLoadShutdownCmd();
                    log.DebugFormat("Writing alarm for {0}", getTurbinePrefixValue());
                    writeAlarmTagValue(true);
                    tm.updateMain(TurbineMediator.TurbineEnum.PausedByArticuno, TurbinePrefix);
                    log.InfoFormat("Turbine {0} is now paused", getTurbinePrefixValue());
                }
            }
        }

        /// <summary>
        /// Start the turbine. This function clears its alarm, reset its CTRCount and empty its queue
        /// </summary>
        public void startTurbine()
        {
            //Unblock Turbine from AGC
            blockTurbine(false);

            log.DebugFormat("Start Command Received for Turbine {0}", getTurbinePrefixValue());
            //Give the turbine some time to start 
            int startupTime = Convert.ToInt32(StartupTime);
            log.DebugFormat("Giving additional {0} minutes to allow turbine to start up....", startupTime);
            writeAlarmTagValue(false);
            emptyQueue();
            log.InfoFormat("Turbine {0} has started", getTurbinePrefixValue());
            log.DebugFormat("Turbine {0} CTR Value reset to: {1}", getTurbinePrefixValue(), (Convert.ToInt32(TurbineCtr)) + startupTime);
            resetCtrTime(startupTime);
        }

        //Function to block/remove turbine in AGC until startup.
        /// <summary>
        /// function to block the turbine from AGC.
        /// </summary>
        /// <param name="state"></param>
        private void blockTurbine(bool state)
        {
            if (state)
                OpcServer.writeOpcTag(OpcServerName, AgcBlockingTag, Convert.ToDouble(AGC_BLOCK_COMMAND));
            else
                OpcServer.writeOpcTag(OpcServerName, AgcBlockingTag, Convert.ToDouble(AGC_UNBLOCK_COMMAND));
        }

    }
}
