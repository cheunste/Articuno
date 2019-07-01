using log4net;
using OpcLabs.EasyOpc.DataAccess;
using OpcLabs.EasyOpc.DataAccess.OperationModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Articuno
{
    /// <summary>
    /// The turbine Mediator is a singleton class that acts also acts as a mediator and it is not only responsible 
    /// for creating the numerous turbines at the site, but also acts as a middle man between the turbine class and the rest of hte program
    /// to provide turbine information (temperature, wind speed, rotor speed, operating state, etc.)
    /// It takes in a list of turbine prefixes and then creates a turbine class based  on the prefix
    /// </summary>

    /*
     * Turbine Mediator class is used to create turbines and provides a layer of abstraction and provide turbine related information upon request.
     * 
     * The reason why this doesn't use an interface is that there is only one type of turbine, but multiple
     * of the same turbine is needed to be created.
     * 
     * Furthermore, this 'factory' class also provides the list of turbines it creates along
     * with providing several methods to get the turbine's OPC tags (via getXXX functions) and values (via readXXX functions).
     * Both the above returns a list
     * 
     */
    class TurbineMediator
    {
        //Log
        private static readonly ILog log = LogManager.GetLogger(typeof(TurbineMediator));

        //List of turbine related lists
        private static List<Turbine> turbineList;
        private List<string> turbinePrefixList;

        //These are temp lists that are used for read and get functions in the class 
        private List<Object> tempObjectList;

        //This is just an unused List that is used temporary. Used so that I don't have to instantiate an object everytime I need a list
        private List<string> tempList;

        //Instance of OpcServer. Might not be needed
        private OpcServer server;
        private string opcServerName;
        private EasyDAClient client = new EasyDAClient();

        /// <summary>
        /// Constructor for the turbine factory class. Takes in a string list of Turbine prefixes (ie T001). Probably should be called only once
        /// </summary>
        /// <param name="turbinePrefixList"></param>
        public TurbineMediator(List<string> turbinePrefixList, OpcServer server)
        {
            turbineList = new List<Turbine>();
            tempList = new List<string>();
            tempObjectList = new List<Object>();

            this.turbinePrefixList = turbinePrefixList;
            this.server = server;

        }
        public TurbineMediator(List<string> turbinePrefixList, string opcServerName)
        {
            turbineList = new List<Turbine>();
            tempList = new List<string>();
            tempObjectList = new List<Object>();

            this.turbinePrefixList = turbinePrefixList;
            this.opcServerName = opcServerName;

        }
        private TurbineMediator()
        {
            turbineList = new List<Turbine>();
            tempList = new List<string>();
            tempObjectList = new List<Object>();
            this.opcServerName = getOpcServerName();
        }

        private string getOpcServerName()
        {
            return DatabaseInterface.Instance.getOpcServer();
        }

        public static TurbineMediator Instance { get { return Nested.instance; } }

        private class Nested
        {
            static Nested()
            {
            }
            internal static readonly TurbineMediator instance = new TurbineMediator();

        }


        /// <summary>
        /// Create the turbines from the database.
        /// </summary>
        public void createTurbines()
        {
            log.Info("Creating turbine lists");
            turbineList = new List<Turbine>();
            turbineList.Clear();
            DatabaseInterface dbi = DatabaseInterface.Instance;
            foreach (string turbinePrefix in turbinePrefixList)
            {
                Turbine turbine = new Turbine(turbinePrefix, opcServerName);

                //For Turbine tags from the  TurbineInputTags Table
                string cmd =
                    String.Format("SELECT * " +
                    "from TurbineInputTags WHERE TurbineId='{0}'", turbinePrefix);
                //Dictionary<string, object> reader = dbi.readCommand2(cmd);
                //Dictionary<string, object> reader = null;
                DataTable reader = dbi.readCommand(cmd);
                turbine.setNrsStateTag(reader.Rows[0]["NrsMode"].ToString());
                turbine.setOperatingStateTag(reader.Rows[0]["OperatingState"].ToString());
                turbine.setRotorSpeedTag(reader.Rows[0]["RotorSpeed"].ToString());
                turbine.setTemperatureTag(reader.Rows[0]["Temperature"].ToString());
                turbine.setWindSpeedTag(reader.Rows[0]["WindSpeed"].ToString());
                turbine.setParticipationTag(reader.Rows[0]["Participation"].ToString());

                string primMetTower = reader.Rows[0]["MetReference"].ToString();
                //MetTower metTower = MetTowerMediator.getMetTower(primMetTower);
                MetTower metTower = MetTowerMediator.Instance.getMetTower(primMetTower);
                turbine.setMetTower(metTower);

                try
                {
                    //If the RedundancyForMet is not empty, then that means
                    //The met tower is noted to be used as a temperature measurement source
                    //In case a met tower fails
                    if (reader.Rows[0]["RedundancyForMet"].ToString() != null)
                    {
                        string backupMetTower = reader.Rows[0]["RedundancyForMet"].ToString();
                        MetTowerMediator.Instance.setTurbineBackup(backupMetTower, turbine);
                    }
                }
                catch (Exception e)
                {
                    //no operation. Reaching here implies this met tower isn't set up for redundancy 
                }

                //For Turbine tags from the TurbineOutputTags Table There might be duplicates
                cmd = String.Format("SELECT * " +
                    "from TurbineOutputTags WHERE TurbineId='{0}'", turbinePrefix);
                reader = dbi.readCommand(cmd);

                turbine.setAlarmTag(reader.Rows[0]["Alarm"].ToString());
                turbine.setLoadShutdownTag(reader.Rows[0]["Pause"].ToString());

                //Add turbine to the turbine list
                turbineList.Add(turbine);
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
                    log.InfoFormat("Attempting to pause turbine {0} from the factory", turbine.getTurbinePrefixValue());
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
                    log.InfoFormat("Attempting to pause turbine {0} from the factory", turbineInList.getTurbinePrefixValue());
                    turbineInList.writeLoadShutdownCmd();
                }
            }
        }

        /// <summary>
        /// Method to get a turbine object given a turbine prefix (ie T001)
        /// </summary>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public static Turbine getTurbine(string prefix)
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
        public List<Turbine> getTurbineList() { return turbineList; }

        /*
         * The next several methods will be returning the OPC tag name for the turbine List
         * (including the turbine prefix) in a List<string>
        */
        public List<string> getTurbineWindSpeedTag()
        {
            tempList.Clear();
            foreach (Turbine turbine in turbineList) { tempList.Add(turbine.getWindSpeedTag()); }
            return tempList;
        }

        public List<string> getRotorSpeedTag()
        {
            tempList.Clear();
            foreach (Turbine turbine in turbineList) { tempList.Add(turbine.getRotorSpeedTag()); }
            return tempList;
        }
        public List<string> getOperatingStateTag()
        {
            tempList.Clear();
            foreach (Turbine turbine in turbineList) { tempList.Add(turbine.getOperatinStateTag()); }
            return tempList;
        }
        public List<string> getNrsStateTag()
        {
            tempList.Clear();
            foreach (Turbine turbine in turbineList) { tempList.Add(turbine.getNrsStateTag()); }
            return tempList;
        }
        public List<string> getTemperatureTag()
        {
            tempList.Clear();
            foreach (Turbine turbine in turbineList) { tempList.Add(turbine.getTemperatureTag()); }
            return tempList;
        }
        public List<string> getLoadShutdownTag()
        {
            tempList.Clear();
            foreach (Turbine turbine in turbineList) { tempList.Add(turbine.getLoadShutdownTag()); }
            return tempList;
        }
        public List<string> getTurbineCtrTag()
        {
            tempList.Clear();
            foreach (Turbine turbine in turbineList) { tempList.Add(turbine.getTurbineCtrTag()); }
            return tempList;
        }
        public List<string> getHumidityTag()
        {
            tempList.Clear();
            foreach (Turbine turbine in turbineList) { tempList.Add(turbine.getTurbineHumidityTag()); }
            return tempList;
        }

        public static string getTurbineWindSpeedTag(string turbineId) { return getTurbine(turbineId).getWindSpeedTag(); }
        public static string getRotorSpeedTag(string turbineId) {return getTurbine(turbineId).getRotorSpeedTag();}
        public static string getOperatingStateTag(string turbineId) {return getTurbine(turbineId).getOperatinStateTag();}
        public static string getNrsStateTag(string turbineId) {return getTurbine(turbineId).getNrsStateTag();}
        public static string getTemperatureTag(string turbineId) {return getTurbine(turbineId).getTemperatureTag();}
        public static string getLoadShutdownTag(string turbineId) {return getTurbine(turbineId).getLoadShutdownTag();}
        public static string getTurbineCtrTag(string turbineId) {return getTurbine(turbineId).getTurbineCtrTag();}
        public static string getHumidityTag(string turbineId) {return getTurbine(turbineId).getTurbineHumidityTag();}

        /*
         * The following methods will return the list containing the vlaue  of the opcTag
         */
        public Object readTurbineWindSpeedTag() { return readMutlipleOpcTags(getTurbineWindSpeedTag()); }
        public Object readRotorSpeedTag() { return readMutlipleOpcTags(getRotorSpeedTag()); }
        public Object readOperatingStateTag() { return readMutlipleOpcTags(getOperatingStateTag()); }
        public Object readNrsStateTag() { return readMutlipleOpcTags(getNrsStateTag()); }
        public Object readTemperatureTag() { return readMutlipleOpcTags(getTemperatureTag()); }
        public Object readTurbineCtrTag() { return readMutlipleOpcTags(getTurbineCtrTag()); }
        public Object readHumidityTag() { return readMutlipleOpcTags(getHumidityTag()); }

        //For reading (using turbineId)
        public Object readTurbineWindSpeedTag(string turbineId) {return client.ReadItemValue("", opcServerName,getTurbineWindSpeedTag(turbineId)); }
        public Object readRotorSpeedTag(string turbineId) {return client.ReadItemValue("", opcServerName,getRotorSpeedTag(turbineId)); }
        public Object readOperatingStateTag(string turbineId) {return client.ReadItemValue("",opcServerName,getOperatingStateTag(turbineId));}
        public Object readNrsStateTag(string turbineId) {return client.ReadItemValue("",opcServerName,getNrsStateTag(turbineId));}
        public Object readTemperatureTag(string turbineId) {return client.ReadItemValue("",opcServerName,getTemperatureTag(turbineId));}
        public Object readTurbineCtrTag(string turbineId) {return client.ReadItemValue("",opcServerName,getTurbineCtrTag(turbineId));}
        public Object readHumidityTag(string turbineId) {return client.ReadItemValue("",opcServerName,getHumidityTag(turbineId));}

        //For writing (using turbineId). Note that the mediator really shouldn't be writing to all the availble turbine tags. If you need to test something, you need to create a turbine object 
        public void writeNrsStateTag(string turbineId,object value) { getTurbine(turbineId).writeNoiseLevel(value); }
        public void writeTurbineCtrTag(string turbineId,int value) { getTurbine(turbineId).writeTurbineCtrValue(value); }
        public void writeLoadShutDownCmd(string turbineId ) { getTurbine(turbineId).writeLoadShutdownCmd(); }

        private Object readOpcTag() { return null; }

        private Object readMutlipleOpcTags(List<string> tempList)
        {

            List<Object> valueList = new List<Object>();
            var itemDescriptors = new DAItemDescriptor[tempList.Count];

            for (int i = 0; i < tempList.Count; i++)
            {
                Console.WriteLine(tempList.ElementAt(i));
                itemDescriptors[i] = new DAItemDescriptor(tempList.ElementAt(i));
            }

            DAVtqResult[] vtqResults = this.client.ReadMultipleItems(this.opcServerName, itemDescriptors);

            for (int i = 0; i < vtqResults.Length; i++)
            {
                //Console.WriteLine(vtqResults[i].Vtq.Value);
                valueList.Add(vtqResults[i].Vtq.Value);
            }
            //return vtqResults;
            return valueList;
        }

        /// <summary>
        /// Used for testing only. This creates a testing scenario that uses only T001 
        /// </summary>
        public void createTestTurbines()
        {
            turbinePrefixList.Add("T001");
            getOpcServerName();
        }
    }
}
