using KRPC.Client;
using System;
using System.Threading;
using kRPCUtilities;
using KRPC.Client.Services.SpaceCenter;
using KRPC.Schema.KRPC;
using Service = KRPC.Client.Services.SpaceCenter.Service;

namespace kRPC.Programs
{
    public class Program
    {
        #region Base Variables

        private static Connection connection = new Connection("Tourist Orbit");
        private static Service spaceCenter = connection.SpaceCenter();
        private static Vessel vessel = spaceCenter.ActiveVessel;
        private static VesselProperty vesselProperty = new VesselProperty();

        #endregion

        #region Setting Up Fueling Streams

        private static Resources resourcesFirstStage = vessel.ResourcesInDecoupleStage(6, false);
        private static Resources resourcesSecondStage = vessel.ResourcesInDecoupleStage(0, false);
        private static Stream<float> firstStageFuel = connection.AddStream(() => resourcesFirstStage.Amount("LiquidFuel"));
        private static Stream<float> secondStageFuel = connection.AddStream(() => resourcesSecondStage.Amount("LiquidFuel"));

        #endregion

        public static void Main()
        {
            #region Variables

                var launchSequence = new LaunchSequence();
                var flightParamaters = new FlightParameters();
                var craftControls = new CraftControls(connection, flightParamaters);
                var flightControls = new FlightControls(connection, vesselProperty, flightParamaters, craftControls);

            #endregion

            Console.WriteLine("Current Vessel: {0}", vessel.Name);
            Console.WriteLine();

            LaunchSetup(flightParamaters);
            StartFueling();
            launchSequence.BeginLaunchSequence(connection, flightParamaters.SpoolEngines);

            flightControls.RollProgram();
            flightControls.GravityTurn();
            flightControls.AscentToApoapsis();
            flightControls.OrbitalInsertion();
        }

        private static void LaunchSetup(FlightParameters FlightParamaters)
        {
            #region Vessel Properties

            vesselProperty.StartingThrottle = 0.75f;
            vesselProperty.MaxFuelFirstStage = 11985;
            vesselProperty.MaxFuelSecondStage = 3325;
            vesselProperty.FuelNeededForCoreRecovery = 1100;

            #endregion

            #region Flight Parameters

            FlightParamaters.SpoolEngines = true;
            FlightParamaters.TurnStartAltitude = 1000;
            FlightParamaters.TurnEndAltitude = 26500;
            FlightParamaters.TargetApoapsis = 200000;

            #endregion

            #region Vessel Controls

            vessel.Control.SAS = true;
            vessel.Control.RCS = false;
            vessel.Control.Lights = true;
            vessel.Control.Throttle = vesselProperty.StartingThrottle;
            vessel.AutoPilot.TargetPitchAndHeading(90, 90);

            #endregion
        }

        private static void StartFueling()
        {
            #region Variables

            var fueling = false;

            var universalTime = spaceCenter.UT;
            var universalTimeIncremental = universalTime + 10;

            var maxFuelFirstStage = vesselProperty.MaxFuelFirstStage;
            var maxFuelSecondStage = vesselProperty.MaxFuelSecondStage;

            #endregion

            Message.SendMessage("Starting The Fueling Process", connection);

            vessel.Control.SetActionGroup(7, true);
            fueling = true;

            if (firstStageFuel.Get() < maxFuelFirstStage)
            {
                spaceCenter.RailsWarpFactor = 3;
            }

            while (fueling)
            {
                universalTime = spaceCenter.UT;

                if (universalTime >= universalTimeIncremental)
                {
                    var percentage = String.Format("{0:P2}", (firstStageFuel.Get() + secondStageFuel.Get()) / (maxFuelFirstStage + maxFuelSecondStage));

                    Console.WriteLine("Fueling is {0} complete.", percentage);

                    universalTimeIncremental = universalTime + 10;
                }

                if (firstStageFuel.Get() >= maxFuelFirstStage)
                {
                    Console.WriteLine();
                    Message.SendMessage("Fueling Process Complete", connection);

                    if (spaceCenter.RailsWarpFactor > 0)
                    {
                        spaceCenter.RailsWarpFactor = 0;
                    }

                    vessel.Control.Throttle = vesselProperty.StartingThrottle;
                    Thread.Sleep(5000);
                    fueling = false;
                }
            }
        }
    }
}
