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

        //Instance of OPC server
        OpcServer server;

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

        public Turbine(string prefix,OpcServer server)
        {
            this.server = server;
        }

        //Detects when operating state (run, pause, etc.) changes
        public string operatingStateChanged() { throw new NotImplementedException(); }

        //Getters to get the value for the wind speed, rotor speed, etc. value from the OPC Server
        public double getWindSpeedValue() { throw new NotImplementedException(); }
        public double getrotorSpeedValue() { throw new NotImplementedException(); }
        public double getOperatinStateValue() { throw new NotImplementedException(); }
        public double getNrsStateValue() { throw new NotImplementedException(); }
        public double getTemperatureValue() { throw new NotImplementedException(); }
        public double getTurbineCtrValue() {  throw new NotImplementedException(); }
        public double getTurbineTemperatureValue() { throw new NotImplementedException(); }
        public double getTurbineHumidityValue() { throw new NotImplementedException(); }
        public double getTurbineSFValue() {throw new NotImplementedException(); }

        //Setters to set the member variables to the  OPC tag
        //THESE ARE USED TO SET OPC TAG NAME TO MEMBER FIELD VARIABLES
        public void setWindSpeedTag(string tag) { this.nacelleWindSpeed = tag; }
        public void setRotorSpeedTag(string tag) { this.rotorSpeed = tag; }
        public void setOperatinStateTag(string tag) { this.operatingState = tag; }
        public void setNrsStateTag(string tag) { this.nrsState = tag; }
        public void setTemperatureTag(string tag) { this.turbineTemperature = tag; }
        public void setLoadShutdownTag(string tag) { this.loadShutDown = tag; }
        public void setTurbineCtrTag(string tag) { this.turbineCtrTag = tag; }
        public void setTurbineTemperatureTag(string tag) { this.turbineTemperature = tag; }
        public void setTurbineHumidityTag(string tag) { this.turbineHumidity = tag; }


        //The setters to set the OPC Tag Values.  There shouldn't be too many of these
        // THESE ARE USED TO SET THE OPC TAG VALUES
        public void setTurbineCtrValue(int ctrValue) { server.setTagValue(this.turbineCtrTag,ctrValue); }
        //Scalign factor is unique as it is not used in the OPC Server and only used internally in this program
        public void setTurbineSFValue(int scalingFactor) { this.currentTurbSF = scalingFactor; }
        //Load shutdown function. 
        //This a bit different as it needs to 'feel' more like a command, but at the same time, it is more of a value setter
        // I am assuming this to be true, so I'll keep the NIE for now
        public double sentLoadShutdownCmd() {
            log.Info("Shutdown command for "+this.turbinePrefix + " has been sent");
            server.setTagValue(loadShutDown, true);
            throw new NotImplementedException();
        }

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
