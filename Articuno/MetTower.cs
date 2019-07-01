using log4net;
using OpcLabs.EasyOpc.DataAccess;
using OpcLabs.EasyOpc.OperationModel;
using System;
using System.Collections.Generic;
using System.Data;
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
        //Constants. The following are SQL lite table column
        //Temperature table columns
        private static readonly string PrimTempValueTagColumn = "PrimTempValueTag";
        private static readonly string SecTempValueColumn = "SecTempValueTag";
        private static readonly string TempPrimBadQualityColumn = "TempPrimBadQualityTag";
        private static readonly string TempPrimOutOfRangeColumn = "TempPrimOutOfRangeTag";
        private static readonly string TempSecBadQualityColumn = "TempSecBadQualityTag";
        private static readonly string TempSecOutOfRangeColumn = "TempSecOutOfRangeTag";

        //Humidity table columns
        private static readonly string PrimHumidityValueColumn = "PrimHumidityValueTag";
        private static readonly string SecHumidityValueColumn = "SecHumidityValueTag";
        private static readonly string HumidityBadQualityColumn = "HumidityBadQualityTag";
        private static readonly string HumidityOutOfRangeColumn = "HumidityOutOfRangeTag";

        //Other table column 
        private static readonly string NoDataAlarmColumn = "NoDataAlarmTag";
        private static readonly string IceIndicationColumn = "IceIndicationTag";

        //Query constants
        readonly String TEMPERATURE_QUERY = "SELECT " + PrimTempValueTagColumn + "," + SecTempValueColumn + "," + TempPrimBadQualityColumn + "," + TempPrimOutOfRangeColumn + " FROM MetTower";
        readonly String RH_QUERY = "SELECT " + PrimHumidityValueColumn + "," + SecHumidityValueColumn + "," + HumidityBadQualityColumn + "," + HumidityOutOfRangeColumn + " FROM MetTower";
        readonly String OTHER_PARAM_QUERY = "SELECT " + NoDataAlarmColumn + ", " + IceIndicationColumn + " FROM MetTower";

        readonly String INPUT_TAG_QUERY = "SELECT * FROM MetTowerInputTags";
        readonly String OUTPUT_TAG_QUERY = "SELECT * FROM MetTowerOutputTags";

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
        private string tempPrimBadQualityTag;
        private string tempPrimOutOfRangeTag;
        private string tempSecBadQualityTag;
        private string tempSecOutOfRangeTag;

        //For humidity
        private string primRHTag;
        private string secRHTag;
        private string rhBadQualityTag;
        private string rhOutOfRangeTag;

        //For other tags relating to the met tower 
        private string noDataAlarmTag;
        private string iceIndicationTag;

        //Turbine reference (for backup)
        private Turbine nearestTurbine;

        //opc server
        private OpcServer opcServer;
        private string opcServerName;
        private EasyDAClient client = new EasyDAClient();

        //database
        private DatabaseInterface dbi;

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
            dbi = DatabaseInterface.Instance;
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
            dbi = DatabaseInterface.Instance;
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
            //DatabaseInterface dbi = new DatabaseInterface();

            //Get everything relating to the MetTowerInputTags table
            DataTable reader = dbi.readCommand(INPUT_TAG_QUERY + String.Format(" WHERE MetId='{0}'",MetId));
            this.primTempTag = reader.Rows[0][PrimTempValueTagColumn].ToString();
            this.secTempTag = reader.Rows[0][SecTempValueColumn].ToString();
            this.primRHTag = reader.Rows[0][PrimHumidityValueColumn].ToString();
            this.secRHTag = reader.Rows[0][SecHumidityValueColumn].ToString();

            //Get everything relating to the MetTowerOutputTags table
            reader = dbi.readCommand(OUTPUT_TAG_QUERY + String.Format(" WHERE MetId='{0}'", MetId));
            this.tempPrimBadQualityTag = reader.Rows[0][TempPrimBadQualityColumn].ToString();
            this.tempPrimOutOfRangeTag = reader.Rows[0][TempPrimOutOfRangeColumn].ToString();
            this.tempSecBadQualityTag = reader.Rows[0][TempSecBadQualityColumn].ToString();
            this.tempSecOutOfRangeTag = reader.Rows[0][TempSecOutOfRangeColumn].ToString();

            this.iceIndicationTag = reader.Rows[0][IceIndicationColumn].ToString();
            this.rhOutOfRangeTag = reader.Rows[0][HumidityOutOfRangeColumn].ToString();
            this.rhBadQualityTag = reader.Rows[0][HumidityBadQualityColumn].ToString();
            this.noDataAlarmTag = reader.Rows[0][NoDataAlarmColumn].ToString();

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
        public string getNoDataAlarmTag() { return noDataAlarmTag; }
        public void setNoDataAlarmTag(string tag) { noDataAlarmTag = tag; }
        public Object readNoDataAlarmValue() { return new EasyDAClient().ReadItemValue("", opcServerName, getNoDataAlarmTag()); }
        public void writeNoDataAlarmValue(object value ) { client.WriteItemValue("", opcServerName, getNoDataAlarmTag(), value); }
        /// <summary>
        /// Gets the IceIndicationValue OPC Value
        /// </summary>
        /// <returns></returns>
        public string getIceIndicationTag() { return iceIndicationTag; }
        public void setIceIndicationTag(string tag) { iceIndicationTag = tag; }
        public Object readIceIndicationValue() { return new EasyDAClient().ReadItemValue("", opcServerName, getIceIndicationTag()); }
        public void writeIceIndicationValue(double value) { client.WriteItemValue("", opcServerName, getIceIndicationTag(), value); }

        //Threshold setters and getters
        public double AmbTempThreshold { get { return ambTempThreshold; } set { ambTempThreshold = value; } }
        public double DeltaTempThreshold { get { return deltaTempThreshold; } set { deltaTempThreshold = value; } }

        //The following are for humidity out of range, bad quality, etc.
        public Object readHumidityOutOfRng() { return new EasyDAClient().ReadItemValue("", opcServerName, rhOutOfRangeTag); }
        public void writeHumidityOutOfRng(bool value) { client.WriteItemValue("", opcServerName, rhOutOfRangeTag, value); }
        public Object readHumidityBadQuality() { return new EasyDAClient().ReadItemValue("", opcServerName, rhBadQualityTag); }
        public void writeHumidityBadQuality(bool value) { client.WriteItemValue("", opcServerName, rhBadQualityTag, value); }

        //The following are for temperature out of range, bad quality
        public Object readTemperaturePrimOutOfRange() { return new EasyDAClient().ReadItemValue("", opcServerName, tempPrimOutOfRangeTag); }
        public void writeTemperaturePrimOutOfRange(bool value) { client.WriteItemValue("", opcServerName, tempPrimOutOfRangeTag, value); }
        public Object readTemperaturePrimBadQuality() { return new EasyDAClient().ReadItemValue("", opcServerName, tempPrimOutOfRangeTag); }
        public void writeTemperaturePrimBadQuality(bool value) { client.WriteItemValue("", opcServerName, tempPrimOutOfRangeTag, value); }

        public Object readTemperatureSecOutOfRange() { return new EasyDAClient().ReadItemValue("", opcServerName, tempSecOutOfRangeTag); }
        public void writeTemperatureSecOutOfRange(bool value) { client.WriteItemValue("", opcServerName, tempSecOutOfRangeTag, value); }
        public Object readTemperatureSecBadQuality() { return new EasyDAClient().ReadItemValue("", opcServerName, tempSecOutOfRangeTag); }
        public void writeTemperatureSecBadQuality(bool value) { client.WriteItemValue("", opcServerName, tempSecOutOfRangeTag, value); }


        public bool isQualityGood(string opcTag)
        {
            DAVtq vtq = client.ReadItem(opcServerName, opcTag);
            return vtq.Quality.IsGood ? true : false;
        }

        //Met Id methods
        public string getMetTowerPrefix
        {
            set { }
            get { return this.metTowerPrefix; }

        }

        //turbien methods for measurement reduedancy
        public void setNearestTurbine(Turbine turbine) { nearestTurbine = turbine; }
        public Turbine getNearestTurbine() { return nearestTurbine; }
    }
}
