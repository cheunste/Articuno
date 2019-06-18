using log4net;
using OpcLabs.EasyOpc.DataAccess;
using OpcLabs.EasyOpc.OperationModel;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("ArticunoTest")]
namespace Articuno
{
    /// <summary>
    /// The Met Tower class is responsible for fetching humidity and temperature from the field's met tower and perform calculations when valid
    /// Fetches: humidty and temperature values from the field
    /// Calculates: Dew Point, Delta Temp threshold
    /// Errors: Thrwos errors if values are bad 
    /// </summary>

    internal class MetTower
    {
        //Constants. Can't Fully Capitalize these ones. Too hard to read. Note the staritng capital letter though
        //Temperature table columns
        private static string PrimTempValueTag = "PrimTempValueTag";
        private static string SecTempValueTag = "SecTempValueTag";
        private static string TempBadQualityTag = "TempBadQualityTag";
        private static string TempOutOfRangeTag = "TempOutOfRangeTag";

        //Humidity table columns
        private static string PrimHumidityValueTag = "PrimHumidityValueTag";
        private static string SecHumidityValueTag = "SecHumidityValueTag";
        private static string HumidityBadQualityTag = "HumidityBadQualityTag";
        private static string HumidityOutOfRangeTag = "HumidityOutOfRangeTag";

        //Other table column 
        private static string NoDataAlarmTag = "NoDataAlarmTag";
        private static string IceIndicationTag = "IceIndicationTag";

        //Query constants
        String TEMPERATURE_QUERY = "SELECT " + PrimTempValueTag + "," + SecTempValueTag + "," + TempBadQualityTag + "," + TempOutOfRangeTag + " FROM MetTower";
        String RH_QUERY = "SELECT " + PrimHumidityValueTag + "," + SecHumidityValueTag + "," + HumidityBadQualityTag + "," + HumidityOutOfRangeTag + " FROM MetTower";
        String OTHER_PARAM_QUERY = "SELECT " + NoDataAlarmTag + ", " + IceIndicationTag + " FROM MetTower";

        String INPUT_TAG_QUERY = "SELECT * FROM MetTowerInputTags";
        String OTUPUT_TAG_QUERY = "SELECT * FROM MetTowerOutputTags";

        //Member variables;
        //Member doubles 
        private double ambientTemperature;
        private double relativeHumidity;
        private double dewPointTemperature;
        private double ambTempThreshold;
        private double deltaTempThreshold;

        //Member bool
        private bool metTowerFailure;

        //Member prefix
        private string metTowerPrefix;

        //Member variable OPC Tags (strings)
        //For temperature
        private string primTempTag;
        private string secTempTag;
        private string tempBadQualityTag;
        private string tempOutOfRangeTag;

        //For humidity
        private string primRHTag;
        private string secRHTag;
        private string rhBadQualityTag;
        private string rhOutOfRangeTag;

        //For other tags relating to the met tower 
        private string noDataAlarmTag;
        private string iceIndicationTag;

        //opc server
        private OpcServer opcServer;
        private string opcServerName;
        private EasyDAClient client = new EasyDAClient();

        //log
        private static readonly ILog log = LogManager.GetLogger(typeof(MetTower));

        /// <summary>
        /// Constructor for the Met Tower Class. Takes in a Met Id. Using  the met Id, the constructor will then query
        /// the DB for all the relevant values
        /// </summary>
        /// <param name="MetId"></param>
        public MetTower(string MetId, double ambTempThreshold, double deltaTempThreshold, OpcServer opcServerInstance)
        {
            //Open a connection to the DB
            DatabaseInterface dbi = new DatabaseInterface();
            //Set up the query
            metTowerQuerySetup(MetId);
            //Set OPC Server
            opcServer = opcServerInstance;
            //set the member thresholds
            this.ambTempThreshold = ambTempThreshold;
            this.deltaTempThreshold = deltaTempThreshold;

            //Set OPC Server Name
            this.opcServerName = opcServerName;

        }

        public MetTower(string MetId, double ambTempThreshold, double deltaTempThreshold, string opcServerName)
        {
            //Open a connection to the DB
            DatabaseInterface dbi = new DatabaseInterface();
            //Set up the query
            metTowerQuerySetup(MetId);
            //Set OPC Server Name
            this.opcServerName = opcServerName;
            //set the member thresholds
            this.ambTempThreshold = ambTempThreshold;
            this.deltaTempThreshold = deltaTempThreshold;

            //Set OPC Server Name
            this.opcServerName = opcServerName;

            //Set up met id
            this.metTowerPrefix = MetId;
        }

        //This method queries the sqlite table and then sets the tags in the table to the members of the class
        private void metTowerQuerySetup(string MetId)
        {
            //Open a connection to the DB
            DatabaseInterface dbi = new DatabaseInterface();
            dbi.openConnection();

            dbi.openConnection();
            //Get everything relating to the MetTowerInputTags table
            SQLiteDataReader reader = dbi.readCommand(INPUT_TAG_QUERY + " HWERE MetId=" + MetId);
            reader.Read();
            this.primTempTag = reader[PrimTempValueTag].ToString();
            this.secTempTag = reader[SecTempValueTag].ToString();
            this.primRHTag = reader[PrimHumidityValueTag].ToString();
            this.secRHTag = reader[SecHumidityValueTag].ToString();

            //Get everything relating to the MetTowerOutputTags table
            reader = dbi.readCommand(OTUPUT_TAG_QUERY + " WHERE MetId=" + MetId);
            reader.Read();
            this.tempBadQualityTag = reader[TempBadQualityTag].ToString();
            this.tempOutOfRangeTag = reader[TempOutOfRangeTag].ToString();
            this.iceIndicationTag = reader[IceIndicationTag].ToString();
            this.rhOutOfRangeTag = reader[HumidityOutOfRangeTag].ToString();
            this.rhBadQualityTag = reader[HumidityBadQualityTag].ToString();
            this.noDataAlarmTag = reader[NoDataAlarmTag].ToString();

            dbi.closeConnection();
        }


        /// <summary>
        /// Get Relative Humidity from OPC Server
        /// </summary>
        /// <returns></returns>
        //public string readRelativeHumidityValue() { return opcServer.readTagValue(primRHTag).ToString(); }
        public Object readRelativeHumidityValue() { return new EasyDAClient().ReadItemValue("", opcServerName, this.primRHTag); }

        /// <summary>
        /// Set the field of relative humidty 
        /// </summary>
        /// <param name="value"></param>
        //public void writeRelativeHumityValue(double value) { opcServer.writeTagValue(readRelativeHumidityTag(), value); }
        public void writeRelativeHumityValue(double value) { client.WriteItemValue("", opcServerName, getRelativeHumidityTag(), value); }

        /// <summary>
        /// Get the prim relative humidity tag
        /// </summary>
        /// <returns></returns>
        public string getRelativeHumidityTag() { return this.primRHTag; }

        /// <summary>
        /// Set the Relativie Humidity Tag. Used only internally to this program
        /// </summary>
        public void setRelativeHumidityTag(string tag) { this.primRHTag = tag; }

        /// <summary>
        /// Get the Primary Temperature value from the met tower
        /// </summary>
        /// <returns></returns>
        //public string readPrimTemperatureValue() { return opcServer.readTagValue(primTempTag).ToString(); }
        public Object readPrimTemperatureValue() { return new EasyDAClient().ReadItemValue("", opcServerName, this.primTempTag); }
        /// <summary>
        /// Sets the primary temperature field. Used for this program only
        /// </summary>
        //public void writePrimTemperatureValue(double value) { opcServer.writeTagValue(getPrimTemperatureTag(), value); }
        public void writePrimTemperatureValue(double value) { client.WriteItemValue("", opcServerName, getPrimTemperatureTag(), value); }

        /// <summary>
        /// Returns the OpcTag for the primary temperature tag
        /// </summary>
        /// <returns></returns>
        public string getPrimTemperatureTag() { return this.primTempTag; }

        /// <summary>
        /// Set the primary temperature tag.
        /// </summary>
        public void setPrimTemperatureTag(string tag) { this.primTempTag = tag; }

        /// <summary>
        /// Get the SEcondary Temperature value from the met tower
        /// </summary>
        /// <returns></returns>
        //public string readSecTemperatureValue() { return opcServer.readTagValue(secTempTag).ToString(); }
        public Object readSecTemperatureValue() { return new EasyDAClient().ReadItemValue("", opcServerName, this.secTempTag); }

        /// <summary>
        /// Sets the primary temperature field. Used for this program only
        /// </summary>
        //public void writeSecTemperatureValue(double value) { opcServer.writeTagValue(getSecTemperatureTag(), value); }
        public void writeSecTemperatureValue(double value) { client.WriteItemValue("", opcServerName, getSecTemperatureTag(), value); }

        /// <summary>
        /// Returns the OpcTag for the primary temperature tag
        /// </summary>
        /// <returns></returns>
        public string getSecTemperatureTag() { return this.secTempTag; }

        /// <summary>
        /// Set the secondary temperature tag.
        /// </summary>
        public void setSecTemperatureTag(string tag) { this.secTempTag = tag; }

        //Getters for System OPC Tag indicators
        /// <summary>
        /// Gets the  NoDataAlaarmValue OPC value
        /// </summary>
        /// <returns></returns>
        public string getNoDataAlarmValue() { return noDataAlarmTag; }
        /// <summary>
        /// Gets the IceIndicationValue OPC Value
        /// </summary>
        /// <returns></returns>
        public string getIceIndicationValue() { return iceIndicationTag; }

        //Threshold setters and getters
        public double AmbTempThreshold { get; set; }
        public double DeltaTempThreshold { get; set; }

        //Met Id methods
        public string getMetTowerPrefix
        {
            set { }
            get { return this.metTowerPrefix; }

        }

    }
}
