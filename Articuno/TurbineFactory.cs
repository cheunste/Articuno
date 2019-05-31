using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Articuno
{
    /// <summary>
    /// The turbine FActory is a factory class responsible for creating the numerous turbines at the site.
    /// It takes in a list of turbine prefixes and then creates a turbine class based  on the prefix
    /// The factory class also have other methods that'll allow interfaction with all the turbines in its turbine list such as throwing them into another list, 
    /// or fetching a turbine based on other factors like operating state, etc.
    /// </summary>
    class TurbineFactory
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(TurbineFactory));
        private List<Turbine> turbineList;
        private List<string> turbinePrefixList;

        private OpcServer server;

        /// <summary>
        /// Constructor for the turbine factory class. Takes in a string list of Turbine prefixes (ie T001). Probably should be called only once
        /// </summary>
        /// <param name="turbinePrefixList"></param>
        public TurbineFactory(List<string> turbinePrefixList,OpcServer server)
        {
            turbineList = new List<Turbine>();
            this.turbinePrefixList = turbinePrefixList;
            this.server = server;
        }

        /// <summary>
        /// Create the turbines given a list of prefixes. 
        /// 
        /// SHOULD ONLY BE CALLED ONCE
        /// </summary>
        public void createTurbines()
        {
            log.Info("Creating turbine lists");
            turbineList = new List<Turbine>();
            foreach (string turbinePrefix in turbinePrefixList)
            {
                this.turbineList.Add(new Turbine(turbinePrefix, server));
            }
        }

        /// <summary>
        /// Command to pause a turbine given a Turbine object. Also known as loadshutdown
        /// </summary>
        /// <param name="turbine"></param>
        public void pauseTurbine(Turbine turbine)
        {
            foreach(Turbine turbineInList in turbineList)
            {
                if (turbineInList.Equals(turbine))
                {
                    log.Info("Attempting to pause turbine "+turbine.getTurbinePrefixValue()+" from the factory");
                    turbine.sentLoadShutdownCmd();
                }
            }
        }

        /// <summary>
        /// Command to pause a turbine given a Turbine prefix. Also known as loadshutdown
        /// </summary>
        /// <param name="turbine"></param>
        public void pauseTurbine(string turbinePrefix)
        {
            foreach(Turbine turbineInList in turbineList)
            {
                if (turbineInList.getTurbinePrefixValue().Equals(turbinePrefix))
                {
                    log.Info("Attempting to pause turbine "+turbineInList.getTurbinePrefixValue()+" from the factory");
                    turbineInList.sentLoadShutdownCmd();
                }
            }
        }

        /// <summary>
        /// Method to get a turbine object given a turbine prefix (ie T001)
        /// </summary>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public Turbine getTurbine(string prefix)
        {
            foreach(Turbine turbine in turbineList)
            {
                if (turbine.getTurbinePrefixValue().Equals(prefix))
                {
                    return turbine;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns the list of turbines for the site
        /// </summary>
        /// <returns></returns>
        public List<Turbine> getTurbineList()
        {
            return this.turbineList;
        }

    }
}
