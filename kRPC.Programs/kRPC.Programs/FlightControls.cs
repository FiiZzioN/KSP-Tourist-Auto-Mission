using KRPC.Client;
using System;
using System.Threading;
using kRPCUtilities;
using KRPC.Client.Services.SpaceCenter;

namespace kRPC.Programs
{
    public class FlightControls
    {
        public FlightControls(Connection Connection, VesselProperty VesselProperty, FlightParameters FlightParams, CraftControls CraftControls)
        {
            connection = Connection;
            vesselProperty = VesselProperty;
            flightParams = FlightParams;
            craftControls = CraftControls;
            
            var spaceCenter = connection.SpaceCenter();
            vessel = spaceCenter.ActiveVessel;

            var flight = vessel.Flight();
            altitudeASL = connection.AddStream(() => flight.MeanAltitude);
    }

        private Connection connection;
        private Vessel vessel;
        private FlightParameters flightParams;
        private VesselProperty vesselProperty;
        private CraftControls craftControls;
        private Stream<double> altitudeASL;

        public bool RollProgramComplete { get; private set; } = false;

        public void RollProgram()
        {
            var flight = vessel.Flight();
            var altitude = connection.AddStream(() => flight.SurfaceAltitude);

            Thread.Sleep(5000);

            vessel.AutoPilot.SAS = true;
            vessel.AutoPilot.RollThreshold = 2.5;
            vessel.AutoPilot.TargetRoll = 90f;
            vessel.AutoPilot.Engage();

            Message.SendMessage("Beginning Roll Program", connection);

            while (!RollProgramComplete)
            {
                if (altitude.Get() >= flightParams.TurnStartAltitude)
                {
                    RollProgramComplete = true;
                    break;
                }
            }
        }

        public void GravityTurn()
        {
            var logicSwitch = false;

            Message.SendMessage("Beginning Gravity Turn", connection);

            while (true)
            {
                if (craftControls.SolidsJettisoned && !logicSwitch)
                {
                    vessel.Control.Throttle = 1;
                    logicSwitch = true;
                }

                if (altitudeASL.Get() >= flightParams.TurnStartAltitude && RollProgramComplete)
                {
                    craftControls.MaxQThrottleSegment();
                    craftControls.JettisonSRBs();
                    vessel.AutoPilot.TargetPitch = Convert.ToSingle(90 - Math.Abs(flightParams.GravityTurnPitchPerMeter * altitudeASL.Get()));

                    if (altitudeASL.Get() >= flightParams.TurnEndAltitude)
                    {
                        break;
                    }
                }
            }
        }
        
        public void AscentToApoapsis()
        {
            var vesselApoapsis = connection.AddStream(() => vessel.Orbit.ApoapsisAltitude);
            var resourcesFirstStage = vessel.ResourcesInDecoupleStage(6, false);
            var firstStageFuel = connection.AddStream(() => resourcesFirstStage.Amount("LiquidFuel"));

            Message.SendMessage("Continuing Ascent To Apoapsis", connection);

            while (!craftControls.TargetApoapsisMet)
            {
                vessel.AutoPilot.TargetPitch = Convert.ToSingle(45 - Math.Abs(flightParams.AscentPitchPerMeter * altitudeASL.Get()));

                if (firstStageFuel.Get() / vesselProperty.MaxFuelFirstStage <= 0.12 && !craftControls.HasMECO)
                {
                    //vessel.AutoPilot.Disengage();
                    //vessel.Control.SAS = true;
                    //vessel.Control.SASMode = SASMode.Prograde;

                    while (!craftControls.HasMECO)
                    {
                        craftControls.MECO(vesselProperty.FuelNeededForCoreRecovery, firstStageFuel);
                    }
                }
                    
                craftControls.SecondStageIgnition();
                craftControls.ExtendSolarPanels(altitudeASL);
                craftControls.ReachingTargetApoapsis(vesselApoapsis);
            }
        }

        public void OrbitalInsertion()
        {
            var ut = connection.AddStream(() => connection.SpaceCenter().UT);

            #region Vis-Viva Equation

            double mu = vessel.Orbit.Body.GravitationalParameter;
            var r = vessel.Orbit.Apoapsis;
            var a1 = vessel.Orbit.SemiMajorAxis;
            var a2 = r;
            var v1 = Math.Sqrt(mu * ((2.0 / r) - (1.0 / a1)));
            var v2 = Math.Sqrt(mu * ((2.0 / r) - (1.0 / a2)));
            var deltaV = v2 - v1;

            #endregion

            var node = vessel.Control.AddNode(ut.Get() + vessel.Orbit.TimeToApoapsis, (float)deltaV);

            #region Calculate Burn Time

            var F = vessel.AvailableThrust;
            var Isp = vessel.SpecificImpulse * 9.82;
            var m0 = vessel.Mass;
            var m1 = m0 / Math.Exp(deltaV / Isp);
            var flowRate = F / Isp;
            var burnTime = (m0 - m1) / flowRate;

            #endregion

            OrientingShip(node);
            WaitUntilNode(ut, burnTime);
            ExecuteBurn(node, burnTime);
        }

        private void OrientingShip(Node node)
        {
            Message.SendMessage("Orienting Ship For Circularization Burn", connection);
            /*
            vessel.AutoPilot.ReferenceFrame = node.ReferenceFrame;
            vessel.AutoPilot.TargetDirection = Tuple.Create(0.0, 1.0, 0.0);
            vessel.AutoPilot.Wait();
            */

            vessel.AutoPilot.Disengage();
            vessel.AutoPilot.SAS = true;
            Thread.Sleep(1000);
            vessel.AutoPilot.SASMode = SASMode.Maneuver;
            Thread.Sleep(25000);
        }

        private void WaitUntilNode(Stream<double> ut, double burnTime)
        {
            Message.SendMessage("Warping To Circularization Burn", connection);

            var burnUT = ut.Get() + vessel.Orbit.TimeToApoapsis - (burnTime / 2.0);
            var leadTime = 10;
            connection.SpaceCenter().WarpTo(burnUT - leadTime);
            ut.Remove();
        }

        private void ExecuteBurn(Node node, double burnTime)
        {
            var remainingDeltaV = connection.AddStream(() => node.RemainingDeltaV);
            var timeToApoapsis = connection.AddStream(() => vessel.Orbit.TimeToApoapsis);

            while (timeToApoapsis.Get() - (burnTime / 2.0) > 0)
            {
            }

            Message.SendMessage("Executing Burn", connection);
            vessel.Control.Throttle = 1;

            while (remainingDeltaV.Get() >= 25)
            {
            }
            vessel.Control.Throttle = 0.5f;

            while (remainingDeltaV.Get() >= 3)
            {                
            }
            vessel.Control.Throttle = 0;

            Message.SendMessage("SECO 2", connection);
            node.Remove();
            craftControls.ExtendSolarPanels(altitudeASL);

            Message.SendMessage("Orbit Achieved", connection);
        }
    }
}