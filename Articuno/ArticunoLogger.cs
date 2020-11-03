using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Articuno {
    internal static class ArticunoLogger {
        internal static readonly NLog.Logger GeneralLogger = NLog.LogManager.GetLogger("General");
        internal static readonly NLog.Logger CurtailmentLogger = NLog.LogManager.GetLogger("Curtailment");
        internal static readonly NLog.Logger DataLogger = NLog.LogManager.GetLogger("Data");
    }
}
