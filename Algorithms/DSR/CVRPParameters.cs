using ACO.DataLoading;
using ACO.Shared;

namespace ACO.Problems.CVRP.CVRP
{
    internal struct CVRPParameters
    {
        public CVRPParameters(CVRPData data)
        {
            Optimum = data.Optimum;
            VehicleCapacity = data.Capacity;

            var cities = new List<City>(data.Dimension);

            for (var i = 0; i < data.Dimension; i++)
            {
                var distances = new List<int>(data.Dimension);

                for (var j = 0; j < data.Dimension; j++)
                {
                    distances.Add(data.EdgeWeights[i, j]);
                }

                cities.Add(new City
                {
                    Index = i,
                    Demand = data.Demands[i],
                    Distances = distances
                });
                CitiesNumber++;
            }
            Cities = cities;
        }

        public int CitiesNumber { get; private set; } = 0;

        public int VehicleCapacity { get; init; }

        public IEnumerable<City> Cities { get; init; }

        public int Optimum { get; init; }
    }
}