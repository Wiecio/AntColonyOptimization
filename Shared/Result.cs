namespace ACO.Shared
{
    internal record Result(int Length, int Iterations);

    internal record VRPResult(int Length, int Iterations, int Vehicles) : Result(Length, Iterations);
}