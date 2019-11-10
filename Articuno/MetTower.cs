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

    sealed internal class MetTower
    {
        //Member variables;
        private double ambTempThreshold;
        private double deltaTempThreshold;

        private int frozenPrimTempCnt;
        private int frozenSecTempCnt;
        private int frozenHumidityCnt;
        private double lastPrimTemp;
        private double lastSecTemp;

        //Constants
        private double MIN_TEMP_VALUE = -20.0;
        private double MAX_TEMP_VALUE = 60.0;
        private double MIN_HUMIDITY_VALUE = 0.0;
        private double MAX_HUMIDITY_VALUE = 0.99;

        //Queues
        private Queue<double> temperatureQueue;
        private Queue<double> humidityQueue;

        //Member prefix
        private string metTowerPrefix;

        //Turbine reference (for backup)
        private Turbine nearestTurbine;

        //opc server
        private string opcServerName;

        //Sensors
        MetTowerSensor primTempSensor;
        MetTowerSensor secTempSensor;
        MetTowerSensor primHumidSensor;


        //log
        private static readonly ILog log = LogManager.GetLogger(typeof(MetTower));

        public MetTower(string MetId, string opcServerName)
        {
            //Set OPC Server Name
            this.opcServerName = opcServerName;

            //Set OPC Server Name
            this.opcServerName = opcServerName;

            //Set up met id
            this.metTowerPrefix = MetId;

            temperatureQueue = new Queue<double>();
            humidityQueue = new Queue<double>();
            frozenHumidityCnt = 0;
            frozenPrimTempCnt = 0;
            frozenSecTempCnt = 0;
            lastPrimTemp= 0;
            lastSecTemp= 0;

            SampleHumidity = 0;
        }

        public void createSensors()
        {
            this.primTempSensor = new MetTowerSensor(opcServerName, PrimTemperatureTag, TemperaturePrimOutOfRangeTag, TemperaturePrimBadQualityTag, MIN_TEMP_VALUE, MAX_TEMP_VALUE);
            this.secTempSensor = new MetTowerSensor(opcServerName, SecTemperatureTag, TemperatureSecOutOfRangeTag, TemperatureSecBadQualityTag, MIN_TEMP_VALUE, MAX_TEMP_VALUE);
            this.primHumidSensor = new MetTowerSensor(opcServerName,HumidityPrimValueTag,HumidtyOutOfRangeTag,HumidityBadQualityTag,MIN_HUMIDITY_VALUE,MAX_HUMIDITY_VALUE); 

        }

        public MetTowerSensor getPrimaryTemperatureSensor() => primTempSensor;
        public MetTowerSensor getSecondaryTemperatureSensor() => secTempSensor;
        public MetTowerSensor getPrimaryHumiditySensor() => primHumidSensor;

        /// <summary>
        /// Accessor the RelativeHumidity OPC Tag
        /// </summary>
        public string RelativeHumidityTag { set; get; }

        public Object RelativeHumidityValue
        {
            set { OpcServer.writeOpcTag(opcServerName, RelativeHumidityTag, value); }
            get { return OpcServer.readAnalogTag(opcServerName, RelativeHumidityTag); }
        }



        /// <summary>
        /// Accessor for the Primary Temperature OPC Tag
        /// </summary>
        public string PrimTemperatureTag { set; get; }

        public Object PrimTemperatureValue
        {
            get { return OpcServer.readAnalogTag(opcServerName, PrimTemperatureTag); }
            set { OpcServer.writeOpcTag(opcServerName, PrimTemperatureTag, value); }
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
            get { return OpcServer.readAnalogTag(opcServerName, SecTemperatureTag); }
            set { OpcServer.writeOpcTag(opcServerName, SecTemperatureTag, value); }
        }


        //Getters for System OPC Tag indicators
        /// <summary>
        /// Gets the  NoDataAlaarmValue OPC value
        /// </summary>
        /// <returns></returns>
        public string NoDataAlarmTag { set; get; }
        public Object NoDataAlarmValue { set { OpcServer.writeOpcTag(opcServerName, NoDataAlarmTag, value); } get { return OpcServer.readBooleanTag(opcServerName, NoDataAlarmTag); } }

        /// <summary>
        /// Gets the IceIndicationValue OPC Value
        /// </summary>
        /// <returns></returns>
        public string IceIndicationTag { set; get; }
        public Object IceIndicationValue
        {
            get { return OpcServer.readBooleanTag(opcServerName, IceIndicationTag); }
            set { OpcServer.writeOpcTag(opcServerName, IceIndicationTag, value); }
        }

        //For Met TOwer siwtching
        public string MetSwitchTag { get; set; }
        public bool MetSwitchValue
        {
            get { return Convert.ToBoolean(OpcServer.readBooleanTag(opcServerName, MetSwitchTag)); }
            set { OpcServer.writeOpcTag(opcServerName, MetSwitchTag, value); }
        }

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

        //CTR average values from the met tower

        public string CtrTemperatureTag { get; set; }
        public string CtrHumidityTag { get; set; }
        public string CtrDewTag { get; set; }
        //The following are for humidity out of range, bad quality, etc.
        public Object HumidityOutOfRng
        {
            get { return OpcServer.readBooleanTag(opcServerName, HumidtyOutOfRangeTag); }
            set { OpcServer.writeOpcTag(opcServerName, HumidtyOutOfRangeTag, value); }
        }

        public Object HumidityBadQuality
        {
            get { return OpcServer.readBooleanTag(opcServerName, HumidityBadQualityTag); }
            set { OpcServer.writeOpcTag(opcServerName, HumidityBadQualityTag, value); }
        }

        //The following are for temperature out of range, bad quality
        public Object TemperaturePrimOutOfRange
        {
            get { return OpcServer.readBooleanTag(opcServerName, TemperaturePrimOutOfRangeTag); }
            set { OpcServer.writeOpcTag(opcServerName, TemperaturePrimOutOfRangeTag, value); }
        }

        public Object TemperaturePrimBadQuality
        {
            get { return OpcServer.readBooleanTag(opcServerName, TemperaturePrimBadQualityTag); }
            set { OpcServer.writeOpcTag(opcServerName, TemperaturePrimBadQualityTag, value); }
        }

        public Object TemperatureSecOutOfRange
        {
            get { return OpcServer.readBooleanTag(opcServerName, TemperatureSecOutOfRangeTag); }
            set { OpcServer.writeOpcTag(opcServerName, TemperatureSecOutOfRangeTag, value); }
        }

        public Object TemperatureSecBadQuality
        {
            get { return OpcServer.readBooleanTag(opcServerName, TemperatureSecBadQualityTag); }
            set { OpcServer.writeOpcTag(opcServerName, TemperatureSecBadQualityTag, value); }
        }
        /// <summary>
        /// Sets the average temperture to the output 
        /// </summary>
        public Object CtrTemperatureValue
        {
            get { return OpcServer.readBooleanTag(opcServerName, CtrTemperatureTag); }
            set { OpcServer.writeOpcTag(opcServerName, CtrTemperatureTag, value); }
        }
        public Object CtrHumidityValue
        {
            get { return OpcServer.readBooleanTag(opcServerName, CtrHumidityTag); }
            set { OpcServer.writeOpcTag(opcServerName, CtrHumidityTag, value); }
        }
        public Object CtrDewValue
        {
            get { return OpcServer.readBooleanTag(opcServerName, CtrDewTag); }
            set { OpcServer.writeOpcTag(opcServerName, CtrDewTag, value); }
        }
        /// <summary>
        /// Verifies whether a quality is good or not
        /// </summary>
        /// <param name="opcTag"></param>
        /// <returns></returns>
        public bool isQualityGood(string opcTag) { return Convert.ToBoolean(OpcServer.readBooleanTag(opcServerName, opcTag)); }

        public double SamplePrimTemperature { get; set; }
        public double SampleSecTemperature { get; set; }
        public void setLastStoredSample(string opcTag,double sampledTemp)
        {
            if (opcTag.Equals(PrimTemperatureTag))
                lastPrimTemp=sampledTemp;
            else
                lastSecTemp=sampledTemp;
        }

        public double getLastStoredSample(string opcTag)
        {
            if (opcTag.Equals(PrimTemperatureTag))
                return lastPrimTemp;
            else
                return lastSecTemp;

        } 
        public double SampleHumidity { get; set; }


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

        public void incrementFrozen(string opcTag)
        {
            if (opcTag.Equals(PrimTemperatureTag))
                frozenPrimTempCnt++;
            else
                frozenSecTempCnt++;
        }

        public int getFrozenIncrement(string opcTag)
        {
            if (opcTag.Equals(PrimTemperatureTag))
                return frozenPrimTempCnt;
            else
                return frozenSecTempCnt;
        }

        public void resetFrozenIncrement(string opcTag)
        {
            if (opcTag.Equals(PrimTemperatureTag))
                frozenPrimTempCnt = 0;
            else
                frozenSecTempCnt = 0;
        }
    }
}
