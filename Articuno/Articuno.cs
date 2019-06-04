using log4net;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Articuno
{
    class Articuno
    {

        public static Queue<Turbine> turbinesInDeRateList { get; set; } 
        public static Queue<Turbine> turbinesExcludedList { get; set; } 
        public static Queue<Turbine> turbinesInParticipationList { get; set; } 
        public static Queue<Turbine> turbinesPausedByArticuno { get; set; }
        private Queue<double> temperatureQueue;

        private List<Turbine> turbineList;

        //Log
        private static readonly ILog log = LogManager.GetLogger(typeof(Articuno));

        public String opcServer { get; set; }

        public Articuno(string opcServer, string metTower, List<Turbine> list)
        {
            this.turbineList = list;
            turbinesInDeRateList = new Queue<Turbine>();
            turbinesExcludedList = new Queue<Turbine>();
            turbinesInParticipationList = new Queue<Turbine>();
            turbinesPausedByArticuno = new Queue<Turbine>();
            temperatureQueue = new Queue<double>();
        }

        public void start()
        {

        }

        /*
        This method is special. It adds the current temperature from either the met tower or the turbine (only if met tower is down) to a temperature queue. 
        This is used for calculating CTR minute temperature averages based on averaging one minute averages, which CORE may or may not be providing
        The way it works is as follows
         - you collect one minute average temperatures values every minute (This will be determined by your timer class)
         - After CTR minute has passed (this could be 10, or 15, or 1, it is dependent on user), then 
         - calculate the average temperature of all the one min temperature values in the entire queue
         - perform a dequeue to remove the first most value
        */
        public void addToTemperatureQueue(double temperature) { throw new NotImplementedException(); } 


    }
}
