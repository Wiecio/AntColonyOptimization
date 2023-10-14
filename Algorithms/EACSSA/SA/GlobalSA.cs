namespace ACO.Algorithms.EACSSA.SA
{
    internal class GlobalSA : SimulatedAnnealingBase
    {
        public GlobalSA(GlobalSAParameters parameters) : base(parameters)
        {
            Temperature = GetInitialTemperature(parameters.Costs);
        }
    }
}