namespace ACO.DataLoading
{
    internal class CVRPData : ProblemData
    {
        public CVRPData()
        {
        }

        public int Capacity { get; set; }
        public int[] Demands { get; set; }
    }
}