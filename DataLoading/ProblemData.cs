namespace ACO.DataLoading
{
    public class ProblemData
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public int Dimension { get; set; }
        public int[,] EdgeWeights { get; set; }
        public string EDGE_WEIGHT_TYPE { get; set; }
        public string EDGE_WEIGHT_FORMAT { get; set; }
        public int Optimum { get; set; }
    }
}