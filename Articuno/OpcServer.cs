using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Articuno
{
    internal class OpcServer
    {
        public OpcServer(String serverName)
        {

        }
        //TODO: Implement
        /// <summary>
        /// The readTags function reads an OPC tag value given an OPC Tag. This always returns a String.
        /// Mainly because I don't trust what the server is returning. 
        /// </summary>
        /// <param name="tags"></param>
        /// <returns></returns>
        public string readTag(string tag)
        {
            throw new NotImplementedException();
        }

        public void setTag(string tag,Object value)
        {
            throw new NotImplementedException();
        }
    }
}
