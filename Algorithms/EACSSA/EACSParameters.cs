namespace ACO.Algorithms.EACSSA
{
    internal struct EACSParameters
    {
        public EACSParameters(int problemSize)
        {
            q0 = (problemSize - 20) / (double)problemSize;
            InitialSampleSize = 1000;

            Psi = 0.01;
            t0 = 1;
            Alpha = 1;
            Beta = 0.5;
            Rho = 0.1;
            Q = 1;
            MaxIterations = 1000;
            PopulationSize = 10;
            MaxNoImprovementIterations = 200;
        }

        public int InitialSampleSize = 1000;
        public double q0;
        public double Psi = 0.01;
        public int t0 = 1;
        public int Alpha = 1;
        public double Beta = 0.5;
        public double Rho = 0.1;
        public double Q = 1;
        public int MaxIterations = 1000;
        public int PopulationSize = 10;
        public int MaxNoImprovementIterations = 200;
    }
}