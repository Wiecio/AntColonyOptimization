using ACO.Shared;
using System.Xml.Serialization;

namespace ACO.Algorithms.AV
{
    internal class ACOAV
    {
        private TSPParameters _problemParameters;
        private ACOAVParameters _acoParameters;

        public ACOAV(TSPParameters problemParameters, ACOAVParameters acoParameters)
        {
            _problemParameters = problemParameters;
            _acoParameters = acoParameters;
        }

        public Result Solve()
        {
            Console.WriteLine("Starting new solving session of ACOAV");
            var population = InitializePopulation();
            var pheromones = InitializePheromoneTrails();

            var best = int.MaxValue;
            var noImprovementIterations = 0;

            for (var i = 0; i < _acoParameters.MaxIterations; i++)
            {
                var improved = false;
                for (var j = 0; j < population.Routes.Length; j++)
                {
                    UpdateSolution(ref population.Routes[j], pheromones);
                    UpdatePheromone(ref pheromones, population.Routes[j]);
                    LocalSearch(ref population.Routes[j]);

                    var cost = population.Routes[j].CalculateCost(_problemParameters.Cities);
                    if (cost <= _problemParameters.Optimum)
                    {
                        Console.WriteLine("Optimum found, returning...");
                        return new Result(cost, i);
                    }
                    if (cost < best)
                    {
                        best = cost;
                        improved = true;
                    }
                }

                if (improved)
                {
                    noImprovementIterations = 0;
                    Console.WriteLine($"Improved solution at iteration {i}");
                    Console.WriteLine(best);
                    continue;
                }
                else
                {
                    noImprovementIterations++;
                    if (noImprovementIterations >= _acoParameters.MaxNoImprovementIterations)
                    {
                        Console.WriteLine("No improvement, returning...");
                        return new Result(best, i);
                    }
                }

                if (i % 100 == 0)
                {
                    Console.WriteLine(i);
                }
            }

            return new Result(best, _acoParameters.MaxIterations);
        }

        private Population InitializePopulation()
        {
            var population = new Population(_acoParameters.PopulationSize);
            for (var i = 0; i < population.Routes.Length; i++)
            {
                population.Routes[i] = new Route(_problemParameters.CitiesNumber);

                var visitedCities = new bool[_problemParameters.CitiesNumber];
                for (var j = 0; j < population.Routes[i].Cities.Length; j++)
                {
                    var newCity = Random.Shared.Next(_problemParameters.CitiesNumber);

                    while (visitedCities[newCity] == true)
                    {
                        newCity = Random.Shared.Next(_problemParameters.CitiesNumber);
                    }

                    population.Routes[i].Cities[j] = newCity;
                    visitedCities[newCity] = true;
                }
            }
            return population;
        }

        private PheromoneTrails InitializePheromoneTrails()
        {
            var pheromones = new PheromoneTrails(_problemParameters.CitiesNumber);
            for (var i = 0; i < pheromones.Pheromones.GetLength(0); i++)
            {
                for (var j = 0; j < pheromones.Pheromones.GetLength(1); j++)
                {
                    pheromones.Pheromones[i, j] = _acoParameters.t0;
                }
            }
            return pheromones;
        }

        internal void UpdateSolution(ref Route originalSolution, PheromoneTrails pheromones)
        {
            var tempSolution = originalSolution.Copy();
            var r1 = Random.Shared.Next(_problemParameters.CitiesNumber);
            var r2 = Random.Shared.Next(_problemParameters.CitiesNumber);

            var r = r1 + 1;
            var citiesToOrder = new HashSet<int>();

            if (r == _problemParameters.CitiesNumber)
                r = 0;

            while (r != r2)
            {
                citiesToOrder.Add(tempSolution.Cities[r]);
                if (r < _problemParameters.CitiesNumber - 1)
                    r++;
                else
                    r = 0;
            }

            r = r1 + 1;

            var s = tempSolution.Cities[r1];
            var e = tempSolution.Cities[r2];

            if (r == _problemParameters.CitiesNumber)
                r = 0;

            while (citiesToOrder.Count != 0)
            {
                tempSolution.Cities[r] = ChooseCity();
                s = tempSolution.Cities[r];
                citiesToOrder.Remove(s);
                if (r < _problemParameters.CitiesNumber - 1)
                    r++;
                else
                    r = 0;
            }

            if (tempSolution.CalculateCost(_problemParameters.Cities) < originalSolution.CalculateCost(_problemParameters.Cities))
                originalSolution = tempSolution;

            int ChooseCity()
            {
                var adaptiveDistances = GetAdaptiveDistances();
                var scaledAdaptiveDistance = GetScaledAdaptiveDistances();
                var probabilities = CalculateProbabilites();

                return citiesToOrder.ElementAt(FindIndexOfMax());

                double[] GetAdaptiveDistances()
                {
                    var adaptiveDistances = new double[citiesToOrder.Count];
                    for (var i = 0; i < citiesToOrder.Count; i++)
                    {
                        adaptiveDistances[i] = _acoParameters.w1 * _problemParameters.Cities.ElementAt(s).Distances.ElementAt(i)
                            - _acoParameters.w2 * _problemParameters.Cities.ElementAt(i).Distances.ElementAt(e); // f_sle = w1 * d_sl - w2 * d_le
                    }
                    return adaptiveDistances;
                }

                double[] GetScaledAdaptiveDistances()
                {
                    var minimum = adaptiveDistances.Min();
                    return adaptiveDistances.Select(ad => ad - minimum + 1).ToArray();
                }

                IEnumerable<double> CalculateProbabilites()
                {
                    var sum = 0d;
                    var probabilites = new double[citiesToOrder.Count];

                    for (var i = 0; i < citiesToOrder.Count; i++)
                    {
                        probabilites[i] = Math.Pow(pheromones.Pheromones[s, citiesToOrder.ElementAt(i)], _acoParameters.Alpha)
                           * Math.Pow(1d / scaledAdaptiveDistance[i], _acoParameters.Beta);

                        sum += probabilites[i];
                    }

                    return probabilites.Select(p => p / sum);
                }

                int FindIndexOfMax()
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
            }
        }

        private void UpdatePheromone(ref PheromoneTrails pheromoneTrails, Route route)
        {
            Evaporate(ref pheromoneTrails);
            LeaveTrace(ref pheromoneTrails);

            void Evaporate(ref PheromoneTrails pheromoneTrails)
            {
                for (var i = 0; i < pheromoneTrails.Pheromones.GetLength(0); i++)
                {
                    for (var j = 0; j < pheromoneTrails.Pheromones.GetLength(1); j++)
                    {
                        pheromoneTrails.Pheromones[i, j] *= (1 - _acoParameters.Rho);
                        pheromoneTrails.Pheromones[j, i] *= (1 - _acoParameters.Rho);
                    }
                }
            }

            void LeaveTrace(ref PheromoneTrails pheromoneTrails)
            {
                var trace = 1d / route.CalculateCost(_problemParameters.Cities);
                for (var j = 0; j < route.Cities.Length - 1; j++)
                {
                    pheromoneTrails.Pheromones[route.Cities[j], route.Cities[j + 1]]
                        += trace;

                    pheromoneTrails.Pheromones[route.Cities[j + 1], route.Cities[j]]
                       += trace;
                }
            }
        }

        private void LocalSearch(ref Route route)
        {
            Optimizer3Opt.ThreeOpt(ref route, _problemParameters);
        }

        private int FindBest(Population population)
        {
            return population
                .Routes
                .Min(r => r
                    .CalculateCost(_problemParameters.Cities));
        }
    }
}