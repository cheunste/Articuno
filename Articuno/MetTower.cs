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
        //readonly String TEMPERATURE_QUERY = "SELECT " + PrimTempValueTagColumn + "," + SecTempValueColumn + "," + TempPrimBadQualityColumn + "," + TempPrimOutOfRangeColumn + " FROM MetTower";
        //readonly String RH_QUERY = "SELECT " + PrimHumidityValueColumn + "," + SecHumidityValueColumn + "," + HumidityBadQualityColumn + "," + HumidityOutOfRangeColumn + " FROM MetTower";
        //readonly String OTHER_PARAM_QUERY = "SELECT " + NoDataAlarmColumn + ", " + IceIndicationColumn + " FROM MetTower";

        readonly String INPUT_TAG_QUERY = "SELECT * FROM MetTowerInputTags";
        readonly String OUTPUT_TAG_QUERY = "SELECT * FROM MetTowerOutputTags";

        //Member variables;
        private double ambTempThreshold;
        private double deltaTempThreshold;

        //Queues
        private Queue<double> temperatureQueue;
        private Queue<double> humidityQueue;

        //Member prefix
        private string metTowerPrefix;

        //Turbine reference (for backup)
        private Turbine nearestTurbine;

        //opc server
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
        /// Accessor the RelativeHumidity OPC Tag
        /// </summary>
        public string RelativeHumidityTag { set; get; }

        public Object RelativeHumidityValue
        {
            set { client.WriteItemValue("", opcServerName, RelativeHumidityTag, value); }
            get { return new EasyDAClient().ReadItemValue("", opcServerName, RelativeHumidityTag); }
        }

        /// <summary>
        /// Accessor for the Primary Temperature OPC Tag
        /// </summary>
        public string PrimTemperatureTag { set; get; }

        public Object PrimTemperatureValue
        {
            get { return new EasyDAClient().ReadItemValue("", opcServerName, PrimTemperatureTag); }
            set { client.WriteItemValue("", opcServerName, PrimTemperatureTag, value); }
        }

        /// <summary>
        /// Accessosr for SEcondary Temperature OPC Tag
        /// </summary>
        public string SecTemperatureTag { set; get; }
        /// <summary>
        /// Accessor for the Secondary Temperature Value 
        /// </summary>
        public Object SecTemperatureValue
        {
            get { return new EasyDAClient().ReadItemValue("", opcServerName, SecTemperatureTag); }
            set { client.WriteItemValue("", opcServerName, SecTemperatureTag, value); }
        }


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
        public Object IceIndicationValue
        {
            get { return new EasyDAClient().ReadItemValue("", opcServerName, IceIndicationTag); }
            set { client.WriteItemValue("", opcServerName, IceIndicationTag, value); }
        }

        //For Met TOwer siwtching
        public string MetSwitch { get; set; }

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
        public Object HumidityOutOfRng
        {
            get { return new EasyDAClient().ReadItemValue("", opcServerName, HumidtyOutOfRangeTag); }
            set { client.WriteItemValue("", opcServerName, HumidtyOutOfRangeTag, value); }
        }

        public Object HumidityBadQuality
        {
            get { return new EasyDAClient().ReadItemValue("", opcServerName, HumidityBadQualityTag); }
            set { client.WriteItemValue("", opcServerName, HumidityBadQualityTag, value); }
        }

        //The following are for temperature out of range, bad quality
        public Object TemperaturePrimOutOfRange
        {
            get { return new EasyDAClient().ReadItemValue("", opcServerName, TemperaturePrimOutOfRangeTag); }
            set { client.WriteItemValue("", opcServerName, TemperaturePrimOutOfRangeTag, value); }
        }

        public Object TemperaturePrimBadQuality
        {
            get { return new EasyDAClient().ReadItemValue("", opcServerName, TemperaturePrimOutOfRangeTag); }
            set { client.WriteItemValue("", opcServerName, TemperaturePrimOutOfRangeTag, value); }
        }

        public Object TemperatureSecOutOfRange
        {
            get { return new EasyDAClient().ReadItemValue("", opcServerName, TemperatureSecOutOfRangeTag); }
            set { client.WriteItemValue("", opcServerName, TemperatureSecOutOfRangeTag, value); }
        }

        public Object TemperatureSecBadQuality
        {
            get { return new EasyDAClient().ReadItemValue("", opcServerName, TemperatureSecOutOfRangeTag); }
            set { client.WriteItemValue("", opcServerName, TemperatureSecOutOfRangeTag, value); }
        }

        /// <summary>
        /// Verifies whether a quality is good or not
        /// </summary>
        /// <param name="opcTag"></param>
        /// <returns></returns>
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
