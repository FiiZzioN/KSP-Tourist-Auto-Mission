namespace kRPC.Programs
{
    public class VesselProperty
    {
        #region Properties

        public float StartingThrottle { get; set; } = 1f;
        public int MaxFuelFirstStage { get; set; } = 11985;
        public int MaxFuelSecondStage { get; set; } = 3325;
        public int FuelNeededForCoreRecovery { get; set; } = 1050;

        #endregion
    }
}