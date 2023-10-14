namespace ACO.Algorithms.EACSSA.SA
{
    internal class SAParametersBase
    {
        public SAParametersBase(double lambda, double gamma)
        {
            Lambda = lambda;
            Gamma = gamma;
        }

        internal double Lambda { get; private init; }
        internal double Gamma { get; private init; }
    }
}