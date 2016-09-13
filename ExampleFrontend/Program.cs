using ScrobbleStatsMechanizerCommon;
using Newtonsoft.Json;
using PMPAudioSelector;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScrobbleStatsMechanizer.ExampleFrontend
{
    public class Program
    {

        static void Main(string[] args)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            ExampleFrontend exampleFrontend = new ExampleFrontend();
            try
            {
                exampleFrontend.LoadSettings();

                var mode = exampleFrontend.DetermineMode(args);

                exampleFrontend.RunMode(mode);

                stopwatch.Stop();
                exampleFrontend.PrintMessage(String.Format("Finished in {0} seconds", stopwatch.Elapsed.TotalSeconds));
                Console.ReadLine();
            }
            catch (Exception e)
            {
                exampleFrontend.PrintError(e.ToString());
                Console.ReadLine();
            }
        }



    }
}
