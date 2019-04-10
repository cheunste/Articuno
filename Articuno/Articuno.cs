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
        public String opcServer { get; set; }

        public Articuno()
        {
           

        }

    }
}
