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

namespace Articuno {
    sealed internal class MetTowerMediator {
        public static int numMetTower;
        private string opcServerName;
        private string sitePrefix;
        private TurbineMediator tm = TurbineMediator.Instance;
        private static List<MetTower> metTowerList = new List<MetTower>();
        private static List<String> metPrefixList = new List<String>();
        private static DatabaseInterface dbi;

        private MetTowerMediator() {
            metPrefixList = new List<string>();
            metTowerList = new List<MetTower>();
            dbi = DatabaseInterface.Instance;
            opcServerName = DatabaseInterface.Instance.getOpcServerName();
            sitePrefix = DatabaseInterface.Instance.getSitePrefixValue();
        }
        public static MetTowerMediator Instance { get { return Nested.instance; } }

        private class Nested {
            static Nested() { }
            internal static readonly MetTowerMediator instance = new MetTowerMediator();
        }
        public void CreateMetTowerObject() {
            ArticunoLogger.DataLogger.Debug("Creating Met Tower lists");
            metTowerList = new List<MetTower>();
            metTowerList.Clear();

            DatabaseInterface dbi = DatabaseInterface.Instance;

            metPrefixList.Clear();
            createPrefixList();

            foreach (string metPrefix in metPrefixList) { InitializeMetTower(metPrefix); }
        }
        /// <summary>
        /// Returns the number of met towers in a project
        /// </summary>
        /// <returns></returns>
        public static int GetNumberOfMetTowers() {
            if (numMetTower == 0) {
                numMetTower = dbi.GetNumberOfMetTowers();
            }
            return numMetTower;
        }

        public List<MetTower> GetMetTowerList() => metTowerList;

        public void CalculateAverage() =>
            GetMetTowerList().ForEach(m => {
                ArticunoLogger.DataLogger.Debug("In MetTowerMediator's CalculateAverage for {0} ",m.MetId);
                double tempAvg = calculateCtrAvgTemperature(m);
                double humidityAvg = calculateCtrAvgHumidity(m);
                ArticunoLogger.DataLogger.Debug("{0} CTR avg temp: {1}, avg Humidity: {2}", m.MetId,tempAvg, humidityAvg);
                CalculateFrozenMetTowerCondition(m, tempAvg, humidityAvg);
                updateDewPoint(m, tempAvg, humidityAvg);
                tm.checkMetTowerFrozen(m);
            });

        /// <summary>
        /// Method to check a met tower to see if it meets the freezing condition and set its condition. Returns true if iti s frozen, false otherwise
        /// </summary>
        public void CalculateFrozenMetTowerCondition(MetTower met, double avgTemperature, double avgHumidity) {
            string metId = met.MetId;

            double tempThreshold = ReadTemperatureThresholdForMetTower(metId);
            double deltaThreshold = readDeltaThreshold(metId);
            double dewPoint = CalculateDewPointTemperature(avgHumidity, avgTemperature);
            double delta = CalculateDewTempAmbientTempDelta(avgTemperature, dewPoint);

            bool isHumidityBad = Convert.ToBoolean(met.getPrimaryHumiditySensor().isSensorBadQuality()) || Convert.ToBoolean(met.getPrimaryHumiditySensor().isSensorOutofRange());

            if (avgTemperature <= tempThreshold) {
                try {
                    //If Humidity is bad, then only relie on temperature
                    if (isHumidityBad) {
                        ArticunoLogger.DataLogger.Debug("{0} Humidity is bad quality. Ignoring and currently using avg temperature {1}", metId, avgTemperature);
                        met.IceIndicationValue = true;
                        ArticunoLogger.DataLogger.Debug("Icing conditions met for {0}. \n" +
                            "{0} Average Temperature {1}, \n" +
                            "{0} Temperature threshold {2} \n",
                            metId, avgTemperature, tempThreshold);
                    }
                    //Freezing Conditions met with good humidity(regular case)
                    else if (delta <= deltaThreshold) {
                        met.IceIndicationValue = true;
                        ArticunoLogger.DataLogger.Debug("No Ice detected for met {0}.\n" +
                            "{0} Average Temperature {1}, \n" + "{0} Temperature threshold {2} \n" +
                            "{0} Average Humidity {3}, \n" + "{0} Delta threshold {4} \n",
                            metId, avgTemperature, tempThreshold, avgHumidity, deltaThreshold
                            );
                    }
                    //Reaching here still implies there's no ice. But it shouldn't ever reach here because at this point, avgTemperature should be > tempThreshold
                    else { }
                }
                catch (Exception e) {
                    //in case you can't write to OPC Server
                    ArticunoLogger.DataLogger.Debug("Error when writing to the Ice indication tag.\n" +
                        "Error: {0}. \n" + "Met: {1}, \n" +
                        "avgTemp: {2}, \n" + "tempThreshold {3}\n",
                        e, metId, avgTemperature, tempThreshold);
                    ArticunoLogger.DataLogger.Error("Error when writing to the Ice indication tag.\n" +
                        "Error: {0}. \n" + "Met: {1}, \n" +
                        "avgTemp: {2}, \n" + "tempThreshold {3}\n",
                        e, metId, avgTemperature, tempThreshold);
                }
            }
            //No ice condition
            else {
                met.IceIndicationValue = false;
                ArticunoLogger.DataLogger.Debug("No Ice detected for met {0}.\n" +
                    "{0} Average Temperature {1}, \n" +
                    "{0} Temperature threshold {2} \n" +
                    "{0} Average Humidity {3}, \n" +
                    "{0} Delta threshold {4} \n",
                    metId, avgTemperature, tempThreshold, avgHumidity, deltaThreshold
                    );
            }
        }
        public void createPrefixList() {
            DataTable reader = dbi.GetMetId();
            foreach (DataRow item in reader.Rows) { metPrefixList.Add(item["MetId"].ToString()); }
        }

        /// <summary>
        /// Returns a tuple containing an ambient Temperature, a relative humidity, calculated dew point and a temperature delta given a met tower id
        /// </summary>
        /// <param name="metId">the met tower id (ie Met) in string</param>
        /// <returns>Tuple of doubles</returns>
        public void getAllMeasurements(string metId) {
            double temperature = ReadTemperatureFromMetTower(metId);
            var humidity = readHumidity(metId);
            double dew = CalculateDewPointTemperature(Convert.ToDouble(humidity), temperature);
            double delta = CalculateDewTempAmbientTempDelta(temperature, dew);
            ArticunoLogger.DataLogger.Debug("{0}, temp: {1}, rh: {2}, dew:{3}, delta: {4}", metId, temperature, humidity, dew, delta);
        }

        public double ReadTemperatureFromMetTower(String metId) {
            metId = isMetTowerSwitched(metId);
            MetTower met = GetMetTowerFromId(metId);
            double temperature;
            var primTempSensor = met.getPrimaryTemperatureSensor();
            var secTempSensor = met.getSecondaryTemperatureSensor();

            //Get the primary sensor temperature. If its quality is bad, then use the sencondary sensor. If secondary is bad, then use the turbine sensor
            temperature = primTempSensor.readValue();
            if (primTempSensor.isSensorBadQuality()) {
                temperature = secTempSensor.readValue();
                if (secTempSensor.isSensorBadQuality())
                    temperature = Convert.ToDouble(met.GetBackupTurbineForMetTower().readTurbineTemperatureValue());
            }
            return temperature;
        }

        public double readHumidity(String metId) {
            metId = isMetTowerSwitched(metId);
            return Convert.ToDouble(GetMetTowerFromId(metId).RelativeHumidityValue);
        }

        /// <summary>
        /// SHOULD ONLY BE USED FOR UNIT TESTING 
        /// This function switches the met tower to use the backup met tower.
        /// For example, if Met is passed in, then it will use Met2 and vice versa.
        /// </summary>
        /// <param name="metId"></param>
        public bool switchMetTower(string metId) {
            MetTower met = GetMetTowerFromId(metId);
            met.MetSwitchValue = !met.MetSwitchValue;
            return met.MetSwitchValue;
        }

        /// <summary>
        /// This function returns the metId of the backup met tower if the met tower has been switched to do so
        /// </summary>
        /// <param name="metId">The metid checked to see if it is swtiched</param>
        /// <returns>A metId. Returns the original metId if it is not switched. Returns a the backup metId otherwise</returns>
        public string isMetTowerSwitched(string metId) {
            if (Convert.ToBoolean(GetMetTowerFromId(metId).MetSwitchValue))
                return metId.Equals("Met") ? "Met2" : "Met";
            else
                return metId;
        }

        internal void writeToQueue(string metId, double temperature, double humidity) => GetMetTowerFromId(metId).writeToQueue(temperature, humidity);

        internal double calculateCtrAvgTemperature(string metId) {
            MetTower met = GetMetTowerFromId(metId);
            Queue<double> tempQueue = met.getTemperatureQueue();
            double average = 0.0;
            if (tempQueue.Count > 0)
                average = tempQueue.Average();

            if (Articuno.isUccActive())
                met.CtrTemperatureValue = average;
            return average;
        }

        internal double calculateCtrAvgHumidity(string metId) {
            MetTower met = GetMetTowerFromId(metId);
            Queue<double> humidityQueue = met.getHumidityQueue();
            double humidityCtrAverage = 0.0;
            double count = humidityQueue.Count;
            double average = 0.0;

            if (humidityQueue.Count > 0)
                average = humidityQueue.Average();

            //You need to multiple the CtrHumidityValue by 100 because it is currently in decimal form and needs to be displayed in percentage form
            if (Articuno.isUccActive())
                met.CtrHumidityValue = average * 100.0;
            return average;
        }
        internal double calculateCtrAvgTemperature(MetTower met) {
            Queue<double> tempQueue = met.getTemperatureQueue();
            double average = 0.0;
            if (tempQueue.Count > 0)
                average = tempQueue.Average();
            if (Articuno.isUccActive())
                met.CtrTemperatureValue = average;
            return average;
        }

        internal double calculateCtrAvgHumidity(MetTower met) {
            Queue<double> humidityQueue = met.getHumidityQueue();
            double humidityCtrAverage = 0.0;
            double count = humidityQueue.Count;
            double average = 0.0;

            if (humidityQueue.Count > 0)
                average = humidityQueue.Average();

            //You need to multiple the CtrHumidityValue by 100 because it is currently in decimal form and needs to be displayed in percentage form
            if (Articuno.isUccActive())
                met.CtrHumidityValue = average * 100.0;
            return average;
        }

        internal void updateDewPoint(MetTower met, double ctrTemperature, double ctrHumidity) {
            string metId = met.MetId;
            double dew = CalculateDewPointTemperature(ctrHumidity, ctrTemperature);
            ArticunoLogger.DataLogger.Debug("CTR Dew: {0}", dew);
            if (Articuno.isUccActive())
                GetMetTowerFromId(metId).CtrDewValue = dew;
        }

        /// <summary>
        /// Write Temperature Threshold for all the met tower
        /// </summary>
        /// <param name="value">A double vlaue that represents the temperature threshold</param>
        public void UpdateTemperatureThresholdForAllMetTowers(double value) {
            foreach (MetTower tower in metTowerList) { tower.AmbTempThreshold = value; }
        }
        /// <summary>
        /// Reads the Ambient Temperature threshold from the OPC tag
        /// </summary>
        /// <param name="metTowerId"></param>
        /// <returns></returns>
        public double ReadTemperatureThresholdForMetTower(string metTowerId) => GetMetTowerFromId(metTowerId).AmbTempThreshold;

        /// <summary>
        /// Writes the delta threshold for all the met tower
        /// </summary>
        /// <param name="value">A double vlaue that represents the delta threshold<</param>
        public void writeDeltaThreshold(double value) { foreach (MetTower tower in metTowerList) { tower.DeltaTempThreshold = value; } }
        public double readDeltaThreshold(string metTowerId) => GetMetTowerFromId(metTowerId).DeltaTempThreshold;
        public void writePrimTemperature(string metId, double value) => GetMetTowerFromId(metId).PrimTemperatureValue = value;
        public void writeSecTemperature(string metId, double value) => GetMetTowerFromId(metId).SecTemperatureValue = value;
        public void writeHumidity(string metId, double value) {
            MetTower met = GetMetTowerFromId(metId);
            met.RelativeHumidityValue = value;
        }

        /// <summary>
        /// Calculates the Dew Point Temperature given the ambient temperature and the the relative humidity. Humidity MUST BE IN DECIMAL FORM
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
        public bool isAllMetTowerSensorBadQuality(string metId) {
            MetTower met = GetMetTowerFromId(metId);
            bool currentNoDataState = Convert.ToBoolean(met.NoDataAlarmValue);
            if (!met.isAllSensorGood()) {
                ArticunoLogger.DataLogger.Debug("{0} No Data alarm {1}. NoData Alarm Value {2}", metId, "raised", met.NoDataAlarmValue);
                return true;
            }
            else {
                //To prevent overlogging, only log if the currentNoDataState was true before
                if (currentNoDataState)
                    ArticunoLogger.DataLogger.Debug("{0} No Data alarm {1}. NoData Alarm Value {2}", metId, "cleared", met.NoDataAlarmValue);
                return false;
            }
        }

        public enum MetTowerEnum {
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
        public enum MetQualityEnum {
            MET_GOOD_QUALITY = 1,
            MET_BAD_QUALITY = 0
        }

        /// <summary>
        /// Checks to see if a met tower is frozen given the prefix of the met
        /// </summary>
        /// <param name="metTowerId"></param>
        /// <returns>Boolean. True if frozen, false otherwise</returns>
        public bool IsMetTowerFrozen(MetTower met) => Convert.ToBoolean(met.IceIndicationValue);

        /// <summary>
        /// This function checks to see if icing is occuring for either of hte met towers on site. 
        /// </summary>
        /// <returns>a Boolean indicating if either of the met tower has froze up</returns>
        public bool IsAnyMetTowerFrozenAtSite() {
            bool icingPossible = false;
            foreach (string metPrefix in metPrefixList) { icingPossible |= Convert.ToBoolean(GetMetTowerFromId(metPrefix).IceIndicationValue); }
            return icingPossible;
        }

        public Enum FindMetTowerTag(string metTowerId, string tag) {
            MetTower tempMet = GetMetTowerFromId(metTowerId);
            if (tag.ToUpper().Equals(tempMet.MetSwitchTag.ToUpper())) { return MetTowerEnum.Switched; }
            return null;
        }

        /// <summary>
        /// Get the Met tower given an metTowerId (ie Met, Met2)
        /// </summary>
        /// <param name="metTowerId"></param>
        /// <returns>A Met Tower Object if exist. Null otherwise. createMetTower() must be called before using this fucntion</returns>
        public MetTower GetMetTowerFromId(string metTowerId) {
            for (int i = 0; i < metTowerList.Count; i++) { if (metTowerList.ElementAt(i).MetId.Equals(metTowerId)) { return metTowerList.ElementAt(i); } }
            return null;
        }
        public double CalculateStdDev(Queue<double> queue) {
            double stdDev = 0;
            int count = queue.Count();
            if (count > 1) {
                double avg = queue.Average();
                double sum = queue.Sum(d => (d - avg) * (d - avg));
                stdDev = Math.Sqrt(sum / count);
            }
            return stdDev;
        }

        private void InitializeMetTower(string metId) {
            MetTower met = new MetTower(metId, opcServerName);
            met.PrimTemperatureTag = sitePrefix + dbi.GetMetTowerPrimTempValueTag(metId);
            met.SecTemperatureTag = sitePrefix + dbi.GetMetTowerSecTempValueTag(metId);
            met.RelativeHumidityTag = sitePrefix + dbi.GetMetTowerPrimHumidityTag(metId);
            met.HumidityPrimValueTag = sitePrefix + dbi.GetMetTowerPrimHumidityTag(metId);
            met.MetSwitchTag = sitePrefix + dbi.GetMetTowerSwitchCommandTag(metId);

            //Do not put a site prefix on teh following. It is from the system input tags table
            met.StaleDataSamples = Convert.ToInt16(dbi.getSampleCountForStaleData());

            met.TemperaturePrimBadQualityTag = sitePrefix + dbi.GetMetBadPrimaryTempSensorAlarmTag(metId);
            met.TemperaturePrimOutOfRangeTag = sitePrefix + dbi.GetMetPrimaryTempOutOfRangeAlarmTag(metId);
            met.TemperatureSecOutOfRangeTag = sitePrefix + dbi.GetMetSecondaryTempOutOfRangeAlarmTag(metId);
            met.TemperatureSecBadQualityTag = sitePrefix + dbi.GetMetBadSecondaryTempSensorAlarmTag(metId);
            met.HumidtyOutOfRangeTag = sitePrefix + dbi.GetMetHumidityOutOfRangeAlarmTag(metId);
            met.HumidityBadQualityTag = sitePrefix + dbi.GetMetHumidityBadQualityAlarmTag(metId);
            met.IceIndicationTag = sitePrefix + dbi.GetMetIceIndicationAlarmTag(metId);
            met.NoDataAlarmTag = sitePrefix + dbi.GetMetNoDataAlarmTag(metId);
            met.CtrTemperatureTag = sitePrefix + dbi.GetMetCtrTemperatureTag(metId);
            met.CtrDewTag = sitePrefix + dbi.GetMetCtrDewTag(metId);
            met.CtrHumidityTag = sitePrefix + dbi.GetMetCtrHumidityTag(metId);
            met.SetBackupTurbineForMetTower(tm.GetTurbine(dbi.GetBackupTurbineForMet(metId)));

            met.createSensors();
            metTowerList.Add(met);
        }
    }
}
