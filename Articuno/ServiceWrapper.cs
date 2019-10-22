using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topshelf;
using System.IO;

namespace Articuno
{
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
