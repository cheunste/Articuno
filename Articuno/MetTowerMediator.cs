using log4net;
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
    class MetTowerMediator
    {
        //the number of met towers. Probably shouldn't be static
        public static int numMetTower;

        //private members
        private string MET_INPUT_TABLE_TAGS =
            "SELECT * FROM MetTowerInputTags WHERE MetId='{0}'";
        private string MET_OUTPUT_TABLE_TAGS =
            "SELECT * FROM MetTowerOutputTags WHERE MetId='{0}'";

        private string MET_NUM =
            "SELECT MetId FROM MetTowerInputTags";

        private static string SELECT_QUERY = "SELECT Count(*) as num FROM MetTowerInputTags";

        private string opcServerName;
        private static List<MetTower> metTowerList = new List<MetTower>();
        private static List<String> metPrefixList = new List<String>();

        //Log
        private static readonly ILog log = LogManager.GetLogger(typeof(MetTowerMediator));

        //Database
        static DatabaseInterface dbi;
        private MetTowerMediator() {

            metPrefixList = new List<string>();
            metTowerList = new List<MetTower>();
            dbi = DatabaseInterface.Instance;
            opcServerName = getOpcServerName();
        }
        private string getOpcServerName() { return DatabaseInterface.Instance.getOpcServer(); }
        public static MetTowerMediator Instance { get { return Nested.instance; } }

        private class Nested
        {
            //Explicit static constructor to tell C# compiler not to mark type as beforefiled init
            static Nested() { }
            internal static readonly MetTowerMediator instance = new MetTowerMediator();
        }

        /// <summary>
        /// Returns the number of met towers in a project
        /// </summary>
        /// <returns></returns>
        public static int getNumMetTower()
        {
            if (numMetTower == 0)
            {
                DataTable reader = dbi.readCommand(SELECT_QUERY);
                numMetTower = Convert.ToInt16(reader.Rows[0]["num"]);
            }
            return numMetTower;
        }

        public void createMetTower()
        {
            log.Info("Creating Met Tower lists");
            metTowerList = new List<MetTower>();
            metTowerList.Clear();

            DatabaseInterface dbi = DatabaseInterface.Instance;

            metPrefixList.Clear();
            createPrefixList();

            foreach (string metPrefix in metPrefixList)
            {
                MetTower met = new MetTower(metPrefix, opcServerName);

                //For Met Tower tags from the MetTowerInputTags table
                string cmd = String.Format(MET_INPUT_TABLE_TAGS, metPrefix);
                DataTable reader = dbi.readCommand(cmd);

                //Set the tags from the MeTowerInputTags table to the accessors
                met.PrimTemperatureTag = reader.Rows[0]["PrimTempValueTag"].ToString();
                met.SecTemperatureTag = reader.Rows[0]["SecTempValueTag"].ToString();
                met.RelativeHumidityTag = reader.Rows[0]["PrimHumidityValueTag"].ToString();
                met.HumidityPrimValueTag = reader.Rows[0]["PrimHumidityValueTag"].ToString();
                met.HumiditySecValueTag = reader.Rows[0]["SecHumidityValueTag"].ToString();
                met.MetSwitchTag = reader.Rows[0]["Switch"].ToString();

                //For Met Tower tags from the MetTowerInputTags table
                cmd = String.Format(MET_OUTPUT_TABLE_TAGS, metPrefix);
                reader = dbi.readCommand(cmd);

                //Set the tags from the MeTowerInputTags table to the accessors
                met.TemperaturePrimBadQualityTag = reader.Rows[0]["TempPrimBadQualityTag"].ToString();
                met.TemperaturePrimOutOfRangeTag = reader.Rows[0]["TempPrimOutOfRangeTag"].ToString();
                met.TemperatureSecOutOfRangeTag = reader.Rows[0]["TempSecOutOfRangeTag"].ToString();
                met.TemperatureSecBadQualityTag = reader.Rows[0]["TempSecBadQualityTag"].ToString();
                met.HumidtyOutOfRangeTag = reader.Rows[0]["HumidityOutOfRangeTag"].ToString();
                met.HumidityBadQualityTag = reader.Rows[0]["HumidityBadQualityTag"].ToString();
                met.IceIndicationTag = reader.Rows[0]["IceIndicationTag"].ToString();
                met.NoDataAlarmTag = reader.Rows[0]["NoDataAlarmTag"].ToString();

                metTowerList.Add(met);
            }
        }

        public void createPrefixList()
        {
            DataTable reader = DatabaseInterface.Instance.readCommand(MET_NUM);
            foreach (DataRow item in reader.Rows) { metPrefixList.Add(item["MetId"].ToString()); }
        }

        /// <summary>
        /// Get the Met tower given an metTowerId (ie Met1, Met2)
        /// </summary>
        /// <param name="metTowerId"></param>
        /// <returns>A Met Tower Object if exist. Null otherwise. createMetTower() must be called before using this fucntion</returns>
        public MetTower getMetTower(string metTowerId)
        {
            for (int i = 0; i <= metTowerList.Count; i++)
            {
                if (metTowerList.ElementAt(i).getMetTowerPrefix.Equals(metTowerId)) { return metTowerList.ElementAt(i); }
            }
            return null;
        }

        /// <summary>
        /// Returns a tuple containing an ambient Temperature, a relative humidity, calculated dew point and a temperature delta given a met tower id
        /// </summary>
        /// <param name="metId">the met tower id (ie Met1) in string</param>
        /// <returns>Tuple of doubles</returns>
        public Tuple<double, double, double, double> getAllMeasurements(string metId)
        {
            double temperature = (double)readTemperature(metId);
            double rh = readHumidity(metId);
            double dew = calculateDewPoint(rh, temperature);
            double delta = calculateDelta(temperature, dew);
            log.DebugFormat("{0}, temp: {1}, rh: {2}, dew:{3}, delta: {4}", metId, temperature, rh, dew, delta);
            return new Tuple<double, double, double, double>(temperature, rh, dew, delta);
        }

        /// <summary>
        /// This function switches the met tower to use the backup met tower.
        /// For example, if Met1 is passed in, then it will use Met2 and vice versa.
        /// 
        /// </summary>
        /// <param name="metId"></param>
        public void switchMetTower(string metId)
        {
            MetTower met = getMetTower(metId);
            met.MetSwitchValue = !met.MetSwitchValue;
        }

        /// <summary>
        /// This function returns the metId of the backup met tower if the met tower has been switched to do so
        /// </summary>
        /// <param name="metId">The metid checked to see if it is swtiched</param>
        /// <returns>A metId. Returns the original metId if it is not switched. Returns a the backup metId otherwise</returns>
        public string isMetTowerSwitched(string metId)
        {
            if (Convert.ToBoolean(getMetTower(metId).MetSwitchValue))
                return metId.Equals("Met1") ? "Met2" : "Met1";
            else
                return metId;
        }

        /// <summary>
        ///  Gets the one minute average temperature of a met tower primary or secondary sensor. 
        /// </summary>
        /// <param name="metId"></param>
        /// <returns>A double if the quality is good for either the primary or secondary sensor. Returns the turbine temperature otherwise
        /// </returns>
        public Object readTemperature(string metId)
        {
            metId = isMetTowerSwitched(metId);
            var tuple = tempQualityCheck(metId);
            var primSensorQuality = tuple.Item1;
            var secSensorQuality = tuple.Item2;
            var primSensorValue = tuple.Item3;
            var secSensorValue = tuple.Item4;


            if (primSensorQuality.Equals(MetQualityEnum.MET_GOOD_QUALITY))
            {
                log.DebugFormat("{0} good quality. Using Primary Temperature Sensor value. Current temperature : {1}", metId, primSensorValue);
                return primSensorValue;
            }
            else if (secSensorQuality.Equals(MetQualityEnum.MET_GOOD_QUALITY))
            {
                log.DebugFormat("{0} bad quality. Using Secondary Temperature Sensor Value. Current temperature : {1}", metId, secSensorValue);
                return secSensorValue;
            }
            else
            {
                Object newTemp = getMetTower(metId).getNearestTurbine().readTemperatureValue();
                log.DebugFormat("{0} both sensors bad quality. Using Turbine Temperature. Current Temperature: {1}. Quality S1: {2}, S2: {3}", metId, Convert.ToDouble(newTemp), primSensorQuality, secSensorQuality);
                return newTemp;
            }
        }

        internal void writeToQueue(string metId, double temperature, double humidity) { getMetTower(metId).writeToQueue(temperature, humidity); }

        internal double calculateCtrAvgTemperature(string metId)
        {
            Queue<double> tempQueue = getMetTower(metId).getTemperatureQueue();
            double temperatureCtrAverage = 0.0;
            double count = tempQueue.Count;
            while (tempQueue.Count != 0) { temperatureCtrAverage += tempQueue.Dequeue(); }
            return temperatureCtrAverage / count;

        }

        internal double calculateCtrAvgHumidity(string metId)
        {
            Queue<double> humidityQueue = getMetTower(metId).getHumidityQueue();
            double humidityCtrAverage = 0.0;
            double count = humidityQueue.Count;
            while (humidityQueue.Count != 0) { humidityCtrAverage += humidityQueue.Dequeue(); }
            return humidityCtrAverage / count;
        }

        /// <summary>
        /// Write Temperature Threshold for all the met tower
        /// </summary>
        /// <param name="value">A double vlaue that represents the temperature threshold</param>
        public void writeTemperatureThreshold(double value)
        {
            foreach (MetTower tower in metTowerList) { tower.AmbTempThreshold = value; }
        }
        public double readTemperatureThreshold(string metTowerId) { return getMetTower(metTowerId).DeltaTempThreshold; }

        /// <summary>
        /// Writes the delta threshold for all the met tower
        /// </summary>
        /// <param name="value">A double vlaue that represents the delta threshold<</param>
        public void writeDeltaThreshold(double value) { foreach (MetTower tower in metTowerList) { tower.DeltaTempThreshold = value; } }

        public double readDeltaThreshold(string metTowerId) { return getMetTower(metTowerId).DeltaTempThreshold; }

        public void writePrimTemperature(string metId, double value) { getMetTower(metId).PrimTemperatureValue = value; }

        public void writeSecTemperature(string metId, double value) { getMetTower(metId).SecTemperatureValue = value; }

        public double readHumidity(string metId)
        {
            metId = isMetTowerSwitched(metId);
            var rhQuality = humidQualityCheck(metId);
            return rhQuality.Item2;
        }
        public void writeHumidity(string metId, double value)
        {
            MetTower met = getMetTower(metId);
            met.RelativeHumidityValue = value;
        }

        /// <summary>
        /// Calculates the Dew Point Temperature given the ambient temperature and the the relative humidity
        /// </summary>
        /// <param name="ambTemp">The ambient temperature value (Celcius) from the met tower in double format</param>
        /// <param name="rh">The relative humidity in double format</param>
        /// <returns>The dew point temperature in double format </returns>
        public double calculateDewPoint(double rh, double ambTemp) => Math.Round(Math.Pow(rh, 1.0 / 8.0) * (112 + (0.9 * ambTemp)) + (0.1 * ambTemp) - 112, 3);

        /// <summary>
        /// Calculates the Delta Temperature given the ambient Temperature and a dew point temperature (from calculateDewPoitn function)
        /// </summary>
        /// <param name="ambTemp">The ambient temperature value (Celcius) from the met tower in double format</param>
        /// <param name="dewPointTemp">The dew point temperature from calculateDewPoint</param>
        /// <returns>The delta temperature in double format</returns>
        public double calculateDelta(double ambTemp, double dewPointTemp)=> Math.Round(Math.Abs(ambTemp - dewPointTemp), 3); 

        /// <summary>
        /// Check the quality of the met tower. Returns True if the data is 'bad quality'. Returns False if met tower data is 'good quality
        /// </summary>
        /// <returns></returns>
        public MetQualityEnum checkMetTowerQuality(string metId)
        {
            //Todo: Implement
            var tempTuple = tempQualityCheck(metId);
            var humidTuple = humidQualityCheck(metId);
            MetQualityEnum noData = MetQualityEnum.MET_GOOD_QUALITY;
            MetTower met = getMetTower(metId);
            //If both the temperature quality and the humidity quality is bad quality (aka false), then there will be no data
            //Note that unlike the quality, noData does NOT imply quality, so if there really is no data, then it will be True, False otherwise
            if (tempTuple.Item1 == MetQualityEnum.MET_BAD_QUALITY && tempTuple.Item2 == MetQualityEnum.MET_BAD_QUALITY && humidTuple.Item1 == MetQualityEnum.MET_BAD_QUALITY)
            {
                noData = MetQualityEnum.MET_BAD_QUALITY;
                alarm(met, MetTowerEnum.NoData, noData);
            }
            else
                alarm(met, MetTowerEnum.NoData, noData);

            return noData;
        }

        /// <summary>
        /// Checks the quality of the relative humidity of the current met tower. 
        /// Returns true if quality is good. False otherwise. IMPORTANT: This function will convert the Humidty from % to decimal
        /// </summary>
        /// <returns>Returns True if good quality, False if bad</returns>
        public Tuple<MetQualityEnum, double> humidQualityCheck(string metId)
        {
            MetTower met = getMetTower(metId);
            //IMPORTANT: The OPC Value is read as a percentage, but this program needs the humidity as a decimal (ie between 0 and 1)
            double rh = Convert.ToDouble(met.RelativeHumidityValue)/100.00;
            double minValue = 0.0;
            double maxValue = 1.0;
            double minCapValue = 0.00;
            double maxCapValue = 0.99;

            var qualityState = MetQualityEnum.MET_GOOD_QUALITY;

            //Bad Quality
            //Set primay relative humidty to either 0 (if below 0) or 100 (if above zero)
            //Also Raise alarm
            if (rh <= minValue || rh >= maxValue)
            {
                qualityState = MetQualityEnum.MET_BAD_QUALITY;
                alarm(met, MetTowerEnum.HumidityOutOfRange, qualityState);
                alarm(met, MetTowerEnum.HumidityQuality, qualityState);
                rh = (rh <= minValue) ? minCapValue : maxCapValue;
                log.DebugFormat("Humidity exceeded allowable range. Capping Relative Humidity of {0} at {1}", metId, rh);
            }
            //CLear the out of range alarm
            else if (rh > minValue && rh < maxValue)
            {
                alarm(met, MetTowerEnum.HumidityOutOfRange, qualityState);
                alarm(met, MetTowerEnum.HumidityQuality, qualityState);
            }

            //If the quality for the relative humidity tag is bad, then immediately make the local  variable bad
            if (!(met.isQualityGood(met.RelativeHumidityTag))) { qualityState = MetQualityEnum.MET_BAD_QUALITY; }

            return new Tuple<MetQualityEnum, double>(qualityState, rh);
        }

        /// <summary>
        /// Checks the quality of the temperature of the current met tower. 
        /// Returns true if quality is good. False otherwise
        /// </summary>
        /// <param name="temperatureTag">The OPC tag for temperature (either prim or sec)</param>
        /// <returns>Returns True if good quality, False if bad</returns>
        private Tuple<MetQualityEnum, double> tempValueCheck(string temperatureTag, double tempValue)
        {
            double minValue = -20.0;
            double maxValue = 60.0;
            //Bad Quality
            if (tempValue <= minValue || tempValue >= maxValue)
            {
                var newTemperature = ((tempValue <= minValue) ? minValue : maxValue);
                log.DebugFormat("Temperature sensor of tag {0} out of range. Capping temperature at {1}", temperatureTag, newTemperature);
                return new Tuple<MetQualityEnum, double>(MetQualityEnum.MET_BAD_QUALITY, newTemperature);
            }
            //Normal oepration
            else { return new Tuple<MetQualityEnum, double>(MetQualityEnum.MET_GOOD_QUALITY, tempValue); }
        }

        /// <summary>
        /// Check the temperature quality of both the primary and secondary sensors
        /// </summary>
        /// <returns>Returns True if good quality, False if bad</returns>
        public Tuple<MetQualityEnum, MetQualityEnum, double, double> tempQualityCheck(string metId)
        {
            MetTower met = getMetTower(metId);
            string primTempTag = met.PrimTemperatureTag;
            string secTempTag = met.SecTemperatureTag;
            double primTempValue = Convert.ToDouble(met.PrimTemperatureValue);
            double secTempValue = Convert.ToDouble(met.SecTemperatureValue);

            //call the ValueQuatliyCheck method to verify
            var primTempCheckTuple = tempValueCheck(primTempTag, primTempValue);
            var secTempCheckTuple = tempValueCheck(secTempTag, secTempValue);

            var primTempQuality = primTempCheckTuple.Item1;
            var secTempQuality = secTempCheckTuple.Item1;

            if (primTempQuality.Equals(MetQualityEnum.MET_GOOD_QUALITY))
            {
                alarm(met, MetTowerEnum.PrimSensorOutOfRange, MetQualityEnum.MET_GOOD_QUALITY);
                alarm(met, MetTowerEnum.PrimSensorQuality, MetQualityEnum.MET_GOOD_QUALITY);
            }
            else
            {
                alarm(met, MetTowerEnum.PrimSensorOutOfRange, MetQualityEnum.MET_BAD_QUALITY);
                alarm(met, MetTowerEnum.PrimSensorQuality, MetQualityEnum.MET_BAD_QUALITY);
            }
            if (secTempQuality.Equals(MetQualityEnum.MET_GOOD_QUALITY))
            {
                alarm(met, MetTowerEnum.SecSensorOutOfRange, MetQualityEnum.MET_GOOD_QUALITY);
                alarm(met, MetTowerEnum.SecSensorQuality, MetQualityEnum.MET_GOOD_QUALITY);
            }
            else
            {
                alarm(met, MetTowerEnum.SecSensorOutOfRange, MetQualityEnum.MET_BAD_QUALITY);
                alarm(met, MetTowerEnum.SecSensorQuality, MetQualityEnum.MET_BAD_QUALITY);
            }

            return new Tuple<MetQualityEnum, MetQualityEnum, double, double>(primTempQuality, secTempQuality, primTempCheckTuple.Item2, secTempCheckTuple.Item2);
        }


        /// <summary>
        /// Method to raise or clear alarm
        /// </summary>
        /// <param name="mt">Met Tower</param>
        /// <param name="metTowerEnum">MetTower Enum</param>
        /// <param name="quality">The quality Enum. </param>
        private void alarm(MetTower mt, MetTowerEnum metTowerEnum, MetQualityEnum quality)
        {
            //Good Quality will return a false (in active alarm). Bad quality  will return a true (active alarm)
            // If Good Quality (true), then clear alarm (set false)
            // If Bad Quality (true), then raise the alarm (set true)
            var logComment = Convert.ToBoolean(quality) ? "cleared" : "raised";
            bool status = Convert.ToBoolean(quality) ? false : true;


            switch (metTowerEnum)
            {
                case MetTowerEnum.HumidityOutOfRange:
                    if (Convert.ToBoolean(mt.HumidityOutOfRng) != Convert.ToBoolean(status))
                        log.InfoFormat("{0} Humidity sensor out of range alarm {1}. Humidity Value: {2}", mt.getMetTowerPrefix, logComment, mt.RelativeHumidityValue);
                    mt.HumidityOutOfRng = Convert.ToBoolean(status);
                    break;
                case MetTowerEnum.HumidityQuality:
                    if (Convert.ToBoolean(mt.HumidityBadQuality) != Convert.ToBoolean(status))
                        log.InfoFormat("{0} Humidity sensor bad status alarm {1}. Humidity Value {2}", mt.getMetTowerPrefix, logComment, mt.RelativeHumidityValue);
                    mt.HumidityBadQuality = Convert.ToBoolean(status);
                    break;
                case MetTowerEnum.PrimSensorQuality:
                    if (Convert.ToBoolean(mt.TemperaturePrimBadQuality) != Convert.ToBoolean(status))
                        log.InfoFormat("{0} Primary Temperature sensor quality status alarm {1}. Primary Temp: {2}", mt.getMetTowerPrefix, logComment, mt.PrimTemperatureValue);
                    mt.TemperaturePrimBadQuality = Convert.ToBoolean(status);
                    break;
                case MetTowerEnum.PrimSensorOutOfRange:
                    if (Convert.ToBoolean(mt.TemperaturePrimOutOfRange) != Convert.ToBoolean(status))
                        log.InfoFormat("{0} Primary Temperature sensor out of range alarm {1}. Primary Temp: {2}. Alarm Status:{3}", mt.getMetTowerPrefix, logComment, mt.PrimTemperatureValue, status);
                    mt.TemperaturePrimOutOfRange = Convert.ToBoolean(status);
                    break;
                case MetTowerEnum.SecSensorQuality:
                    if (Convert.ToBoolean(mt.TemperatureSecBadQuality) != Convert.ToBoolean(status))
                        log.InfoFormat("{0} Secondary Temperature sensor quality status alarm {1}. Sec Temp: {2}", mt.getMetTowerPrefix, logComment, mt.SecTemperatureValue);
                    mt.TemperatureSecBadQuality = Convert.ToBoolean(status);
                    break;
                case MetTowerEnum.SecSensorOutOfRange:
                    if (Convert.ToBoolean(mt.TemperatureSecOutOfRange) != Convert.ToBoolean(status))
                        log.InfoFormat("{0} Secondary Temperature sensor out of range alarm {1}. Sec Temp: {2}", mt.getMetTowerPrefix, logComment, mt.SecTemperatureValue);
                    mt.TemperatureSecOutOfRange = Convert.ToBoolean(status);
                    break;
                case MetTowerEnum.NoData:
                    if (Convert.ToBoolean(mt.NoDataAlarmValue) != Convert.ToBoolean(status))
                        log.InfoFormat("{0} No Data alarm {1}. NoData Alarm Value {2}", mt.getMetTowerPrefix, logComment, mt.NoDataAlarmValue);
                    mt.NoDataAlarmValue = Convert.ToBoolean(status);
                    break;
            }
        }

        /// <summary>
        /// Method used to set a met tower to a turbine
        /// </summary>
        /// <param name="metId">The met id. Met1 or Met2</param>
        /// <param name="turbine">The turbine id (ie T001)</param>
        public void setTurbineBackup(string metId, Turbine turbine)
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

        //Function that is called by the main Articuno class to determine if the temperature average calculated
        // by ARticuno is considered freezing or not
        /// <summary>
        /// Method to check a met tower to see if it meets the freezing condition and set its condition. Returns true if iti s frozen, false otherwise
        /// </summary>
        public bool setFrozenCondition(string metId, double avgTemperature, double avgHumidity)
        {
            double tempThreshold = readTemperatureThreshold(metId);
            double deltaThreshold = readDeltaThreshold(metId);

            double dewPoint =calculateDewPoint(avgHumidity, avgTemperature);
            double delta =calculateDelta(avgTemperature, dewPoint);

            Console.WriteLine("Temp Threshold {0}", tempThreshold);
            Console.WriteLine("Delta Threshold {0}", deltaThreshold);

            MetTower met = getMetTower(metId);
            //Freezing Conditions met
            if ((avgTemperature <= tempThreshold) && (delta<=deltaThreshold))
            {
                try
                {
                    met.IceIndicationValue = true;
                    log.InfoFormat("Icing conditions met for {0}. \n" +
                        "{0} Average Temperature {1}, \n" +
                        "{0} Temperature threshold {2} \n",
                        metId, avgTemperature, tempThreshold);
                }
                catch (Exception e)
                {
                    //in case you can't write to OPC
                    log.ErrorFormat("Error when writing to the " +
                        "Ice indication.\n" +
                        "Error: {0}. \n" +
                        "Met: {1}, \n" +
                        "avgTemp: {2}, \n" +
                        "tempThreshold {3}\n",
                        e, metId, avgTemperature, tempThreshold);
                }
            }
            else { met.IceIndicationValue = false; }

            return Convert.ToBoolean(met.IceIndicationValue);
        }

        /// <summary>
        /// Checks to see if a met tower is frozen given the prefix of the met
        /// </summary>
        /// <param name="metTowerId"></param>
        /// <returns>Boolean. True if frozen, false otherwise</returns>
        public bool isMetFrozen(string metTowerId) { return Convert.ToBoolean(getMetTower(metTowerId).IceIndicationValue); }

        public Enum findMetTowerTag(string metTowerId, string tag)
        {
            MetTower tempMet = getMetTower(metTowerId);
            if (tag.ToUpper().Equals(tempMet.MetSwitchTag.ToUpper())) { return MetTowerEnum.Switched; }
            return null;
        }
    }
}
