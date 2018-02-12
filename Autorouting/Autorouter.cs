﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Idlorio.Autorouting
{
    class Autorouter
    {
        public static void Autoroute(Map originalMap, Net originalNet)
        {
            var ret = GetAutoroutingSolution(originalMap, originalNet);

            if (ret == null)
                return;

            foreach (Net net in ret.Keys)
            {
                foreach (Tile t in net.Tiles)
                    t.Net = null;
                net.Tiles.Clear();
            }

            foreach (var netRouting in ret)
            {
                foreach(Point p in netRouting.Value)
                {
                    netRouting.Key.Tiles.Add(originalMap.tiles[p.X, p.Y]);
                    originalMap.tiles[p.X, p.Y].Net = netRouting.Key;
                }
            }

            if (!originalMap.Nets.Contains(originalNet))
                originalMap.Nets.Add(originalNet);
        }

        static List<Net> GetNetsInTheWay(Map originalMap, Net netToRoute, out bool routingComplete)
        {
            AutoroutingMap map = new AutoroutingMap(originalMap);
            AutoroutingNet net = new AutoroutingNet();

            net.Start = map.tiles[netToRoute.Start.X, netToRoute.Start.Y];
            net.End = map.tiles[netToRoute.End.X, netToRoute.End.Y];
            
            List<AutoroutingNet> netIdsInTheWay = new List<AutoroutingNet>();

            var autorouteResult = Autoroute(map, net, netIdsInTheWay);
            routingComplete = autorouteResult != null;

            List<Net> ret = netIdsInTheWay.Select(x => originalMap.tiles[x.Start.X, x.Start.Y].Net).ToList();

            if (originalMap.tiles[netToRoute.Start.X, netToRoute.Start.Y].Net != null && !ret.Contains(originalMap.tiles[netToRoute.Start.X, netToRoute.Start.Y].Net))
                ret.Add(originalMap.tiles[netToRoute.Start.X, netToRoute.Start.Y].Net);
            if (originalMap.tiles[netToRoute.End.X, netToRoute.End.Y].Net != null && !ret.Contains(originalMap.tiles[netToRoute.End.X, netToRoute.End.Y].Net))
                ret.Add(originalMap.tiles[netToRoute.End.X, netToRoute.End.Y].Net);

            return ret;
        }

        public static Dictionary<Net, List<Point>> GetAutoroutingSolution(Map originalMap, Net originalNet)
        {
            originalMap.RemoveNet(originalNet);
            
            
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            bool autorouteSuccess = false;
            List<Net> netIdsInTheWay = GetNetsInTheWay(originalMap, originalNet, out autorouteSuccess);
            
            int bestCost = Int32.MaxValue;
            Net[] bestPermutation = null;
            List<List<Point>> bestRouting = null;

            List<Net> netsToPermute = new List<Net>();
            netsToPermute.AddRange(netIdsInTheWay);
            netsToPermute.Add(originalNet);

            Parallel.ForEach<IEnumerable<Net>>(netsToPermute.GetPermutations(), permutation =>
            {
                /*if (stopwatch.ElapsedMilliseconds >= 50 && (bestCost < Int32.MaxValue || autorouteSuccess))
                    return;
                else if (stopwatch.ElapsedMilliseconds >= 500)
                    return;*/

                AutoroutingMap possibleMap = new AutoroutingMap(originalMap);
                List<AutoroutingNet> possibleNets = new List<AutoroutingNet>();

                AutoroutingNet originalNet2 = new AutoroutingNet();
                originalNet2.Start = possibleMap.tiles[originalNet.Start.X, originalNet.Start.Y];
                originalNet2.End = possibleMap.tiles[originalNet.End.X, originalNet.End.Y];

                foreach (Net x in netIdsInTheWay)
                    possibleMap.RipupNet(x);
                possibleMap.RipupNet(originalNet2);
                
                int cost = 0;

                List<List<Point>> routing = new List<List<Point>>();

                foreach (Net n in permutation)
                {
                    List<Point> result = Autoroute(possibleMap, possibleMap.tiles[n.Start.X, n.Start.Y].Net);

                    if (result == null)
                        return; //Bad permutation, can't route everything
                    routing.Add(result);
                    cost += result.Count;
                }

                if (cost < bestCost)
                {
                    bestCost = cost;
                    bestRouting = routing;
                    bestPermutation = permutation.ToArray();
                }
            });

            Dictionary<Net, List<Point>> ret = new Dictionary<Net, List<Point>>();

            if (bestCost != Int32.MaxValue)
            {
                for (int i = 0; i < bestPermutation.Length; i++)
                {
                    if (bestPermutation[i] == originalNet)
                        ret.Add(originalNet, bestRouting[i]);
                    else
                        ret.Add(originalMap.tiles[bestPermutation[i].Start.X, bestPermutation[i].Start.Y].Net, bestRouting[i]);
                }
                
                return ret;
            }
            else
            {
                return null;
            }
        }

        static List<Point> Autoroute(AutoroutingMap map, AutoroutingNet net, List<AutoroutingNet> netIdsInTheWay = null)
        {
            Func<Point, float> costEvaluator = delegate (Point to)
            {
                if (map.tiles[to.X, to.Y].Net != null)
                {
                    if (netIdsInTheWay != null && !netIdsInTheWay.Contains(map.tiles[to.X, to.Y].Net))
                        netIdsInTheWay.Add(map.tiles[to.X, to.Y].Net);

                    return float.PositiveInfinity;
                }
                else
                    return 1;
            };

            if (net.Start.Net != null && net.Start.Net != net)
                return null;
            if (net.End.Net != null && net.End.Net != net)
                return null;

            net.Start.Net = null;
            net.End.Net = null;

            var aStarResult = PolishedAStar.Find(map.Width, map.Height, costEvaluator, net.Start.X, net.Start.Y, net.End.X, net.End.Y);

            if(aStarResult != null)
                foreach (Point p in aStarResult)
                    map.tiles[p.X, p.Y].Net = net;

            return aStarResult;
        }

    }
}
