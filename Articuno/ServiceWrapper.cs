using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topshelf;
using System.IO;

namespace Articuno
{
    /// <summary>
    /// The entry point of Articuno. This is used so Articuno will start as a service. 
    /// To install as a service, you need to do 
    /// Articuno.exe install
    /// from the the command line
    /// 
    /// However, because the service isn't being used, there really isn't a point to having this class. However, this is still useful for/when PcVue becomes a service in the (unforseeable) future
    /// </summary>
    class ServiceWrapper
    {
        static void Main(string[] args)
        {
            HostFactory.Run(serviceConfig =>
            {
                //Call NLog
                //serviceConfig.UseNLog();

                //ConverterService is the class
                serviceConfig.Service<ArticunoService>(serviceInstance =>
                {
                    //Function that'll create an instnace of our class
                    serviceInstance.ConstructUsing(() => new ArticunoService());
                    serviceInstance.WhenStarted(execute => execute.Start());
                    serviceInstance.WhenStopped(execute => execute.Stop());
                });

                //Restart after one minute
                serviceConfig.EnableServiceRecovery(recoveryOption =>
                {
                    recoveryOption.RestartService(1);
                });

                serviceConfig.SetServiceName("ArticunoService");
                serviceConfig.SetDisplayName("Articuno");
                serviceConfig.SetDescription("The Avangrid Ice Control Protocol");

                serviceConfig.StartAutomatically();
            });
        }
    }
}
