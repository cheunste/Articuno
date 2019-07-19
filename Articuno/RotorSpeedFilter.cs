using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Articuno
{
    class RotorSpeedFilter
    {
        //Constants
        private static readonly string WindSpeed = "WindSpeed";
        private static readonly string NRS_RotorSpeed = "RotorSpeedNRS";
        private static readonly string NRS_StdDev = "StandardDeviationNRS";
        private static readonly string RotorSpeed = "RotorSpeedNonNRS";
        private static readonly string StdDev = "StandardDeviationNonNRS";
        private static readonly string cmd = String.Format("SELECT * FROM RotorSpeedLookupTable ORDER BY {0} ASC", WindSpeed);

        //private lists
        /*
         * What's going on here is that the wind speed is used to determine rotor speed and stddev. So what's happenging here is that
         * a list of WindSpeed is constructed which serves as a lookup list and once the closest wind speed is found, you then return another
         * list, the data row list, which contains the wind speed as well as the rotor speed, stddev, and their NRS version
         *
         * Note that the windSPeedList list returns a dataContent list. Change it to a better name when I find a better one
         * 
         */

        //List<List<FilterElement>> windSpeedList;
        List<FilterElement> windSpeedList;
        List<FilterElement> dataContent;
        List<double> keyList;

        //This is a private class that's only used in the RotorSpeedFIlter class. Mainly, creating a separate class seems...pointless
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

        public RotorSpeedFilter()
        {
            windSpeedList = new List<FilterElement>();
            keyList = new List<double>();

            DataTable reader = DatabaseInterface.Instance.readCommand(cmd);
            int rotorSpeedRows = reader.Rows.Count;
            for (int i = 0; i < rotorSpeedRows; i++)
            {
                windSpeedList.Add(
                new FilterElement(Convert.ToDouble(reader.Rows[i][WindSpeed]),
                Convert.ToDouble(reader.Rows[i][NRS_RotorSpeed]),
                Convert.ToDouble(reader.Rows[i][NRS_StdDev]),
                Convert.ToDouble(reader.Rows[i][RotorSpeed]),
                Convert.ToDouble(reader.Rows[i][StdDev])));
            }

            //Build a Key list from the above List
            keyList = windSpeedList.Select(y => y.WindSpeed).ToList();

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
            var result = windSpeedList[index];

            if (nrsMode)
                return new Tuple<double,double>(result.NrsRotorSpeed, result.NrsStdDev);
            else
                return new Tuple<double, double>(result.RotorSpeed,result.StdDev);
        }

    }
}
