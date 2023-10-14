namespace ACO.Algorithms.EACSSA.SA
{
    internal class LocalSA : SimulatedAnnealingBase
    {
        public bool IsInitialized { get; private set; }

        public LocalSA(SAParametersBase parameters) : base(parameters)
        {
        }

        public void Initialize(IList<int> costs)
        {
            Temperature = GetInitialTemperature(costs);
            IsInitialized = true;
        }
    }
}