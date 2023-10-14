namespace ACO.Algorithms.EACSSA.SA
{
    internal abstract class SimulatedAnnealingBase
    {
        public double Temperature { get; protected set; }
        public SAParametersBase Parameters { get; private init; }

        public SimulatedAnnealingBase(SAParametersBase parameters)
        {
            Parameters = parameters;
        }

        public void UpdateTemperature()
        {
            Temperature = GetUpdatedTemperature();
        }

        protected virtual double GetInitialTemperature(IList<int> costs)
        {
            var meanDeltaC = CalculateMeanDeltaC(costs);
            var sdv = CalculateStandardDeviation(costs);
            var nominator = meanDeltaC + 3 * sdv;
            var denominator = Math.Log(1d / Parameters.Gamma);

            return nominator / denominator;
        }

        protected virtual double GetUpdatedTemperature()
        {
            return Parameters.Lambda * Temperature;
        }

        private double CalculateStandardDeviation(IList<int> costs)
        {
            var mean = costs.Average();
            var nominator = costs.Sum(c => Math.Pow(c - mean, 2));
            var denominator = costs.Count - 1;
            return Math.Sqrt(nominator / denominator);
        }

        private double CalculateMeanDeltaC(IList<int> costs)
        {
            var costsCount = costs.Count;
            var evenCount = costsCount - costsCount % 2;

            var sum = 0d;
            for (var i = 0; i < evenCount; i += 2)
            {
                var delta = Math.Abs(costs[i] - costs[i + 1]);
                sum += delta;
            }
            return sum / costsCount - costsCount % 2;
        }
    }
}