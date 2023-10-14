namespace ACO.Shared
{
    internal struct City
    {
        private const int _inf = 1000000;

        public int Index { get; init; }
        public int Demand { get; init; }
        public IEnumerable<int> Distances { get; init; }

        public bool IsTraversableTo(int index)
        {
            var distance = Distances.ElementAt(index);
            return distance >= 0 && distance < _inf;
        }

        public bool IsAfterCities(IEnumerable<City> cities)
        {
            var thisCity = this;
            return cities.All(c => thisCity.IsTraversableTo(c.Index));
        }
    }
}