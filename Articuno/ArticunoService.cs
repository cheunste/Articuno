using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Topshelf.Logging;
using System;

namespace Articuno
{
    /// <summary>
    /// This class handles service states for Articuno and is used with the ServiceWrapper Class.
    /// This handles service states such as start, pause, etc.
    /// Unfortunately, because PcVue is not a service, this is technically, not used
    /// </summary>
    class ArticunoService
    {
        private FileSystemWatcher watcher;

        private static readonly LogWriter log = HostLogger.Get<ArticunoService>();

        public bool Start()
        {
            //watcher.Created += Articuno.Main;
            Articuno.Main(null,null);
            return true;
        }

        public bool Pause()
        {
            return true;
        }

        public bool Continue()
        {
            return true;
        }

        public bool Stop()
        {
            return true;
        }

        public bool Restart()
        {
            Stop();
            Start();
            return true;
        }

    }
}
