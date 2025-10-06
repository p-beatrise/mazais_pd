using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace Application
{
    class Program
    {
        public struct Variant
        {
            public string route;
            public int fitness;
            public int generation;

            public Variant(string route, int fitness, int generation)
            {
                this.route = route;
                this.fitness = fitness;
                this.generation = generation;
            }

        }

        public class TSP
        {
            public static List<string> GenerateAllRoutes(string cities)
            {
                var routes = new List<string>();

                foreach (var startCity in cities)
                {
                    var remaining = new string(cities.Where(c => c != startCity).ToArray());

                    foreach (var perm in GetPermutations(remaining))
                    {
                        string route = startCity + perm + startCity;    // TSP route start and end is the same
                        routes.Add(route);
                    }
                }
                Shuffle(routes);
                return routes;
            }

            public static IEnumerable<string> GetPermutations(string str)
            {
                if (str.Length == 1)
                    yield return str;
                else
                {
                    for (int i = 0; i < str.Length; i++)
                    {
                        char c = str[i];
                        string rest = str.Remove(i, 1);

                        foreach (var perm in GetPermutations(rest))
                            yield return c + perm;
                    }
                }
            }

            public static int CalculateRouteLength(int[,] map, string route)
            {
                int total = 0;

                for (int i = 0; i < route.Length - 1; i++)
                {
                    int from = route[i] - 'a';      //Get row
                    int to = route[i + 1] - 'a';    //Get column
                    total += map[from, to];
                }

                return total;
            }

            //Fitness is the same as route length
            public static int GetFitnessScore(string route, int[,] map)
            {
                int length = CalculateRouteLength(map, route);
                return length;
            }

            public class CrossoverHelper
            {
                public static List<Variant> GenerateChildren(List<Variant> parents, int[,] map, int gen, string possibleCities)
                {
                    var children = new List<Variant>();

                    for (int i = 0; i < parents.Count - 1; i += 2)
                    {
                        var p1 = parents[i];
                        var p2 = parents[i + 1];

                        var kids = Crossover(p1.route, p2.route, possibleCities);

                        foreach (var route in kids)
                        {
                            int fitness = CalculateRouteLength(map, route);
                            children.Add(new Variant(route, fitness, gen));
                        }
                    }

                    return children;
                }

                public static List<string> Crossover(string p1, string p2, string possibleCities)
                {
                    var kids = new List<string>();

                    for (int k = 0; k < 1; k++)
                    {
                        var (c1, c2) = HalfSplitCrossover(p1, p2, possibleCities);
                        kids.Add(c1);
                        kids.Add(c2);
                    }

                    return kids;
                }

                public static string MutateRouteToAvoidConsecutiveCities(string route, char city1, char city2)
                {
                    var character = route.ToCharArray();
                    Random rnd = new Random();

                    while (true)
                    {
                        bool isPair = false;

                        for (int i = 0; i < character.Length - 1; i++)
                        {
                            if (character[i] == city1 && character[i + 1] == city2)
                            {
                                isPair = true;

                                // Swap cities if they are in the route middle
                                if (i > 0 && i + 1 < character.Length - 1)
                                {
                                    char temp = character[i];
                                    character[i] = character[i + 1];
                                    character[i + 1] = temp;
                                }
                                else
                                {
                                    // Swap city that is not at the start or end of rute
                                    int swapIdx;
                                    do
                                    {
                                        swapIdx = rnd.Next(1, character.Length - 1);
                                    }
                                    while (swapIdx == i || swapIdx == i + 1);

                                    if (i == 0) // Case when pair city is at start
                                    {
                                        char temp = character[i + 1];
                                        character[i + 1] = character[swapIdx];
                                        character[swapIdx] = temp;
                                    }
                                    else if (i + 1 == character.Length - 1) // Case when pair city is at end
                                    {
                                        char temp = character[i];
                                        character[i] = character[swapIdx];
                                        character[swapIdx] = temp;
                                    }
                                }

                                character[0] = character[character.Length - 1];
                                break;
                            }
                        }

                        if (!isPair)
                            break;
                    }

                    return new string(character);
                }


                public static (string child1, string child2) HalfSplitCrossover(string parent1, string parent2, string possibleCities)
                {
                    int len = parent1.Length - 2; //Exclude start and end cities
                    int half = len / 2;

                    char startCity1 = parent1[0];
                    char endCity1 = parent1[parent1.Length - 1];

                    char startCity2 = parent2[0];
                    char endCity2 = parent2[parent2.Length - 1];

                    // Get city middle segment
                    string mid1 = parent1.Substring(1, len);
                    string mid2 = parent2.Substring(1, len);

                    // Split middle segments in half
                    string mid1FirstHalf = mid1.Substring(0, half);
                    string mid1SecondHalf = mid1.Substring(half);

                    string mid2FirstHalf = mid2.Substring(0, half);
                    string mid2SecondHalf = mid2.Substring(half);

                    // Create middle of new route by swapping halves
                    string child1Mid = mid1FirstHalf + mid2SecondHalf;
                    string child2Mid = mid2FirstHalf + mid1SecondHalf;

                    string newChild1 = startCity1 + child1Mid + endCity1;
                    string newChild2 = startCity2 + child2Mid + endCity2;

                    // Remove duplicates and preserve all cities exactly once
                    newChild1 = RemoveDuplicatesPreserveOrder(newChild1, possibleCities);
                    newChild2 = RemoveDuplicatesPreserveOrder(newChild2, possibleCities);

                    // Change route to awoid cities 'a' and 'b' in that order
                    newChild1 = MutateRouteToAvoidConsecutiveCities(newChild1, 'a', 'b');
                    newChild2 = MutateRouteToAvoidConsecutiveCities(newChild2, 'a', 'b');

                    return (newChild1, newChild2);
                }

                public static string RemoveDuplicatesPreserveOrder(string route, string allCities)
                {
                    char startCity = route.First();
                    char endCity = route.Last();

                    var middle = route.Substring(1, route.Length - 2).ToCharArray();

                    var validCities = new HashSet<char>(allCities.Where(c => c != startCity));      //Get cities that are tot at the start and end

                    var seen = new HashSet<char>();
                    var missing = new HashSet<char>(validCities);

                    for (int i = 0; i < middle.Length; i++)
                    {
                        char c = middle[i];

                        if (c == startCity || seen.Contains(c))
                        {
                            if (missing.Count > 0)
                            {
                                char replacement = missing.First();
                                middle[i] = replacement;
                                seen.Add(replacement);
                                missing.Remove(replacement);
                            }
                        }
                        else
                        {
                            seen.Add(c);
                            missing.Remove(c);
                        }
                    }

                    return startCity + new string(middle) + endCity;
                }

            }

            public static List<string> FilterValidRoutes(List<string> routes)
            {
                return routes
                    .Where(route => route.First() == route.Last())
                    .ToList();
            }

            static Random random = new Random();

            private static void Shuffle<T>(IList<T> list)
            {
                for (int i = list.Count - 1; i > 0; i--)
                {
                    int j = RandomNumberGenerator.GetInt32(i + 1); // 0..i
                    (list[i], list[j]) = (list[j], list[i]);
                }
            }
            public void GeneticAlgorithmExecution(int[,] map, string possibleCities)
            {
                // Generation Number
                int gen = 1;

                List<Variant> existingPopulation = new List<Variant>();
                Variant temp;

                //string input = "abcde"; //Possible city names test case 1
                //string input = "abcdefg"; //Possible city names test case 2
                //string input = "abcdefghi"; //Possible city names test case 2

                var allRoutes = GenerateAllRoutes(possibleCities); //Create TSP like routes
                var validRoutes = FilterValidRoutes(allRoutes);

                foreach (var s in validRoutes)
                {
                    temp.route = s;
                    temp.fitness = GetFitnessScore(temp.route, map);
                    temp.generation = 0;
                    existingPopulation.Add(temp);
                }

                var bestVariant = existingPopulation
                    .OrderBy(ind => ind.fitness)
                    .First();

                var worstVariant = existingPopulation
                    .OrderBy(ind => ind.fitness)
                    .Last();

                Console.WriteLine("Population count: " + validRoutes.Count + "; Base population ");
                Console.WriteLine("Average fitness of population: " + existingPopulation.Average(i => i.fitness));
                Console.WriteLine("Best fitness score in population: " + bestVariant.fitness);
                Console.WriteLine("Worst fitness score in population: " + worstVariant.fitness);
                Console.WriteLine();

                var watchOverall = System.Diagnostics.Stopwatch.StartNew();

                double? previousAvgFitness = null;

                while (existingPopulation.Count()>2)
                {
                    existingPopulation = existingPopulation.OrderBy(x => x.fitness).ToList();

                    List<Variant> new_population = new List<Variant>();
                    var halfList = existingPopulation.Take((existingPopulation.Count + 1) / 2).ToList();    //Get half of the population to crossover paths with better length
                    Shuffle(halfList);

                    var children = CrossoverHelper.GenerateChildren(halfList, map, gen, possibleCities);

                    bestVariant = children
                    .OrderBy(ind => ind.fitness)
                    .First();

                    worstVariant = children
                    .OrderBy(ind => ind.fitness)
                    .Last();

                    bool hasAb = children.Any(x => x.route.Contains("ab")); //Check if ab comes one after another in any route
                    var currentAvgFitness = children.Average(i => i.fitness);

                    Console.WriteLine("Generation " + gen + "; Average fitness of population: " + currentAvgFitness);
                    Console.WriteLine("Best fitness score in population: " + bestVariant.fitness);
                    Console.WriteLine("Worst fitness score in population: " + worstVariant.fitness);
                    Console.WriteLine("Contains ab: " + hasAb);
                    if (previousAvgFitness.HasValue && currentAvgFitness > previousAvgFitness)
                    {
                        Console.WriteLine($"Population has a bigger average fitness ({currentAvgFitness}) than previous ({previousAvgFitness})");
                    }
                    previousAvgFitness = currentAvgFitness;

                    existingPopulation = children;
                    Console.WriteLine();
                    Console.WriteLine("Population count: " + existingPopulation.Count + "; Generation: " + gen);

                    gen++;
                }
                watchOverall.Stop();
                var elapsedMs2 = watchOverall.ElapsedMilliseconds;
                Console.WriteLine("Overall calculation time in ms: " + elapsedMs2);
            }

        }

        static void Main()
        {
            var tsp = new TSP();
        
            //Map for test case 1
            int[,] map1 = new int[,] { { 0, 2, 1, 12, 5 },
                                                 { 2, 0, 4, 8, 1 },
                                                 { 1, 4, 0, 3, 3 },
                                                 { 12, 8, 3, 0, 10 },
                                                 { 5, 1, 3, 10, 0 } };

            string input1 = "abcde"; //Possible city names test case 1
            Console.WriteLine("1st test case start");
            Console.WriteLine();
            tsp.GeneticAlgorithmExecution(map1, input1);
            Console.WriteLine();
            Console.WriteLine("1st test case end");
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("–----------------------------------------------------------");
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();
            //Map for test case 2
            int[,] map2 = new int[,]
        {
            { 0, 12, 25, 30, 18, 40, 22 },
            { 12, 0, 14, 28, 16, 35, 20 },
            { 25, 14, 0, 26, 10, 22, 18 },
            { 30, 28, 26, 0, 24, 15, 27 },
            { 18, 16, 10, 24, 0, 20, 14 },
            { 40, 35, 22, 15, 20, 0, 32 },
            { 22, 20, 18, 27, 14, 32, 0 }
        };
            string input2 = "abcdefg"; //Possible city names test case 2
            Console.WriteLine("2nd test case start");
            Console.WriteLine();
            tsp.GeneticAlgorithmExecution(map2, input2);
            Console.WriteLine();
            Console.WriteLine("2nd test case end");
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("–----------------------------------------------------------");
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();
            //Map for test case 3

            int[,] map3 = new int[,]
            {
                {  0, 29, 20, 21, 16, 31, 100, 12,  4 },
                { 29,  0, 15, 29, 28, 40,  72, 21, 29 },
                { 20, 15,  0, 15, 14, 25,  81,  9, 23 },
                { 21, 29, 15,  0,  4, 12,  92, 12, 25 },
                { 16, 28, 14,  4,  0, 16,  94,  9, 20 },
                { 31, 40, 25, 12, 16,  0,  95, 24, 36 },
                {100, 72, 81, 92, 94, 95,   0, 90, 101},
                { 12, 21,  9, 12,  9, 24,  90,  0, 15 },
                {  4, 29, 23, 25, 20, 36, 101, 15,  0 }
            };
            
            string input3 = "abcdefghi"; //Possible city names test case 3
           
            Console.WriteLine("3rd test case start"); Console.WriteLine();
            tsp.GeneticAlgorithmExecution(map3, input3);
            Console.WriteLine();
            Console.WriteLine("3rd test case end");
        }
    }


}
