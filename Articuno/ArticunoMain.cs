﻿using log4net;
using OpcLabs.EasyOpc.DataAccess;
using OpcLabs.EasyOpc.DataAccess.Generic;
using OpcLabs.EasyOpc.DataAccess.OperationModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Articuno
{
    class ArticunoMain
    {

        public static List<Turbine> turbinesInDeRateList { get; set; }
        public static List<Turbine> turbinesExcludedList { get; set; }
        public static List<Turbine> turbinesInParticipationList { get; set; }
        public static List<Turbine> turbinesPausedByArticuno { get; set; }

        private static bool articunoEnable;
        private Queue<double> temperatureQueue;

        private List<Turbine> turbineList;

        //Constants
        private int ONE_MINUTE_POLLING = 60 * 1000;
        private static int NOISE_LEV = 5;
        private static int RUN_STATE = 100;

        //Log
        private static readonly ILog log = LogManager.GetLogger(typeof(ArticunoMain));

        public String opcServer { get; set; }

        public ArticunoMain(string opcServer, string metTower, List<Turbine> list)
        {
            this.turbineList = list;
            turbinesInDeRateList = new List<Turbine>();
            turbinesExcludedList = new List<Turbine>();
            turbinesInParticipationList = new List<Turbine>();
            turbinesPausedByArticuno = new List<Turbine>();

            temperatureQueue = new Queue<double>();
        }

        public void start()
        {
            //Set up subscription event handler for system input OPC tags 
            //These should be polling every second and should include  tags like turbine participation
            using (var inputClient = new EasyDAClient())
            {
                inputClient.ItemChanged += SystemInputOnChanged;
                inputClient.SubscribeMultipleItems(
                    new[] {
                            new DAItemGroupArguments("", "OPCLabs.KitServer.2", "Simulation.Random", 1000, null),
                            new DAItemGroupArguments("", "OPCLabs.KitServer.2", "Trends.Ramp (1 min)", 1000, null),
                            new DAItemGroupArguments("", "OPCLabs.KitServer.2", "Trends.Sine (1 min)", 1000, null),
                            new DAItemGroupArguments("", "OPCLabs.KitServer.2", "Simulation.Register_I4", 1000, null)
                        });
            }

            //The following lines starts a threading lambda and executes a function every minute. THis is used for events that require minute polling and CTR polling 
            var startTimeSpan = TimeSpan.Zero;
            var periodTimeSpan = TimeSpan.FromMilliseconds(ONE_MINUTE_POLLING);
            var timer = new System.Threading.Timer((e) => { minuteUpdate(); }, null, startTimeSpan, periodTimeSpan);

            //start of the infinite loop
            while (true)
            {

            }

        }

        /// <summary>
        /// Function to handle tasks that should be executed every minute (ie get temperature measurements) and every CTR minute (ie check rotor speed, run calculations, etc.) 
        /// </summary>
        private void minuteUpdate()
        {
            //For every minute, read the met tower measurements and the turbine temperature measurements
            //TODO: Fill this shit out
            for (int i = 1; i <= MetTowerMediator.getNumMetTower();i++) {
                //Get all measurements from the met tower
                Tuple<double, double, double, double> metMeasurements = MetTowerMediator.Instance.getAllMeasurements("Met"+i);
                //Get the temperature value of the nearest turbine from the met tower as well
                MetTowerMediator.Instance.getMetTower("Met" + i).getNearestTurbine().readTemperatureValue();
            }

            //For every CTR minute, do the other calculation stuff
            

        }

        /// <summary>
        /// An event handler that handles value changes from FrontVue. This will mean system tags that end user have control of such as CTR or thresholds
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /*
         * Event Handler that is executed whenever  the system input tags changed
         * System input tags are the following:
         *  - ICE.TmpThreshold
         *  - ICE.CurtailEna
         *  - ICE.EvalTm
         *  - ICE.TmpDelta
         *  - ICE.TmpDew
         *  
         *  CurtailEna is the most important one, which enables Articuno. 
         *  Thresholds requires the Met Tower class to do an update 
         *  CTR Period should be updated in both the ArticunoMain and the Turbine classes
         * 
         */
        static void SystemInputOnChanged(object sender, EasyDAItemChangedEventArgs e)
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
            //if (e.Arguments.ItemDescriptor.ItemId.ToString().ToUpper().Contains(ENABLE))
            //{
            //    articunoEnable = Convert.ToBoolean(e.Vtq.DisplayValue());
            //}
        }

        /// <summary>
        /// method called upon NRS or turbine Operating Status or participation change
        /// </summary>
        /// <param name="sender"></param>
        /// <param name=""></param>
        /**
         * Event handler executed when turbine input tags are changed by the user
         *  Turbine input tags are the following:
         *  - ArticunoParticipation (Participation)
         *  - NrsMode (Noise Level - Not used at all site)
         * 
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
        static void TurbineInputOnChanged(object sender, EasyDAItemChangedEventArgs e)
        {
            //get the turbine prefix from tag
            string tag = e.Arguments.ItemDescriptor.ItemId;
            string pattern = "(\\w\\d+)";

            //Regex to find the prefix
            Regex rgxLookup = new Regex(pattern, RegexOptions.Singleline, TimeSpan.FromSeconds(1));
            Match mLookup = rgxLookup.Match(tag);
            string prefix = mLookup.Groups[1].Value;

            //TODO: Get the turbine based on the prefix
            Turbine currentTurbine = TurbineMediator.getTurbine(prefix);

            //If it is null, then something bad happened
            if (currentTurbine is null)
            {
                log.ErrorFormat("Error. Turbine {0} cannot be found in the factory class (getTurbine). No further action executed", prefix);
                return;
            }
            //If it is NRS that changed
            // if (e.Arguments.ItemDescriptor.ItemId.ToString().ToUpper().Contains(NRS_TAG))
            // {
            //     string noiseLev = e.Vtq.DisplayValue();
            //     log.InfoFormat("Noise level for {0} is now: {1}", prefix, noiseLev);
            //     //Nrs condition is false if the noise level is not 5
            //     bool nrsCondition = Convert.ToInt16(noiseLev) == NOISE_LEV ? true : false;
            //     log.InfoFormat("NRS Condition for {0} setting to: {1}", prefix, nrsCondition);
            //     currentTurbine.setNrsCondition(nrsCondition);
            // }

            // //If it is an operating State that changed, throw it into derate queue
            // else if (e.Arguments.ItemDescriptor.ItemId.ToString().ToUpper().Contains(OPERATING_TAG))
            // {
            //     string operatingStatus = e.Vtq.DisplayValue();
            //     log.InfoFormat("Turbine {0} is in {1}", prefix, operatingStatus);
            //     //If not in run (100), then that means it is derated
            //     if (Convert.ToInt16(operatingStatus) != RUN_STATE)
            //     {
            //         currentTurbine.setDeRateCondition(true);
            //         log.InfoFormat("Adding {0} to Derate List", prefix);
            //         turbinesInDeRateList.Add(currentTurbine);
            //     }
            //     else
            //     {
            //         currentTurbine.setDeRateCondition(false);
            //         log.InfoFormat("Removing {0} to Derate List", prefix);
            //         turbinesInDeRateList.Remove(currentTurbine);
            //     }
            // }
            //If participation status changed
            else
            {
                bool participationStatus = Convert.ToBoolean(e.Vtq.DisplayValue());
                log.InfoFormat("Turbine {0} Participation Status: {1}", prefix, participationStatus);
                //Add or remove turbine from the participation queue
                if (participationStatus)
                {

                    log.InfoFormat("Adding Turbine {0} to Participation List", prefix);
                    turbinesInParticipationList.Add(currentTurbine);
                }
                else
                {
                    log.InfoFormat("Removing Turbine {0} to Participation List", prefix);
                    turbinesInParticipationList.Remove(currentTurbine);
                }
                currentTurbine.setParticipation(participationStatus);
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

        /// <summary>
        ///  ONLY USED FROM EVENT HANDLERS. This Method that is used to find enums so Articuno knows what other method should call. 
        /// </summary>
        /// <param name="opcTag"></param>
        private static void findChange(string opcTag,Object e)
        {
            /*
             * The following will find the met and turbine indicator for any given OPC Tag that changed by finding any words that are four characters long. First three can be alphanumeric,
             * but the last one must be a number
             *
             * Ex: SCRAB.T001.WROT.RotSpdAv will match T001 and SCRAB.MET1.AmbRh1 will match MET1
             */
            string pattern = @"\b\w{3}\d{1}\b";
            string input = opcTag;
            Regex lookup = new Regex(pattern, RegexOptions.Singleline);
            Match matchLookup = lookup.Match(opcTag);
            string prefix = matchLookup.ToString();


            //If it matches the met tower
            if (matchLookup.Value.ToUpper().Contains("MET")) { throw new NotImplementedException(); }
            //Else, assume met tower. Not like there's anything else given the regex
            else
            {
                Enum turbineEnum = TurbineMediator.Instance.findTurbineTag(matchLookup.Value, opcTag);

                switch (turbineEnum)
                {
                    case TurbineMediator.TurbineEnum.NrsMode:
                        throw new NotImplementedException();
                        break;
                    case TurbineMediator.TurbineEnum.OperatingState:
                        throw new NotImplementedException();
                        break;
                    case TurbineMediator.TurbineEnum.RotorSpeed:
                        throw new NotImplementedException();
                        break;
                    case TurbineMediator.TurbineEnum.Temperature:
                        throw new NotImplementedException();
                        break;
                    case TurbineMediator.TurbineEnum.WindSpeed:
                        throw new NotImplementedException();
                        break;
                    case TurbineMediator.TurbineEnum.Participation:
                        throw new NotImplementedException();
                        break;

                    //TODO: Take care of hte cases for system input tags

                }

            }
        }

        //This is a method that is triggered upon any value changes for certain OPC Tags 
        static void onItemChangeHandler(object sender, EasyDAItemChangedEventArgs e)
        {
            if (e.Succeeded)
            {
                string tag = e.Arguments.ItemDescriptor.ItemId;
                findChange(tag,e.Vtq.Value);
            }
            else { log.ErrorFormat("Error occured in onItemChangeHandler with {0}. Msg: {1}", e.Arguments.ItemDescriptor.ItemId, e.ErrorMessageBrief); }

        }
    }
}
