namespace ACO.Algorithms.EACSSA.SA
{
    internal class GlobalSAParameters : SAParametersBase
    {
        internal IList<int> Costs { get; private init; }

        public GlobalSAParameters(double lambda, double gamma, IList<int> costs) : base(lambda, gamma)
        {
            Costs = costs;
        }
    }
}