using ACO.Algorithms.AV;
using ACO.Algorithms.EACSSA.SA;
using ACO.Shared;

namespace ACO.Algorithms.EACSSA
{
    internal class EACS
    {
        private readonly TSPParameters _problemParameters;
        private readonly EACSParameters _acoParameters;
        private readonly GlobalSAParameters _globalSAParameters;

        public EACS(TSPParameters problemParameters, EACSParameters acoParameters)
        {
            _problemParameters = problemParameters;
            _acoParameters = acoParameters;
            _globalSAParameters = new GlobalSAParameters(0.9999, 0.1, GetRandomSample());
        }

        public Result Solve()
        {
            var globalSA = new GlobalSA(_globalSAParameters);

            var localOptimizer = new OptimizerSop3(
                new LocalSA(
                    new SAParametersBase(0.99, 0.1)));

            Console.WriteLine("Starting new solving session of EACS_SA_SOP3EX_SA");
            var pheromones = new PheromoneTrails(_problemParameters.CitiesNumber, _acoParameters.t0);
            Route? globalBest = null;
            Route? activeSolution = null;

            var noImprovementIterations = 0;

            for (var i = 0; i < _acoParameters.MaxIterations; i++)
            {
                var population = GetNewPopulation();
                for (var k = 1; k < _problemParameters.CitiesNumber - 1; k++)
                {
                    for (var j = 0; j < population.Routes.Length; j++)
                    {
                        population.Routes[j].Cities[k] = ChooseNextCity(globalBest, population.Routes[j], pheromones, k);
                        LocalPheromoneUpdate(ref pheromones, k - 1, k);
                    }
                }

                for (var l = 0; l < population.Routes.Length; l++)
                {
                    localOptimizer.Sop3Exchange(ref population.Routes[l].Cities, _problemParameters);
                }

                var localBest = FindBest(population);
                if (UpdateGlobalBest(ref globalBest, ref noImprovementIterations, localBest))
                {
                    Console.WriteLine("Found optimum, returning");
                    return new Result(globalBest!.Value.CalculateCost(_problemParameters.Cities, true), i);
                }

                if (noImprovementIterations >= _acoParameters.MaxNoImprovementIterations)
                {
                    Console.WriteLine("No improvement, returning");
                    return new Result(globalBest!.Value.CalculateCost(_problemParameters.Cities, true), i);
                }

                SelectAciveSolution(ref activeSolution, ref globalSA, population, globalBest!.Value);

                GlobalPheromoneUpdate(ref pheromones, Random.Shared.NextSingle() < 0.1 ? globalBest!.Value : activeSolution!.Value);
                globalSA.UpdateTemperature();

                if (globalBest?.CalculateCost(_problemParameters.Cities, true) is int cost)
                    Console.WriteLine(cost);
            }

            var length = globalBest!.Value.CalculateCost(_problemParameters.Cities, true); ;
            Console.WriteLine("Max iterations, returning");
            return new Result(length, _acoParameters.MaxIterations);
        }

        private void SelectAciveSolution(ref Route? activeSolution, ref GlobalSA globalSA, Population population, Route globalBest)
        {
            if (activeSolution == null)
            {
                activeSolution = globalBest;
                return;
            }

            for (var i = 0; i < population.Routes.Length; i++)
            {
                var activeSolutionCost = activeSolution.Value.CalculateCost(_problemParameters.Cities, true);
                var antSolutionCost = population.Routes[i].CalculateCost(_problemParameters.Cities, true);
                if (antSolutionCost < activeSolutionCost)
                {
                    activeSolution = population.Routes[i];
                }
                else
                {
                    var deltaCost = antSolutionCost - activeSolutionCost;
                    if (Random.Shared.NextSingle() < Math.Pow(Math.E, -deltaCost / globalSA.Temperature))
                        activeSolution = population.Routes[i];
                }
            }
        }

        private int ChooseNextCity(Route? globalBest, Route currentRoute, PheromoneTrails pheromones, int citiesInCurrentRoute)
        {
            //select last node if it is the only one that's left
            if (citiesInCurrentRoute == _problemParameters.CitiesNumber - 1)
                return _problemParameters.CitiesNumber - 1;

            var currentCity = currentRoute.Cities[citiesInCurrentRoute - 1];
            var candidates = GetCandidates(currentRoute);
            var probabilites = candidates.Select(c => CalculatePropability(pheromones, currentCity, c.Index));

            if (globalBest != null && Random.Shared.NextSingle() < _acoParameters.q0)
            {
                var currentCityIndexInGlobalBest = Array.IndexOf(globalBest.Value.Cities, currentCity);
                var followingCityInGlobalBest = globalBest.Value.Cities[currentCityIndexInGlobalBest + 1];

                if (currentRoute.Cities.Contains(followingCityInGlobalBest))
                {
                    return candidates.ElementAt(FindIndexOfMax(probabilites)).Index;
                }

                if (candidates.Any(c => c.Index == followingCityInGlobalBest))
                    return followingCityInGlobalBest;
            }

            var probabilitiesSum = probabilites.Sum();
            var normalizedProbabilities = probabilites.Select(p => p / probabilitiesSum);

            var sum = normalizedProbabilities.Sum();
            var randomNumber = Random.Shared.NextDouble() * sum;
            sum = 0;
            for (var i = 0; i < normalizedProbabilities.Count(); i++)
            {
                sum += normalizedProbabilities.ElementAt(i);
                if (sum >= randomNumber)
                    return candidates.ElementAt(i).Index;
            }

            return candidates.Last().Index;
        }

        private IEnumerable<City> GetCandidates(Route currentRoute)
        {
            var notVisitedCities = _problemParameters.Cities.Where(c1 => !currentRoute.Cities.Any(c2 => c1.Index == c2));
            return notVisitedCities.Where(c => c.IsAfterCities(notVisitedCities.Except(new[] { c })));
        }

        private int FindIndexOfMax(IEnumerable<double> probabilities)
        {
            var maxProbability = 0d;
            var indexOfMax = -1;
            var i = 0;
            foreach (var probability in probabilities)
            {
                if (probability > maxProbability)
                {
                    maxProbability = probability;
                    indexOfMax = i;
                }
                i++;
            }
            return indexOfMax;
        }

        private double CalculatePropability(PheromoneTrails pheromones, int currentCity, int nextCity)
        {
            return pheromones.Pheromones[currentCity, nextCity]
                * Math.Pow(1d / _problemParameters.GetDistance(currentCity, nextCity), _acoParameters.Beta);
        }

        private bool UpdateGlobalBest(ref Route? globalBest, ref int noImprovementIterations, Route newRoute)
        {
            var isOptimum = false;
            if (globalBest == null)
            {
                globalBest = newRoute;
                return isOptimum;
            }
            var newRouteCost = newRoute.CalculateCost(_problemParameters.Cities, true);
            if (newRouteCost < globalBest.Value.CalculateCost(_problemParameters.Cities, true))
            {
                noImprovementIterations = 0;
                globalBest = newRoute;

                Console.WriteLine($"Found better: {globalBest?.CalculateCost(_problemParameters.Cities, true)}");
            }
            else
            {
                noImprovementIterations++;
            }

            if (newRouteCost == _problemParameters.Optimum)
                isOptimum = true;

            return isOptimum;
        }

        private void GlobalPheromoneUpdate(ref PheromoneTrails pheromones, Route route)
        {
            var lBest = route.CalculateCost(_problemParameters.Cities, true);
            for (var i = 0; i < route.Cities.Length - 1; i++)
            {
                pheromones.Pheromones[i, i + 1] *= 1 - _acoParameters.Rho;
                pheromones.Pheromones[i, i + 1] += _acoParameters.Rho / lBest;
            }
        }

        private void LocalPheromoneUpdate(ref PheromoneTrails pheromoneTrails, int fromCity, int toCity)
        {
            pheromoneTrails.Pheromones[fromCity, toCity] *= 1 - _acoParameters.Psi;
            pheromoneTrails.Pheromones[fromCity, toCity] += _acoParameters.Psi * _acoParameters.t0;
        }

        private Population GetNewPopulation()
        {
            var population = new Population(_acoParameters.PopulationSize);
            for (var r = 0; r < population.Routes.Length; r++)
            {
                population.Routes[r] = new Route(_problemParameters.CitiesNumber);
                population.Routes[r].Cities[0] = 0;
                population.Routes[r].Cities[_problemParameters.CitiesNumber - 1] = _problemParameters.CitiesNumber - 1;
            }
            return population;
        }

        private Route FindBest(Population population)
        {
            return population
                .Routes
                .MinBy(r => r
                    .CalculateCost(_problemParameters.Cities, true));
        }

        private IList<int> GetRandomSample()
        {
            var sample = new int[_acoParameters.InitialSampleSize];
            for (var i = 0; i < _acoParameters.InitialSampleSize; i++)
            {
                var route = new Route(_problemParameters.CitiesNumber);
                route.Cities[0] = 0;
                route.Cities[_problemParameters.CitiesNumber - 1] = _problemParameters.CitiesNumber - 1;

                for (var j = 1; j < _problemParameters.CitiesNumber - 1; j++)
                {
                    var candidates = GetCandidates(route);
                    var nextCityFromCandidates = Random.Shared.Next(candidates.Count());
                    route.Cities[j] = candidates.ElementAt(nextCityFromCandidates).Index;
                }

                sample[i] = route.CalculateCost(_problemParameters.Cities, true);
            }
            return sample;
        }
    }
}