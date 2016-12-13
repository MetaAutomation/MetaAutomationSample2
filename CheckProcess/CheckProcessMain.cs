////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//
//  MetaAutomation (C) 2016 by Matt Griscom.
//
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace CheckProcess
{
    using MetaAutomationClientSt;
    using System;
    using System.Diagnostics;
    using System.Threading;

    class CheckProcessMain
    {
        static void Main(string[] args)
        {
            // DEBUG Uncomment the next four lines and do a complete rebuild, just for debugging...    
            //while (!Debugger.IsAttached)
            //{
            //    Thread.Sleep(200);
            //}

            try
            {
                CheckRunner checkRunner = new CheckRunner();
                checkRunner.Run(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                // Do nothing else; the error(s) should be reported through the artifact from this process
            }
        }
    }
}
