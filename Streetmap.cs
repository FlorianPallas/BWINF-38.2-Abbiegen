// +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

// +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

namespace Abbiegen
{
    // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
    // CLASS TStreetmap
    // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

    public class TStreetmap
    {
        // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        // GLOBAL VARIABLES
        // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public TJunction[] Junctions;
        public TJunction StartJunction;
        public TJunction EndJunction;

        private List<TJunction> _ShortestPath;
        public TJunction[] ShortestPath;
        public double ShortestPath_Length;

        // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        // CONSTRUCTOR
        // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public TStreetmap(Point[,] _Streets, Point _Startpoint, Point _Endpoint)
        {
            // Reset variables
            Junctions = null;
            StartJunction = null;
            EndJunction = null;
            ShortestPath = null;
            ShortestPath_Length = 0.0;

            // Generate streetmap
            GenerateFromStreets(_Streets, _Startpoint, _Endpoint);
        }

        // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        // METHODS
        // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private void GenerateFromStreets(Point[,] _Streets, Point _Startpoint, Point _Endpoint)
        {
            List<TJunction> _Junctions = new List<TJunction>();

            // Create junctions
            for(int I = 0; I < _Streets.GetLength(0); I++)
            {
                int Index;
                Point A = _Streets[I, 0];
                Point B = _Streets[I, 1];

                Index = _Junctions.FindIndex(X => X.Location == A);
                if (Index == -1)
                {
                    _Junctions.Add(new TJunction() { Location = A });
                }

                Index = _Junctions.FindIndex(X => X.Location == B);
                if (Index == -1)
                {
                    _Junctions.Add(new TJunction() { Location = B });
                }
            }

            // Set neighbor junctions
            for (int I = 0; I < _Streets.GetLength(0); I++)
            {
                Point A = _Streets[I, 0];
                Point B = _Streets[I, 1];

                int IndexA = _Junctions.FindIndex(X => X.Location == A);
                int IndexB = _Junctions.FindIndex(X => X.Location == B);
                double Length = new Vector(B.X - A.X, B.Y - A.Y).Length;

                _Junctions[IndexA].Neighbors.Add(_Junctions[IndexB]);
                _Junctions[IndexA].NeighborValues.Add(Length);
                _Junctions[IndexB].Neighbors.Add(_Junctions[IndexA]);
                _Junctions[IndexB].NeighborValues.Add(Length);
            }

            // Find junction at start and end
            int IndexStart = _Junctions.FindIndex(X => X.Location == _Startpoint);
            StartJunction = _Junctions[IndexStart];

            int IndexEnd = _Junctions.FindIndex(X => X.Location == _Endpoint);
            EndJunction = _Junctions[IndexEnd];

            // Store in global array
            Junctions = _Junctions.ToArray();
        }

        // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public bool CalculateShortestPath()
        {
            /* DIJKSTRA ALGORITHM */

            // Store unvisited junctions
            List<TJunction> Remaining = new List<TJunction>();

            // Set defaults
            foreach (TJunction J in Junctions)
            {
                J.Distance = double.PositiveInfinity;
                J.PreVertex = null;
                Remaining.Add(J);
            }

            StartJunction.Distance = 0;

            while (Remaining.Count > 0)
            {
                // Find next junction (closest)
                TJunction Current = ClosestJunction(Remaining);

                // Remove current junction from calculation
                Remaining.Remove(Current);

                // Finish
                if (Current == EndJunction)
                {
                    GetShortestPath();
                    ShortestPath = _ShortestPath.ToArray();
                    return true;
                }

                // Update neighbor values
                foreach (TJunction Neighbor in Current.Neighbors)
                {
                    double Alt = Current.Distance + Current.DistanceTo(Neighbor);
                    if (Alt < Neighbor.Distance)
                    {
                        Neighbor.Distance = Alt;
                        Neighbor.PreVertex = Current;
                    }
                }
            }

            // Return false if no path is found
            return false;
        }

        private void GetShortestPath()
        {
            ShortestPath_Length = 0.0;

            List<TJunction> S = new List<TJunction>();
            TJunction Current = EndJunction;

            if (Current.PreVertex != null || Current == StartJunction)
            {
                while (Current != null)
                {
                    S.Add(Current);
                    if(Current != StartJunction)
                    {
                        ShortestPath_Length += Current.DistanceTo(Current.PreVertex);
                    }
                    Current = Current.PreVertex;
                }
            }

            S.Reverse();
            _ShortestPath = S;
        }

        private TJunction ClosestJunction(List<TJunction> Q)
        {
            TJunction ClosestJunction = null;
            foreach (TJunction V in Q)
            {
                if (ClosestJunction == null || V.Distance < ClosestJunction.Distance)
                {
                    ClosestJunction = V;
                }
            }

            return ClosestJunction;
        }

        // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
    }

    // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
    // CLASS TJunction
    // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

    public class TJunction
    {
        public TJunction()
        {
            Neighbors = new List<TJunction>();
            NeighborValues = new List<double>();
        }

        public Point Location;
        public List<TJunction> Neighbors;
        public List<double> NeighborValues;

        public TJunction PreVertex;
        public double Distance;

        public double DistanceTo(TJunction _Neighbor)
        {
            int Index = Neighbors.FindIndex(x => x == _Neighbor);

            return NeighborValues[Index];
        }
    }

    // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
}

// +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++