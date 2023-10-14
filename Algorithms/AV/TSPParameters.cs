using ACO.DataLoading;
using ACO.Shared;
using System.Collections;

namespace ACO.Algorithms.AV
{
    internal struct TSPParameters
    {
        public TSPParameters(ProblemData data)
        {
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
                    Distances = distances
                });

                CitiesNumber++;
            }
            Cities = cities;

            Optimum = data.Optimum;
        }

        public int CitiesNumber { get; private set; } = 0;

        public IEnumerable<City> Cities { get; init; }

        public int Optimum { get; init; }

        public int GetDistance(int i, int j)
        {
            return Cities.ElementAt(i).Distances.ElementAt(j);
        }

        public bool CanTravel(int from, int to)
        {
            return Cities.ElementAt(from).IsTraversableTo(to);
        }

        // list of cities that have to be before city index
        //index -> node = -1 => node is before index
        public IEnumerable<int> GetIncomingEdges(int index)
        {
            var city = Cities.ElementAt(index);
            return Cities
                .Where(c => city.Distances.ElementAt(c.Index) == -1)
                .Select(c => c.Index);
        }

        // list of cities that have to be after city index
        //node -> index = -1 => node is after index
        public IEnumerable<int> GetOutcomingEdges(int index)
        {
            return Cities
                .Where(c => c.Distances.ElementAt(index) == -1)
                .Select(c => c.Index);
        }
    }
}