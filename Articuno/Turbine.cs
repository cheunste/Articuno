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
        OpcServer server;
        string OpcServerName;
        private EasyDAClient client = new EasyDAClient();

        //Member variables for OPC Tags in the turbine
        private string turbinePrefix;
        private string operatingState;
        private string rotorSpeed;
        private string nacelleWindSpeed;
        private string deRate;
        private string nrsState;
        private string primMetReference;
        private string secMetReference;
        private string loadShutDown;
        private string turbineCtrTag;
        private string turbineScalingFactor;
        private string turbineTemperature;
        private string turbineHumidity;
        private string turbineParticipationTag;
        private string turbineAlarmTag;

        //Member variables for algorithm
        private bool temperatureConditionMet;
        private bool operatingStateConditionMet;
        private bool nrsConditionMet;
        private bool turbinePerformanceConditionMet;
        private bool derateConditionMet;

        //Other fields
        //scaling factor for turbine
        private int currentTurbSF;
        //this determines if the turbine is participating in Articuno or not. This must be a 'high priority check'  
        private bool articunoParicipation;

        //Met Tower Fields
        private MetTower primaryMet;
        private MetTower secondaryMet;
        private MetTower currentMetTower;
        private bool isMetTowerBackup;

        //Log
        private static readonly ILog log = LogManager.GetLogger(typeof(Turbine));

        //Constructors
        public Turbine(string prefix, OpcServer server)
        {
            this.turbinePrefix = prefix;
            this.server = server;
        }

        public Turbine(string prefix, String OpcServerName)
        {
            this.turbinePrefix = prefix;
            this.OpcServerName = OpcServerName;
        }

        public Turbine(string prefix)
        {
            this.turbinePrefix = prefix;
        }

        //Detects when operating state (run, pause, etc.) changes
        public string operatingStateChanged() { throw new NotImplementedException(); }

        //Methods to get the value for the wind speed, rotor speed, etc. value from the OPC Server
        public Object readWindSpeedValue() { return new EasyDAClient().ReadItemValue("", OpcServerName, this.nacelleWindSpeed); }
        public Object readRotorSpeedValue() { return new EasyDAClient().ReadItemValue("", OpcServerName, this.rotorSpeed); }
        public Object readOperatinStateValue() { return new EasyDAClient().ReadItemValue("", OpcServerName, this.operatingState); }
        public Object readNrsStateValue() { return new EasyDAClient().ReadItemValue("", OpcServerName, this.nrsState); }
        public Object readTemperatureValue() { return new EasyDAClient().ReadItemValue("", OpcServerName, this.turbineTemperature); }
        public Object readTurbineCtrValue() { return new EasyDAClient().ReadItemValue("", OpcServerName, this.turbineCtrTag); }
        public Object readTurbineTemperatureValue() { return new EasyDAClient().ReadItemValue("", OpcServerName, this.turbineTemperature); }
        public Object readTurbineHumidityValue() { return new EasyDAClient().ReadItemValue("", OpcServerName, turbineHumidity); }
        public Object readTurbineScalingFactorValue() { return new EasyDAClient().ReadItemValue("", OpcServerName, turbineScalingFactor); }
        public Object readParticipationValue() { return new EasyDAClient().ReadItemValue("", OpcServerName, turbineParticipationTag); }
        public Object readAlarmValue() { return new EasyDAClient().ReadItemValue("", OpcServerName, turbineAlarmTag); }

        //Setters to set the member variables to the  OPC tag
        //These are used to set the tag name to the member variable
        public void setWindSpeedTag(string tag) { this.nacelleWindSpeed = tag; }
        public void setRotorSpeedTag(string tag) { this.rotorSpeed = tag; }
        public void setOperatingStateTag(string tag) { this.operatingState = tag; }
        public void setNrsStateTag(string tag) { this.nrsState = tag; }
        public void setTemperatureTag(string tag) { this.turbineTemperature = tag; }
        public void setLoadShutdownTag(string tag) { this.loadShutDown = tag; }
        public void setTurbineCtrTag(string tag) { this.turbineCtrTag = tag; }
        public void setTurbineTemperatureTag(string tag) { this.turbineTemperature = tag; }
        public void setTurbineHumidityTag(string tag) { this.turbineHumidity = tag; }
        public void setParticipationTag(string tag) { this.turbineParticipationTag = tag; }
        public void setAlarmTag(string tag) { this.turbineAlarmTag = tag; }

        //Getters to get the Name of the OPC Tags.
        //These are mainly used by the factory class's other methods to get multiple OPC values at once
        public string getWindSpeedTag() { return this.nacelleWindSpeed; }
        public string getRotorSpeedTag() { return this.rotorSpeed; }
        public string getOperatinStateTag() { return this.operatingState; }
        public string getNrsStateTag() { return this.nrsState; }
        public string getTemperatureTag() { return this.turbineTemperature; }
        public string getLoadShutdownTag() { return this.loadShutDown; }
        public string getTurbineCtrTag() { return this.turbineCtrTag; }
        public string getTurbineTemperatureTag() { return this.turbineTemperature; }
        public string getTurbineHumidityTag() { return this.turbineHumidity; }
        public string getParticipationTag() { return this.turbineParticipationTag; }
        public string getAlarmTag() { return this.turbineAlarmTag; }


        //Theses are used to write to the OP Tag Values.  There shouldn't be too many of these
        public void writeTurbineCtrValue(int ctrValue) { server.writeTagValue(this.turbineCtrTag, ctrValue); }
        //Scalign factor is unique as it is not used in the OPC Server and only used internally in this program
        public void writeTurbineSFValue(int scalingFactor) { this.currentTurbSF = scalingFactor; }
        //Load shutdown function. Probably the most important function
        public double writeLoadShutdownCmd()
        {
            log.InfoFormat("Shutdown command for {0} has been sent", this.turbinePrefix);
            try
            {
                client.WriteItemValue("", OpcServerName, getLoadShutdownTag(), 1.00);
                return 1.0;
            }
            catch (OpcException opcException)
            {
                log.ErrorFormat("Error stropping {0}: {1}", this.turbinePrefix, opcException.GetBaseException().Message);
                return -1.0;
            }
        }
        public void writeAlarmTagValue(Object value) { client.WriteItemValue("", OpcServerName, turbineAlarmTag, Convert.ToDouble(value)); }
        public void writeNoiseLevel(Object value) { client.WriteItemValue("", OpcServerName, nrsState, Convert.ToDouble(value)); }
        public void writeOperatingState(Object value) { client.WriteItemValue("", OpcServerName, operatingState, Convert.ToDouble(value)); }

        //Misc functions
        public string getTurbinePrefixValue() { return this.turbinePrefix; }
        public string isDerated() { return this.deRate; }

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

        //The actual method that checks all conditions and throws a load shutdown command if needed
        public void checkIcingConditions()
        {
            if (articunoParicipation && temperatureConditionMet && operatingStateConditionMet && nrsConditionMet && turbinePerformanceConditionMet && derateConditionMet)
            {

            }

        }
    }
}
