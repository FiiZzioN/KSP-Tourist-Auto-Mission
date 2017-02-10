using KRPC.Client;
using System.Threading;
using kRPCUtilities;
using KRPC.Client.Services.SpaceCenter;

namespace kRPC.Programs
{
    public class CraftControls
    {
        public CraftControls(Connection Connection, FlightParameters FlightParams)
        {
            connection = Connection;
            flightParams = FlightParams;

            var spaceCenter = connection.SpaceCenter();
            vessel = spaceCenter.ActiveVessel;
        }

        private Connection connection;
        private Vessel vessel;
        private FlightParameters flightParams;

        #region Properties

        public bool PassedMachOne { get; private set; } = false;
        public bool SolidsJettisoned { get; private set; } = false;
        public bool FairingsJettisoned { get; private set; } = false;
        public bool HasMECO { get; private set; } = false;
        public bool ApproachingTargetApoapsis { get; private set; } = false;
        public bool TargetApoapsisMet { get; private set; } = false;
        public bool SecondStageIgnited { get; private set; } = false;
        public bool PanelsExtended { get; private set; } = false;

        #endregion

        public void JettisonSRBs()
        {
            var resourcesSolidFuel = vessel.ResourcesInDecoupleStage(7, false);
            var currentSolidFuel = connection.AddStream(() => resourcesSolidFuel.Amount("SolidFuel"));

            if (!SolidsJettisoned && currentSolidFuel.Get() <= 12)
            {
                Message.SendMessage("SRB's Jettisoned", connection);

                vessel.Control.ActivateNextStage();
                SolidsJettisoned = true;
                currentSolidFuel.Remove();
            }
        }

        public void JettisonFairings()
        {
            var flight = vessel.Flight();
            var altitudeASL = connection.AddStream(() => flight.MeanAltitude);

            if (!FairingsJettisoned && altitudeASL.Get() >= 85000)
            {
                Message.SendMessage("Fairings Jettisoned", connection);

                vessel.Control.SetActionGroup(1, true);
                FairingsJettisoned = true;
                altitudeASL.Remove();
            }
        }

        public void MaxQThrottleSegment()
        {
            var flight = vessel.Flight(vessel.Orbit.Body.ReferenceFrame);
            var currentSpeed = connection.AddStream(() => flight.Speed);

            if (!PassedMachOne && currentSpeed.Get() >= 320)
            {
                Message.SendMessage("MaxQ Throttle Segment", connection);

                vessel.Control.Throttle = 0.5f;
                PassedMachOne = true;
                currentSpeed.Remove();
            }
        }

        public void MECO(int FuelForLanding, Stream<float> FuelInStage)
        {
            if (!HasMECO && FuelInStage.Get() <= FuelForLanding)
            {
                vessel.Control.Throttle = 0;
                Message.SendMessage("MECO", connection);
                Thread.Sleep(2000);
                vessel.Control.ActivateNextStage();
                //vessel.AutoPilot.Engage();
                //vessel.Control.SASMode = SASMode.StabilityAssist;
                HasMECO = true;
            }
        }

        public void ReachingTargetApoapsis(Stream<double> vesselApoapsisAltitude)
        {
            if (!TargetApoapsisMet)
            {
                if (vesselApoapsisAltitude.Get() >= flightParams.TargetApoapsis * 0.9 && !ApproachingTargetApoapsis)
                {
                    vessel.Control.Throttle = 0.75f;
                    ApproachingTargetApoapsis = true;
                    Message.SendMessage("Approaching Target Apoapsis", connection);
                }

                if (vesselApoapsisAltitude.Get() >= flightParams.TargetApoapsis && !TargetApoapsisMet)
                {
                    vessel.Control.Throttle = 0;
                    TargetApoapsisMet = true;
                    Message.SendMessage("SECO 1", connection);
                    Message.SendMessage("Target Apoapsis Met", connection);
                }
            }
        }

        public void SecondStageIgnition()
        {
            if (HasMECO && !SecondStageIgnited)
            {
                vessel.Control.Throttle = 1;
                Thread.Sleep(3000);
                Message.SendMessage("Second Stage Ignition", connection);
                vessel.Control.ActivateNextStage();
                SecondStageIgnited = true;
            }
        }

        public void ExtendSolarPanels(Stream<double> vesselAltitude)
        {
            if (vesselAltitude.Get() >= 105000 && !PanelsExtended)
            {
                Message.SendMessage("Extending Solar Panels", connection);
                vessel.Control.SetActionGroup(2, true);
                PanelsExtended = true;
            }
        }
    }
}