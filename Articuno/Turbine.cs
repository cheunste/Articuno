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
    class Turbine
    {
        //Instance of OPC server
        string OpcServerName;
        private EasyDAClient client = new EasyDAClient();

        //Member variables for algorithm
        private bool temperatureConditionMet;
        private bool operatingStateConditionMet;
        private bool nrsConditionMet;
        private bool turbinePerformanceConditionMet;
        private bool derateConditionMet;

        //CTR Time. This is used to count down to zero. NOT set it.
        private int ctrCountDown;

        //Queues
        private Queue<Double> windSpeedQueue;
        private Queue<Double> rotorSpeedQueue;

        //Other fields
        //scaling factor for turbine
        private int currentTurbSF;
        //this determines if the turbine is participating in Articuno or not. This must be a 'high priority check'  
        private bool articunoParicipation;

        //Met Tower Fields
        private MetTower currentMetTower;

        //Log
        private static readonly ILog log = LogManager.GetLogger(typeof(Turbine));

        //Constructors
        public Turbine(string prefix, String OpcServerName)
        {
            this.TurbinePrefix = prefix;
            this.OpcServerName = OpcServerName;
            windSpeedQueue = new Queue<double>();
            rotorSpeedQueue = new Queue<double>();

        }

        //Detects when operating state (run, pause, etc.) changes
        public string operatingStateChanged() { throw new NotImplementedException(); }

        //Methods to get the value for the wind speed, rotor speed, etc. value from the OPC Server
        public Object readWindSpeedValue() { return new EasyDAClient().ReadItemValue("", OpcServerName, WindSpeedTag); }
        public Object readRotorSpeedValue() { return new EasyDAClient().ReadItemValue("", OpcServerName, RotorSpeedTag); }
        public Object readOperatinStateValue() { return new EasyDAClient().ReadItemValue("", OpcServerName, OperatingStateTag); }
        public Object readNrsStateValue() { return new EasyDAClient().ReadItemValue("", OpcServerName, NrsStateTag); }
        public Object readTemperatureValue() { return new EasyDAClient().ReadItemValue("", OpcServerName, TemperatureTag); }
        //public Object readTurbineCtrValue() { return new EasyDAClient().ReadItemValue("", OpcServerName, TurbineCtrTag); }
        public Object readTurbineCtrValue() { return TurbineCtr; }
        public Object readTurbineHumidityValue() { return new EasyDAClient().ReadItemValue("", OpcServerName, TurbineHumidityTag); }
        public Object readTurbineScalingFactorValue() { return new EasyDAClient().ReadItemValue("", OpcServerName, ScalingFactorTag); }
        public Object readParticipationValue() { return new EasyDAClient().ReadItemValue("", OpcServerName, ParticipationTag); }
        public Object readAlarmValue() { return new EasyDAClient().ReadItemValue("", OpcServerName, AlarmTag); }
        public int readCtrCurrentValue() { return ctrCountDown; }

        //public Accessors (Getters and Setters)  to set the member variables to the  OPC tag
        // Not entirely sure if these should be public or not, but it does make reading code easier
        //These are used to set the tag name to the member variable
        public string WindSpeedTag { set; get; }
        public string RotorSpeedTag { set; get; }
        public string OperatingStateTag { set; get; }
        public string NrsStateTag { set; get; }
        public string LoadShutdownTag { set; get; }
        public string StartCommandTag { internal set; get; }
        public string TurbineCtr { set; get; }
        public string TemperatureTag { set; get; }
        public string TurbineHumidityTag { set; get; }
        public string ScalingFactorTag { set; get; }
        public string ParticipationTag { set; get; }
        public string AlarmTag { set; get; }
        public string TurbinePrefix { set; get; }
        public string DeRate { set; get; }

        //Theses are used to write to the OP Tag Values.  There shouldn't be too many of these
        public void writeTurbineCtrValue(int articunoCtrValue) { TurbineCtr = articunoCtrValue.ToString(); ctrCountDown = articunoCtrValue; }
        //Scalign factor is unique as it is not used in the OPC Server and only used internally in this program
        public void writeTurbineSFValue(int scalingFactor) { this.currentTurbSF = scalingFactor; }
        //Load shutdown function. Probably the most important function
        public double writeLoadShutdownCmd()
        {
            log.InfoFormat("Shutdown command for {0} has been sent", this.TurbinePrefix);
            try
            {
                client.WriteItemValue("", OpcServerName, this.LoadShutdownTag, 1.00);
                return 1.0;
            }
            catch (OpcException opcException)
            {
                log.ErrorFormat("Error stropping {0}: {1}", this.TurbinePrefix, opcException.GetBaseException().Message);
                return -1.0;
            }
        }
        //public void writeAlarmTagValue(Object value) { client.WriteItemValue("", OpcServerName, AlarmTag, Convert.ToDouble(value)); }
        public void writeAlarmTagValue(Object value) { client.WriteItemValue("", OpcServerName, AlarmTag, Convert.ToBoolean(value)); }
        public void writeNoiseLevel(Object value) { client.WriteItemValue("", OpcServerName, NrsStateTag, Convert.ToDouble(value)); }
        public void writeOperatingState(Object value) { client.WriteItemValue("", OpcServerName, OperatingStateTag, Convert.ToDouble(value)); }
        public void decrementCtrTime()
        {
            ctrCountDown--;
            log.DebugFormat("{0} Current CTR: {1}",getTurbinePrefixValue(),ctrCountDown);
            if (ctrCountDown <= 0)
            {
                //ctrCountDown = articunoCtrTime;
                //writeTurbineCtrValue(articunoCtrTime);
                log.InfoFormat("CTR period for Turbine {0} reached Zero.",getTurbinePrefixValue());
                ctrCountDown = Convert.ToInt32(TurbineCtr);
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
        public void setNrsCondition(bool state) { this.nrsConditionMet = state; }
        public void setTurbinePerformanceCondition(bool state) { turbinePerformanceConditionMet = state; }
        public void setDeRateCondition(bool state) { derateConditionMet = state; }

        /*
         * Met Tower related methods for turbines. 
         * One sets the met tower reference (upon create) and the other gets it. 
         * These can be set to another reference  if/when they fail 
         * The set Met Reference takes in a MetTower object
         */
        public void setMetTower(MetTower met) { currentMetTower = met; }
        public MetTower getMetTower() { return currentMetTower; }

        //Function to determine participation
        public void setParticipation(bool participationStatus) { articunoParicipation = participationStatus; }
        public bool getParticipation() { return articunoParicipation; }

        public bool Participation { get; set; }

        //The actual method that checks all conditions and throws a load shutdown command if needed
        public void checkIcingConditions()
        {
            Console.WriteLine("Full condition {0}\nparticipation: {1}\nTemp Condition: {2}\n" +
                "OperatingState: {3}\nNRS:{4}\nTurbinePerf Condition {5}", Convert.ToBoolean(readParticipationValue()) && temperatureConditionMet && operatingStateConditionMet && nrsConditionMet && turbinePerformanceConditionMet,
                Convert.ToBoolean(readParticipationValue()) ,temperatureConditionMet,operatingStateConditionMet,nrsConditionMet,turbinePerformanceConditionMet);
            //if (Convert.ToBoolean(readParticipationValue()) && temperatureConditionMet && operatingStateConditionMet && nrsConditionMet && turbinePerformanceConditionMet && derateConditionMet)
            if (Convert.ToBoolean(readParticipationValue()) && temperatureConditionMet && operatingStateConditionMet && nrsConditionMet && turbinePerformanceConditionMet )
            {
                log.InfoFormat("Icing conditions satisfied for {0}",getTurbinePrefixValue());
                pauseByArticuno(true);
            }
            else
            {
                log.InfoFormat("No ice detected for turbine {0}",getTurbinePrefixValue());
                pauseByArticuno(false);
            }
        }

        //For Wind speed and Rotor Speed queues
        public void addWindSpeedToQueue(double windSpeed) { windSpeedQueue.Enqueue(windSpeed); }
        public void addRotorSpeedToQueue(double rotorSpeed) { rotorSpeedQueue.Enqueue(rotorSpeed); }
        public Queue<double> getWindSpeedQueue() { return windSpeedQueue; }
        public Queue<double> getRotorSpeedQueue() { return rotorSpeedQueue; }
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
                log.DebugFormat("Sending pause commmand for {0}", getTurbinePrefixValue());
                writeLoadShutdownCmd();
                log.DebugFormat("Writing alarm for {0}", getTurbinePrefixValue());
                writeAlarmTagValue(true);
                TurbineMediator.Instance.updateMain(TurbineMediator.TurbineEnum.PausedByArticuno,TurbinePrefix);
            }
            else {

                log.DebugFormat("Clearing Pause Status for {0}", getTurbinePrefixValue());
                writeAlarmTagValue(false);
                TurbineMediator.Instance.updateMain(TurbineMediator.TurbineEnum.ClearBySite,TurbinePrefix);
            }
        }

        public void startTurbine() { pauseByArticuno(false); }

    }
}
