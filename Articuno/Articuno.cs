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

        }

        public void start()
        {

        }

    }
}
