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
        private bool nrsConditionMet;
        private bool turbinePerformanceConditionMet;
        private bool derateConditionMet;
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
        private readonly int STARTUP_TIME_BUFFER = 100;
        private readonly double AGC_BLOCK_COMMAND = 0.00;
        private readonly double AGC_UNBLOCK_COMMAND = 1.00;

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
        public Object readNrsStateValue() { return OpcServer.readAnalogTag(OpcServerName, NrsStateTag); }
        public Object readTemperatureValue() { return OpcServer.readAnalogTag(OpcServerName, TemperatureTag); }

        public Object readTurbineScalingFactorValue() { return OpcServer.readAnalogTag(OpcServerName, ScalingFactorTag); }
        public Object readParticipationValue() { return OpcServer.readAnalogTag(OpcServerName, ParticipationTag); }
        public Object readAlarmValue() { return OpcServer.readAnalogTag(OpcServerName, AlarmTag); }
        public Object readCtrCurrentValue() { return OpcServer.readAnalogTag(OpcServerName, CtrCountdownTag); }
        public Object readNrsFlagConditionValue() { return OpcServer.readAnalogTag(OpcServerName, NrsConditionFlagTag); }
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
        public string ScalingFactorTag { set; get; }
        public string ParticipationTag { set; get; }
        public string AlarmTag { set; get; }
        public string TurbinePrefix { set; get; }
        public string DeRate { set; get; }

        //THese four tags are meant for Articuno to write to.
        public string LoadShutdownTag { set; get; }
        public string AgcBlockingTag { set; get; }
        public string LowRotorSpeedFlagTag { get; set; }
        public string CtrCountdownTag { get; set; }
        public string NrsConditionFlagTag { get; set; }

        //Theses are used to write to the OP Tag Values.  There shouldn't be too many of these
        public void writeTurbineCtrValue(int articunoCtrValue) { TurbineCtr = articunoCtrValue.ToString(); ctrCountDown = articunoCtrValue; }

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
        //public void writeAlarmTagValue(Object value) { OpcServer.writeOpcTag( OpcServerName, AlarmTag, Convert.ToDouble(value)); }
        public void writeAlarmTagValue(Object value) { OpcServer.writeOpcTag(OpcServerName, AlarmTag, Convert.ToBoolean(value)); }
        public void writeNoiseLevel(Object value) { OpcServer.writeOpcTag(OpcServerName, NrsStateTag, Convert.ToDouble(value)); }
        public void writeOperatingState(Object value) { OpcServer.writeOpcTag(OpcServerName, OperatingStateTag, Convert.ToDouble(value)); }
        public void decrementCtrTime()
        {
            ctrCountDown--;
            log.InfoFormat("{0} Current CTR: {1}", getTurbinePrefixValue(), ctrCountDown);
            OpcServer.writeOpcTag(OpcServerName, CtrCountdownTag, ctrCountDown);
            if (ctrCountDown < 0)
            {
                log.InfoFormat("CTR period for Turbine {0} reached Zero.", getTurbinePrefixValue());
                //Reset CTR countdown
                ctrCountDown = Convert.ToInt32(TurbineCtr);
                //Call the RotorSPeedCheck function to compare rotor speed for all turbines
                tm.RotorSpeedCheck(getTurbinePrefixValue());

                //Does Check the rest of the icing conditions
                checkIcingConditions();
            }
        }

        //Misc functions
        public string getTurbinePrefixValue() { return this.TurbinePrefix; }
        public string isDerated() { return this.DeRate; }

        //The following five fucntions are set by the main Articuno class. They show if each of the four/five 
        //algorithms are true
        public void setTemperatureCondition(bool state) { this.temperatureConditionMet = state; }
        public void setOperatingStateCondition(bool state) { this.operatingStateConditionMet = state; }

        //Changing the NRS condition also resets the CTR
        public void setNrsCondition(bool state)
        {
            this.nrsConditionMet = state;
            //Reset CTR in this condition and empty queue. Essentually, start from scratch
            //This is because a turbine must remain in its NRS without level change the ENTIRE CTR period.
            this.ctrCountDown = Convert.ToInt32(TurbineCtr);
            OpcServer.writeOpcTag(OpcServerName, NrsConditionFlagTag, state);
            emptyQueue();
        }
        public void setTurbinePerformanceCondition(bool state)
        {
            turbinePerformanceConditionMet = state;
            OpcServer.writeOpcTag(OpcServerName, this.LowRotorSpeedFlagTag, state);
        }
        public void setDeRateCondition(bool state) { derateConditionMet = state; }

        /*
         * Met Tower accessor. Note that it only takes a prefix (ie Met1, Met2)
         */
        public string MetTowerPrefix { set; get; }

        //Function to determine turbine participation in Articuno
        public void setParticipation(bool participationStatus) { articunoParicipation = participationStatus; }
        public bool getParticipation() { return articunoParicipation; }

        public bool Participation { get; set; }

        //The actual method that checks all conditions and throws a load shutdown command if needed
        public void checkIcingConditions()
        {

            bool frozenCondition = Convert.ToBoolean(readParticipationValue()) && temperatureConditionMet && operatingStateConditionMet && nrsConditionMet && turbinePerformanceConditionMet;
            log.DebugFormat("Checking ice condition for {6}. Frozen condition: {0},Participation: {1}\n" +
                "Temp Condition: {2}, OperatingState: {3}, NRS:{4}, TurbinePerf Condition {5}", frozenCondition,
                Convert.ToBoolean(readParticipationValue()), temperatureConditionMet, operatingStateConditionMet, nrsConditionMet, turbinePerformanceConditionMet, getTurbinePrefixValue());

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
            System.Threading.Thread.Sleep(STARTUP_TIME_BUFFER);
            log.DebugFormat("Waiting {0} seconds to allow turbine to start up....", STARTUP_TIME_BUFFER);
            writeAlarmTagValue(false);
            emptyQueue();
            log.DebugFormat("Turbine {0} CTR Value reset to: {1}", getTurbinePrefixValue(), TurbineCtr);
            this.ctrCountDown = Convert.ToInt32(TurbineCtr);
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
