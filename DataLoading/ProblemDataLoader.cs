using ACO.DataLoading;
using System.Reflection;
using System.Xml.Linq;

namespace ACO
{
    internal class ProblemDataLoader
    {
        public static CVRPData LoadVRPD(string path)
        {
            var data = LoadByReflection<CVRPData>(path);
            var streamReader = new StreamReader(path);
            var content = streamReader.ReadToEnd();

            var words = content.Split(
                new string[] { Environment.NewLine, " " },
                StringSplitOptions.None
            );

            var section = "";

            for (var i = 0; i < words.Length; i++)
            {
                if (string.IsNullOrEmpty(words[i]))
                    continue;

                if (words[i] == "EDGE_WEIGHT_SECTION" || words[i] == "NODE_COORD_SECTION")
                {
                    section = "edge";
                    continue;
                }

                if (words[i] == "DEMAND_SECTION")
                {
                    section = "demands";
                    continue;
                }

                if (words[i] == "DEPOT_SECTION" || words[i] == "EOF")
                    return data;

                switch (section)
                {
                    case "edge":
                        data.EdgeWeights = LoadEdges(words, data).EdgeWeights;
                        section = "";
                        i--;
                        break;

                    case "demands":
                        data.Demands = new int[data.Dimension];
                        for (var j = 0; j < data.Dimension; j++)
                        {
                            while (string.IsNullOrEmpty(words[i]))
                                i++;
                            i++;
                            while (string.IsNullOrEmpty(words[i]))
                                i++;
                            data.Demands[j] = int.Parse(words[i]);
                            i++;
                        }
                        i--;
                        break;

                    default:
                        continue;
                }
            }

            return data;
        }

        public static T LoadByReflection<T>(string path) where T : new()
        {
            var data = new T();

            var fileData = File.ReadAllText(path);
            var lines = fileData.Split(
                new string[] { Environment.NewLine },
                StringSplitOptions.None
            );

            var type = typeof(T);
            var fields = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var line in lines)
            {
                var words = line.Split(' ');
                if (words.Length == 3)
                {
                    var fieldName = words[0];
                    var fieldValue = words[2];

                    var field = fields.FirstOrDefault(field => field.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));

                    if (int.TryParse(fieldValue, out var intValue))
                        field?.SetValue(data, intValue);
                    else
                        field?.SetValue(data, fieldValue);
                }
            }

            return data;
        }

        private static ProblemData LoadEdges(string[] words, ProblemData problem)
        {
            var edgeSection = false;
            var loaded = false;
            for (var i = 0; !loaded; i++)
            {
                var word = words[i];

                if (string.IsNullOrEmpty(words[i]))
                    continue;
                if (words[i] == "EDGE_WEIGHT_SECTION" || words[i] == "NODE_COORD_SECTION")
                {
                    edgeSection = true;
                    continue;
                }
                if (!edgeSection)
                    continue;

                switch (problem.EDGE_WEIGHT_TYPE)
                {
                    case "EXPLICIT":
                        switch (problem.EDGE_WEIGHT_FORMAT)
                        {
                            case "FULL_MATRIX":
                                LoadExplictFull(ref problem);
                                loaded = true;
                                break;

                            case "LOWER_ROW":
                                LoadExplicitLowerRow(ref problem);
                                loaded = true;
                                break;
                        }
                        break;

                    case "EUC_2D":
                        LoadEuc2D(ref problem);
                        loaded = true;
                        break;
                }

                void LoadExplicitLowerRow(ref ProblemData problem)
                {
                    problem.EdgeWeights = new int[problem.Dimension, problem.Dimension];
                    for (var col = 0; col < problem.Dimension - 1; col++)
                        for (var row = col + 1; row < problem.Dimension; row++)
                        {
                            while (string.IsNullOrEmpty(words[i]))
                                i++;
                            var edge = int.Parse(words[i]);
                            problem.EdgeWeights[col, row] = edge;
                            problem.EdgeWeights[row, col] = edge;
                            i++;
                        }
                }

                void LoadExplictFull(ref ProblemData problem)
                {
                    problem.EdgeWeights = new int[problem.Dimension, problem.Dimension];
                    for (var col = 0; col < problem.Dimension; col++)
                        for (var row = 0; row < problem.Dimension; row++)
                        {
                            while (string.IsNullOrEmpty(words[i]))
                                i++;
                            var edge = int.Parse(words[i]);
                            problem.EdgeWeights[col, row] = edge;
                            i++;
                        }
                }

                void LoadEuc2D(ref ProblemData problem)
                {
                    problem.EdgeWeights = new int[problem.Dimension, problem.Dimension];
                    var points = new int[problem.Dimension, 2];
                    for (var node = 0; node < problem.Dimension; node++)
                    {
                        while (string.IsNullOrEmpty(words[i]))
                            i++;
                        i++;
                        while (string.IsNullOrEmpty(words[i]))
                            i++;
                        points[node, 0] = int.Parse(words[i]);
                        i++;
                        while (string.IsNullOrEmpty(words[i]))
                            i++;
                        points[node, 1] = int.Parse(words[i]);
                        i++;
                    }
                    for (var row = 0; row < problem.Dimension - 1; row++)
                        for (var col = row + 1; col < problem.Dimension; col++)
                        {
                            var weight = (int)(Math.Sqrt(Math.Pow(points[row, 0] - points[col, 0], 2) + Math.Pow(points[row, 1] - points[col, 1], 2)) + 0.5);
                            problem.EdgeWeights[row, col] = weight;
                            problem.EdgeWeights[col, row] = weight;
                        }
                }
            }
            return problem;
        }

        public static ProblemData LoadXml(string path)
        {
            var problemData = new ProblemData();
            var xml = XDocument.Load(path);

            var elements = xml.Elements().Elements();

            problemData.Name = elements.Named("name");
            problemData.Description = elements.Named("description");
            problemData.Dimension = int.Parse(elements.Named("size"));
            problemData.EdgeWeights = new int[problemData.Dimension, problemData.Dimension];
            problemData.Optimum = int.Parse(elements.Named("optimal"));

            var doublePrecision = elements.Named("doublePrecision");
            var ignoredDigits = elements.Named("ignoredDigits");

            var vertices = elements.First(e => e.Name == "graph").Elements();

            var vertexIndex = 0;
            IFormatProvider formatProvider = new System.Globalization.NumberFormatInfo
            {
                NumberDecimalSeparator = ".",
                NumberDecimalDigits = 3
            };
            foreach (var vertex in vertices)
            {
                foreach (var edge in vertex.Elements())
                {
                    var cost = (int)(double.Parse(edge.Attribute("cost").Value, formatProvider) + 0.5d);
                    problemData.EdgeWeights[vertexIndex, int.Parse(edge.Value)] = cost;
                }
                vertexIndex++;
            }

            return problemData;
        }
    }
}

internal static class XmlExtensions
{
    internal static string Named(this IEnumerable<XElement> xes, string name)
    {
        return xes.First(e => e.Name == name).Value;
    }
}