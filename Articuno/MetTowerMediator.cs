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
        private string THRESHOLD_QUERY =
            "SELECT OpcTag FROM SystemInputTags WHERE Description = 'AmbTempThreshold' or Description = 'DeltaTmpThreshold'";
        private string SERVER_NAME_QUERY =
            "SELECT OpcTag FROM SystemInputTags WHERE Description ='OpcServerName'";
        private string opcServerName;
        private static List<MetTower> metTowerList = new List<MetTower>();
        private EasyDAClient client = new EasyDAClient();

        //constant bool for quality
        private bool BAD_QUALITY = false;
        private bool GOOD_QUALITY = true;

        //constant bool for managing switched mettower 
        private bool met1Switched;
        private bool met2Switched;

        //thresholds 
        private double deltaThreshold;
        private double ambTempThreshold;

        //Log
        private static readonly ILog log = LogManager.GetLogger(typeof(MetTowerMediator));

        //Database
        static DatabaseInterface dbi;


        private MetTowerMediator()
        {
            met1Switched = false;
            met2Switched = false;
            dbi = DatabaseInterface.Instance;

        }

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
                DataTable reader = dbi.readCommand("SELECT Count(*) as num FROM MetTowerInputTags");
                numMetTower = Convert.ToInt16(reader.Rows[0]["num"]);
            }
            return numMetTower;
        }

        public void createMetTower()
        {
            //Get he threshold OpcTags from the database
            DataTable reader = dbi.readCommand(THRESHOLD_QUERY);
            string temp1 = reader.Rows[0]["OpcTag"].ToString();
            string temp2 = reader.Rows[1]["OpcTag"].ToString();

            reader = dbi.readCommand(SERVER_NAME_QUERY);
            opcServerName = reader.Rows[0]["OpcTag"].ToString();

            //Get the current threshold values 
            DAVtqResult[] vtqResults = client.ReadMultipleItems(opcServerName,
                new DAItemDescriptor[]{
                    temp1,
                    temp2
                });

            ambTempThreshold = Convert.ToDouble(vtqResults[0].Vtq.Value);
            deltaThreshold = Convert.ToDouble(vtqResults[1].Vtq.Value);

            for (int i = 1; i <= getNumMetTower(); i++)
            {
                MetTower metTower = new MetTower("Met" + i.ToString(),
                    ambTempThreshold,
                    deltaThreshold,
                    opcServerName);

                metTowerList.Add(metTower);
            }
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
            metId = isMetTowerSwitched(metId);
            double temperature = (double)readTemperature(metId);
            double rh = readHumidity(metId);
            double dew = calculateDewPoint(rh, temperature);
            double delta = calculateDelta(temperature, dew);
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
            switch (metId.ToUpper())
            {
                case "MET1":
                    log.InfoFormat("met1 switched state from {0} to {1}", met1Switched, !met1Switched);
                    met1Switched = !met1Switched;
                    break;
                case "MET2":
                    log.InfoFormat("met2 switched state from {0} to {1}", met2Switched, !met2Switched);
                    met2Switched = !met2Switched;
                    break;
            }
        }

        /// <summary>
        /// This function returns the metId of the backup met tower if the met tower has been switched to do so
        /// </summary>
        /// <param name="metId">The metid checked to see if it is swtiched</param>
        /// <returns>A metId. Returns the original metId if it is not switched. Returns a the backup metId otherwise</returns>
        private string isMetTowerSwitched(string metId)
        {
            switch (metId.ToUpper())
            {
                case "MET1":
                    log.InfoFormat("currently using : {0}", met1Switched ? "Met2" : metId);
                    return (met1Switched ? "Met2" : metId);
                case "MET2":
                    log.InfoFormat("currently using : {0}", met1Switched ? "Met1" : metId);
                    return (met2Switched ? "Met1" : metId);
                default:
                    log.ErrorFormat("Something went wrong in isMetTOwerSwitched(), metId: {0}", metId);
                    return "";
            }
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
            MetTower met = getMetTower(metId);
            var tuple = tempQualityCheck(metId);
            if (tuple.Item1) { return tuple.Item2; }
            else { return getMetTower(metId).getNearestTurbine().readTemperatureValue(); }
        }

        internal void writeToQueue(string metId, double temperature, double humidity)
        {
            MetTower met = getMetTower(metId);
            met.writeToQueue(temperature, humidity);
        }

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
        public void writeDeltaThreshold(double value)
        {
            foreach (MetTower tower in metTowerList) { tower.DeltaTempThreshold = value; }
        }

        public double readDeltaThreshold(string metTowerId) { return getMetTower(metTowerId).DeltaTempThreshold; }

        public void writePrimTemperature(string metId, double value)
        {
            MetTower met = getMetTower(metId);
            met.writePrimTemperatureValue(value);
        }

        public void writeSecTemperature(string metId, double value)
        {
            MetTower met = getMetTower(metId);
            met.writeSecTemperatureValue(value);
        }

        public double readHumidity(string metId)
        {
            metId = isMetTowerSwitched(metId);
            MetTower met = getMetTower(metId);
            var rhQuality = humidQualityCheck(metId);

            //If the quality for the  humidty value is bad then an alarm should trigger
            //The value is already capped off at 0.00% or 100%
            if (!rhQuality.Item1) { raiseAlarm(met, MetTowerEnum.HumidityOutOfRange); }
            else { clearAlarm(met, MetTowerEnum.HumidityOutOfRange); }
            return rhQuality.Item2;
        }
        public void writeHumidity(string metId, double value)
        {
            MetTower met = getMetTower(metId);
            met.writeRelativeHumityValue(value);
        }

        /// <summary>
        /// Calculates the Dew Point Temperature given the ambient temperature and the the relative humidity
        /// </summary>
        /// <param name="ambTemp">The ambient temperature value (Celcius) from the met tower in double format</param>
        /// <param name="rh">The relative humidity in double format</param>
        /// <returns>The dew point temperature in double format </returns>
        public double calculateDewPoint(double rh, double ambTemp)
        {
            //The following formula is given by Nick Johansen, ask him for more details
            return Math.Pow(rh, 1.0 / 8.0) * (112 + (0.9 * ambTemp)) + (0.1 * ambTemp) - 112;
        }

        /// <summary>
        /// Calculates the Delta Temperature given the ambient Temperature and a dew point temperature (from calculateDewPoitn function)
        /// </summary>
        /// <param name="ambTemp">The ambient temperature value (Celcius) from the met tower in double format</param>
        /// <param name="dewPointTemp">The dew point temperature from calculateDewPoint</param>
        /// <returns>The delta temperature in double format</returns>
        public double calculateDelta(double ambTemp, double dewPointTemp) { return Math.Abs(ambTemp - dewPointTemp); }

        /// <summary>
        /// Check the quality of the met tower. Returns True if the data is 'bad quality'. Returns False if met tower data is 'good quality
        /// </summary>
        /// <returns></returns>
        public bool checkMetTowerQuality(string metId)
        {
            //Todo: Implement
            var tempTuple = tempQualityCheck(metId);
            var humidTuple = humidQualityCheck(metId);
            bool noData = false;
            MetTower met = getMetTower(metId);
            //If both the temperature quality and the humidity quality is bad quality (aka false), then there will be no data
            //Note that unlike the quality, noData does NOT imply quality, so if there really is no data, then it will be True, False otherwise
            if (tempTuple.Item1 == false && humidTuple.Item1 == false)
            {
                noData = true;
                raiseAlarm(met, MetTowerEnum.NoData);
            }
            else
            {
                noData = false;
                clearAlarm(met, MetTowerEnum.NoData);
            }

            return noData;
        }

        /// <summary>
        /// Checks the quality of the relative humidity of the current met tower. 
        /// Returns true if quality is good. False otherwise
        /// </summary>
        /// <returns>Returns True if good quality, False if bad</returns>
        public Tuple<bool, double> humidQualityCheck(string metId)
        {
            MetTower met = getMetTower(metId);
            var rhOpcObject = met.readRelativeHumidityValue();
            double rh = (Double)rhOpcObject;
            double minValue = 0.0;
            double maxValue = 100.0;

            bool state = true;


            //Bad Quality
            //Set primay relative humidty to either 0 (if below 0) or 100 (if above zero)
            //Also Raise alarm
            if (rh < 0.0 || rh > 100.0)
            {
                state = false;
                raiseAlarm(met, MetTowerEnum.HumidityOutOfRange);
                raiseAlarm(met, MetTowerEnum.HumidityQuality);
                rh = ((Math.Abs(0.0 - rh) > 0.0001) ? 0.0 : 100.0);
            }
            //CLear the out of range alarm
            else if (rh > 0.0 && rh < 100.0)
            {
                clearAlarm(met, MetTowerEnum.HumidityOutOfRange);
                clearAlarm(met, MetTowerEnum.HumidityQuality);
            }


            if (!(met.isQualityGood(met.RelativeHumidityTag)))
            {
                state = false;
            }

            return new Tuple<bool, double>(state, rh);
        }

        /// <summary>
        /// Checks the quality of the temperature of the current met tower. 
        /// Returns true if quality is good. False otherwise
        /// </summary>
        /// <param name="temperatureTag">The OPC tag for temperature (either prim or sec)</param>
        /// <returns>Returns True if good quality, False if bad</returns>
        private Tuple<bool, double> tempValueQualityCheck(string temperatureTag)
        {
            var temp = client.ReadItemValue("", opcServerName, temperatureTag);
            double tempValue = Convert.ToDouble(temp);
            double minValue = -20.0;
            double maxValue = 60.0;
            //Bad Quality
            if (tempValue < minValue || tempValue > maxValue) { return new Tuple<bool, double>(false, ((tempValue < minValue) ? minValue : maxValue)); }
            //Normal oepration
            else { return new Tuple<bool, double>(true, tempValue); }
        }

        /// <summary>
        /// Check the temperature quality of both the primary and secondary sensors
        /// </summary>
        /// <returns>Returns True if good quality, False if bad</returns>
        public Tuple<bool, double, double> tempQualityCheck(string metId)
        {
            MetTower met = getMetTower(metId);
            string primTempTag = met.PrimTemperatureTag;
            string secTempTag = met.SecTemperatureTag;
            //call the ValueQuatliyCheck method to verify
            var primTempCheckTuple = tempValueQualityCheck(primTempTag);
            var secTempCheckTuple = tempValueQualityCheck(secTempTag);

            //normal operaiton
            //If both cases are true, then both sensors are working correctly
            if ((primTempCheckTuple.Item1) && (secTempCheckTuple.Item1))
            {
                clearAlarm(met, MetTowerEnum.PrimSensorQuality);
                clearAlarm(met, MetTowerEnum.SecSensorQuality);
                return new Tuple<bool, double, double>(true, primTempCheckTuple.Item2, secTempCheckTuple.Item2);
            }
            //only the secondary Temperature value is suspect. Raise temperature out of range alarm for Temp sensor 1  
            else if (primTempCheckTuple.Item1 && !secTempCheckTuple.Item1)
            {
                raiseAlarm(met, MetTowerEnum.PrimSensorQuality);
                return new Tuple<bool, double, double>(true, primTempCheckTuple.Item2, secTempCheckTuple.Item2);
            }
            //only the primary Temperature value is suspect Raise temperature out of range alarm for Temp sensor 2  
            else if (!primTempCheckTuple.Item1 && secTempCheckTuple.Item1)
            {
                raiseAlarm(met, MetTowerEnum.SecSensorQuality);
                return new Tuple<bool, double, double>(true, primTempCheckTuple.Item2, secTempCheckTuple.Item2);
            }
            //If both sensors are bad.  Use turbine data. Raise alarm
            else
            {
                raiseAlarm(met, MetTowerEnum.PrimSensorQuality);
                raiseAlarm(met, MetTowerEnum.SecSensorQuality);
                return new Tuple<bool, double, double>(false, primTempCheckTuple.Item2, secTempCheckTuple.Item2);
            }
        }

        /// <summary>
        /// Method to set alarms on the met tower class. this is one of the methods that will log alarms
        /// </summary>
        /// <param name="mt"></param>
        /// <param name="metTowerEnum"></param>
        // Note that there is an if statement check to see if it wasn't already in the state beforehand. This is to prevent from constantly logging and constantly overwriting the OPC tag
        private void raiseAlarm(MetTower mt, MetTowerEnum metTowerEnum)
        {
            switch (metTowerEnum)
            {
                case MetTowerEnum.HumidityOutOfRange:
                    if (Convert.ToBoolean(mt.readHumidityOutOfRng()) != BAD_QUALITY)
                    {
                        log.InfoFormat("{0} Humidity sensor out of range alarm raised", mt.getMetTowerPrefix);
                        mt.writeHumidityOutOfRng(BAD_QUALITY);
                    }
                    break;
                case MetTowerEnum.HumidityQuality:
                    if (Convert.ToBoolean(mt.readHumidityBadQuality()) != BAD_QUALITY)
                    {
                        log.InfoFormat("{0} Humidity sensor bad quality alarm raised", mt.getMetTowerPrefix);
                        mt.writeHumidityBadQuality(BAD_QUALITY);
                    }
                    break;
                case MetTowerEnum.PrimSensorQuality:
                    if (Convert.ToBoolean(mt.readTemperaturePrimBadQuality()) != BAD_QUALITY)
                    {
                        log.InfoFormat("{0} Primary Temperature sensor quality alarm raised", mt.getMetTowerPrefix);
                        mt.writeTemperaturePrimBadQuality(BAD_QUALITY);
                    }
                    break;
                case MetTowerEnum.PrimSensorOutOfRange:
                    if (Convert.ToBoolean(mt.readTemperaturePrimOutOfRange()) != BAD_QUALITY)
                    {
                        log.InfoFormat("{0} Primary Temperature sensor quality alarm raised", mt.getMetTowerPrefix);
                        mt.writeTemperaturePrimOutOfRange(BAD_QUALITY);
                    }
                    break;
                case MetTowerEnum.SecSensorQuality:
                    if (Convert.ToBoolean(mt.readTemperatureSecBadQuality()) != BAD_QUALITY)
                    {
                        log.InfoFormat("{0} Secondary Temperature sensor quality alarm raised", mt.getMetTowerPrefix);
                        mt.writeTemperaturePrimBadQuality(BAD_QUALITY);
                    }
                    break;
                case MetTowerEnum.SecSensorOutOfRange:
                    if (Convert.ToBoolean(mt.readTemperatureSecOutOfRange()) != BAD_QUALITY)
                    {
                        log.InfoFormat("{0} Secondary Temperature sensor quality alarm raised", mt.getMetTowerPrefix);
                        mt.writeTemperatureSecOutOfRange(BAD_QUALITY);
                    }
                    break;
                case MetTowerEnum.NoData:
                    if (Convert.ToBoolean(mt.NoDataAlarmValue) != BAD_QUALITY)
                    {
                        log.InfoFormat("{0} No Data alarm raised ", mt.getMetTowerPrefix);
                        mt.NoDataAlarmValue = BAD_QUALITY;
                    }
                    break;

            }
        }

        /// <summary>
        /// Method to clear the alarms on the met tower class. this is one of the methods that will log alarms
        /// </summary>
        /// <param name="mt"></param>
        /// <param name="metTowerEnum"></param>
        private void clearAlarm(MetTower mt, MetTowerEnum metTowerEnum)
        {
            switch (metTowerEnum)
            {
                case MetTowerEnum.HumidityOutOfRange:
                    if (Convert.ToBoolean(mt.readHumidityOutOfRng()) != GOOD_QUALITY)
                    {
                        log.InfoFormat("{0} Humidity sensor out of range alarm cleared", mt.getMetTowerPrefix);
                        mt.writeHumidityOutOfRng(GOOD_QUALITY);
                    }
                    break;
                case MetTowerEnum.HumidityQuality:
                    if (Convert.ToBoolean(mt.readHumidityBadQuality()) != GOOD_QUALITY)
                    {
                        log.InfoFormat("{0} Humidity sensor bad quality alarm cleared", mt.getMetTowerPrefix);
                        mt.writeHumidityBadQuality(GOOD_QUALITY);
                    }
                    break;
                case MetTowerEnum.PrimSensorQuality:
                    if (Convert.ToBoolean(mt.readTemperaturePrimBadQuality()) != GOOD_QUALITY)
                    {
                        log.InfoFormat("{0} Primary Temperature sensor quality alarm cleared", mt.getMetTowerPrefix);
                        mt.writeTemperaturePrimBadQuality(GOOD_QUALITY);
                    }
                    break;
                case MetTowerEnum.PrimSensorOutOfRange:
                    if (Convert.ToBoolean(mt.readTemperaturePrimOutOfRange()) != GOOD_QUALITY)
                    {
                        log.InfoFormat("{0} Primary Temperature sensor quality alarm cleared", mt.getMetTowerPrefix);
                        mt.writeTemperaturePrimOutOfRange(GOOD_QUALITY);
                    }
                    break;
                case MetTowerEnum.SecSensorQuality:
                    if (Convert.ToBoolean(mt.readTemperatureSecBadQuality()) != GOOD_QUALITY)
                    {
                        log.InfoFormat("{0} Secondary Temperature sensor quality alarm cleared", mt.getMetTowerPrefix);
                        mt.writeTemperaturePrimBadQuality(GOOD_QUALITY);
                    }
                    Console.WriteLine(Convert.ToBoolean(mt.readTemperatureSecBadQuality()));
                    break;
                case MetTowerEnum.SecSensorOutOfRange:
                    if (Convert.ToBoolean(mt.readTemperatureSecOutOfRange()) != GOOD_QUALITY)
                    {
                        log.InfoFormat("{0} Secondary Temperature sensor quality alarm cleared", mt.getMetTowerPrefix);
                        mt.writeTemperatureSecOutOfRange(GOOD_QUALITY);
                    }
                    break;
                case MetTowerEnum.NoData:
                    if (Convert.ToBoolean(mt.NoDataAlarmValue) != GOOD_QUALITY)
                    {
                        log.InfoFormat("{0} No Data alarm cleared ", mt.getMetTowerPrefix);
                        mt.NoDataAlarmValue = GOOD_QUALITY;
                    }
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

        private enum MetTowerEnum
        {
            HumidityQuality,
            HumidityOutOfRange,
            PrimSensorQuality,
            PrimSensorOutOfRange,
            SecSensorQuality,
            SecSensorOutOfRange,
            NoData
        }

        //Function that is called by the main Articuno class to determine if the temperature average calculated
        // by ARticuno is considered freezing or not
        public void isFreezing(string metId, double averageTemperature)
        {
            double tempThreshold = readTemperatureThreshold(metId);

            //Freezing Conditions met
            if (averageTemperature < tempThreshold)
            {
                MetTower met = getMetTower(metId);
                met.readIceIndicationValue();
                try
                {
                    met.writeIceIndicationValue(1.00);
                    log.InfoFormat("Icing conditions met for {0}. \n" +
                        "average Temperature {1}, \n" +
                        "Temperature threshold {2} \n",
                        metId, averageTemperature, tempThreshold);
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
                        e, metId, averageTemperature, tempThreshold);
                }
            }
        }

    }
}
