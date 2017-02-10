using System;
using kRPCUtilities;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

namespace kRPC.Programs
{
    public class LaunchSequence
    {
        private bool spoolUpEngines = false;

        public void BeginLaunchSequence(Connection connection, bool spoolEngines)
        {
            var spaceCenter = connection.SpaceCenter();
            var vessel = spaceCenter.ActiveVessel;

            if (vessel.Situation == VesselSituation.PreLaunch)
            {
                Message.CountdownMessage("Launch in T minus:", connection, true);

                Message.CountdownMessage("10...", connection, true);

                Message.CountdownMessage("9...", connection, true);

                Message.CountdownMessage("8...", connection, true);

                Message.CountdownMessage("7...", connection, true);

                Message.CountdownMessage("6...", connection, true);

                Message.CountdownMessage("5...", connection, true);

                Message.CountdownMessage("4...", connection, true);

                if (spoolUpEngines != spoolEngines)
                {
                    SpoolEngines(connection);
                }

                Message.CountdownMessage("3...", connection, true);

                Message.CountdownMessage("2...", connection, true);

                Message.CountdownMessage("1...", connection, true);

                LaunchVessel(connection);
            }
        }

        public void LaunchVessel(Connection connection)
        {
            var spaceCenter = connection.SpaceCenter();
            var vessel = spaceCenter.ActiveVessel;

            vessel.Control.ActivateNextStage();

            Message.SendMessage("Liftoff!", connection);
        }

        private void SpoolEngines(Connection connection)
        {
            var spaceCenter = connection.SpaceCenter();
            var vessel = spaceCenter.ActiveVessel;

            vessel.Control.ActivateNextStage();
            
            Console.WriteLine("Spooling Engines");
        }
    }
}