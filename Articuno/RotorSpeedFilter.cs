using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Articuno
{
    /// <summary>
    /// This class handles the turbine rotor speed filter data from the database.
    /// Should only be used by the TurbineMediator Class.
    /// </summary>
    class RotorSpeedFilter
    {
        //Constants
        private static readonly string WindSpeed = "WindSpeed";
        private static readonly string NRS_RotorSpeed = "RotorSpeedNRS";
        private static readonly string NRS_StdDev = "StandardDeviationNRS";
        private static readonly string RotorSpeed = "RotorSpeedNonNRS";
        private static readonly string StdDev = "StandardDeviationNonNRS";
        private static readonly string TableName = "RotorSpeedLookupTable";
        private static readonly string cmd = String.Format("SELECT * FROM {0} ORDER BY {1} ASC", TableName,WindSpeed);

        //private lists
        /*
         * These lists contain the data (FilterList) and the other is a list of keys that's used to find the item in the filterList.
         * 
         * Yeah, now that I think about it, I might not really need another class, but the TurbineMediator class is already getting too busy
         * 
         */
        List<FilterElement> filterList;
        List<double> keyList;

        /*
         * This is a private class that's only used in the RotorSpeedFIlter class.
         * It just stores 
         * Mainly, creating a separate class seems...pointless
         */
        private class FilterElement
        {

            public FilterElement(double windSpeed, double nrsRotorSpeed, double nrsStdDev, double rotorSpeed, double stdDev)
            {
                WindSpeed = windSpeed;
                NrsRotorSpeed = nrsRotorSpeed;
                NrsStdDev = nrsStdDev;
                RotorSpeed = rotorSpeed;
                StdDev = stdDev;

            }
            public double WindSpeed { get; set; }
            public double NrsRotorSpeed { get; set; }
            public double NrsStdDev { get; set; }
            public double RotorSpeed { get; set; }
            public double StdDev { get; set; }
        }

        /// Constructor. Doesn't take any arguments as it can just get its data from the database
        public RotorSpeedFilter()
        {
            filterList = new List<FilterElement>();
            keyList = new List<double>();

            DataTable reader = DatabaseInterface.Instance.readCommand(cmd);
            int rotorSpeedRows = reader.Rows.Count;
            for (int i = 0; i < rotorSpeedRows; i++)
            {
                filterList.Add(
                new FilterElement(Convert.ToDouble(reader.Rows[i][WindSpeed]),
                Convert.ToDouble(reader.Rows[i][NRS_RotorSpeed]),
                Convert.ToDouble(reader.Rows[i][NRS_StdDev]),
                Convert.ToDouble(reader.Rows[i][RotorSpeed]),
                Convert.ToDouble(reader.Rows[i][StdDev])));
            }

            //Build a Key list from the above List
            keyList = filterList.Select(y => y.WindSpeed).ToList();

        }

        /// <summary>
        /// Find the rotor speed in the lookup table given a wind speed and whether it is in NRS state or not
        /// </summary>
        /// <param name="windSpeed"></param>
        /// <returns></returns>
        public Tuple<double,double> search(double windSpeed, bool nrsMode)
        {
            var index = keyList.BinarySearch(windSpeed);
            if (index < 0)
            {
                index = ~index;
            }
            var result = filterList[index];

            if (nrsMode)
                return new Tuple<double,double>(result.NrsRotorSpeed, result.NrsStdDev);
            else
                return new Tuple<double, double>(result.RotorSpeed,result.StdDev);
        }

    }
}
