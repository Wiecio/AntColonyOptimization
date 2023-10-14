using ACO.Algorithms.DSR;
using ACO.Problems.CVRP.CVRP;
using ACO.Shared;

namespace ACO.Problems.CVRProblem
{
    internal class DSRACO
    {
        public CVRPParameters ProblemParameters { get; init; }
        public DSRACOParameters DSRACOParameters { get; init; }

        public DSRACO(CVRPParameters problemParameters, DSRACOParameters acoParameters)
        {
            ProblemParameters = problemParameters;
            DSRACOParameters = acoParameters;
        }

        public VRPResult Solve()
        {
            Console.WriteLine("Starting new solving session of DSRACO");
            var bestSolution = int.MaxValue;
            var bestSolutionVehicles = 0;
            var pheromones = new double[ProblemParameters.CitiesNumber][];
            for (var i = 0; i < pheromones.Length; i++)
            {
                pheromones[i] = new double[ProblemParameters.CitiesNumber];
                Array.Fill(pheromones[i], 1d);
            }

            var bs = new List<List<int>>();
            var iterationsWithoutImprovement = 0;
            for (var iter = 1; iter <= DSRACOParameters.MaxIterations; iter++)
            {
                // space reduction parameter
                var SR = (int)Math.Floor(ProblemParameters.CitiesNumber * Math.Pow(Math.E, -10d * (iter / (double)DSRACOParameters.MaxIterations)) + 3);
                var colony = new List<List<int>>(DSRACOParameters.PopulationSize);
                var k = 1;
                for (var ant = 0; ant < DSRACOParameters.PopulationSize; ant++)
                {
                    // empty ant solution
                    var antSolution = new List<int>();
                    // start in depot
                    var currentCity = ProblemParameters.Cities.First();
                    antSolution.Add(currentCity.Index);
                    // first vehicle
                    k = 1;
                    var currentCapacity = ProblemParameters.VehicleCapacity;

                    for (var numberOfVisitedCities = 1; numberOfVisitedCities < ProblemParameters.CitiesNumber;)
                    {
                        // reduce search space
                        var F = ProblemParameters.Cities.Where(city => !antSolution.Contains(city.Index))
                            .OrderBy(city => currentCity.Distances.ElementAt(city.Index))
                            .Take(SR);

                        var nextCity = SelectNextCity(currentCity, F, pheromones);

                        currentCapacity -= nextCity.Demand;

                        if (currentCapacity < 0)
                        {
                            // return to depot
                            currentCity = ProblemParameters.Cities.First();
                            // new vehicle
                            k++;
                            currentCapacity = ProblemParameters.VehicleCapacity;
                        }
                        else
                        {
                            currentCity = nextCity;
                            numberOfVisitedCities++;
                        }
                        antSolution.Add(currentCity.Index);
                    }
                    //return to depot
                    if (antSolution.Last() != 0)
                        antSolution.Add(0);
                    colony.Add(antSolution);
                }

                var neighborColony = GetNeighborColony(colony);
                var elites = SelectElites(neighborColony);
                UpdatePheromone(elites, pheromones);

                var iterationBestSolution = GetRouteLength(elites.First());
                if (iterationBestSolution < bestSolution)
                {
                    bestSolution = iterationBestSolution;
                    bestSolutionVehicles = k;
                    Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " " + bestSolution + " " + k);
                    if (bestSolution == ProblemParameters.Optimum)
                    {
                        Console.WriteLine("Found best known solution, returning");
                        return new VRPResult(bestSolution, iter, bestSolutionVehicles);
                    }
                    iterationsWithoutImprovement = 0;
                }
                else
                {
                    iterationsWithoutImprovement++;
                    if (iterationsWithoutImprovement >= DSRACOParameters.NoImprovementMaxIterations)
                    {
                        Console.WriteLine("No improvement, returning");
                        return new VRPResult(bestSolution, iter, bestSolutionVehicles);
                    }
                }
            }
            Console.WriteLine("Max iteration number was reached, returning");
            return new VRPResult(bestSolution, DSRACOParameters.MaxIterations, bestSolutionVehicles);
        }

        private void UpdatePheromone(List<List<int>> elites, double[][] pheromones)
        {
            //evaporate
            for (var i = 0; i < pheromones.Length; i++)
                for (var j = 0; j < pheromones[i].Length; j++)
                {
                    pheromones[i][j] *= (1 - DSRACOParameters.Rho);
                }

            //elites leave some pheromone trace
            foreach (var solution in elites)
                for (var city = 0; city < solution.Count - 1; city++)
                {
                    var pheromonLeaved = DSRACOParameters.Q / GetRouteLength(solution);
                    pheromones[solution[city]][solution[city + 1]] += pheromonLeaved;
                    pheromones[solution[city + 1]][solution[city]] += pheromonLeaved;
                }
        }

        private List<List<int>> SelectElites(List<List<int>> colony)
        {
            return colony.OrderBy(solution => GetRouteLength(solution)).Take(DSRACOParameters.ElitesNumber).ToList();
        }

        private List<List<int>> GetNeighborColony(List<List<int>> initialColony)
        {
            var neighborColony = new List<List<int>>();
            foreach (var solution in initialColony)
            {
                neighborColony.Add(GetNeighborSolution(solution));
            }
            return neighborColony;
        }

        private List<int> GetNeighborSolution(List<int> initialSolution)
        {
            var bestLength = GetRouteLength(initialSolution);
            var bestSolution = initialSolution;
            var operators = new Func<List<int>, List<int>>[]
                {
                    TwoOpt,
                    Reverse,
                    Shift,
                    RandomSort
                };

            for (var j = 0; j < 50; j++)
            {
                var solutionWithoutDepot = bestSolution.Where(city => city != 0).ToList();
                var locOperator = operators[Random.Shared.Next(operators.Length)];
                var newSolution = locOperator(solutionWithoutDepot);
                var newsolutionWithDepots = AddDepotsToSolution(newSolution);
                var newLength = GetRouteLength(newsolutionWithDepots);
                if (newLength < bestLength)
                {
                    bestLength = newLength;
                    bestSolution = newsolutionWithDepots;
                }
            }

            return bestSolution;

            List<int> AddDepotsToSolution(List<int> solution)
            {
                var cap = ProblemParameters.VehicleCapacity;

                var modSolution = new List<int>(solution);

                for (var city = 0; city < modSolution.Count; city++)
                {
                    var cityIndex = modSolution[city];
                    cap -= ProblemParameters.Cities.ElementAt(cityIndex).Demand;
                    if (cap < 0)
                    {
                        modSolution.Insert(city, 0);
                        cap = ProblemParameters.VehicleCapacity;
                    }
                }
                modSolution.Insert(0, 0);
                if (modSolution.Last() != 0)
                    modSolution.Add(0);

                return modSolution;
            }
        }

        private List<int> TwoOpt(List<int> route)
        {
            var retRoute = new List<int>(route);

            var firstNodeIndex = Random.Shared.Next(retRoute.Count);
            var secondNodeIndex = Random.Shared.Next(retRoute.Count);

            var firstNodeVal = retRoute[firstNodeIndex];
            var secondNodeVal = retRoute[secondNodeIndex];

            retRoute.RemoveAt(firstNodeIndex);
            retRoute.Insert(firstNodeIndex, secondNodeVal);

            retRoute.RemoveAt(secondNodeIndex);
            retRoute.Insert(secondNodeIndex, firstNodeVal);

            return retRoute;
        }

        private List<int> Reverse(List<int> route)
        {
            var retRoute = new List<int>(route);

            var firstNodeIndex = Random.Shared.Next(retRoute.Count);
            var count = Random.Shared.Next(retRoute.Count - firstNodeIndex);

            retRoute.Reverse(firstNodeIndex, count);

            return retRoute;
        }

        private List<int> Shift(List<int> route)
        {
            var retRoute = new List<int>(route);

            var firstNodeIndex = Random.Shared.Next(retRoute.Count);
            var secondNodeIndex = Random.Shared.Next(retRoute.Count);
            var firstNodeValue = retRoute[firstNodeIndex];

            retRoute.RemoveAt(firstNodeIndex);
            retRoute.Insert(secondNodeIndex, firstNodeValue);

            return retRoute;
        }

        private List<int> RandomSort(List<int> route)
        {
            var retRoute = new List<int>(route);

            if (route.Count < 10)
                return retRoute;

            var vectorLength = Random.Shared.Next(5, retRoute.Count / 2);
            var vector = new int[vectorLength];
            for (var i = 0; i < vectorLength; i++)
            {
                int random;
                do
                {
                    random = Random.Shared.Next(retRoute.Count);
                } while (vector.Contains(random));
                vector[i] = random;
            }
            var shuffledVector = new int[vectorLength];
            Array.Copy(vector, shuffledVector, vectorLength);

            // Fisher-Yates algorithm AKA the Knuth Shuffle
            var n = vectorLength;
            while (n > 1)
            {
                var k = Random.Shared.Next(n--);
                var temp = shuffledVector[n];
                shuffledVector[n] = shuffledVector[k];
                shuffledVector[k] = temp;
            }
            for (var i = 0; i < vectorLength; i++)
            {
                retRoute[vector[i]] = route[shuffledVector[i]];
            }

            return retRoute;
        }

        private List<List<int>> TransformSolution(List<int> antSolution)
        {
            var transformedSolution = new List<List<int>>();
            var lastSplitIndex = 0;
            for (var cityIndex = 1; cityIndex < antSolution.Count; cityIndex++)
            {
                if (antSolution[cityIndex] == 0) //depot
                {
                    transformedSolution.Add(antSolution.Take(lastSplitIndex..(cityIndex + 1)).ToList());
                    lastSplitIndex = cityIndex;
                }
            }
            return transformedSolution;
        }

        private List<List<List<int>>> TransformColony(List<List<int>> colony)
        {
            var transformedColony = new List<List<List<int>>>();
            foreach (var solution in colony)
            {
                transformedColony.Add(TransformSolution(solution));
            }
            return transformedColony;
        }

        private int GetSolutionLength(IEnumerable<IEnumerable<int>> solution)
        {
            var solutionLength = 0;
            foreach (var route in solution)
            {
                solutionLength += GetRouteLength(route);
            }
            return solutionLength;
        }

        private int GetRouteLength(IEnumerable<int> route)
        {
            var routeLength = 0;
            for (var i = 0; i < route.Count() - 1; i++)
            {
                routeLength += GetTwoCitiesDistance(route.ElementAt(i), route.ElementAt(i + 1));
            }
            return routeLength;
        }

        private int GetTwoCitiesDistance(int firstCityIndex, int secondCityIndex)
        {
            return ProblemParameters.Cities.ElementAt(firstCityIndex).Distances.ElementAt(secondCityIndex);
        }

        private City SelectNextCity(City currentCity, IEnumerable<City> F, double[][] pheromones)
        {
            var totalFromOmega = 0.0;

            foreach (var city in ProblemParameters.Cities)
            {
                if (city.Index == currentCity.Index)
                    continue;
                totalFromOmega += Math.Pow(pheromones[currentCity.Index][city.Index], DSRACOParameters.Alpha) * Math.Pow(1.0d / currentCity.Distances.ElementAt(city.Index), DSRACOParameters.Beta);
            }

            var probabilities = new List<double>(F.Count());
            foreach (var city in F)
            {
                if (city.Index == currentCity.Index)
                {
                    probabilities.Add(0);
                }
                else
                {
                    var propability = Math.Pow(pheromones[currentCity.Index][city.Index], DSRACOParameters.Alpha) * Math.Pow(1.0d / currentCity.Distances.ElementAt(city.Index), DSRACOParameters.Beta);
                    probabilities.Add(propability);
                }
            }

            var p = probabilities.Select((propability) => propability / totalFromOmega);

            var sum = p.Sum();
            var randomNumber = Random.Shared.NextDouble() * sum;
            sum = 0;
            for (var i = 0; i < p.Count(); i++)
            {
                sum += p.ElementAt(i);
                if (sum >= randomNumber)
                    return F.ElementAt(i);
            }

            return F.Last();
        }

        private bool IsSolutionFeasable(List<int> solution)
        {
            if (solution.Where(c => c != 0).Distinct().Count() != ProblemParameters.CitiesNumber - 1)
                return false;

            var capacity = ProblemParameters.VehicleCapacity;
            for (var i = 0; i < solution.Count; i++)
            {
                var city = ProblemParameters.Cities.ElementAt(solution[i]);
                capacity -= city.Demand;

                if (city.Index == 0)
                    capacity = ProblemParameters.VehicleCapacity;

                if (capacity < 0)
                    return false;
            }

            return true;
        }
    }
}