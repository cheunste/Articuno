using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using OpcLabs.EasyOpc.DataAccess;

namespace Articuno
{
    /// <summary>
    /// The OpcServer class sets up an interface to the OpcServer (PcVue). This probalby should be a singleton as there should only be one instance of this
    /// </summary>
    internal class OpcServer
    {
        //member variables
        EasyDAClient client = new EasyDAClient();
        private string serverName;

        //log
        private static readonly ILog log = LogManager.GetLogger(typeof(MetTower));

        //Constructor. Tages in a server name and sets the 
        public OpcServer(String serverName)
        {
            this.serverName = serverName;
        }
        /// <summary>
        /// The readTags function reads an OPC tag value given an OPC Tag. This always returns a String.
        /// Mainly because I don't trust what the server is returning. 
        /// </summary>
        /// <param name="tags"></param>
        /// <returns>A string value if successful. A Null if read failed</returns>
        public string readTagValue(string tag)
        {
            try
            {
                var value = client.ReadItemValue("", serverName, tag);
                return value.ToString();
            }
            catch(Exception e)
            {
                //Log Exception here
                log.ErrorFormat("Reading tag: {0} failed. Does {0} exist on the server?", tag);
                return null;
            }
        }

        /// <summary>
        /// Sets a value given  an OPC tagname and a value
        /// </summary>
        /// <param name="tag">the name of hte OPC tag</param>
        /// <param name="value">the value to set to</param>
        /// <returns>true if write was successful. False otherwise</returns>
        //This method is used to set the value of an OPC tag to some value. mainly used for the sensors in the met tower so it doesn't go out of range 
        public bool writeTagValue(string tag,Object value)
        {
            try
            {
                client.WriteItemValue(serverName, tag, value);
                return true;
            }
            catch (Exception e)
            {
                //Log exception here
                log.ErrorFormat("Write to tag: {0} failed. Does {0} exist on the server?", tag);
                return false;
            }
        }

        /// <summary>
        /// Reads several OPC tag given a string array of tagnames.
        /// </summary>
        /// <param name="tags"></param>
        /// <returns>An array  string format</returns>
        //This method reads in an array of tagnames (in String) and returns an array of string values (relative to the tagname's position)
        //It returns a string array as depending on the OPC tag, it could return a boolean, a text, or a double. Mind as well cast them all to a string
        //to prevent parameter guessing 
        public List<String> readTagsValues(string[] tagNames)
        {
            List<String> tagValueList = new List<string>();

            foreach(string tag in tagNames)
            {
                tagValueList.Add(readTagValue(tag).ToString());
            }
            return tagValueList;
        }
    }
}
