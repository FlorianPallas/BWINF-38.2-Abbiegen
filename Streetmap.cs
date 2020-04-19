using System.Collections.Generic;
using System.Windows;

namespace Abbiegen
{
    public class TStreetmap
    {
        const double Epsilon = 10E-15;

        public TJunction[] Junctions;
        public TJunction StartJunction;
        public TJunction EndJunction;

        public TJunction[] ShortestPath;
        public double ShortestPath_Length;
        public int ShortestPath_Turns;

        private int TurnLimit;
        private double LengthLimit;
        public TJunction[] PathWithLeastTurns;
        public double PathWithLeastTurns_Length;
        public int PathWithLeastTurns_Turns;

        // Store unvisited junctions
        List<TJunction> Remaining = new List<TJunction>();

        public TStreetmap(Point[,] _Streets, Point _Startpoint, Point _Endpoint)
        {
            // Reset variables
            Junctions = null;
            StartJunction = null;
            EndJunction = null;
            ShortestPath = null;
            ShortestPath_Length = 0.0;
            ShortestPath_Turns = 0;

            // Generate streetmap
            GenerateFromStreets(_Streets, _Startpoint, _Endpoint);
        }

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

        public bool CalculateShortestPath()
        {
            /* DIJKSTRA ALGORITHM */

            // Store unvisited junctions
            List<TJunction> Remaining = new List<TJunction>();

            // Set defaults
            foreach (TJunction J in Junctions)
            {
                J.Distance = double.PositiveInfinity;
                J.PreviousJunction = null;
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
                    GetCurrentPath(out ShortestPath, out ShortestPath_Length, out ShortestPath_Turns);
                    return true;
                }

                // Update neighbor values
                foreach (TJunction Neighbor in Current.Neighbors)
                {
                    double Alt = Current.Distance + Current.DistanceTo(Neighbor);
                    if (Alt < Neighbor.Distance)
                    {
                        Neighbor.Distance = Alt;
                        Neighbor.PreviousJunction = Current;
                    }
                }
            }

            // Return false if no path is found
            return false;
        }

        private TJunction ClosestJunction(List<TJunction> Junctions)
        {
            // Loop through all junctions to find the closest
            TJunction ClosestJunction = null;
            foreach (TJunction Junction in Junctions)
            {
                if (ClosestJunction == null || Junction.Distance < ClosestJunction.Distance)
                {
                    ClosestJunction = Junction;
                }
            }

            return ClosestJunction;
        }

        public bool CalculatePathWithLeastTurns(double Percentage)
        {
            // Set limits
            double AdditionalLength = ShortestPath_Length / 100 * Percentage;
            LengthLimit = ShortestPath_Length + AdditionalLength;
            TurnLimit = ShortestPath_Turns;

            // Set defaults
            PathWithLeastTurns = null;
            PathWithLeastTurns_Turns = TurnLimit;
            PathWithLeastTurns_Length = 0.0;

            Remaining.Clear();
            Remaining.AddRange(Junctions);

            foreach (TJunction J in Junctions)
            {
                J.PreviousJunction = null;
            }

            Step(0, 0, StartJunction);

            // Return false if there is not better path
            if(PathWithLeastTurns == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private void Step(double Length, int Turns, TJunction Current)
        {
            Remaining.Remove(Current);

            // Step back over length limit
            if(Length - LengthLimit > Epsilon)
            {
                Remaining.Add(Current);
                return;
            }

            // Steck back if over turn limit
            if (Turns > TurnLimit)
            {
                Remaining.Add(Current);
                return;
            }

            // Check for end of path
            if (Current == EndJunction)
            {
                // Get junction array, length, turns
                GetCurrentPath(out TJunction[] _Path, out double _Length, out int _Turns);

                // Prioritize Turns
                if(_Turns < PathWithLeastTurns_Turns)
                {
                    PathWithLeastTurns = _Path;
                    PathWithLeastTurns_Length = _Length;
                    PathWithLeastTurns_Turns = _Turns;
                    TurnLimit = _Turns;
                }
                else if(_Turns == PathWithLeastTurns_Turns && _Length - Epsilon < PathWithLeastTurns_Length)
                {
                    PathWithLeastTurns = _Path;
                    PathWithLeastTurns_Length = _Length;
                    PathWithLeastTurns_Turns = _Turns;
                    TurnLimit = _Turns;
                }

                Remaining.Add(Current);
                return;
            }

            // Iterate
            foreach (TJunction Neighbor in Current.Neighbors)
            {
                if (Remaining.Contains(Neighbor))
                {
                    // Update turns and length
                    int NewTurns = Turns;
                    Neighbor.PreviousJunction = Current;
                    if(Current.PreviousJunction != null)
                    {
                        if(Current.HasToTurn(Current.PreviousJunction, Neighbor))
                        {
                            NewTurns++;
                        }
                    }
                    Step(Length + Current.DistanceTo(Neighbor), NewTurns, Neighbor);
                    Neighbor.PreviousJunction = null;
                }
            }

            Remaining.Add(Current);
        }

        // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public int TurnsAlongPath(TJunction[] Path)
        {
            // Loop through all path junctions and count turns
            int Turns = 0;
            for(int I = 1; I < Path.Length - 1; I++)
            {
                if(Path[I].HasToTurn(Path[I - 1], Path[I + 1]))
                {
                    Turns++;
                }
            }

            return Turns;
        }

        private void GetCurrentPath(out TJunction[] Path, out double Length, out int Turns)
        {
            Length = 0.0;
            List<TJunction> _Path = new List<TJunction>();
            TJunction Current = EndJunction;

            // Go backwards from endpoint and save path in array
            if (Current.PreviousJunction != null || Current == StartJunction)
            {
                while (Current != null)
                {
                    _Path.Add(Current);

                    // Add to length
                    if (Current != StartJunction)
                    {
                        Length += Current.DistanceTo(Current.PreviousJunction);
                    }

                    Current = Current.PreviousJunction;
                }
            }

            _Path.Reverse();

            Path = _Path.ToArray();
            Turns = TurnsAlongPath(Path);
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

        public TJunction PreviousJunction;
        public double Distance;

        public double DistanceTo(TJunction _Neighbor)
        {
            int Index = Neighbors.FindIndex(x => x == _Neighbor);

            return NeighborValues[Index];
        }

        public bool HasToTurn(TJunction A, TJunction C)
        {
            // Calculate angle between incoming and outgoing street
            Vector AB = new Vector(Location.X - A.Location.X, Location.Y - A.Location.Y);
            Vector BC = new Vector(C.Location.X - Location.X, C.Location.Y - Location.Y);
            double Angle = Vector.AngleBetween(AB, BC);

            // If the angle is other than 0 its a turn
            if(Angle == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}