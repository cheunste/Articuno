﻿using log4net;
using OpcLabs.EasyOpc.DataAccess;
using OpcLabs.EasyOpc.DataAccess.Generic;
using OpcLabs.EasyOpc.DataAccess.OperationModel;
using System;
using System.Collections.Generic;
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
        private List<MetTower> metTowerList = new List<MetTower>();
        private EasyDAClient client = new EasyDAClient();

        //constant doubles for quality
        private double BAD_QUALITY = 1.00;
        private double GOOD_QUALITY = 1.00;

        //Log
        private static readonly ILog log = LogManager.GetLogger(typeof(TurbineFactory));

        public MetTowerMediator()
        {
        }

        /// <summary>
        /// Returns the number of met towers in a project
        /// </summary>
        /// <returns></returns>
        public static int getNumMetTower()
        {
            if (numMetTower == 0)
            {
                DatabaseInterface dbi = new DatabaseInterface();
                SQLiteDataReader reader = dbi.readCommand("SELECT Count(*) FROM MetTowerInputTags");
                reader.Read();
                numMetTower = Convert.ToInt16(reader.Read());
            }
            return numMetTower;
        }

        private void createMetTower()
        {
            DatabaseInterface dbi = new DatabaseInterface();
            dbi.openConnection();
            SQLiteDataReader reader = dbi.readCommand(THRESHOLD_QUERY);
            reader.Read();
            DAVtqResult[] vtqResults = client.ReadMultipleItems("",
                new DAItemDescriptor[]{
                    reader[0].ToString(),
                    reader[1].ToString()
                });

            double ambTempThreshold = Convert.ToDouble(vtqResults[0].Vtq);
            double deltaThreshold = Convert.ToDouble(vtqResults[1].Vtq);

            reader = dbi.readCommand(SERVER_NAME_QUERY);
            reader.Read();
            this.opcServerName = reader["Description"].ToString();

            dbi.closeConnection();

            for (int i = 1; i <= getNumMetTower(); i++)
            {
                MetTower metTower = new MetTower(i.ToString(),
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
        /// <returns>A Met Tower Object</returns>
        public MetTower getMetTower(string metTowerId)
        {
            if (metTowerList.Count == 0)
            {
                createMetTower();
            }

            for (int i = 0; i <= metTowerList.Count; i++)
            {
                if (metTowerList.ElementAt(i).getMetTowerPrefix.Equals(metTowerId))
                {
                    return metTowerList.ElementAt(i);
                }
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
            double ambTemp = getTemperature(metId);
            double rh = getHumidity(metId);
            double dew = calculateDewPoint(rh, ambTemp);
            double delta = calculateDelta(ambTemp, dew);
            return new Tuple<double, double, double, double>(ambTemp, rh, dew, delta);
        }

        /// <summary>
        /// This function switches the met tower to use the backup met tower.
        /// For example, if Met1 is passed in, then it will use Met2 and vice versa.
        /// </summary>
        /// <returns>A true for success or false for failed </returns>
        public bool switchMetTower(string metId)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///  Gets the one minute average temperature of a met tower primary or secondary sensor
        /// </summary>
        /// <param name="metId"></param>
        /// <returns>A double if the quality is good for either the primary or secondary sensor. Null otherwise. This MUST be handled by the supporting class</returns>
        public Object getTemperature(string metId)
        {
            MetTower met = getMetTower(metId);
            var tuple = tempQualityCheck(met, met.getPrimTemperatureTag(), met.getSecTemperatureTag());
            if (tuple.Item1)
            {
                return tuple.Item2;
            }
            else
            {
                return null;
            }
        }

        public double getHumidity(string metId)
        {
            MetTower met = getMetTower(metId);
            var rhQuality = rhQualityCheck((Double)met.readRelativeHumidityValue());

            //If the quality for the  humidty value is bad then an alarm should trigger
            //The value is already capped off at 0.00% or 100%
            if (!rhQuality.Item1)
            {
                //Raise RH out of range alarm and log it
                raiseAlarm(met, MetTowerEnum.HumidityOutOfRange);
                log.DebugFormat("Relative Humidity alarm raised for {0}", metId);
            }
            else
            {
                clearAlarm(met, MetTowerEnum.HumidityOutOfRange);
                log.DebugFormat("Relative Humidity alarm cleared for {0}", metId);
            }
            return rhQuality.Item2;
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
        public double calculateDelta(double ambTemp, double dewPointTemp)
        {
            return Math.Abs(ambTemp - dewPointTemp);
        }

        /// <summary>
        /// Check the quality of the met tower
        /// </summary>
        /// <param name="ambTemp"></param>
        /// <param name="rh"></param>
        /// <returns></returns>
        public bool checkMetTower(double ambTemp, double rh)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Checks the quality of the relative humidity of the current met tower. 
        /// Returns true if quality is good. False otherwise
        /// </summary>
        /// <returns>Returns True if good quality, False if bad</returns>
        public Tuple<bool, double> rhQualityCheck(double rh)
        {
            double minValue = 0.0;
            double maxValue = 100.0;

            //Bad Quality
            if (rh < 0.0 || rh > 100.0)
            {
                //Cap it off and throw an alarm
                //Set primay relative humidty to either 0 (if below 0) or 100 (if above zero)
                return new Tuple<bool, double>(false, ((rh < 0.0) ? 0.0 : 100.0));
            }
            //Normal operation 
            else { return new Tuple<bool, double>(true, rh); }
        }

        /// <summary>
        /// Checks the quality of the temperature of the current met tower. 
        /// Returns true if quality is good. False otherwise
        /// </summary>
        /// <param name="temperatureTag">The OPC tag for temperature (either prim or sec)</param>
        /// <returns>Returns True if good quality, False if bad</returns>
        private Tuple<bool, double> tempValueQualityCheck(string temperatureTag)
        {
            double tempValue = Convert.ToDouble(temperatureTag);
            double minValue = -20.0;
            double maxValue = 60.0;
            //Bad Quality
            if (tempValue < minValue || tempValue > maxValue)
            {
                //Cap it off and throw an alarm
                //Set primay temperature sensor to either 0 (if below 0) or 100 (if above zero)
                return new Tuple<bool, double>(false, ((tempValue < minValue) ? minValue : maxValue));
            }
            //Normal oepration
            else
            {
                return new Tuple<bool, double>(true, tempValue);
            }
        }

        /// <summary>
        /// Check the temperature quality of both the primary and secondary sensors
        /// </summary>
        /// <returns>Returns True if good quality, False if bad</returns>
        private Tuple<bool, double, double> tempQualityCheck(MetTower met, string primTempTag, string secTempTag)
        {
            //If both cases are true, then both sensors are working correctly

            //call the ValueQuatliyCheck method to verify
            var primTempCheck = tempValueQualityCheck(primTempTag);
            var secTempCheck = tempValueQualityCheck(secTempTag);

            //normal operaiton
            if ((primTempCheck.Item1) && (secTempCheck.Item1))
            {
                clearAlarm(met, MetTowerEnum.PrimSensorQuality);
                clearAlarm(met, MetTowerEnum.SecSensorQuality);
                return new Tuple<bool, double, double>(true, primTempCheck.Item2, secTempCheck.Item2);
            }
            //only the secondary Temperature value is suspect. Raise temperature out of range alarm for Temp sensor 1  
            else if (primTempCheck.Item1 && !secTempCheck.Item1)
            {
                raiseAlarm(met, MetTowerEnum.PrimSensorQuality);
                return new Tuple<bool, double, double>(true, primTempCheck.Item2, secTempCheck.Item2);
            }
            //only the primary Temperature value is suspect Raise temperature out of range alarm for Temp sensor 2  
            else if (!primTempCheck.Item1 && secTempCheck.Item1)
            {
                raiseAlarm(met, MetTowerEnum.SecSensorQuality);
                return new Tuple<bool, double, double>(true, primTempCheck.Item2, secTempCheck.Item2);
            }
            //If both sensors are bad.  Use turbine data. Raise alarm
            else
            {
                raiseAlarm(met, MetTowerEnum.PrimSensorQuality);
                raiseAlarm(met, MetTowerEnum.SecSensorQuality);
                return new Tuple<bool, double, double>(false, primTempCheck.Item2, secTempCheck.Item2);
            }
        }

        private void raiseAlarm(MetTower mt, MetTowerEnum metTowerEnum)
        {
            switch (metTowerEnum)
            {
                case MetTowerEnum.HumidityOutOfRange:
                    mt.writeHumidityOutOfRng(BAD_QUALITY);
                    break;
                case MetTowerEnum.HumidityQuality:
                    mt.writeHumidityBadQuality(BAD_QUALITY);
                    break;
                case MetTowerEnum.PrimSensorQuality:
                    mt.writeTemperaturePrimBadQuality(BAD_QUALITY);
                    break;
                case MetTowerEnum.PrimSensorOutOfRange:
                    mt.writeTemperaturePrimOutOfRange(BAD_QUALITY);
                    break;
                case MetTowerEnum.SecSensorQuality:
                    mt.writeTemperaturePrimBadQuality(BAD_QUALITY);
                    break;
                case MetTowerEnum.SecSensorOutOfRange:
                    mt.writeTemperatureSecOutOfRange(BAD_QUALITY);
                    break;
            }
        }

        private void clearAlarm(MetTower mt, MetTowerEnum metTowerEnum)
        {
            switch (metTowerEnum)
            {
                case MetTowerEnum.HumidityOutOfRange:
                    mt.writeHumidityOutOfRng(GOOD_QUALITY);
                    break;
                case MetTowerEnum.HumidityQuality:
                    mt.writeHumidityBadQuality(GOOD_QUALITY);
                    break;
                case MetTowerEnum.PrimSensorQuality:
                    mt.writeTemperaturePrimBadQuality(GOOD_QUALITY);
                    break;
                case MetTowerEnum.PrimSensorOutOfRange:
                    mt.writeTemperaturePrimOutOfRange(GOOD_QUALITY);
                    break;
                case MetTowerEnum.SecSensorQuality:
                    mt.writeTemperaturePrimBadQuality(GOOD_QUALITY);
                    break;
                case MetTowerEnum.SecSensorOutOfRange:
                    mt.writeTemperatureSecOutOfRange(GOOD_QUALITY);
                    break;
            }
        }


        private enum MetTowerEnum
        {
            HumidityQuality,
            HumidityOutOfRange,
            PrimSensorQuality,
            PrimSensorOutOfRange,
            SecSensorQuality,
            SecSensorOutOfRange
        }

    }
}
