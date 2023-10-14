namespace ACO.Algorithms.AV
{
    internal struct ACOAVParameters
    {
        public ACOAVParameters()
        {
        }

        public int t0 = 1;
        public int Alpha = 1;
        public int Beta = 4;
        public double Rho = 0.1;
        public double Q = 1;
        public int MaxIterations = 1000;
        public int MaxNoImprovementIterations = 200;
        public double w1 = 1;
        public double w2 = 0.5;
        public int PopulationSize = 50;
    }
}