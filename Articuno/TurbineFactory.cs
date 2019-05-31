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

        public TurbineFactory(List<string> turbinePrefixList)
        {
            turbineList = new List<Turbine>();

        }

        /// <summary>
        /// Create the turbines given a list of prefixes. 
        /// 
        /// SHOULD ONLY BE CALLED ONCE
        /// </summary>
        public void createTurbines()
        {
            log.Info("Creating turbine lists");
            throw new NotImplementedException();
        }

        public void pauseTurbine(Turbine turbine)
        {
            throw new NotImplementedException();
        }

        private void addTurbineToList(Turbine turbine)
        {
            throw new NotImplementedException();
        }
        
        /// <summary>
        /// Method to get a turbine object given a turbine prefix
        /// </summary>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public Turbine getTurbine(string prefix)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Method to get turbine object given a turbine 
        /// </summary>
        /// <param name="turbine"></param>
        /// <returns></returns>
        public Turbine getTurbine(Turbine turbine)
        {
            throw new NotImplementedException();
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
