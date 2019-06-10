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
    class ArticunoMain
    {

        public static Queue<Turbine> turbinesInDeRateList { get; set; }
        public static Queue<Turbine> turbinesExcludedList { get; set; }
        public static Queue<Turbine> turbinesInParticipationList { get; set; }
        public static Queue<Turbine> turbinesPausedByArticuno { get; set; }
        private static bool articunoEnable;
        private Queue<double> temperatureQueue;

        private List<Turbine> turbineList;

        //Constants
        private int ONE_MINUTE_POLLING = 60 * 1000;
        private static int NOISE_LEV = 5;
        private static int RUN_STATE = 100;
        private static string ENABLE = "CURTAILENA";
        private static string OPERATING_TAG = "ACTST";
        private static string NRS_TAG = "NRS";

        //Log
        private static readonly ILog log = LogManager.GetLogger(typeof(ArticunoMain));

        public String opcServer { get; set; }

        public ArticunoMain(string opcServer, string metTower, List<Turbine> list)
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
                inputClient.ItemChanged += inputTagOnChanged;
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
        /*
         * System input tags are the following:
         *  - ICE.TmpThreshold
         *  - ICE.CurtailEna
         *  - ICE.EvalTm
         *  - ICE.TmpDelta
         *  - ICE.TmpDew
         *  
         *  Turbine input tags are the following:
         *  - ArticunoParticipation (Participation)
         *  
         *  The only one that matters the most is the CurtailEna, which enables Articuno. Everythign else requires the Met Tower class to do an update
         * 
         */
        static void inputTagOnChanged(object sender, EasyDAItemChangedEventArgs e)
        {
            throw new NotImplementedException();
            if (e.Succeeded)
            {
                Console.WriteLine("{0}: {1}", e.Arguments.ItemDescriptor.ItemId, e.Vtq);
            }
            else
            {
                Console.WriteLine("{0}: ***Failure: {1}", e.Arguments.ItemDescriptor.ItemId, e.Vtq);
            }
            //If it is the Articuno enable change
            if (e.Arguments.ItemDescriptor.ItemId.ToString().ToUpper().Contains(ENABLE))
            {
                articunoEnable = Convert.ToBoolean(e.Vtq.DisplayValue());
            }
        }

        /// <summary>
        /// method called upon NRS or turbine Operating Status change
        /// </summary>
        /// <param name="sender"></param>
        /// <param name=""></param>
        /*
         * This method is used to monitor the tag change from a turbine's NRS state or a turbine's oeprating state 
         * What happens is that once either state changes, Articuno will remove them from its internal queue
         * until Operating state is back to 100 (Run status) or when NRS is 5. Remove it from the queue otherwise
         * 
         * You might need to get the turbine prefix (via substring) and then get the actual turbine object 
         * 
         * HOWEVER, you need to check to see if the turbine also
         * - participating in Articuno
         * - Not in derate, or excluded or some other weird shit
         * - 
         * 
         */
        static void turbineStatusInputChanged(object sender, EasyDAItemChangedEventArgs e)
        {
            //TODO: The following
            //get the turbine prefix
            //get thte turbine object from the prefix
            //update the turbine's NRS status (calling the writeNRS function in turbine object)
            //

            //If it is NRS that changed
            if (e.Arguments.ItemDescriptor.ItemId.ToString().ToUpper().Contains(NRS_TAG))
            {

                throw new NotImplementedException();
                //If it isn't equivalent to the noise level (5) remove from the NRS queue
                if (Convert.ToInt16(e.Vtq.DisplayValue()) != NOISE_LEV)
                {

                }
                //Add it back to the NRS queue otherwise
                else
                {

                }

            }
            //If it is an operating State that changed AND the turbine is 
            else if (e.Arguments.ItemDescriptor.ItemId.ToString().ToUpper().Contains(OPERATING_TAG))
            {
                throw new NotImplementedException();
                //If it isn't equivalent to the run status (100), then remove it from the queu
                if (Convert.ToInt16(e.Vtq.DisplayValue()) != NOISE_LEV)
                {

                }
                //Add it back to the NRS queue otherwise
                else
                {

                }
            }
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
