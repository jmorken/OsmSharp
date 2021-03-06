﻿// OsmSharp - OpenStreetMap (OSM) SDK
// Copyright (C) 2013 Abelshausen Ben
// 
// This file is part of OsmSharp.
// 
// OsmSharp is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// OsmSharp is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with OsmSharp. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using OsmSharp.Collections.PriorityQueues;
using OsmSharp.Collections.Tags;
using OsmSharp.Math.Geo;
using OsmSharp.Routing.Constraints;
using OsmSharp.Routing.Interpreter;
using OsmSharp.Routing.Osm.Graphs;
using OsmSharp.Logging;

namespace OsmSharp.Routing.Graph.Router.Dykstra
{
    /// <summary>
    /// A class containing a dykstra implementation suitable for a simple graph.
    /// </summary>
    public class DykstraRoutingLive : DykstraRoutingBase<LiveEdge>, IBasicRouter<LiveEdge>
    {
        /// <summary>
        /// Creates a new dykstra routing object.
        /// </summary>
        public DykstraRoutingLive()
        {

        }

        /// <summary>
        /// Calculates the shortest path from the given vertex to the given vertex given the weights in the graph.
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="graph"></param>
        /// <param name="interpreter"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public PathSegment<long> Calculate(IBasicRouterDataSource<LiveEdge> graph, IRoutingInterpreter interpreter, 
            Vehicle vehicle, PathSegmentVisitList from, PathSegmentVisitList to, double max)
        {
            return this.CalculateToClosest(graph, interpreter, vehicle, from,
                new PathSegmentVisitList[] { to }, max);
        }

        /// <summary>
        /// Calculates the shortest path from all sources to all targets.
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="interpreter"></param>
        /// <param name="vehicle"></param>
        /// <param name="sources"></param>
        /// <param name="targets"></param>
        /// <param name="maxSearch"></param>
        /// <returns></returns>
        public PathSegment<long>[][] CalculateManyToMany(IBasicRouterDataSource<LiveEdge> graph, IRoutingInterpreter interpreter, 
            Vehicle vehicle, PathSegmentVisitList[] sources, PathSegmentVisitList[] targets, double maxSearch)
        {
            var results = new PathSegment<long>[sources.Length][];
            for (int sourceIdx = 0; sourceIdx < sources.Length; sourceIdx++)
            {
                results[sourceIdx] = this.DoCalculation(graph, interpreter, vehicle,
                   sources[sourceIdx], targets, maxSearch, false, false);
            }
            return results;
        }

        /// <summary>
        /// Calculates the shortest path from the given vertex to the given vertex given the weights in the graph.
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="graph"></param>
        /// <param name="interpreter"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public double CalculateWeight(IBasicRouterDataSource<LiveEdge> graph, IRoutingInterpreter interpreter, Vehicle vehicle,
            PathSegmentVisitList from, PathSegmentVisitList to, double max)
        {
            PathSegment<long> closest = this.CalculateToClosest(graph, interpreter, vehicle, from,
                new PathSegmentVisitList[] { to }, max);
            if (closest != null)
            {
                return closest.Weight;
            }
            return double.MaxValue;
        }

        /// <summary>
        /// Calculates a shortest path between the source vertex and any of the targets and returns the shortest.
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="interpreter"></param>
        /// <param name="vehicle"></param>
        /// <param name="from"></param>
        /// <param name="targets"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public PathSegment<long> CalculateToClosest(IBasicRouterDataSource<LiveEdge> graph, IRoutingInterpreter interpreter, 
            Vehicle vehicle, PathSegmentVisitList from, PathSegmentVisitList[] targets, double max)
        {
            PathSegment<long>[] result = this.DoCalculation(graph, interpreter, vehicle,
                from, targets, max, false, false);
            if (result != null && result.Length == 1)
            {
                return result[0];
            }
            return null;
        }

        /// <summary>
        /// Calculates all routes from a given source to all given targets.
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="interpreter"></param>
        /// <param name="vehicle"></param>
        /// <param name="source"></param>
        /// <param name="targets"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public double[] CalculateOneToManyWeight(IBasicRouterDataSource<LiveEdge> graph, IRoutingInterpreter interpreter, Vehicle vehicle,
            PathSegmentVisitList source, PathSegmentVisitList[] targets, double max)
        {
            PathSegment<long>[] many = this.DoCalculation(graph, interpreter, vehicle,
                   source, targets, max, false, false);

            var weights = new double[many.Length];
            for (int idx = 0; idx < many.Length; idx++)
            {
                if (many[idx] != null)
                {
                    weights[idx] = many[idx].Weight;
                }
                else
                {
                    weights[idx] = double.MaxValue;
                }
            }
            return weights;
        }

        /// <summary>
        /// Calculates all routes from a given sources to all given targets.
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="interpreter"></param>
        /// <param name="vehicle"></param>
        /// <param name="sources"></param>
        /// <param name="targets"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public double[][] CalculateManyToManyWeight(IBasicRouterDataSource<LiveEdge> graph, IRoutingInterpreter interpreter, 
            Vehicle vehicle, PathSegmentVisitList[] sources, PathSegmentVisitList[] targets, double max)
        {
            var results = new double[sources.Length][];
            for (int idx = 0; idx < sources.Length; idx++)
            {
                results[idx] = this.CalculateOneToManyWeight(graph, interpreter, vehicle, sources[idx], targets, max);

                OsmSharp.Logging.Log.TraceEvent("DykstraRoutingLive", TraceEventType.Information, "Calculating weights... {0}%",
                    (int)(((float)idx / (float)sources.Length) * 100));
            }
            return results;
        }

        /// <summary>
        /// Returns true, range calculation is supported.
        /// </summary>
        public bool IsCalculateRangeSupported
        {
            get
            {
                return true;
            }
        }


        /// <summary>
        /// Calculates all points that are at or close to the given weight.
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="interpreter"></param>
        /// <param name="vehicle"></param>
        /// <param name="source"></param>
        /// <param name="weight"></param>
        /// <returns></returns>
        public HashSet<long> CalculateRange(IBasicRouterDataSource<LiveEdge> graph, IRoutingInterpreter interpreter, 
            Vehicle vehicle, PathSegmentVisitList source, double weight)
        {
            return this.CalculateRange(graph, interpreter, vehicle, source, weight, true);
        }

        /// <summary>
        /// Calculates all points that are at or close to the given weight.
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="interpreter"></param>
        /// <param name="vehicle"></param>
        /// <param name="source"></param>
        /// <param name="weight"></param>
        /// <param name="forward"></param>
        /// <returns></returns>
        public HashSet<long> CalculateRange(IBasicRouterDataSource<LiveEdge> graph, IRoutingInterpreter interpreter, 
            Vehicle vehicle, PathSegmentVisitList source, double weight, bool forward)
        {
            PathSegment<long>[] result = this.DoCalculation(graph, interpreter, vehicle,
                   source, new PathSegmentVisitList[0], weight, false, true, forward);

            var resultVertices = new HashSet<long>();
            for (int idx = 0; idx < result.Length; idx++)
            {
                resultVertices.Add(result[idx].VertexId);
            }
            return resultVertices;
        }

        /// <summary>
        /// Returns true if the search can move beyond the given weight.
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="interpreter"></param>
        /// <param name="vehicle"></param>
        /// <param name="source"></param>
        /// <param name="weight"></param>
        /// <returns></returns>
        public bool CheckConnectivity(IBasicRouterDataSource<LiveEdge> graph, IRoutingInterpreter interpreter, Vehicle vehicle,
            PathSegmentVisitList source, double weight)
        {
            HashSet<long> range = this.CalculateRange(graph, interpreter, vehicle, source, weight, true);

            if (range.Count > 0)
            {
                range = this.CalculateRange(graph, interpreter, vehicle, source, weight, false);
                if (range.Count > 0)
                {
                    return true;
                }
            }
            return false;
        }

        #region Implementation

        /// <summary>
        /// Does forward dykstra calculation(s) with several options.
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="interpreter"></param>
        /// <param name="vehicle"></param>
        /// <param name="source"></param>
        /// <param name="targets"></param>
        /// <param name="weight"></param>
        /// <param name="stopAtFirst"></param>
        /// <param name="returnAtWeight"></param>
        /// <returns></returns>
        private PathSegment<long>[] DoCalculation(IBasicRouterDataSource<LiveEdge> graph, IRoutingInterpreter interpreter, 
            Vehicle vehicle, PathSegmentVisitList source, PathSegmentVisitList[] targets, double weight,
            bool stopAtFirst, bool returnAtWeight)
        {
            return this.DoCalculation(graph, interpreter, vehicle, source, targets, weight, stopAtFirst, returnAtWeight, true);
        }

        /// <summary>
        /// Does dykstra calculation(s) with several options.
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="interpreter"></param>
        /// <param name="vehicle"></param>
        /// <param name="sourceList"></param>
        /// <param name="targetList"></param>
        /// <param name="weight"></param>
        /// <param name="stopAtFirst"></param>
        /// <param name="returnAtWeight"></param>
        /// <param name="forward"></param>
        /// <returns></returns>
        private PathSegment<long>[] DoCalculation(IBasicRouterDataSource<LiveEdge> graph, IRoutingInterpreter interpreter, Vehicle vehicle,
            PathSegmentVisitList sourceList, PathSegmentVisitList[] targetList, double weight,
            bool stopAtFirst, bool returnAtWeight, bool forward)
        {
            // make copies of the target and source visitlist.
            PathSegmentVisitList source = sourceList.Clone() as PathSegmentVisitList;
            PathSegmentVisitList[] targets = new PathSegmentVisitList[targetList.Length];
            for (int targetIdx = 0; targetIdx < targetList.Length; targetIdx++)
            {
                targets[targetIdx] = targetList[targetIdx].Clone() as PathSegmentVisitList;
            }

            //  initialize the result data structures.
            var segmentsAtWeight = new List<PathSegment<long>>();
            var segmentsToTarget = new PathSegment<long>[targets.Length]; // the resulting target segments.
            long foundTargets = 0;

            // intialize dykstra data structures.
            IPriorityQueue<PathSegment<long>> heap = new BinairyHeap<PathSegment<long>>();
            var chosenVertices = new HashSet<long>();
            var labels = new Dictionary<long, IList<RoutingLabel>>();
            foreach (long vertex in source.GetVertices())
            {
                labels[vertex] = new List<RoutingLabel>();

                PathSegment<long> path = source.GetPathTo(vertex);
                heap.Push(path, (float)path.Weight);
            }

            // set the from node as the current node and put it in the correct data structures.
            // intialize the source's neighbours.
            PathSegment<long> current = heap.Pop();
            while (current != null &&
                chosenVertices.Contains(current.VertexId))
            { // keep dequeuing.
                current = heap.Pop();
            }

            // test each target for the source.
            // test each source for any of the targets.
            var pathsFromSource = new Dictionary<long, PathSegment<long>>();
            foreach (long sourceVertex in source.GetVertices())
            { // get the path to the vertex.
                PathSegment<long> sourcePath = source.GetPathTo(sourceVertex); // get the source path.
                sourcePath = sourcePath.From;
                while (sourcePath != null)
                { // add the path to the paths from source.
                    pathsFromSource[sourcePath.VertexId] = sourcePath;
                    sourcePath = sourcePath.From;
                }
            }
            // loop over all targets
            for (int idx = 0; idx < targets.Length; idx++)
            { // check for each target if there are paths to the source.
                foreach (long targetVertex in targets[idx].GetVertices())
                {
                    PathSegment<long> targetPath = targets[idx].GetPathTo(targetVertex); // get the target path.
                    targetPath = targetPath.From;
                    while (targetPath != null)
                    { // add the path to the paths from source.
                        PathSegment<long> pathFromSource;
                        if (pathsFromSource.TryGetValue(targetPath.VertexId, out pathFromSource))
                        { // a path is found.
                            // get the existing path if any.
                            PathSegment<long> existing = segmentsToTarget[idx];
                            if (existing == null)
                            { // a path did not exist yet!
                                segmentsToTarget[idx] = targetPath.Reverse().ConcatenateAfter(pathFromSource);
                                foundTargets++;
                            }
                            else if (existing.Weight > targetPath.Weight + pathFromSource.Weight)
                            { // a new path is found with a lower weight.
                                segmentsToTarget[idx] = targetPath.Reverse().ConcatenateAfter(pathFromSource);
                            }
                        }
                        targetPath = targetPath.From;
                    }
                }
            }
            if (foundTargets == targets.Length && targets.Length > 0)
            { // routing is finished!
                return segmentsToTarget.ToArray();
            }

            if (stopAtFirst)
            { // only one entry is needed.
                if (foundTargets > 0)
                { // targets found, return the shortest!
                    PathSegment<long> shortest = null;
                    foreach (PathSegment<long> foundTarget in segmentsToTarget)
                    {
                        if (shortest == null)
                        {
                            shortest = foundTarget;
                        }
                        else if (foundTarget != null &&
                            shortest.Weight > foundTarget.Weight)
                        {
                            shortest = foundTarget;
                        }
                    }
                    segmentsToTarget = new PathSegment<long>[1];
                    segmentsToTarget[0] = shortest;
                    return segmentsToTarget;
                }
                else
                { // not targets found yet!
                    segmentsToTarget = new PathSegment<long>[1];
                }
            }

            // test for identical start/end point.
            for (int idx = 0; idx < targets.Length; idx++)
            {
                PathSegmentVisitList target = targets[idx];
                if (returnAtWeight)
                { // add all the reached vertices larger than weight to the results.
                    if (current.Weight > weight)
                    {
                        PathSegment<long> toPath = target.GetPathTo(current.VertexId);
                        toPath.Reverse();
                        toPath = toPath.ConcatenateAfter(current);
                        segmentsAtWeight.Add(toPath);
                    }
                }
                else if (target.Contains(current.VertexId))
                { // the current is a target!
                    PathSegment<long> toPath = target.GetPathTo(current.VertexId);
                    toPath = toPath.Reverse();
                    toPath = toPath.ConcatenateAfter(current);

                    if (stopAtFirst)
                    { // stop at the first occurance.
                        segmentsToTarget[0] = toPath;
                        return segmentsToTarget;
                    }
                    else
                    { // normal one-to-many; add to the result.
                        // check if routing is finished.
                        if (segmentsToTarget[idx] == null)
                        { // make sure only the first route is set.
                            foundTargets++;
                            segmentsToTarget[idx] = toPath;
                            if (foundTargets == targets.Length)
                            { // routing is finished!
                                return segmentsToTarget.ToArray();
                            }
                        }
                        else if (segmentsToTarget[idx].Weight > toPath.Weight)
                        { // check if the second, third or later is shorter.
                            segmentsToTarget[idx] = toPath;
                        }
                    }
                }
            }

            // start OsmSharp.Routing.
            KeyValuePair<uint, LiveEdge>[] arcs = graph.GetArcs(
                Convert.ToUInt32(current.VertexId));
            chosenVertices.Add(current.VertexId);

            // loop until target is found and the route is the shortest!
            while (true)
            {
                // get the current labels list (if needed).
                IList<RoutingLabel> currentLabels = null;
                if (interpreter.Constraints != null)
                { // there are constraints, get the labels.
                    currentLabels = labels[current.VertexId];
                    labels.Remove(current.VertexId);
                }

                float latitude, longitude;
                graph.GetVertex(Convert.ToUInt32(current.VertexId), out latitude, out longitude);
                var currentCoordinates = new GeoCoordinate(latitude, longitude);

                // update the visited nodes.
                foreach (KeyValuePair<uint, LiveEdge> neighbour in arcs)
                {
                    // check the tags against the interpreter.
                    TagsCollectionBase tags = graph.TagsIndex.Get(neighbour.Value.Tags);
                    if (vehicle.CanTraverse(tags))
                    { // it's ok; the edge can be traversed by the given vehicle.
                        bool? oneWay = vehicle.IsOneWay(tags);
                        bool canBeTraversedOneWay = (!oneWay.HasValue || oneWay.Value == neighbour.Value.Forward);
                        if ((current.From == null || 
                            interpreter.CanBeTraversed(current.From.VertexId, current.VertexId, neighbour.Key)) && // test for turning restrictions.
                            canBeTraversedOneWay &&
                            !chosenVertices.Contains(neighbour.Key))
                        { // the neigbour is forward and is not settled yet!
                            // check the labels (if needed).
                            bool constraintsOk = true;
                            if (interpreter.Constraints != null)
                            { // check if the label is ok.
                                RoutingLabel neighbourLabel = interpreter.Constraints.GetLabelFor(
                                    graph.TagsIndex.Get(neighbour.Value.Tags));

                                // only test labels if there is a change.
                                if (currentLabels.Count == 0 || !neighbourLabel.Equals(currentLabels[currentLabels.Count - 1]))
                                { // labels are different, test them!
                                    constraintsOk = interpreter.Constraints.ForwardSequenceAllowed(currentLabels,
                                        neighbourLabel);

                                    if (constraintsOk)
                                    { // update the labels.
                                        var neighbourLabels = new List<RoutingLabel>(currentLabels);
                                        neighbourLabels.Add(neighbourLabel);

                                        labels[neighbour.Key] = neighbourLabels;
                                    }
                                }
                                else
                                { // set the same label(s).
                                    labels[neighbour.Key] = currentLabels;
                                }
                            }

                            if (constraintsOk)
                            { // all constraints are validated or there are none.
                                graph.GetVertex(Convert.ToUInt32(neighbour.Key), out latitude, out longitude);
                                var neighbourCoordinates = new GeoCoordinate(latitude, longitude);

                                // calculate the weight.
                                double weightToNeighbour = vehicle.Weight(tags, currentCoordinates, neighbourCoordinates);

                                // calculate neighbours weight.
                                double totalWeight = current.Weight + weightToNeighbour;

                                // update the visit list;
                                var neighbourRoute = new PathSegment<long>(neighbour.Key, totalWeight, current);
                                heap.Push(neighbourRoute, (float)neighbourRoute.Weight);
                            }
                        }
                    }
                }

                // while the visit list is not empty.
                current = null;
                if (heap.Count > 0)
                {
                    // choose the next vertex.
                    current = heap.Pop();
                    while (current != null &&
                        chosenVertices.Contains(current.VertexId))
                    { // keep dequeuing.
                        current = heap.Pop();
                    }
                    if (current != null)
                    {
                        chosenVertices.Add(current.VertexId);
                    }
                }
                while (current != null && current.Weight > weight)
                {
                    if (returnAtWeight)
                    { // add all the reached vertices larger than weight to the results.
                        segmentsAtWeight.Add(current);
                    }

                    // choose the next vertex.
                    current = heap.Pop();
                    while (current != null &&
                        chosenVertices.Contains(current.VertexId))
                    { // keep dequeuing.
                        current = heap.Pop();
                    }
                }

                if (current == null)
                { // route is not found, there are no vertices left
                    // or the search whent outside of the max bounds.
                    break;
                }

                // check target.
                for (int idx = 0; idx < targets.Length; idx++)
                {
                    PathSegmentVisitList target = targets[idx];
                    if (target.Contains(current.VertexId))
                    { // the current is a target!
                        PathSegment<long> toPath = target.GetPathTo(current.VertexId);
                        toPath = toPath.Reverse();
                        toPath = toPath.ConcatenateAfter(current);

                        if (stopAtFirst)
                        { // stop at the first occurance.
                            segmentsToTarget[0] = toPath;
                            return segmentsToTarget;
                        }
                        else
                        { // normal one-to-many; add to the result.
                            // check if routing is finished.
                            if (segmentsToTarget[idx] == null)
                            { // make sure only the first route is set.
                                segmentsToTarget[idx] = toPath;
                            }
                            else if (segmentsToTarget[idx].Weight > toPath.Weight)
                            { // check if the second, third or later is shorter.
                                segmentsToTarget[idx] = toPath;
                            }

                            // remove this vertex from this target's paths.
                            target.Remove(current.VertexId);

                            // if this target is empty it's optimal route has been found.
                            if (target.Count == 0)
                            { // now the shortest route has been found for sure!
                                foundTargets++;
                                if (foundTargets == targets.Length)
                                { // routing is finished!
                                    return segmentsToTarget.ToArray();
                                }
                            }
                        }
                    }
                }

                // get the neigbours of the current node.
                arcs = graph.GetArcs(Convert.ToUInt32(current.VertexId));
            }

            // return the result.
            if (!returnAtWeight)
            {
                return segmentsToTarget.ToArray();
            }
            return segmentsAtWeight.ToArray();
        }

        #endregion
    }
}