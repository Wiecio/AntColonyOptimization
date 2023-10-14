namespace ACO.Algorithms.DSR
{
    internal struct DSRACOParameters
    {
        public DSRACOParameters()
        {
        }

        public int Alpha = 1;
        public int Beta = 2;
        public double Rho = 0.1;
        public double Q = 1;
        public int ElitesNumber = 3;
        public int MaxIterations = 1500;
        public int PopulationSize = 130;
        public int NoImprovementMaxIterations = 200;
    }
}