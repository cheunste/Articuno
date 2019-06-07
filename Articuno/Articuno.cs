using log4net;
using OpcLabs.EasyOpc.DataAccess;
using OpcLabs.EasyOpc.DataAccess.Generic;
using OpcLabs.EasyOpc.DataAccess.OperationModel;
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

        //Constants
        private int ONE_MINUTE_POLLING = 60 * 1000;

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
            //Set up subscription event handler for system input OPC tags 
            //These should be polling every second and should include  tags like turbine participation
            using (var inputClient = new EasyDAClient())
            {
                inputClient.ItemChanged += systemInputChanged;
                inputClient.SubscribeMultipleItems(
                    new[] {
                            new DAItemGroupArguments("", "OPCLabs.KitServer.2", "Simulation.Random", 1000, null),
                            new DAItemGroupArguments("", "OPCLabs.KitServer.2", "Trends.Ramp (1 min)", 1000, null),
                            new DAItemGroupArguments("", "OPCLabs.KitServer.2", "Trends.Sine (1 min)", 1000, null),
                            new DAItemGroupArguments("", "OPCLabs.KitServer.2", "Simulation.Register_I4", 1000, null)
                        });
            }

            //start of the infinite loop
            while (true)
            {

            }

        }

        /// <summary>
        /// An event handler that handles value changes from FrontVue. This will mean system tags that end user have control of such as CTR or thresholds
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void systemInputChanged(object sender, EasyDAItemChangedEventArgs e)
        {
            if (e.Succeeded)
                Console.WriteLine("{0}: {1}", e.Arguments.ItemDescriptor.ItemId, e.Vtq);
            else
                Console.WriteLine("{0}: ***Failure: {1}", e.Arguments.ItemDescriptor.ItemId, e.Vtq);
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
