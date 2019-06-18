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

        public MetTowerMediator()
        {
        }

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
        /// This function switches the met tower to use the backup met tower.
        /// For example, if Met1 is passed in, then it will use Met2 and vice versa.
        /// </summary>
        /// <returns>A true for success or false for failed </returns>
        public bool switchMetTower()
        {
            throw new NotImplementedException();
        }

        public double getTemperature()
        {
            tempQualityCheck();
            throw new NotImplementedException();
        }

        public double getHumidity()
        {
            throw new NotImplementedException();
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
        public bool rhQualityCheck()
        {
            //double rh = Convert.ToDouble(readRelativeHumidityValue());
            double rh = 0.00;
            double minValue = 0.0;
            double maxValue = 100.0;

            //Bad Quality
            if (rh < 0.0 || rh > 100.0)
            {
                //Cap it off and throw an alarm
                //Set primay relative humidty to either 0 (if below 0) or 100 (if above zero)

                //opcServer.writeTagValue(getPrimTemperatureTag(), ((rh < 0.0) ? 0.0 : 100.0));
                throw new NotImplementedException();
                return false;
            }
            //Normal operation 
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Checks the quality of the temperature of the current met tower. 
        /// Returns true if quality is good. False otherwise
        /// </summary>
        /// <param name="temperatureTag">The OPC tag for temperature (either prim or sec)</param>
        /// <returns>Returns True if good quality, False if bad</returns>
        private bool tempQualityCheck(string temperatureTag)
        {
            double tempValue = Convert.ToDouble(temperatureTag);
            double minValue = -20.0;
            double maxValue = 60.0;
            //Bad Quality
            if (tempValue < minValue || tempValue > maxValue)
            {
                //Cap it off and throw an alarm
                //Set primay relative humidty to either 0 (if below 0) or 100 (if above zero)
                //opcServer.writeTagValue(temperatureTag, ((tempValue < minValue) ? -20.0 : 60.0));

                throw new NotImplementedException();
                return false;
            }
            else
            {
                return true;
            }
            throw new NotImplementedException();
        }

        /// <summary>
        /// Check the temperature quality of both the primary and secondary sensors
        /// </summary>
        /// <returns>Returns True if good quality, False if bad</returns>
        public bool tempQualityCheck()
        {
            //If both cases are true, then both sensors are working correctly

            //bool primTempCheck = tempQualityCheck(getPrimTemperatureTag());
            //bool secTempCheck = tempQualityCheck(getSecTemperatureTag());
            bool primTempCheck = false;
            bool secTempCheck = false;

            //normal operaiton
            if (primTempCheck && secTempCheck)
            {
                return true;
            }
            //only the secondary Temperature value is suspect
            else if (primTempCheck && !secTempCheck)
            {

            }
            //only the primary Temperature value is suspect
            else if (!primTempCheck && secTempCheck)
            {

            }
            //If both sensors are bad.  Use turbine data
            else
            {
                return false;
            }
            throw new NotImplementedException();
        }

    }
}
