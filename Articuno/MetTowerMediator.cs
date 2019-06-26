using log4net;
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
        private static readonly ILog log = LogManager.GetLogger(typeof(TurbineFactory));

        private MetTowerMediator()
        {
            met1Switched = false;
            met2Switched = false;
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
                DatabaseInterface dbi = new DatabaseInterface();
                dbi.openConnection();
                SQLiteDataReader reader = dbi.readCommand("SELECT Count(*) as num FROM MetTowerInputTags");
                reader.Read();
                numMetTower = Convert.ToInt16(reader["num"]);
                dbi.closeConnection();
            }
            return numMetTower;
        }

        public void createMetTower()
        {
            DatabaseInterface dbi = new DatabaseInterface();
            dbi.openConnection();
            SQLiteDataReader reader = dbi.readCommand(THRESHOLD_QUERY);
            reader.Read();
            string temp1 = reader["OpcTag"].ToString();
            reader.Read();
            string temp2 = reader["OpcTag"].ToString();

            reader = dbi.readCommand(SERVER_NAME_QUERY);
            reader.Read();
            opcServerName = reader["OpcTag"].ToString();

            DAVtqResult[] vtqResults = client.ReadMultipleItems(opcServerName,
                new DAItemDescriptor[]{
                    temp1,
                    temp2
                });

            ambTempThreshold = Convert.ToDouble(vtqResults[0].Vtq.Value);
            deltaThreshold = Convert.ToDouble(vtqResults[1].Vtq.Value);


            dbi.closeConnection();

            for (int i = 1; i <= getNumMetTower(); i++)
            {
                MetTower metTower = new MetTower("Met"+i.ToString(),
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
            //if (metTowerList.Count == 0)
            //{
            //    createMetTower();
            //}
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
            metId = isMetTowerSwitched(metId);
            var temperature = getTemperature(metId);
            double ambTemp;
            //Check if the value returned from getTemperature is null or not
            if (temperature != null)
            {
                ambTemp = (Double)temperature;
            }
            //If null, then that means you need to get the temperature from the turbines. 
            else
            {
                ambTemp = (double)getMetTower(metId).getNearestTurbine().readTurbineTemperatureValue();
            }
            double rh = getHumidity(metId);
            double dew = calculateDewPoint(rh, ambTemp);
            double delta = calculateDelta(ambTemp, dew);
            return new Tuple<double, double, double, double>(ambTemp, rh, dew, delta);
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
        /// <returns>A double if the quality is good for either the primary or secondary sensor. Null otherwise. This MUST be handled by the supporting class</returns>
        public Object getTemperature(string metId)
        {
            metId = isMetTowerSwitched(metId);
            MetTower met = getMetTower(metId);
            var tuple = tempQualityCheck(met, met.getPrimTemperatureTag(), met.getSecTemperatureTag());
            if (tuple.Item1) { return tuple.Item2; }
            else { return null; }

        }

        public double getHumidity(string metId)
        {
            metId = isMetTowerSwitched(metId);
            MetTower met = getMetTower(metId);
            var rhQuality = rhQualityCheck(met);

            //If the quality for the  humidty value is bad then an alarm should trigger
            //The value is already capped off at 0.00% or 100%
            if (!rhQuality.Item1) { raiseAlarm(met, MetTowerEnum.HumidityOutOfRange); }
            else { clearAlarm(met, MetTowerEnum.HumidityOutOfRange); }
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
        public double calculateDelta(double ambTemp, double dewPointTemp) { return Math.Abs(ambTemp - dewPointTemp); }

        /// <summary>
        /// Check the quality of the met tower
        /// </summary>
        /// <param name="ambTemp"></param>
        /// <param name="rh"></param>
        /// <returns></returns>
        public bool checkMetTower(double ambTemp, double rh)
        {
            //Todo: Implement
            throw new NotImplementedException();
        }

        /// <summary>
        /// Checks the quality of the relative humidity of the current met tower. 
        /// Returns true if quality is good. False otherwise
        /// </summary>
        /// <returns>Returns True if good quality, False if bad</returns>
        public Tuple<bool, double> rhQualityCheck(MetTower met)
        {

            var rhOpcObject = met.readRelativeHumidityValue();
            double rh = (Double)rhOpcObject;
            double minValue = 0.0;
            double maxValue = 100.0;

            bool state = true;


            //Bad Quality
            //Set primay relative humidty to either 0 (if below 0) or 100 (if above zero)
            if (rh < 0.0 || rh > 100.0)
            {
                state = false;
                rh = ((Math.Abs(0.0 - rh) > 0.0001) ? 0.0 : 100.0);
            }
            if (!(met.isQualityGood(met.getRelativeHumidityTag())))
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
            var temp = client.ReadItemValue("",opcServerName,temperatureTag);
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
        private Tuple<bool, double, double> tempQualityCheck(MetTower met, string primTempTag, string secTempTag)
        {

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
                    if (Convert.ToBoolean(mt.readTemperatureSecBadQuality())!= GOOD_QUALITY)
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
                if (metTowerList.ElementAt(i).getMetTowerPrefix.Equals(metId))
                {
                    metTowerList.ElementAt(i).setNearestTurbine(turbine);
                }
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
