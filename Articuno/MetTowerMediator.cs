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
    class MetTowerMediator
    {
        //the number of met towers. Probably shouldn't be static
        public static int numMetTower;

        //private members
        private string THRESHOLD_QUERY =
            "SELECT OpcTag FROM SystemInputTags WHERE Description = 'AmbTempThreshold' or Description = 'DeltaTmpThreshold'";
        private string SERVER_NAME_QUERY =
            "SELECT OpcTag FROM SystemInputTags WHERE Description ='OpcServerName'";
        private static string SELECT_QUERY = "SELECT Count(*) as num FROM MetTowerInputTags";

        private string opcServerName;
        private static List<MetTower> metTowerList = new List<MetTower>();
        private EasyDAClient client = new EasyDAClient();

        //constant bool for quality

        //thresholds 
        private double deltaThreshold;
        private double ambTempThreshold;

        //Log
        private static readonly ILog log = LogManager.GetLogger(typeof(MetTowerMediator));

        //Database
        static DatabaseInterface dbi;
        private MetTowerMediator()
        {
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
                DataTable reader = dbi.readCommand(SELECT_QUERY);
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
        private string isMetTowerSwitched(string metId)
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
            MetTower met = getMetTower(metId);
            var tuple = tempQualityCheck(metId);
            if (tuple.Item1.Equals(MetQualityEnum.MET_GOOD_QUALITY))
            {
                log.DebugFormat("{0} good quality. Current temperature : {1}", metId, tuple.Item2);
                return tuple.Item2;
            }
            else
            {
                Object newTemp = getMetTower(metId).getNearestTurbine().readTemperatureValue();
                log.DebugFormat("{0} bad quality Using Turbine Temperature. Current Temperature: {1}", metId, Convert.ToDouble(newTemp));
                return newTemp;
            }

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
        public void writeDeltaThreshold(double value) { foreach (MetTower tower in metTowerList) { tower.DeltaTempThreshold = value; } }

        public double readDeltaThreshold(string metTowerId) { return getMetTower(metTowerId).DeltaTempThreshold; }

        public void writePrimTemperature(string metId, double value)
        {
            MetTower met = getMetTower(metId);
            met.PrimTemperatureValue = value;
        }

        public void writeSecTemperature(string metId, double value)
        {
            MetTower met = getMetTower(metId);
            met.SecTemperatureValue = value;
        }

        public double readHumidity(string metId)
        {
            metId = isMetTowerSwitched(metId);
            MetTower met = getMetTower(metId);
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
        public double calculateDewPoint(double rh, double ambTemp)
        {
            //The following formula is given by Nick Johansen, ask him for more details
            return Math.Round(Math.Pow(rh, 1.0 / 8.0) * (112 + (0.9 * ambTemp)) + (0.1 * ambTemp) - 112, 3);
        }

        /// <summary>
        /// Calculates the Delta Temperature given the ambient Temperature and a dew point temperature (from calculateDewPoitn function)
        /// </summary>
        /// <param name="ambTemp">The ambient temperature value (Celcius) from the met tower in double format</param>
        /// <param name="dewPointTemp">The dew point temperature from calculateDewPoint</param>
        /// <returns>The delta temperature in double format</returns>
        public double calculateDelta(double ambTemp, double dewPointTemp) { return Math.Round(Math.Abs(ambTemp - dewPointTemp), 3); }

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
            if (tempTuple.Item1 == MetQualityEnum.MET_BAD_QUALITY && humidTuple.Item1 == MetQualityEnum.MET_BAD_QUALITY)
            {
                noData = MetQualityEnum.MET_BAD_QUALITY;
                raiseAlarm(met, MetTowerEnum.NoData);
            }
            else
            {
                noData = MetQualityEnum.MET_GOOD_QUALITY;
                clearAlarm(met, MetTowerEnum.NoData);
            }

            return noData;
        }

        /// <summary>
        /// Checks the quality of the relative humidity of the current met tower. 
        /// Returns true if quality is good. False otherwise
        /// </summary>
        /// <returns>Returns True if good quality, False if bad</returns>
        public Tuple<MetQualityEnum, double> humidQualityCheck(string metId)
        {
            MetTower met = getMetTower(metId);
            var rhOpcObject = met.RelativeHumidityValue;
            double rh = (Double)rhOpcObject;
            double minValue = 0.0;
            double maxValue = 100.0;

            var qualityState = MetQualityEnum.MET_GOOD_QUALITY;

            //Bad Quality
            //Set primay relative humidty to either 0 (if below 0) or 100 (if above zero)
            //Also Raise alarm
            if (rh < 0.0 || rh > 100.0)
            {
                qualityState = MetQualityEnum.MET_BAD_QUALITY;
                raiseAlarm(met, MetTowerEnum.HumidityOutOfRange);
                raiseAlarm(met, MetTowerEnum.HumidityQuality);
                //rh = ((Math.Abs(0.0 - rh) > 0.0001) ? 0.0 : 100.0);
                rh = (rh < 0.0) ? 0.000 : 99.00;
            }
            //CLear the out of range alarm
            else if (rh > 0.0 && rh < 100.0)
            {
                clearAlarm(met, MetTowerEnum.HumidityOutOfRange);
                clearAlarm(met, MetTowerEnum.HumidityQuality);
            }


            if (!(met.isQualityGood(met.RelativeHumidityTag)))
            {
                qualityState = MetQualityEnum.MET_BAD_QUALITY;
            }

            return new Tuple<MetQualityEnum, double>(qualityState, rh);
        }

        /// <summary>
        /// Checks the quality of the temperature of the current met tower. 
        /// Returns true if quality is good. False otherwise
        /// </summary>
        /// <param name="temperatureTag">The OPC tag for temperature (either prim or sec)</param>
        /// <returns>Returns True if good quality, False if bad</returns>
        private Tuple<MetQualityEnum, double> tempValueQualityCheck(string temperatureTag)
        {
            var temp = client.ReadItemValue("", opcServerName, temperatureTag);
            double tempValue = Convert.ToDouble(temp);
            double minValue = -20.0;
            double maxValue = 60.0;
            //Bad Quality
            if (tempValue < minValue || tempValue > maxValue) { return new Tuple<MetQualityEnum, double>(MetQualityEnum.MET_BAD_QUALITY, ((tempValue < minValue) ? minValue : maxValue)); }
            //Normal oepration
            else { return new Tuple<MetQualityEnum, double>(MetQualityEnum.MET_GOOD_QUALITY, tempValue); }
        }

        /// <summary>
        /// Check the temperature quality of both the primary and secondary sensors
        /// </summary>
        /// <returns>Returns True if good quality, False if bad</returns>
        public Tuple<MetQualityEnum, double, double> tempQualityCheck(string metId)
        {
            MetTower met = getMetTower(metId);
            string primTempTag = met.PrimTemperatureTag;
            string secTempTag = met.SecTemperatureTag;
            //call the ValueQuatliyCheck method to verify
            var primTempCheckTuple = tempValueQualityCheck(primTempTag);
            var secTempCheckTuple = tempValueQualityCheck(secTempTag);

            var primTempQuality = primTempCheckTuple.Item1;
            var secTempQuality = secTempCheckTuple.Item1;

            if (primTempQuality.Equals(MetQualityEnum.MET_GOOD_QUALITY)) { clearAlarm(met, MetTowerEnum.PrimSensorQuality); }
            else { raiseAlarm(met, MetTowerEnum.PrimSensorQuality); }
            if (secTempQuality.Equals(MetQualityEnum.MET_GOOD_QUALITY)) { clearAlarm(met, MetTowerEnum.SecSensorQuality); }
            else { raiseAlarm(met, MetTowerEnum.SecSensorQuality); }

            MetQualityEnum temp = primTempQuality & secTempQuality;

            return new Tuple<MetQualityEnum, double, double>(temp, primTempCheckTuple.Item2, secTempCheckTuple.Item2);
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
                    if (Convert.ToBoolean(mt.HumidityOutOfRng) != Convert.ToBoolean(MetQualityEnum.MET_BAD_QUALITY))
                    {
                        log.InfoFormat("{0} Humidity sensor out of range alarm raised", mt.getMetTowerPrefix);
                        mt.HumidityOutOfRng = Convert.ToBoolean(MetQualityEnum.MET_BAD_QUALITY);
                    }
                    break;
                case MetTowerEnum.HumidityQuality:
                    if (Convert.ToBoolean(mt.HumidityBadQuality) != Convert.ToBoolean(MetQualityEnum.MET_BAD_QUALITY))
                    {
                        log.InfoFormat("{0} Humidity sensor bad quality alarm raised", mt.getMetTowerPrefix);
                        mt.HumidityBadQuality = Convert.ToBoolean(MetQualityEnum.MET_BAD_QUALITY);
                    }
                    break;
                case MetTowerEnum.PrimSensorQuality:
                    if (Convert.ToBoolean(mt.TemperaturePrimBadQuality) != Convert.ToBoolean(MetQualityEnum.MET_BAD_QUALITY))
                    {
                        log.InfoFormat("{0} Primary Temperature sensor quality alarm raised", mt.getMetTowerPrefix);
                        mt.TemperaturePrimBadQuality = Convert.ToBoolean(MetQualityEnum.MET_BAD_QUALITY);
                    }
                    break;
                case MetTowerEnum.PrimSensorOutOfRange:
                    if (Convert.ToBoolean(mt.TemperaturePrimOutOfRange) != Convert.ToBoolean(MetQualityEnum.MET_BAD_QUALITY))
                    {
                        log.InfoFormat("{0} Primary Temperature sensor quality alarm raised", mt.getMetTowerPrefix);
                        mt.TemperaturePrimOutOfRange = Convert.ToBoolean(MetQualityEnum.MET_BAD_QUALITY);
                    }
                    break;
                case MetTowerEnum.SecSensorQuality:
                    if (Convert.ToBoolean(mt.TemperatureSecBadQuality) != Convert.ToBoolean(MetQualityEnum.MET_BAD_QUALITY))
                    {
                        log.InfoFormat("{0} Secondary Temperature sensor quality alarm raised", mt.getMetTowerPrefix);
                        mt.TemperatureSecBadQuality = Convert.ToBoolean(MetQualityEnum.MET_BAD_QUALITY);
                    }
                    break;
                case MetTowerEnum.SecSensorOutOfRange:
                    if (Convert.ToBoolean(mt.TemperatureSecOutOfRange) != Convert.ToBoolean(MetQualityEnum.MET_BAD_QUALITY))
                    {
                        log.InfoFormat("{0} Secondary Temperature sensor quality alarm raised", mt.getMetTowerPrefix);
                        mt.TemperatureSecOutOfRange = Convert.ToBoolean(MetQualityEnum.MET_BAD_QUALITY);
                    }
                    break;
                case MetTowerEnum.NoData:
                    if (Convert.ToBoolean(mt.NoDataAlarmValue) != Convert.ToBoolean(MetQualityEnum.MET_BAD_QUALITY))
                    {
                        log.InfoFormat("{0} No Data alarm raised ", mt.getMetTowerPrefix);
                        mt.NoDataAlarmValue = Convert.ToBoolean(MetQualityEnum.MET_BAD_QUALITY);
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
                    if (Convert.ToBoolean(mt.HumidityOutOfRng) != Convert.ToBoolean(MetQualityEnum.MET_GOOD_QUALITY))
                    {
                        log.InfoFormat("{0} Humidity sensor out of range alarm cleared", mt.getMetTowerPrefix);
                        mt.HumidityOutOfRng = Convert.ToBoolean(MetQualityEnum.MET_GOOD_QUALITY);
                    }
                    break;
                case MetTowerEnum.HumidityQuality:
                    if (Convert.ToBoolean(mt.HumidityBadQuality) != Convert.ToBoolean(MetQualityEnum.MET_GOOD_QUALITY))
                    {
                        log.InfoFormat("{0} Humidity sensor bad quality alarm cleared", mt.getMetTowerPrefix);
                        mt.HumidityBadQuality = Convert.ToBoolean(MetQualityEnum.MET_GOOD_QUALITY);
                    }
                    break;
                case MetTowerEnum.PrimSensorQuality:
                    if (Convert.ToBoolean(mt.TemperaturePrimBadQuality) != Convert.ToBoolean(MetQualityEnum.MET_GOOD_QUALITY))
                    {
                        log.InfoFormat("{0} Primary Temperature sensor quality alarm cleared", mt.getMetTowerPrefix);
                        mt.TemperaturePrimBadQuality = Convert.ToBoolean(MetQualityEnum.MET_GOOD_QUALITY);
                    }
                    break;
                case MetTowerEnum.PrimSensorOutOfRange:
                    if (Convert.ToBoolean(mt.TemperaturePrimOutOfRange) != Convert.ToBoolean(MetQualityEnum.MET_GOOD_QUALITY))
                    {
                        log.InfoFormat("{0} Primary Temperature sensor quality alarm cleared", mt.getMetTowerPrefix);
                        mt.TemperaturePrimOutOfRange = Convert.ToBoolean(MetQualityEnum.MET_GOOD_QUALITY);
                    }
                    break;
                case MetTowerEnum.SecSensorQuality:
                    if (Convert.ToBoolean(mt.TemperatureSecBadQuality) != Convert.ToBoolean(MetQualityEnum.MET_GOOD_QUALITY))
                    {
                        log.InfoFormat("{0} Secondary Temperature sensor quality alarm cleared", mt.getMetTowerPrefix);
                        mt.TemperatureSecBadQuality = Convert.ToBoolean(MetQualityEnum.MET_GOOD_QUALITY);
                    }
                    break;
                case MetTowerEnum.SecSensorOutOfRange:
                    if (Convert.ToBoolean(mt.TemperatureSecOutOfRange) != Convert.ToBoolean(MetQualityEnum.MET_GOOD_QUALITY))
                    {
                        log.InfoFormat("{0} Secondary Temperature sensor quality alarm cleared", mt.getMetTowerPrefix);
                        mt.TemperatureSecOutOfRange = Convert.ToBoolean(MetQualityEnum.MET_GOOD_QUALITY);
                    }
                    break;
                case MetTowerEnum.NoData:
                    if (Convert.ToBoolean(mt.NoDataAlarmValue) != Convert.ToBoolean(MetQualityEnum.MET_GOOD_QUALITY))
                    {
                        log.InfoFormat("{0} No Data alarm cleared ", mt.getMetTowerPrefix);
                        mt.NoDataAlarmValue = Convert.ToBoolean(MetQualityEnum.MET_GOOD_QUALITY);
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

        public enum MetQualityEnum
        {
            MET_GOOD_QUALITY=1,
            MET_BAD_QUALITY = 0
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
                //met.IceIndicationValue;
                try
                {
                    met.IceIndicationValue = 1.00;
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

        public Enum findMetTowerTag(string metTowerId, string tag)
        {
            MetTower tempMet = getMetTower(metTowerId);
            if (tag.ToUpper().Equals(tempMet.MetSwitchTag.ToUpper())) { return MetTowerEnum.Switched; }
            return null;

        }

        public static bool goodQuality(bool state) { return (state == Convert.ToBoolean(MetQualityEnum.MET_GOOD_QUALITY)); }

    }
}
