using ACO.Algorithms.AV;
using ACO.Algorithms.EACSSA.SA;
using System.Diagnostics;

namespace ACO.Shared
{
    internal class OptimizerSop3
    {
        private readonly LocalSA _sa;
        private IList<int> _samples = new List<int>();

        public OptimizerSop3(LocalSA localSA)
        {
            _sa = localSA;
        }

        public int Sop3Exchange(ref int[] tour, TSPParameters instance)
        {
            var n = tour.Length;

            int bh = 0, bi = 0, bj = 0;
            var mark = new int[n];
            var stack = new List<int>();
            var used = new bool[n];
            int count_h = 1;

            // init stack
            stack.AddRange(Enumerable.Range(0, n - 1));

            for (var i = 0; i < n - 1; i++)
            {
                used[i] = true;
            }

            var improvements = 0;
            bool gainExists = true;

            while (gainExists)
            {
                gainExists = false;
                // randomizeStack(stack);

                while (!gainExists && stack.Count > 0)
                {
                    var h = stack.Last();
                    stack.RemoveAt(stack.Count - 1);
                    Debug.Assert(h < n);

                    count_h++;
                    var bestGain = 0;

                    used[h] = false;
                    for (var i = h + 1; i + 1 < n; i++)
                    {
                        var bestj = n;
                        var cannotAdd = false;

                        foreach (var node in instance.GetOutcomingEdges(tour[i]))
                        {
                            mark[node] = count_h;
                        }
                        for (var j = i + 1; j + 1 < n && !cannotAdd; j++)
                        {
                            var res = 0;

                            // feasible
                            if (mark[tour[j]] == count_h)
                            {
                                cannotAdd = true;
                            }
                            else
                            {
                                // calculate gain
                                res += instance.GetDistance(tour[h], tour[h + 1]);
                                res += instance.GetDistance(tour[i], tour[i + 1]);
                                res += instance.GetDistance(tour[j], tour[j + 1]);

                                res -= instance.GetDistance(tour[h], tour[i + 1]);
                                res -= instance.GetDistance(tour[j], tour[h + 1]);
                                res -= instance.GetDistance(tour[i], tour[j + 1]);
                                if (IsMoveAccepted(res, bestGain))
                                {
                                    bestj = j;
                                    bestGain = res;
                                    bh = h;
                                    bi = i;
                                    bj = j;
                                }
                            }
                        }
                        if (bestj != n) break;
                    }

                    // Apply the exchange
                    if (bestGain > 0)
                    {
                        improvements++;

                        tour = SwapAdjSubsequences(tour.ToList(), bh + 1, bi + 1, bj + 1);

                        gainExists = true;
                        Debug.Assert(bi + 1 < n);
                        Debug.Assert(bh + 1 < n);
                        Debug.Assert(bj + 1 < n);
                        if (!used[bj + 1]) { stack.Add(bj + 1); used[bj + 1] = true; }
                        if (!used[bj]) { stack.Add(bj); used[bj] = true; }
                        if (!used[bi + 1]) { stack.Add(bi + 1); used[bi + 1] = true; }
                        if (!used[bi]) { stack.Add(bi); used[bi] = true; }
                        if (!used[bh + 1]) { stack.Add(bh + 1); used[bh + 1] = true; }
                        if (!used[bh]) { stack.Add(bh); used[bh] = true; }

                        continue;
                    }

                    count_h++;

                    // Backward search
                    bestGain = 0;
                    if (h + 1 == n || h < 2)
                    {
                        continue;
                    }
                    for (var i = h - 1; i >= 1; --i)
                    {
                        var bestj = n;
                        var cannotAdd = false;

                        foreach (var node in instance.GetIncomingEdges(tour[i + 1]))
                        {
                            mark[node] = count_h;
                        }
                        // Special case for backward direction
                        // if (mark[tour[i]] == count_h) {
                        //     cannotAdd = true;
                        // }
                        for (int j = i - 1, o = i; o >= 1 && !cannotAdd; --j, --o)
                        {
                            var res = 0;

                            // feasible
                            if (mark[tour[j + 1]] == count_h)
                            {
                                cannotAdd = true;
                            }
                            else
                            {
                                // calculate gain
                                Debug.Assert(h + 1 < n);
                                // [...j] [j+1...i] [i+1..h] [h+1 ...]
                                // [...j] [i+1..h] [j+1...i] [h+1 ...]
                                res += instance.GetDistance(tour[h], tour[h + 1]);
                                res += instance.GetDistance(tour[i], tour[i + 1]);
                                res += instance.GetDistance(tour[j], tour[j + 1]);

                                res -= instance.GetDistance(tour[i], tour[h + 1]);
                                res -= instance.GetDistance(tour[j], tour[i + 1]);
                                res -= instance.GetDistance(tour[h], tour[j + 1]);

                                if (IsMoveAccepted(res, bestGain))
                                {
                                    bestj = j;
                                    bestGain = res;
                                    bh = h;
                                    bi = i;
                                    bj = j;
                                }
                            }
                        }
                        if (bestj != n) break;
                    }

                    // Apply the exchange
                    if (bestGain != 0)
                    {
                        improvements++;

                        tour = SwapAdjSubsequences(tour.ToList(), bj + 1, bi + 1, bh + 1);

                        gainExists = true;

                        Debug.Assert(bh + 1 < n);
                        Debug.Assert(bi + 1 < n);
                        Debug.Assert(bj + 1 < n);
                        if (!used[bh + 1]) { stack.Add(bh + 1); used[bh + 1] = true; }
                        if (!used[bh]) { stack.Add(bh); used[bh] = true; }
                        if (!used[bi + 1]) { stack.Add(bi + 1); used[bi + 1] = true; }
                        if (!used[bi]) { stack.Add(bi); used[bi] = true; }
                        if (!used[bj + 1]) { stack.Add(bj + 1); used[bj + 1] = true; }
                        if (!used[bj]) { stack.Add(bj); used[bj] = true; }
                    }
                }
            }
            return improvements;
        }

        private bool IsMoveAccepted(int change, int bestChange)
        {
            var delta = change - bestChange;
            var accept = false;
            if (delta > 0)
            {
                accept = true;
            }
            else if (delta == 0 && Random.Shared.NextSingle() < 0.1f)
            {
                accept = true;
            }
            else
            {
                if (_sa.IsInitialized && _sa.Temperature > 0)
                {
                    var probability = Math.Pow(Math.E, delta / _sa.Temperature);
                    if (Random.Shared.NextSingle() < probability)
                    {
                        accept = true;
                    }
                    _sa.UpdateTemperature();
                }
                else
                {
                    _samples.Add(-delta);
                    if (_samples.Count >= 10000)
                    {
                        _sa.Initialize(_samples);
                    }
                }
            }
            return accept;
        }

        private int[] SwapAdjSubsequences(List<int> v, int start, int mid, int end)
        {
            Debug.Assert(end <= v.Count);
            Debug.Assert(start < mid && mid < end);

            var subsequence = v.GetRange(start, mid - start);
            v.RemoveRange(start, mid - start);
            v.InsertRange(end - mid + start, subsequence);
            return v.ToArray();
        }
    }
}