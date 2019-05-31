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

        //Member variables for algorithm
        private bool temperatureConditionMet;
        private bool operatingStateConditionMet;
        private bool nrsConditionMet;
        private bool turbinePerformanceConditionMet;
        private bool derateConditionMet;

        private int currentTurbSP;
        private Queue temperatureQueue;
        private bool articunoParicipation;



        //Log
        private static readonly ILog log = LogManager.GetLogger(typeof(Setup));

        public Turbine(string prefix)
        {

        }

        //Detects when operating state (run, pause, etc.) changes
        public string operatingStateChanged() { throw new NotImplementedException(); }

        //Get the value for the wind speed, rotor speed, etc. value from the OPC Server
        public double getWindSpeedValue(OpcServer server) { throw new NotImplementedException(); }
        public double getrotorSpeedValue(OpcServer server) { throw new NotImplementedException(); }
        public double getOperatinStateValue(OpcServer server) { throw new NotImplementedException(); }
        public double getNrsStateValue(OpcServer server) { throw new NotImplementedException(); }
        public double getTemperatureValue(OpcServer server) { throw new NotImplementedException(); }
        //Load shutdown is a bit different as it is a command
        public double sentLoadShutdownCmd(OpcServer server) {
            log.Info("Shutdown command for "+this.turbinePrefix + " has been sent");
            throw new NotImplementedException();
        }

        //Sets the OPC tag to the class's  member variable
        public void setWindSpeedTag(string tag) { throw new NotImplementedException(); }
        public void setrotorSpeedTag(string tag) { throw new NotImplementedException(); }
        public void setOperatinStateTag(string tag) { throw new NotImplementedException(); }
        public void setNrsStateTag(string tag) { throw new NotImplementedException(); }
        public void setTemperatureTag(string tag) { throw new NotImplementedException(); }
        public void setLoadShutdownTag(string tag) { throw new NotImplementedException(); }

        //The following five fucntions are set by the main Articuno class. They show if each of the four/five 
        //algorithms are true
        public void setTemperatureCondition(bool state) { this.temperatureConditionMet = state; }
        public void setOperatingStateCondition(bool state) { this.operatingStateConditionMet = state; }
        public void setNrsCondition(bool state) { this.nrsConditionMet = state; }
        public void setTurbinePerformanceCondition(bool state) { turbinePerformanceConditionMet = state; }
        public void setDeRateCondition(bool state) { derateConditionMet = state; }
        
        //Misc functions
        public string getTurbinePrefix() { return this.turbinePrefix; }

        public string getTurbineCTR() {   throw new NotImplementedException(); }

        //Met Tower References for turbines. One sets the met tower reference (upon create) and the other gets it. 
        //These can be set to another reference  if/when they fail
        public void setMetReference() {    throw new NotImplementedException(); }
        public int getMetReference() {    throw new NotImplementedException(); }

        //Turbine set point getter and setter
        public int getTurbineSP(){throw new NotImplementedException(); }
        public void setTurbineSP(int setPoint){throw new NotImplementedException(); }

        /*
        This method is special. It adds the current temperature to a temperature queue. 
        This method is also in the main Articuno class and
        This is used for calculating CTR minute temperature averages based on averaging one minute averages, which CORE may or may not be providing
        IF AND ONLY IF The met tower temperatures are NOT working. This is a used for turbines that are supposed to be met tower redundancy
        The way it works is as follows
         - you collect one minute average temperatures values every minute (This will be determined by your timer class)
         - After CTR minute has passed (this could be 10, or 15, or 1, it is dependent on user), then 
         - calculate the average temperature of all the one min temperature values in the entire queue
         - perform a dequeue to remove the first most value
        */
        public void addToTemperatureQueue(double temperature) { throw new NotImplementedException(); } 

    }
}
