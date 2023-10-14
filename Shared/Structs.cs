using ACO.Algorithms.AV;

namespace ACO.Shared
{
    internal struct Route
    {
        public Route(int cities)
        {
            Cities = new int[cities];
        }

        public int[] Cities;

        public void SwapRanges(int start, int mid, int end)
        {
            var tmp1 = Cities[mid..end];
            var tmp2 = Cities[start..mid];

            Array.Copy(tmp1, 0, Cities, start, tmp1.Length);
            Array.Copy(tmp2, 0, Cities, start + tmp1.Length, tmp2.Length);
        }

        public Route Copy()
        {
            var newRoute = new Route(Cities.Length);
            Array.Copy(Cities, newRoute.Cities, Cities.Length);
            return newRoute;
        }

        public bool IsFeasable(TSPParameters parameters)
        {
            if (Cities.Distinct().Count() != parameters.Cities.Count())
                return false;

            return true;
        }

        public bool IsSopFeasable(TSPParameters parameters)
        {
            if (Cities.Distinct().Count() != parameters.Cities.Count())
                return false;

            for (var i = 0; i < Cities.Length; i++)
            {
                var city = Cities[i];

                var citiesBeforeCity = Cities.Take(i);
                var citiesThatMustBeBeforeCity = parameters.GetIncomingEdges(city);

                if (citiesThatMustBeBeforeCity.Except(citiesBeforeCity).Any())
                    return false;
            }
            return true;
        }

        public int CalculateCost(IEnumerable<City> cities, bool sop = false)
        {
            var cost = 0;
            for (var i = 0; i < Cities.Length - 1; i++)
            {
                cost += cities.ElementAt(Cities[i]).Distances.ElementAt(Cities[i + 1]);
            }
            if (!sop)
                cost += cities.ElementAt(Cities[Cities.Length - 1]).Distances.ElementAt(Cities[0]);
            return cost;
        }

        public void PrintRoute()
        {
            Console.WriteLine("Route:");
            Console.WriteLine();
            foreach (var city in Cities)
            {
                Console.Write($"{city} \t");
            }
            Console.WriteLine();
            Console.ReadKey();
        }

        public void Reverse(int i, int j)
        {
            Array.Reverse(Cities, i, j - i + 1);
        }
    }

    internal struct Population
    {
        public Population(int size)
        {
            Routes = new Route[size];
        }

        public Route[] Routes;
    }

    internal struct PheromoneTrails
    {
        public PheromoneTrails(int cities)
        {
            Pheromones = new double[cities, cities];
        }

        public PheromoneTrails(int citiesNumber, double t0) : this(citiesNumber)
        {
            for (var i = 0; i < Pheromones.GetLength(0); i++)
            {
                for (var j = 0; j < Pheromones.GetLength(1); j++)
                {
                    Pheromones[i, j] = t0;
                }
            }
        }

        public double[,] Pheromones;
    }
}