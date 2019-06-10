using log4net;
using OpcLabs.EasyOpc.DataAccess;
using OpcLabs.EasyOpc.DataAccess.OperationModel;
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

    /*
     * Turbine FActory is technically a factory class that is used to create turbines.
     * 
     * The reason why this doesn't use an interface is that there is only one type of turbine, but multiple
     * of the same turbine is needed to be created.
     * 
     * Furthermore, this 'factory' class also provides the list of turbines it creates along
     * with providing several methods to get the turbine's OPC tags (via getXXX functions) and values (via readXXX functions).
     * Both the above returns a list
     * 
     */
    class TurbineFactory
    {
        //Log
        private static readonly ILog log = LogManager.GetLogger(typeof(TurbineFactory));

        //List of turbine related lists
        private List<Turbine> turbineList;
        private List<string> turbinePrefixList;

        //These are temp lists that are used for read and get functions in the class 
        private List<Object> tempObjectList;
        private List<string> tempList;

        //Instance of OpcServer. Might not be needed
        private OpcServer server;
        private string opcServerName;

        /// <summary>
        /// Constructor for the turbine factory class. Takes in a string list of Turbine prefixes (ie T001). Probably should be called only once
        /// </summary>
        /// <param name="turbinePrefixList"></param>
        public TurbineFactory(List<string> turbinePrefixList, OpcServer server)
        {
            turbineList = new List<Turbine>();
            tempList = new List<string>();
            tempObjectList = new List<Object>();

            this.turbinePrefixList = turbinePrefixList;
            this.server = server;

        }
        public TurbineFactory(List<string> turbinePrefixList,string opcServerName)
        {
            turbineList = new List<Turbine>();
            tempList = new List<string>();
            tempObjectList = new List<Object>();

            this.turbinePrefixList = turbinePrefixList;
            this.opcServerName = opcServerName;

        }


        /// <summary>
        /// Create the turbines from the database.
        /// </summary>
        public void createTurbines()
        {
            log.Info("Creating turbine lists");
            turbineList = new List<Turbine>();
            turbineList.Clear();
            DatabaseInterface dbi = new DatabaseInterface();
            foreach (string turbinePrefix in turbinePrefixList)
            {
                Turbine turbine = new Turbine(turbinePrefix, "");
                turbine.setLoadShutdownTag(dbi.readCommand("SELECT Pause from TurbineInputTags WHERE TurbineId=" + turbinePrefix).ToString());
                turbine.setNrsStateTag(dbi.readCommand("SELECT NrsMode from TurbineInputTags WHERE TurbineId=" + turbinePrefix).ToString());
                turbine.setOperatinStateTag(dbi.readCommand("SELECT OperatingState from TurbineInputTags WHERE TurbineId=" + turbinePrefix).ToString());
                turbine.setRotorSpeedTag(dbi.readCommand("SELECT RotorSpeed from TurbineInputTags WHERE TurbineId=" + turbinePrefix).ToString());
                turbine.setTemperatureTag(dbi.readCommand("SELECT Temperature from TurbineInputTags WHERE TurbineId=" + turbinePrefix).ToString());
                turbine.setWindSpeedTag(dbi.readCommand("SELECT WindSpeed from TurbineInputTags WHERE TurbineId=" + turbinePrefix).ToString());
                this.turbineList.Add(turbine);
            }
        }

        /// <summary>
        /// Command to pause a turbine given a Turbine object. Also known as loadshutdown
        /// </summary>
        /// <param name="turbine"></param>
        public void pauseTurbine(Turbine turbine)
        {
            foreach (Turbine turbineInList in turbineList)
            {
                if (turbineInList.Equals(turbine))
                {
                    log.Info("Attempting to pause turbine " + turbine.getTurbinePrefixValue() + " from the factory");
                    turbine.writeLoadShutdownCmd();
                }
            }
        }

        /// <summary>
        /// Command to pause a turbine given a Turbine prefix. Also known as loadshutdown
        /// </summary>
        /// <param name="turbine"></param>
        public void pauseTurbine(string turbinePrefix)
        {
            foreach (Turbine turbineInList in turbineList)
            {
                if (turbineInList.getTurbinePrefixValue().Equals(turbinePrefix))
                {
                    log.Info("Attempting to pause turbine " + turbineInList.getTurbinePrefixValue() + " from the factory");
                    turbineInList.writeLoadShutdownCmd();
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
            foreach (Turbine turbine in turbineList)
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

        /*
         * The next several methods will be returning the OPC tag name for the turbine List
         * (including the turbine prefix) in a List<string>
        */
        public List<string> getTurbineWindSpeedTag()
        {
            tempList.Clear();
            foreach (Turbine turbine in this.turbineList)
            {
                tempList.Add(turbine.getTurbinePrefixValue() + turbine.getWindSpeedTag());
            }
            return tempList;
        }

        public List<string> getRotorSpeedTag()
        {
            tempList.Clear();
            foreach (Turbine turbine in this.turbineList)
            {
                tempList.Add(turbine.getTurbinePrefixValue() + turbine.getRotorSpeedTag());
            }
            return tempList;
        }
        public List<string> getOperatingStateTag()
        {
            tempList.Clear();
            foreach (Turbine turbine in this.turbineList)
            {
                tempList.Add(turbine.getTurbinePrefixValue() + turbine.getOperatinStateTag());
            }
            return tempList;
        }
        public List<string> getNrsStateTag()
        {
            tempList.Clear();
            foreach (Turbine turbine in this.turbineList)
            {
                tempList.Add(turbine.getTurbinePrefixValue() + turbine.getNrsStateTag());
            }
            return tempList;
        }
        public List<string> getTemperatureTag()
        {
            tempList.Clear();
            foreach (Turbine turbine in this.turbineList)
            {
                tempList.Add(turbine.getTurbinePrefixValue() + turbine.getTemperatureTag());
            }
            return tempList;
        }
        public List<string> getLoadShutdownTag()
        {
            tempList.Clear();
            foreach (Turbine turbine in this.turbineList)
            {
                tempList.Add(turbine.getTurbinePrefixValue() + turbine.getLoadShutdownTag());
            }
            return tempList;
        }
        public List<string> getTurbineCtrTag()
        {
            foreach (Turbine turbine in this.turbineList)
            {
                tempList.Add(turbine.getTurbinePrefixValue() + turbine.getTurbineCtrTag());
            }
            return tempList;
        }
        public List<string> getHumidityTag()
        {
            tempList.Clear();
            foreach (Turbine turbine in this.turbineList)
            {
                tempList.Add(turbine.getTurbinePrefixValue() + turbine.getTurbineHumidityTag());
            }
            return tempList;
        }

        /*
         * The following methods will return the list containing the vlaue  of the opcTag
         * 
         * 
         * 
         */
        //public List<Object> readTurbineWindSpeedTag()
        public Object readTurbineWindSpeedTag()
        {
            var client = new EasyDAClient();
            DAItemDescriptor temp = new DAItemDescriptor();

            DAVtqResult[] vtqResults = client.ReadMultipleItems(this.opcServerName,
                new DAItemDescriptor[]
                { this.getTurbineWindSpeedTag().ToString()
                } 
            );

            return vtqResults;
            //tempObjectList.Clear();
            //foreach (Turbine turbine in this.turbineList)
            //{
            //    tempObjectList.Add(turbine.readWindSpeedValue());
            //}
            //return tempObjectList;
        }

        public Object readRotorSpeedTag()
        {
            throw new NotImplementedException();
        }
        public Object readOperatingStateTag()
        {
            throw new NotImplementedException();
        }
        public Object readNrsStateTag()
        {
            throw new NotImplementedException();
        }
        public Object readTemperatureTag()
        {
            throw new NotImplementedException();
        }
        public Object readTurbineCtrTag()
        {
            throw new NotImplementedException();
        }
        public Object readHumidityTag()
        {
            throw new NotImplementedException();
        }

    }
}
