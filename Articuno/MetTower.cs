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

        //Queues
        private Queue<double> temperatureQueue;
        private Queue<double> humidityQueue;

        //Member bool
        private bool metTowerFailure;

        //Member prefix
        private string metTowerPrefix;

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

        public MetTower(string MetId, double ambTempThreshold, double deltaTempThreshold, string opcServerName)
        {
            //Open a connection to the DB
            dbi = DatabaseInterface.Instance;
            //Set up the query
            metTowerQuerySetup(MetId);
            //Set OPC Server Name
            this.opcServerName = opcServerName;
            //set the member thresholds
            AmbTempThreshold = ambTempThreshold;
            DeltaTempThreshold = deltaTempThreshold;

            //Set OPC Server Name
            this.opcServerName = opcServerName;

            //Set up met id
            this.metTowerPrefix = MetId;

            temperatureQueue = new Queue<double>();
            humidityQueue = new Queue<double>();
        }

        //This method queries the sqlite table and then sets the tags in the table to the members of the class
        private void metTowerQuerySetup(string MetId)
        {
            //Get everything relating to the MetTowerInputTags table
            DataTable reader = dbi.readCommand(INPUT_TAG_QUERY + String.Format(" WHERE MetId='{0}'", MetId));

            PrimTemperatureTag = reader.Rows[0][PrimTempValueTagColumn].ToString();
            SecTemperatureTag = reader.Rows[0][SecTempValueColumn].ToString();
            RelativeHumidityTag = reader.Rows[0][PrimHumidityValueColumn].ToString();
            HumiditySecValueTag = reader.Rows[0][SecHumidityValueColumn].ToString();

            //Get everything relating to the MetTowerOutputTags table
            reader = dbi.readCommand(OUTPUT_TAG_QUERY + String.Format(" WHERE MetId='{0}'", MetId));
            TemperaturePrimBadQualityTag = reader.Rows[0][TempPrimBadQualityColumn].ToString();
            TemperaturePrimOutOfRangeTag = reader.Rows[0][TempPrimOutOfRangeColumn].ToString();
            TemperatureSecBadQualityTag = reader.Rows[0][TempSecBadQualityColumn].ToString();
            TemperatureSecOutOfRangeTag = reader.Rows[0][TempSecOutOfRangeColumn].ToString();

            HumidtyOutOfRangeTag = reader.Rows[0][HumidityOutOfRangeColumn].ToString();
            HumidityBadQualityTag = reader.Rows[0][HumidityBadQualityColumn].ToString();

            IceIndicationTag = reader.Rows[0][IceIndicationColumn].ToString();
            NoDataAlarmTag = reader.Rows[0][NoDataAlarmColumn].ToString();

        }


        /// <summary>
        /// Get Relative Humidity from OPC Server
        /// </summary>
        /// <returns></returns>
        //public string readRelativeHumidityValue() { return opcServer.readTagValue(primRHTag).ToString(); }
        public Object readRelativeHumidityValue() { return new EasyDAClient().ReadItemValue("", opcServerName, RelativeHumidityTag); }

        /// <summary>
        /// Set the field of relative humidty 
        /// </summary>
        /// <param name="value"></param>
        //public void writeRelativeHumityValue(double value) { opcServer.writeTagValue(readRelativeHumidityTag(), value); }
        public void writeRelativeHumityValue(double value) { client.WriteItemValue("", opcServerName, RelativeHumidityTag, value); }

        public Object RelativeHumityValue
        {
            set { client.WriteItemValue("", opcServerName, RelativeHumidityTag, value); }
            get { return new EasyDAClient().ReadItemValue("", opcServerName, RelativeHumidityTag); }

        }

        /// <summary>
        /// Accessor the RelativeHumidity OPC Tag
        /// </summary>
        public string RelativeHumidityTag { set; get; }

        /// <summary>
        /// Get the Primary Temperature value from the met tower
        /// </summary>
        /// <returns></returns>
        //public string readPrimTemperatureValue() { return opcServer.readTagValue(primTempTag).ToString(); }
        public Object readPrimTemperatureValue() { return new EasyDAClient().ReadItemValue("", opcServerName, PrimTemperatureTag); }
        /// <summary>
        /// Sets the primary temperature field. Used for this program only
        /// </summary>
        //public void writePrimTemperatureValue(double value) { opcServer.writeTagValue(getPrimTemperatureTag(), value); }
        public void writePrimTemperatureValue(double value) { client.WriteItemValue("", opcServerName, PrimTemperatureTag, value); }

        /// <summary>
        /// Accessor for the Primary Temperature OPC Tag
        /// </summary>
        public string PrimTemperatureTag { set; get; }

        /// <summary>
        /// Get the SEcondary Temperature value from the met tower
        /// </summary>
        /// <returns></returns>
        //public string readSecTemperatureValue() { return opcServer.readTagValue(secTempTag).ToString(); }
        public Object readSecTemperatureValue() { return new EasyDAClient().ReadItemValue("", opcServerName, SecTemperatureTag); }

        /// <summary>
        /// Sets the primary temperature field. Used for this program only
        /// </summary>
        //public void writeSecTemperatureValue(double value) { opcServer.writeTagValue(getSecTemperatureTag(), value); }
        public void writeSecTemperatureValue(double value) { client.WriteItemValue("", opcServerName, SecTemperatureTag, value); }

        /// <summary>
        /// Accessosr for SEcondary Temperature OPC Tag
        /// </summary>
        public string SecTemperatureTag { set; get; }


        //Getters for System OPC Tag indicators
        /// <summary>
        /// Gets the  NoDataAlaarmValue OPC value
        /// </summary>
        /// <returns></returns>

        public string NoDataAlarmTag { set; get; }
        public Object NoDataAlarmValue { set { client.WriteItemValue("", opcServerName, NoDataAlarmTag, value); } get { return new EasyDAClient().ReadItemValue("", opcServerName, NoDataAlarmTag); } }
        /// <summary>
        /// Gets the IceIndicationValue OPC Value
        /// </summary>
        /// <returns></returns>
        public string IceIndicationTag { set; get; }
        public Object readIceIndicationValue() { return new EasyDAClient().ReadItemValue("", opcServerName, IceIndicationTag); }
        public void writeIceIndicationValue(double value) { client.WriteItemValue("", opcServerName, IceIndicationTag, value); }

        //Threshold setters and getters
        public double AmbTempThreshold { get { return ambTempThreshold; } set { ambTempThreshold = value; } }
        public double DeltaTempThreshold { get { return deltaTempThreshold; } set { deltaTempThreshold = value; } }

        //Humidity Accessors
        public string HumidityPrimValueTag { get; set; }
        public string HumiditySecValueTag { get; set; }
        public string HumidtyOutOfRangeTag { get; set; }
        public string HumidityBadQualityTag { get; set; }

        //Primary Temperature  Accessors
        public string TemperaturePrimBadQualityTag { get; set; }
        public string TemperaturePrimOutOfRangeTag { get; set; }

        //Secondary Temperature Accessors
        public string TemperatureSecBadQualityTag { get; set; }
        public string TemperatureSecOutOfRangeTag { get; set; }

        //The following are for humidity out of range, bad quality, etc.
        public Object readHumidityOutOfRng() { return new EasyDAClient().ReadItemValue("", opcServerName, HumidtyOutOfRangeTag); }
        public void writeHumidityOutOfRng(bool value) { client.WriteItemValue("", opcServerName, HumidtyOutOfRangeTag, value); }
        public Object readHumidityBadQuality() { return new EasyDAClient().ReadItemValue("", opcServerName, HumidityBadQualityTag); }
        public void writeHumidityBadQuality(bool value) { client.WriteItemValue("", opcServerName, HumidityBadQualityTag, value); }

        //The following are for temperature out of range, bad quality
        public Object readTemperaturePrimOutOfRange() { return new EasyDAClient().ReadItemValue("", opcServerName, TemperaturePrimOutOfRangeTag); }
        public void writeTemperaturePrimOutOfRange(bool value) { client.WriteItemValue("", opcServerName, TemperaturePrimOutOfRangeTag, value); }
        public Object readTemperaturePrimBadQuality() { return new EasyDAClient().ReadItemValue("", opcServerName, TemperaturePrimOutOfRangeTag); }
        public void writeTemperaturePrimBadQuality(bool value) { client.WriteItemValue("", opcServerName, TemperaturePrimOutOfRangeTag, value); }

        public Object readTemperatureSecOutOfRange() { return new EasyDAClient().ReadItemValue("", opcServerName, TemperatureSecOutOfRangeTag); }
        public void writeTemperatureSecOutOfRange(bool value) { client.WriteItemValue("", opcServerName, TemperatureSecOutOfRangeTag, value); }
        public Object readTemperatureSecBadQuality() { return new EasyDAClient().ReadItemValue("", opcServerName, TemperatureSecOutOfRangeTag); }
        public void writeTemperatureSecBadQuality(bool value) { client.WriteItemValue("", opcServerName, TemperatureSecOutOfRangeTag, value); }


        public bool isQualityGood(string opcTag)
        {
            DAVtq vtq = client.ReadItem(opcServerName, opcTag);
            return vtq.Quality.IsGood ? true : false;
        }

        //Met Id methods
        public string getMetTowerPrefix { set { } get { return this.metTowerPrefix; } }

        //turbien methods for measurement reduedancy
        public void setNearestTurbine(Turbine turbine) { nearestTurbine = turbine; }
        public Turbine getNearestTurbine() { return nearestTurbine; }

        public void writeToQueue(double temperature, double humidity)
        {
            //Should this be prim temp or something else?
            temperatureQueue.Enqueue(temperature);
            humidityQueue.Enqueue(humidity);
        }

        public Queue<double> getTemperatureQueue() { return this.temperatureQueue; }
        public Queue<double> getHumidityQueue() { return this.humidityQueue; }
    }
}
