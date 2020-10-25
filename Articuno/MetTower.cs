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
        private double lastPrimTemp;
        private double lastSecTemp;

        //Constants
        private double MIN_TEMP_VALUE = -20.0;
        private double MAX_TEMP_VALUE = 60.0;
        private double MIN_HUMIDITY_VALUE = 0.0;
        private double MAX_HUMIDITY_VALUE = 0.99;

        private Queue<double> temperatureQueue;
        private Queue<double> humidityQueue;

        private string metTowerPrefix;
        private Turbine nearestTurbine;

        private string opcServerName;

        MetTowerSensor primTempSensor;
        MetTowerSensor secTempSensor;
        MetTowerSensor primHumidSensor;

        private static readonly ILog log = LogManager.GetLogger(typeof(MetTower));

        public MetTower(string MetId, string opcServerName)
        {
            this.opcServerName = opcServerName;
            this.metTowerPrefix = MetId;

            temperatureQueue = new Queue<double>();
            humidityQueue = new Queue<double>();
            lastPrimTemp = 0;
            lastSecTemp = 0;

            SampleHumidity = 0;
        }

        public void createSensors()
        {
            this.primTempSensor = new MetTowerSensor(opcServerName, PrimTemperatureTag, TemperaturePrimOutOfRangeTag, TemperaturePrimBadQualityTag, MIN_TEMP_VALUE, MAX_TEMP_VALUE,StaleDataSamples);
            this.secTempSensor = new MetTowerSensor(opcServerName, SecTemperatureTag, TemperatureSecOutOfRangeTag, TemperatureSecBadQualityTag, MIN_TEMP_VALUE, MAX_TEMP_VALUE,StaleDataSamples);
            this.primHumidSensor = new MetTowerSensor(opcServerName, HumidityPrimValueTag, HumidtyOutOfRangeTag, HumidityBadQualityTag, MIN_HUMIDITY_VALUE, MAX_HUMIDITY_VALUE,StaleDataSamples);

        }

        public MetTowerSensor getPrimaryTemperatureSensor() => primTempSensor;
        public MetTowerSensor getSecondaryTemperatureSensor() => secTempSensor;

        /// <summary>
        /// Get the Primary Humidity Sensor. WARNING: Caller MUST provide the divide by 100.0 scaling after a readValue() call
        /// </summary>
        /// <returns></returns>
        public MetTowerSensor getPrimaryHumiditySensor() => primHumidSensor;

        /// <summary>
        /// Accessor the RelativeHumidity OPC Tag
        /// </summary>
        public string RelativeHumidityTag { set; get; }

        /// <summary>
        /// Returns the Relative Humdity value from the sensor. Returned value will be in decimal format and NOT percentage
        /// </summary>
        public Object RelativeHumidityValue
        {
            set { OpcServer.writeOpcTag(opcServerName, RelativeHumidityTag, value); }
            get { return primHumidSensor.readValue(true); }
        }

        /// <summary>
        /// funciton to check whether all sensor quality is good or not. If they're all bad, the "No Data" alarm will be raised"
        /// </summary>
        /// <returns>False if all data sensor is bad. True otherwise</returns>
        public bool isAllSensorGood()
        {
            bool badQuality =
                (getPrimaryHumiditySensor().isSensorBadQuality() && getPrimaryTemperatureSensor().isSensorBadQuality() && getSecondaryTemperatureSensor().isSensorBadQuality()) ||
                (getPrimaryHumiditySensor().isSensorOutofRange() && getPrimaryTemperatureSensor().isSensorOutofRange() && getSecondaryTemperatureSensor().isSensorOutofRange())
                ;

            //All sensors bad quality
            if (badQuality)
            {
                NoDataAlarmValue = true;
                return false;
            }
            //Normal oepration
            else
            {
                NoDataAlarmValue = false;
                return true;
            }
        }

        /// <summary>
        /// Accessor for the Primary Temperature OPC Tag
        /// </summary>
        public string PrimTemperatureTag { set; get; }

        public Object PrimTemperatureValue
        {
            get { return primTempSensor.readValue(); }
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
            get { return secTempSensor.readValue(); }
            set { OpcServer.writeOpcTag(opcServerName, SecTemperatureTag, value); }
        }


        //Getters for System OPC Tag indicators
        /// <summary>
        /// Gets the  NoDataAlaarmValue OPC value
        /// </summary>
        /// <returns></returns>
        public string NoDataAlarmTag { set; get; }
        public Object NoDataAlarmValue
        {
            get { return OpcServer.readBooleanTag(opcServerName, NoDataAlarmTag); }
            set { OpcServer.writeOpcTag(opcServerName, NoDataAlarmTag, value); }
        }

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

        public string MetSwitchTag { get; set; }
        public bool MetSwitchValue
        {
            get { return Convert.ToBoolean(OpcServer.readBooleanTag(opcServerName, MetSwitchTag)); }
            set { OpcServer.writeOpcTag(opcServerName, MetSwitchTag, value); }
        }

        //Threshold setters and getters
        public double AmbTempThreshold { get; set; }
        public double DeltaTempThreshold { get; set; }

        public int StaleDataSamples { get; set; }


        //Humidity Accessors
        public string HumidityPrimValueTag { get; set; }

        //Secondary humidity is unused because there's no secondary humidity designed wit hthe sepeciifcation
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

        //The following are for humidity out of range, bad quality
        public Object HumidityOutOfRng
        {
            get { return primHumidSensor.isSensorOutofRange(); }
            set { OpcServer.writeOpcTag(opcServerName, HumidtyOutOfRangeTag, value); }
        }

        public Object HumidityBadQuality
        {
            get { return primHumidSensor.isSensorBadQuality(); }
            set { OpcServer.writeOpcTag(opcServerName, HumidityBadQualityTag, value); }
        }

        //The following are for temperature out of range, bad quality
        public Object TemperaturePrimOutOfRange
        {
            get { return primTempSensor.isSensorOutofRange(); }
            set { OpcServer.writeOpcTag(opcServerName, TemperaturePrimOutOfRangeTag, value); }
        }

        public Object TemperaturePrimBadQuality
        {
            get { return primTempSensor.isSensorBadQuality(); }
            set { OpcServer.writeOpcTag(opcServerName, TemperaturePrimBadQualityTag, value); }
        }

        public Object TemperatureSecOutOfRange
        {
            get { return secTempSensor.isSensorOutofRange(); }
            set { OpcServer.writeOpcTag(opcServerName, TemperatureSecOutOfRangeTag, value); }
        }

        public Object TemperatureSecBadQuality
        {
            get { return secTempSensor.isSensorBadQuality(); }
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
        public double SampleHumidity { get; set; }

        public string getMetTowerPrefix { set { } get { return this.metTowerPrefix; } }

        public void SetBackupTurbineForMetTower(Turbine turbine) { nearestTurbine = turbine; }
        public Turbine GetBackupTurbineForMetTower() { return nearestTurbine; }

        public void writeToQueue(double temperature, double humidity)
        {
            temperatureQueue.Enqueue(temperature);
            humidityQueue.Enqueue(humidity);
        }

        public Queue<double> getTemperatureQueue() { return this.temperatureQueue; }
        public Queue<double> getHumidityQueue() { return this.humidityQueue; }
    }
}
