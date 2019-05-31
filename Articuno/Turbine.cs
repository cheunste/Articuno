using log4net;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Articuno
{
    /// <summary>
    /// The turbine class exposes OPC tags that are relevant to the program.
    /// You can set the tags via the setXXXXXTag method and get the value of said tag via the getXXXXXXValue method
    /// </summary>

    //IMPORTANT: Getters and Setters MUST specify whether they're SETting the OPC Tag name to the field or SETting the OPC Value
    // Getters don't really have this problem as it just GETs the OPC Value. 

    class Turbine
    {

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

        //Log
        private static readonly ILog log = LogManager.GetLogger(typeof(Setup));

        public Turbine(string prefix)
        {

        }

        //Detects when operating state (run, pause, etc.) changes
        public string operatingStateChanged() { throw new NotImplementedException(); }

        //Getters to get the value for the wind speed, rotor speed, etc. value from the OPC Server
        public double getWindSpeedValue(OpcServer server) { throw new NotImplementedException(); }
        public double getrotorSpeedValue(OpcServer server) { throw new NotImplementedException(); }
        public double getOperatinStateValue(OpcServer server) { throw new NotImplementedException(); }
        public double getNrsStateValue(OpcServer server) { throw new NotImplementedException(); }
        public double getTemperatureValue(OpcServer server) { throw new NotImplementedException(); }
        public double getTurbineCtrValue(OpcServer server) {  throw new NotImplementedException(); }
        public double getTurbineTemperatureValue(OpcServer server) { throw new NotImplementedException(); }
        public double getTurbineHumidityValue(OpcServer server) { throw new NotImplementedException(); }
        public double getTurbineSFValue() {throw new NotImplementedException(); }

        //Load shutdown is a bit different as it is a command
        public double sentLoadShutdownCmd(OpcServer server) {
            log.Info("Shutdown command for "+this.turbinePrefix + " has been sent");
            throw new NotImplementedException();
        }

        //Setters to set the member variables to the  OPC tag
        // THESE ARE NOT USED TO SET OPC VALUE
        public void setWindSpeedTag(string tag) { throw new NotImplementedException(); }
        public void setrotorSpeedTag(string tag) { throw new NotImplementedException(); }
        public void setOperatinStateTag(string tag) { throw new NotImplementedException(); }
        public void setNrsStateTag(string tag) { throw new NotImplementedException(); }
        public void setTemperatureTag(string tag) { throw new NotImplementedException(); }
        public void setLoadShutdownTag(string tag) { throw new NotImplementedException(); }
        public void setTurbineCtrTag(string tag) { throw new NotImplementedException(); }
        public void setTurbineTemperatureTag(string tag) { throw new NotImplementedException(); }
        public void setTurbineHumidityTag(string tag) { throw new NotImplementedException(); }

        //The setters to set the OPC Tag Values. There shouldn't be too many of these
        public string setTurbineCtrValue() {   throw new NotImplementedException(); }
        public void setTurbineSFValue(int scalingFactor) { this.currentTurbSF = scalingFactor; }
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
        

        //Met Tower References for turbines. One sets the met tower reference (upon create) and the other gets it. 
        //These can be set to another reference  if/when they fail
        public void setMetReference() {    throw new NotImplementedException(); }
        public int getMetReference() {    throw new NotImplementedException(); }
    }
}
