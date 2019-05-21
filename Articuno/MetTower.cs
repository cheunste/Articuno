using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Articuno
{
    /// <summary>
    /// The Met Tower class is responsible for fetching humidity and temperature from the field's met tower and perform calculations when valid
    /// Fetches: humidty and temperature values from the field
    /// Calculates: Dew Point, Delta Temp threshold
    /// Errors: Thrwos errors if values are bad 
    /// </summary>
    class MetTower
    {
        //Constants
        String TEMPERATURE_QUERY = "SELECT PrimTempValueTag, SecTempValueTag, TempBadQuality, TempOutOfRangeTag FROM MetTower";
        String RH_QUERY = "SELECT PrimHumidityValueTag, SecHumidityValueTag, HumidityBadQuality, HumidityOutOfRangeTag FROM MetTower";
        String OTHER_PARAM_QUERY = "SELECT NoDataAlarmTag, IceIndicationTag FROM MetTower";

        //Member variables;
        //Member doubles 
        private double ambientTemperature;
        private double relativeHumidity;
        private double dewPointTemperature;
        private double ambTempThreshold;
        private double deltaTempThreshold;

        //Member bool
        private bool metTowerFailure;

        //member variable OPC Tags (strings)
        private string primTempTag;
        private string secTempTag;
        private string primRHTag;
        
        /// <summary>
        /// Constructor for the Met Tower Class. Takes in a Met Id. Using  the met Id, the constructor will then query
        /// the DB for all the relevant values
        /// </summary>
        /// <param name="MetId"></param>
        public MetTower(string MetId, double ambTempThreshold, double deltaTempThreshold)
        {
            DatabaseInterface dbi = new DatabaseInterface();
            //Get everything relating to the Temperaturea
            dbi.readCommand(TEMPERATURE_QUERY +" WHERE MetId="+MetId);

            //Get everything relating to the Humidity
            dbi.readCommand(RH_QUERY +" WHERE MetId="+MetId);

            //Get the other tags relating to the met tower
            dbi.readCommand(OTHER_PARAM_QUERY +" WHERE MetId="+MetId);

            //set the member thresholds
            this.ambTempThreshold = ambTempThreshold;
            this.deltaTempThreshold = deltaTempThreshold;

        }

        /// <summary>
        /// Get Relative Humidity from OPC Server
        /// </summary>
        /// <param name="opcServer">the OpcServer you'll be looking the value from</param>
        /// <returns></returns>
        public string getRelativeHumidity(OpcServer opcServer)
        {
            return "";
        }
        /// <summary>
        /// Set the Relativie Humidity Tag
        /// </summary>
        public void setRelativeHumidityTag(string tag)
        {
            this.primRHTag = tag;
        }


        /// <summary>
        /// Get the Primary Temperature value from the met tower
        /// </summary>
        /// <param name="opcServer">the OpcServer you'll be looking the value from</param>
        /// <returns></returns>
        public string getPrimTemperature(OpcServer opcServer)
        {
            return "";
        }
        /// <summary>
        /// Set the primary temperature tag.
        /// </summary>
        public void setPrimTemperature(string tag)
        {
            this.primTempTag = tag;
        }

        /// <summary>
        /// Get the SEcondary Temperature value from the met tower
        /// </summary>
        /// <param name="opcServer">the OpcServer you'll be looking the value from</param>
        /// <returns></returns>
        public string getSecTemperature(OpcServer opcServer)
        {
            return "";
        }
        /// <summary>
        /// Set the secondary temperature tag.
        /// </summary>
        public void setSecTemperature(string tag)
        {
            this.secTempTag = tag;

        }

        /// <summary>
        /// Calculates the Dew Point Temperature given the ambient temperature and the the relative humidity
        /// </summary>
        /// <param name="ambTemp">The ambient temperature value (Celcius) from the met tower in double format</param>
        /// <param name="rh">The relative humidity in double format</param>
        /// <returns></returns>
        public double calculateDewPoint(double rh, double ambTemp)
        {

            return 0.0;
        }

        /// <summary>
        /// Calculates the Delta Temperature given the ambient Temperature and a dew point temperature (from calculateDewPoitn function)
        /// </summary>
        /// <param name="ambTemp">The ambient temperature value (Celcius) from the met tower in double format</param>
        /// <param name="dewPointTemp">The dew point temperature from calculateDewPoint</param>
        /// <returns></returns>
        public double calculateDelta(double ambTemp,double dewPointTemp)
        {
            return 0.0;
        }
    }
}
