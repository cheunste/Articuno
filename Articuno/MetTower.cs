using log4net;
using System;
using System.Collections.Generic;
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

    internal class MetTower
    {
        //Constants. Can't Fully Capitalize these ones. Too hard to read. Note the staritng capital letter though
        //Temperature table columns
        private static string PrimTempValueTag = "PrimTempValueTag";
        private static string SecTempValueTag = "SecTempValueTag";
        private static string TempBadQualityTag = "TempBadQualityTag";
        private static string TempOutOfRangeTag = "TempOutOfRangeTag";

        //Humidity table columns
        private static string PrimHumidityValueTag = "PrimHumidityValueTag";
        private static string SecHumidityValueTag = "SecHumidityValueTag";
        private static string HumidityBadQualityTag = "HumidityBadQualityTag";
        private static string HumidityOutOfRangeTag = "HumidityOutOfRangeTag";

        //Other table column 
        private static string NoDataAlarmTag = "NoDataAlarmTag";
        private static string IceIndicationTag = "IceIndicationTag";

        //Query constants
        String TEMPERATURE_QUERY = "SELECT " + PrimTempValueTag + "," + SecTempValueTag + "," + TempBadQualityTag + "," + TempOutOfRangeTag + " FROM MetTower";
        String RH_QUERY = "SELECT " + PrimHumidityValueTag + "," + SecHumidityValueTag + "," + HumidityBadQualityTag + "," + HumidityOutOfRangeTag + " FROM MetTower";
        String OTHER_PARAM_QUERY = "SELECT " + NoDataAlarmTag + ", " + IceIndicationTag + " FROM MetTower";

        //Member variables;
        //Member doubles 
        private double ambientTemperature;
        private double relativeHumidity;
        private double dewPointTemperature;
        private double ambTempThreshold;
        private double deltaTempThreshold;

        //Member bool
        private bool metTowerFailure;

        //Member variable OPC Tags (strings)
        //For temperature
        private string primTempTag;
        private string secTempTag;
        private string tempBadQualityTag;
        private string tempOutOfRangeTag;

        //For humidity
        private string primRHTag;
        private string secRHTag;
        private string rhBadQualityTag;
        private string rhOutOfRangeTag;

        //For other tags relating to the met tower 
        private string noDataAlarmTag;
        private string iceIndicationTag;

        //opc server
        private OpcServer opcServer;

        //log
        private static readonly ILog log = LogManager.GetLogger(typeof(MetTower));

        /// <summary>
        /// Constructor for the Met Tower Class. Takes in a Met Id. Using  the met Id, the constructor will then query
        /// the DB for all the relevant values
        /// </summary>
        /// <param name="MetId"></param>
        public MetTower(string MetId, double ambTempThreshold, double deltaTempThreshold, OpcServer opcServerInstance)
        {
            //Open a connection to the DB
            DatabaseInterface dbi = new DatabaseInterface();
            dbi.openConnection();
            //Get everything relating to the Temperaturea
            SQLiteDataReader reader = dbi.readCommand(TEMPERATURE_QUERY + " WHERE MetId=" + MetId);
            reader.Read();
            this.primTempTag = reader[PrimTempValueTag].ToString();
            this.secTempTag = reader[SecTempValueTag].ToString();
            this.tempBadQualityTag = reader[TempBadQualityTag].ToString();
            this.tempOutOfRangeTag = reader[TempOutOfRangeTag].ToString();

            //Get everything relating to the Humidity
            reader = dbi.readCommand(RH_QUERY + " WHERE MetId=" + MetId);
            reader.Read();
            this.primRHTag = reader[PrimHumidityValueTag].ToString();
            this.secRHTag = reader[SecHumidityValueTag].ToString();
            this.rhBadQualityTag = reader[HumidityBadQualityTag].ToString();
            this.rhOutOfRangeTag = reader[HumidityOutOfRangeTag].ToString();

            //Get the other tags relating to the met tower
            reader = dbi.readCommand(OTHER_PARAM_QUERY + " WHERE MetId=" + MetId);
            this.noDataAlarmTag = reader[NoDataAlarmTag].ToString();
            this.iceIndicationTag = reader[IceIndicationTag].ToString();
            dbi.closeConnection();

            //set the member thresholds
            this.ambTempThreshold = ambTempThreshold;
            this.deltaTempThreshold = deltaTempThreshold;

            //Set OPC Server
            opcServer = opcServerInstance;

        }

        /// <summary>
        /// Get Relative Humidity from OPC Server
        /// </summary>
        /// <returns></returns>
        public string getRelativeHumidityValue() { return opcServer.readTagValue(primRHTag).ToString(); }

        /// <summary>
        /// Set the field of relative humidty 
        /// </summary>
        /// <param name="value"></param>
        public void setRelativeHumityValue(double value) { opcServer.setTagValue(getRelativeHumidityTag(), value); }

        /// <summary>
        /// Get the prim relative humidity tag
        /// </summary>
        /// <returns></returns>
        public string getRelativeHumidityTag() { return this.primRHTag; }

        /// <summary>
        /// Set the Relativie Humidity Tag. Used only internally to this program
        /// </summary>
        public void setRelativeHumidityTag(string tag) { this.primRHTag = tag; }

        /// <summary>
        /// Get the Primary Temperature value from the met tower
        /// </summary>
        /// <returns></returns>
        public string getPrimTemperatureValue() { return opcServer.readTagValue(primTempTag).ToString(); }
        /// <summary>
        /// Sets the primary temperature field. Used for this program only
        /// </summary>
        public void setPrimTemperatureValue(double value) { opcServer.setTagValue(getPrimTemperatureTag(),value); }

        /// <summary>
        /// Returns the OpcTag for the primary temperature tag
        /// </summary>
        /// <returns></returns>
        public string getPrimTemperatureTag() { return this.primTempTag; }

        /// <summary>
        /// Set the primary temperature tag.
        /// </summary>
        public void setPrimTemperatureTag(string tag) { this.primTempTag = tag; }

        /// <summary>
        /// Get the SEcondary Temperature value from the met tower
        /// </summary>
        /// <returns></returns>
        public string getSecTemperatureValue() { return opcServer.readTagValue(secTempTag).ToString(); }

        /// <summary>
        /// Sets the primary temperature field. Used for this program only
        /// </summary>
        public void setSecTemperatureValue(double value) { opcServer.setTagValue(getSecTemperatureTag(), value); }

        /// <summary>
        /// Returns the OpcTag for the primary temperature tag
        /// </summary>
        /// <returns></returns>
        public string getSecTemperatureTag() { return this.secTempTag; }

        /// <summary>
        /// Set the secondary temperature tag.
        /// </summary>
        public void setSecTemperatureTag(string tag) { this.secTempTag = tag; }

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
            double rh = Convert.ToDouble(getRelativeHumidityValue());
            double minValue = 0.0;
            double maxValue = 100.0;

            //Bad Quality
            if(rh < 0.0 || rh > 100.0)
            {
                //Cap it off and throw an alarm
                //Set primay relative humidty to either 0 (if below 0) or 100 (if above zero)
                opcServer.setTagValue(getPrimTemperatureTag(), ((rh < 0.0) ? 0.0 : 100.0) );
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
            if  (tempValue < minValue || tempValue > maxValue)
            {
                //Cap it off and throw an alarm
                //Set primay relative humidty to either 0 (if below 0) or 100 (if above zero)
                opcServer.setTagValue(temperatureTag, ((tempValue < minValue) ? -20.0 : 60.0));

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

            bool primTempCheck = tempQualityCheck(getPrimTemperatureTag());
            bool secTempCheck = tempQualityCheck(getSecTemperatureTag());
            //normal operaiton
            if(primTempCheck && secTempCheck){
                return true;
            }
            //only the secondary Temperature value is suspect
            else if(primTempCheck && !secTempCheck)
            {

            }
            //only the primary Temperature value is suspect
            else if(!primTempCheck && secTempCheck)
            {

            }
            //If both sensors are bad.  Use turbine data
            else
            {
                return false;
            }
            throw new NotImplementedException();
        }

        //Getters for System OPC Tag indicators
        /// <summary>
        /// Gets the  NoDataAlaarmValue OPC value
        /// </summary>
        /// <returns></returns>
        public string getNoDataAlarmValue() { return noDataAlarmTag;}
        /// <summary>
        /// Gets the IceIndicationValue OPC Value
        /// </summary>
        /// <returns></returns>
        public string getIceIndicationValue() { return iceIndicationTag;}

        //Threshold setters and getters
        public double AmbTempThreshold { get; set; }
        public double DeltaTempThreshold { get; set; }

    }
}
