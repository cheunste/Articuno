﻿using log4net;
using OpcLabs.EasyOpc.DataAccess;
using OpcLabs.EasyOpc.DataAccess.Generic;
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
    sealed internal class MetTowerMediator
    {
        public static int numMetTower;

        private string MET_INPUT_TABLE_TAGS =
            "SELECT * FROM MetTowerInputTags WHERE MetId='{0}'";
        private string MET_OUTPUT_TABLE_TAGS =
            "SELECT * FROM MetTowerOutputTags WHERE MetId='{0}'";

        private string MET_NUM =
            "SELECT MetId FROM MetTowerInputTags";

        private static string SELECT_QUERY = "SELECT Count(*) as num FROM MetTowerInputTags";

        private string opcServerName;
        private string sitePrefix;
        private string uccActiveTag;
        private static List<MetTower> metTowerList = new List<MetTower>();
        private static List<String> metPrefixList = new List<String>();

        private static readonly ILog log = LogManager.GetLogger(typeof(MetTowerMediator));
        private static DatabaseInterface dbi;

        private MetTowerMediator()
        {

            metPrefixList = new List<string>();
            metTowerList = new List<MetTower>();
            dbi = DatabaseInterface.Instance;
            opcServerName = DatabaseInterface.Instance.getOpcServerName();
            sitePrefix = DatabaseInterface.Instance.getSitePrefixValue();
        }
        public static MetTowerMediator Instance { get { return Nested.instance; } }

        private class Nested
        {
            //Explicit static constructor to tell C# compiler not to mark type as beforefiled init
            static Nested() { }
            internal static readonly MetTowerMediator instance = new MetTowerMediator();
        }
        public void CreateMetTowerObject()
        {
            log.Info("Creating Met Tower lists");
            metTowerList = new List<MetTower>();
            metTowerList.Clear();

            DatabaseInterface dbi = DatabaseInterface.Instance;

            metPrefixList.Clear();
            createPrefixList();

            foreach (string metPrefix in metPrefixList) { InitializeMetTower(metPrefix); }
            uccActiveTag = dbi.getActiveUccOpcTag();
        }
        /// <summary>
        /// Returns the number of met towers in a project
        /// </summary>
        /// <returns></returns>
        public static int GetNumberOfMetTowers()
        {
            if (numMetTower == 0)
            {
                DataTable reader = dbi.readQuery(SELECT_QUERY);
                numMetTower = Convert.ToInt16(reader.Rows[0]["num"]);
            }
            return numMetTower;
        }

        //Function that is called by the main Articuno class to determine if the temperature average calculated
        // by ARticuno is considered freezing or not
        /// <summary>
        /// Method to check a met tower to see if it meets the freezing condition and set its condition. Returns true if iti s frozen, false otherwise
        /// </summary>
        public bool IsMetTowerFrozen(string metId, double avgTemperature, double avgHumidity)
        {

            MetTower met = GetMetTowerFromId(metId);

            double tempThreshold = ReadTemperatureThresholdForMetTower(metId);
            double deltaThreshold = readDeltaThreshold(metId);

            double dewPoint = CalculateDewPointTemperature(avgHumidity, avgTemperature);
            double delta = CalculateDewTempAmbientTempDelta(avgTemperature, dewPoint);

            //If the humidity quality is bad, then do not use it and only rely on temperature
            bool isHumidityBad = Convert.ToBoolean(met.getPrimaryHumiditySensor().isSensorBadQuality()) || Convert.ToBoolean(met.getPrimaryHumiditySensor().isSensorOutofRange());
            if (avgTemperature <= tempThreshold)
            {

                try
                {
                    //If Humidity is bad, then only relie on temperature
                    if (isHumidityBad)
                    {
                        log.InfoFormat("{0} Humidity is bad quality. Ignoring and currently using avg temperature {1}", metId, avgTemperature);
                        met.IceIndicationValue = true;
                        log.DebugFormat("Icing conditions met for {0}. \n" +
                            "{0} Average Temperature {1}, \n" +
                            "{0} Temperature threshold {2} \n",
                            metId, avgTemperature, tempThreshold);

                    }
                    //Freezing Conditions met with good humidity(regular case)
                    else if (delta <= deltaThreshold)
                    {
                        met.IceIndicationValue = true;
                        log.InfoFormat("No Ice detected for met {0}.\n" +
                            "{0} Average Temperature {1}, \n" +
                            "{0} Temperature threshold {2} \n" +
                            "{0} Average Humidity {3}, \n" +
                            "{0} Delta threshold {4} \n"
                            ,
                            metId, avgTemperature, tempThreshold, avgHumidity, deltaThreshold
                            );
                    }
                    //Reaching here still implies there's no ice. But it shouldn't ever reach here because at this point, avgTemperature should be > tempThreshold
                    else { }
                }
                catch (Exception e)
                {
                    //in case you can't write to OPC Server
                    log.ErrorFormat("Error when writing to the " +
                        "Ice indication.\n" +
                        "Error: {0}. \n" +
                        "Met: {1}, \n" +
                        "avgTemp: {2}, \n" +
                        "tempThreshold {3}\n",
                        e, metId, avgTemperature, tempThreshold);
                    log.ErrorFormat("Error:\n{0}", e);

                }
            }

            //No ice condition
            else
            {
                met.IceIndicationValue = false;
                log.DebugFormat("No Ice detected for met {0}.\n" +
                    "{0} Average Temperature {1}, \n" +
                    "{0} Temperature threshold {2} \n" +
                    "{0} Average Humidity {3}, \n" +
                    "{0} Delta threshold {4} \n"
                    ,
                    metId, avgTemperature, tempThreshold, avgHumidity, deltaThreshold
                    );
            }
            return Convert.ToBoolean(met.IceIndicationValue);
        }


        public void createPrefixList()
        {
            DataTable reader = DatabaseInterface.Instance.readQuery(MET_NUM);
            foreach (DataRow item in reader.Rows) { metPrefixList.Add(item["MetId"].ToString()); }
        }



        /// <summary>
        /// Returns a tuple containing an ambient Temperature, a relative humidity, calculated dew point and a temperature delta given a met tower id
        /// </summary>
        /// <param name="metId">the met tower id (ie Met) in string</param>
        /// <returns>Tuple of doubles</returns>
        public void getAllMeasurements(string metId)
        {
            double temperature = ReadTemperatureFromMetTower(metId);
            var humidity = readHumidity(metId);
            double dew = CalculateDewPointTemperature(Convert.ToDouble(humidity), temperature);
            double delta = CalculateDewTempAmbientTempDelta(temperature, dew);
            log.DebugFormat("{0}, temp: {1}, rh: {2}, dew:{3}, delta: {4}", metId, temperature, humidity, dew, delta);
        }

        ///EXPERIMENTAL. Not tested
        /// <summary>
        /// Calculates Dew Poing and Delta based on the average temperature and average humidty
        /// </summary>
        /// <param name="metId"></param>
        public void calculate(string metId)
        {
            MetTower met = GetMetTowerFromId(metId);

            double temperature = Convert.ToDouble(met.CtrTemperatureValue);
            double humidity = Convert.ToDouble(met.CtrHumidityValue);
            double dew = CalculateDewPointTemperature(Convert.ToDouble(humidity), temperature);
            double delta = CalculateDewTempAmbientTempDelta(temperature, dew);
            log.DebugFormat("{0}, temp: {1}, rh: {2}, dew:{3}, delta: {4}", metId, temperature, humidity, dew, delta);

        }

        public double ReadTemperatureFromMetTower(String metId)
        {
            metId = isMetTowerSwitched(metId);
            MetTower met = GetMetTowerFromId(metId);
            double temperature;
            var primTempSensor = met.getPrimaryTemperatureSensor();
            var secTempSensor = met.getSecondaryTemperatureSensor();

            //Get the primary sensor temperature. If its quality is bad, then use the sencondary sensor. If secondary is bad, then use the turbine sensor
            temperature = primTempSensor.readValue();
            if (primTempSensor.isSensorBadQuality())
            {
                temperature = secTempSensor.readValue();
                if (secTempSensor.isSensorBadQuality())
                {
                    temperature = Convert.ToDouble(met.getNearestTurbine().readTurbineTemperatureValue());
                }
            }
            return temperature;
        }

        public double readHumidity(String metId)
        {
            metId = isMetTowerSwitched(metId);
            return Convert.ToDouble(GetMetTowerFromId(metId).RelativeHumidityValue);
        }

        /// <summary>
        /// SHOULD ONLY BE USED FOR UNIT TESTING 
        /// This function switches the met tower to use the backup met tower.
        /// For example, if Met is passed in, then it will use Met2 and vice versa.
        /// </summary>
        /// <param name="metId"></param>
        public bool switchMetTower(string metId)
        {
            MetTower met = GetMetTowerFromId(metId);
            met.MetSwitchValue = !met.MetSwitchValue;
            return met.MetSwitchValue;
        }

        /// <summary>
        /// This function returns the metId of the backup met tower if the met tower has been switched to do so
        /// </summary>
        /// <param name="metId">The metid checked to see if it is swtiched</param>
        /// <returns>A metId. Returns the original metId if it is not switched. Returns a the backup metId otherwise</returns>
        public string isMetTowerSwitched(string metId)
        {
            if (Convert.ToBoolean(GetMetTowerFromId(metId).MetSwitchValue))
                return metId.Equals("Met") ? "Met2" : "Met";
            else
                return metId;
        }

        internal void writeToQueue(string metId, double temperature, double humidity) { GetMetTowerFromId(metId).writeToQueue(temperature, humidity); }

        internal double calculateCtrAvgTemperature(string metId)
        {
            MetTower met = GetMetTowerFromId(metId);
            Queue<double> tempQueue = met.getTemperatureQueue();
            double temperatureCtrAverage = 0.0;
            double count = tempQueue.Count;
            while (tempQueue.Count != 0) { temperatureCtrAverage += tempQueue.Dequeue(); }

            double average = temperatureCtrAverage / count;
            //Only write if and only if the UCC is active
            if (OpcServer.isActiveUCC(opcServerName, uccActiveTag))
                met.CtrTemperatureValue = average;

            return average;

        }

        internal double calculateCtrAvgHumidity(string metId)
        {
            MetTower met = GetMetTowerFromId(metId);
            Queue<double> humidityQueue = met.getHumidityQueue();
            double humidityCtrAverage = 0.0;
            double count = humidityQueue.Count;
            while (humidityQueue.Count != 0) { humidityCtrAverage += humidityQueue.Dequeue(); }

            double average = humidityCtrAverage / count;
            //You need to multiple the CtrHumidityValue by 100 because it is currently in decimal form. 
            //This needs to be displayed in percentage form
            //Also, this should only write if and only if the UCC is active
            if (OpcServer.isActiveUCC(opcServerName, uccActiveTag))
                met.CtrHumidityValue = average * 100.0;
            return average;
        }

        internal void updateDewPoint(string metId, double ctrTemperature, double ctrHumidity)
        {
            double dew = CalculateDewPointTemperature(ctrHumidity, ctrTemperature);
            log.DebugFormat("CTR Dew: {0}", dew);
            //Only write the dew point temperature if and only if the UCC is active
            if (OpcServer.isActiveUCC(opcServerName, uccActiveTag))
                GetMetTowerFromId(metId).CtrDewValue = dew;
        }


        /// <summary>
        /// Write Temperature Threshold for all the met tower
        /// </summary>
        /// <param name="value">A double vlaue that represents the temperature threshold</param>
        public void UpdateTemperatureThresholdForAllMetTowers(double value)
        {
            foreach (MetTower tower in metTowerList) { tower.AmbTempThreshold = value; }
        }
        /// <summary>
        /// Reads the Ambient Temperature threshold from the OPC tag
        /// </summary>
        /// <param name="metTowerId"></param>
        /// <returns></returns>
        public double ReadTemperatureThresholdForMetTower(string metTowerId) { return GetMetTowerFromId(metTowerId).AmbTempThreshold; }

        /// <summary>
        /// Writes the delta threshold for all the met tower
        /// </summary>
        /// <param name="value">A double vlaue that represents the delta threshold<</param>
        public void writeDeltaThreshold(double value)
        {
            foreach (MetTower tower in metTowerList)
            {
                tower.DeltaTempThreshold = value;
                OpcServer.writeOpcTag(opcServerName, sitePrefix + dbi.GetDeltaThresholdTag(), value);
            }
        }
        public double readDeltaThreshold(string metTowerId) { return GetMetTowerFromId(metTowerId).DeltaTempThreshold; }
        public void writePrimTemperature(string metId, double value) { GetMetTowerFromId(metId).PrimTemperatureValue = value; }
        public void writeSecTemperature(string metId, double value) { GetMetTowerFromId(metId).SecTemperatureValue = value; }
        public void writeHumidity(string metId, double value)
        {
            MetTower met = GetMetTowerFromId(metId);
            met.RelativeHumidityValue = value;
        }

        /// <summary>
        /// Calculates the Dew Point Temperature given the ambient temperature and the the relative humidity
        /// </summary>
        /// <param name="ambTemp">The ambient temperature value (Celcius) from the met tower in double format</param>
        /// <param name="rh">The relative humidity in double format</param>
        /// <returns>The dew point temperature in double format </returns>
        public double CalculateDewPointTemperature(double rh, double ambTemp) => Math.Round(Math.Pow(rh, 1.0 / 8.0) * (112 + (0.9 * ambTemp)) + (0.1 * ambTemp) - 112, 3);

        /// <summary>
        /// Calculates the Delta Temperature given the ambient Temperature and a dew point temperature (from calculateDewPoitn function)
        /// </summary>
        /// <param name="ambTemp">The ambient temperature value (Celcius) from the met tower in double format</param>
        /// <param name="dewPointTemp">The dew point temperature from calculateDewPoint</param>
        /// <returns>The delta temperature in double format</returns>
        public double CalculateDewTempAmbientTempDelta(double ambTemp, double dewPointTemp) => Math.Round(Math.Abs(ambTemp - dewPointTemp), 3);

        /// <summary>
        /// Check the quality of the met tower. Returns True If the met tower is good qualty. False if the met tower is bad quality
        /// </summary>
        /// <param name="metId"></param>
        /// <returns></returns>
        public bool isAllMetTowerSensorBadQuality(string metId)
        {
            MetTower met = GetMetTowerFromId(metId);
            bool currentNoDataState = Convert.ToBoolean(met.NoDataAlarmValue);
            //Checks to see if the snesor is good or not. Note that the call to isAllSensorGood will raise/clear the 'no data' alarm. This function logs it

            if (!met.isAllSensorGood())
            {
                log.DebugFormat("{0} No Data alarm {1}. NoData Alarm Value {2}", metId, "raised", met.NoDataAlarmValue);
                return true;
            }
            else
            {
                //To prevent overlogging, only log if the currentNoDataState was true before
                if (currentNoDataState)
                    log.DebugFormat("{0} No Data alarm {1}. NoData Alarm Value {2}", metId, "cleared", met.NoDataAlarmValue);
                return false;
            }
        }


        /// <summary>
        /// Method used to set a met tower to a turbine
        /// </summary>
        /// <param name="metId">The met id. Met or Met2</param>
        /// <param name="turbine">The turbine id (ie T001)</param>
        public void SetTurbineBackupForMet(string metId, Turbine turbine)
        {
            for (int i = 0; i <= metTowerList.Count; i++)
            {
                if (metTowerList.ElementAt(i).getMetTowerPrefix.Equals(metId)) { metTowerList.ElementAt(i).setNearestTurbine(turbine); }
            }
        }

        public enum MetTowerEnum
        {
            HumidityQuality,
            HumidityOutOfRange,
            PrimSensorQuality,
            PrimSensorOutOfRange,
            SecSensorQuality,
            SecSensorOutOfRange,
            NoData,
            Switched,
        }

        /// <summary>
        /// Good Quality gives a true (1). Bad Quality gives a false (0)
        /// </summary>
        public enum MetQualityEnum
        {
            MET_GOOD_QUALITY = 1,
            MET_BAD_QUALITY = 0
        }


        /// <summary>
        /// Checks to see if a met tower is frozen given the prefix of the met
        /// </summary>
        /// <param name="metTowerId"></param>
        /// <returns>Boolean. True if frozen, false otherwise</returns>
        public bool IsMetTowerFrozen(string metTowerId) { return Convert.ToBoolean(GetMetTowerFromId(metTowerId).IceIndicationValue); }

        /// <summary>
        /// This function checks to see if icing is occuring for either of hte met towers on site. 
        /// </summary>
        /// <returns>a Boolean indicating if either of the met tower has froze up</returns>
        public bool IsAnyMetTowerFrozenAtSite()
        {
            bool icingPossible = false;
            foreach (string metPrefix in metPrefixList)
            {
                icingPossible |= Convert.ToBoolean(GetMetTowerFromId(metPrefix).IceIndicationValue);
            }
            return icingPossible;
        }

        public Enum FindMetTowerTag(string metTowerId, string tag)
        {
            MetTower tempMet = GetMetTowerFromId(metTowerId);
            if (tag.ToUpper().Equals(tempMet.MetSwitchTag.ToUpper())) { return MetTowerEnum.Switched; }
            return null;
        }

        /// <summary>
        /// Get the Met tower given an metTowerId (ie Met, Met2)
        /// </summary>
        /// <param name="metTowerId"></param>
        /// <returns>A Met Tower Object if exist. Null otherwise. createMetTower() must be called before using this fucntion</returns>
        public MetTower GetMetTowerFromId(string metTowerId)
        {
            for (int i = 0; i < metTowerList.Count; i++)
            {
                if (metTowerList.ElementAt(i).getMetTowerPrefix.Equals(metTowerId)) { return metTowerList.ElementAt(i); }
            }
            return null;
        }

        private void InitializeMetTower(string metPrefix)
        {
            MetTower met = new MetTower(metPrefix, opcServerName);

            //For Met Tower tags from the MetTowerInputTags table
            string cmd = String.Format(MET_INPUT_TABLE_TAGS, metPrefix);
            DataTable reader = dbi.readQuery(cmd);

            //Set the tags from the MeTowerInputTags table to the accessors
            met.PrimTemperatureTag = sitePrefix + reader.Rows[0]["PrimTempValueTag"].ToString();
            met.SecTemperatureTag = sitePrefix + reader.Rows[0]["SecTempValueTag"].ToString();
            met.RelativeHumidityTag = sitePrefix + reader.Rows[0]["PrimHumidityValueTag"].ToString();
            met.HumidityPrimValueTag = sitePrefix + reader.Rows[0]["PrimHumidityValueTag"].ToString();
            met.HumiditySecValueTag = sitePrefix + reader.Rows[0]["SecHumidityValueTag"].ToString();
            met.MetSwitchTag = sitePrefix + reader.Rows[0]["Switch"].ToString();

            //For Met Tower tags from the MetTowerInputTags table
            cmd = String.Format(MET_OUTPUT_TABLE_TAGS, metPrefix);
            reader = dbi.readQuery(cmd);

            //Set the tags from the MeTowerInputTags table to the accessors
            met.TemperaturePrimBadQualityTag = sitePrefix + reader.Rows[0]["TempPrimBadQualityTag"].ToString();
            met.TemperaturePrimOutOfRangeTag = sitePrefix + reader.Rows[0]["TempPrimOutOfRangeTag"].ToString();
            met.TemperatureSecOutOfRangeTag = sitePrefix + reader.Rows[0]["TempSecOutOfRangeTag"].ToString();
            met.TemperatureSecBadQualityTag = sitePrefix + reader.Rows[0]["TempSecBadQualityTag"].ToString();
            met.HumidtyOutOfRangeTag = sitePrefix + reader.Rows[0]["HumidityOutOfRangeTag"].ToString();
            met.HumidityBadQualityTag = sitePrefix + reader.Rows[0]["HumidityBadQualityTag"].ToString();
            met.IceIndicationTag = sitePrefix + reader.Rows[0]["IceIndicationTag"].ToString();
            met.NoDataAlarmTag = sitePrefix + reader.Rows[0]["NoDataAlarmTag"].ToString();
            met.CtrTemperatureTag = sitePrefix + reader.Rows[0]["CtrTemperature"].ToString();
            met.CtrDewTag = sitePrefix + reader.Rows[0]["CtrDew"].ToString();
            met.CtrHumidityTag = sitePrefix + reader.Rows[0]["CtrHumidity"].ToString();

            //Create the sensors for the met tower. Must be declared after every variable has been set
            met.createSensors();
            metTowerList.Add(met);
        }
    }
}
