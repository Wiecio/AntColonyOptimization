using ACO.Algorithms.AV;

namespace ACO.Shared
{
    internal static class Optimizer3Opt
    {
        public static void ThreeOpt(ref Route tour, TSPParameters problemParameters)
        {
            Route bestTour = tour.Copy();
            foreach (var (i, j, k) in GetAllSegments(tour.Cities.Length))
            {
                var newTour = tour.Copy();
                if (MakeBest3OptMove(ref newTour, i, j, k, problemParameters))
                {
                    if (newTour.CalculateCost(problemParameters.Cities) <= bestTour.CalculateCost(problemParameters.Cities))
                        bestTour = newTour;
                }
            }
            tour = bestTour;
        }

        private static IEnumerable<(int, int, int)> GetAllSegments(int n)
        {
            for (int i = 1; i < n; i++)
            {
                for (int j = i + 2; j < n; j++)
                {
                    for (int k = j + 2; k < n + (i > 0 ? 1 : 0); k++)
                    {
                        yield return (i, j, k);
                    }
                }
            }
        }

        private static bool MakeBest3OptMove(ref Route tour, int i, int j, int k, TSPParameters tSPParameters)
        {
            var A = tour.Cities[i - 1];
            var B = tour.Cities[i];
            var C = tour.Cities[j - 1];
            var D = tour.Cities[j];
            var E = tour.Cities[k - 1];
            var F = tour.Cities[k % tour.Cities.Length];

            var d0 = tSPParameters.GetDistance(A, B) + tSPParameters.GetDistance(C, D) + tSPParameters.GetDistance(E, F); //abc
            var d1 = tSPParameters.GetDistance(A, C) + tSPParameters.GetDistance(B, D) + tSPParameters.GetDistance(E, F); //a-bc
            var d2 = tSPParameters.GetDistance(A, B) + tSPParameters.GetDistance(C, E) + tSPParameters.GetDistance(D, F); //ab-c
            var d3 = tSPParameters.GetDistance(A, D) + tSPParameters.GetDistance(E, B) + tSPParameters.GetDistance(C, F); //acb
            var d4 = tSPParameters.GetDistance(F, B) + tSPParameters.GetDistance(C, D) + tSPParameters.GetDistance(E, A); //-abc aka a-c-b
            var d5 = tSPParameters.GetDistance(A, E) + tSPParameters.GetDistance(D, B) + tSPParameters.GetDistance(C, F); // a-cb
            var d6 = tSPParameters.GetDistance(A, D) + tSPParameters.GetDistance(E, C) + tSPParameters.GetDistance(B, F); // ac-b
            var d7 = tSPParameters.GetDistance(A, C) + tSPParameters.GetDistance(B, E) + tSPParameters.GetDistance(D, F); // A-B-C

            var distances = new[] { d1, d2, d3, d4, d5, d6, d7 };
            var min = distances.Min();

            if (min >= d0)
                return false;

            if (min == d1)//A-BC
            {
                tour.Reverse(i, j - 1);
                return true; ;
            }
            else if (min == d2)//AB-C
            {
                tour.Reverse(j, k - 1);
                return true;
            }
            else if (min == d3)//ACB
            {
                tour.SwapRanges(i, j, k);

                return true;
            }
            else if (min == d4)//A-C-B
            {
                tour.Reverse(i, k - 1);
                return true;
            }
            else if (min == d5)// a-cb
            {
                tour.Reverse(j, k - 1);
                tour.SwapRanges(i, j, k);
                return true;
            }
            else if (min == d6) //ac - b
            {
                tour.Reverse(i, j - 1);
                tour.SwapRanges(i, j, k);
                return true;
            }
            else if (min == d7) //A - B - C
            {
                tour.Reverse(i, j - 1);
                tour.Reverse(j, k - 1);
                return true;
            }
            return false;
        }
    }
}