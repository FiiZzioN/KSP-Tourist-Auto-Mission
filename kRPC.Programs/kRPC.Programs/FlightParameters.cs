namespace kRPC.Programs
{
    public class FlightParameters
    {
        private float turnEndAltitude = 20000;
        private float targetApoapsis = 200000;

        #region Properties

        public float TurnEndAltitude
        {
            get { return turnEndAltitude; }
            set
            {
                turnEndAltitude = value;
                SetGravityTurnPitchPerMeter();
            }
        }
        public float TargetApoapsis
        {
            get { return targetApoapsis; }
            set
            {
                targetApoapsis = value;
                SetAscentPitchPerMeter();
            }
        }
        public float TurnStartAltitude { get; set; } = 1000;
        public double AscentPitchPerMeter { get; set; } = 0.00225f;
        public double GravityTurnPitchPerMeter { get; set; } = 0.00225f;
        public bool SpoolEngines { get; set; } = false;

        #endregion

        private void SetGravityTurnPitchPerMeter()
        {
            GravityTurnPitchPerMeter = 45 / TurnEndAltitude;
        }

        private void SetAscentPitchPerMeter()
        {
            AscentPitchPerMeter = 72 / TargetApoapsis;
        }
    }
}