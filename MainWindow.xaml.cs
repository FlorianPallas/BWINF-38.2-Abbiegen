// +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

// +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

namespace Abbiegen
{
    // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
    // CLASS MainWindow
    // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

    public partial class MainWindow : Window
    {
        // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        // GLOBAL VARIABLES
        // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        Point Startpoint;
        Point Endpoint;
        Point[,] Streets;
        TStreetmap Streetmap;

        // Drawing
        private double Scale;
        private double MinY;
        private double MaxY;
        private double MinX;
        private double MaxX;
        private const double Border = 5;
        private double OffsetX = 0;
        private double OffsetY = 0;

        // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        // CONSTRUCTOR
        // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public MainWindow()
        {
            InitializeComponent();
        }

        // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        // METHODS
        // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private void CalculatePath(double Percentage)
        {
            // Calculate path
            if (Streetmap.CalculatePathWithLeastTurns(Percentage))
            {
                // Display new path
                LabelPathLength.Content = Streetmap.PathWithLeastTurns_Length.ToString("#.00") + "LE";
                double Increase = (Streetmap.PathWithLeastTurns_Length - Streetmap.ShortestPath_Length) / (Streetmap.ShortestPath_Length / 100);
                LabelPathIncrease.Content = "(+ " + Increase.ToString("#.00") + "%)";
                LabelPathTurns.Content = Streetmap.PathWithLeastTurns_Turns + " mal";
            }
            else
            {
                // Display shortest path if its the best
                LabelPathLength.Content = Streetmap.ShortestPath_Length.ToString("#.00") + "LE";
                LabelPathIncrease.Content = "(+ 0%)";
                LabelPathTurns.Content = Streetmap.ShortestPath_Turns + " mal";
            }

            // Remove increase percentage if it's the shortest path
            if (Percentage == 0)
            {
                LabelPathIncrease.Content = String.Empty;
            }
        }

        // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private void Draw()
        {
            if (Streetmap == null || Streetmap.Junctions == null || Streets == null) { return; }
            CanvasMain.Children.Clear();

            Draw_SetParameters();
            Draw_Streets();

            // Draw shortest or best path
            if(Streetmap.PathWithLeastTurns == null)
            {
                Draw_Path(Streetmap.ShortestPath, Brushes.Blue, 0.05);
            }
            else
            {
                Draw_Path(Streetmap.PathWithLeastTurns, Brushes.Blue, 0.05);
            }

            Draw_StartEnd();
            Draw_Junctions();
            Draw_StartEndText();
        }

        private void Draw_SetParameters()
        {
            // Determine real size of map
            MinY = double.PositiveInfinity;
            MaxY = double.NegativeInfinity;
            MinX = double.PositiveInfinity;
            MaxX = double.NegativeInfinity;

            foreach (TJunction J in Streetmap.Junctions)
            {
                if (J.Location.Y > MaxY) { MaxY = J.Location.Y; }
                if (J.Location.Y < MinY) { MinY = J.Location.Y; }
                if (J.Location.X > MaxX) { MaxX = J.Location.X; }
                if (J.Location.X < MinX) { MinX = J.Location.X; }
            }

            // Calculate scale and offset
            double ScaleX = CanvasMain.ActualWidth / (MaxX - MinX + Border);
            double ScaleY = CanvasMain.ActualHeight / (MaxY - MinY + Border);

            Scale = Math.Min(ScaleX, ScaleY);

            OffsetX = CanvasMain.ActualWidth - (MaxX - MinX) * Scale;
            OffsetY = CanvasMain.ActualHeight - (MaxY - MinY) * Scale;
        }

        private void Draw_StartEnd()
        {
            DrawCircle(Startpoint, 0.25, Brushes.Blue);
            DrawCircle(Endpoint, 0.25, Brushes.Blue);
        }

        private void Draw_StartEndText()
        {
            DrawText(Startpoint, "S", Brushes.Blue, 0.15);
            DrawText(Endpoint, "E", Brushes.Blue, 0.15);
        }

        private void Draw_Junctions()
        {
            foreach (TJunction J in Streetmap.Junctions)
            {
                DrawCircle(J.Location, 0.2, Brushes.LightGray);
            }
        }

        private void Draw_Streets()
        {
            for(int I = 0; I < Streets.GetLength(0); I++)
            {
                Point A = Streets[I, 0];
                Point B = Streets[I, 1];
                DrawLine(A, B, Brushes.LightGray, 0.05);
            }
        }

        public void Draw_Path(TJunction[] Path, Brush Color, double Thickness)
        {
            if(Path == null) { return; }

            for (int I = 1; I < Path.Length; I++)
            {
                Point A = Path[I - 1].Location;
                Point B = Path[I].Location;
                DrawLine(A, B, Color, Thickness);
            }
        }

        public void DrawText(Point P, string Text, Brush Color, double Size)
        {
            P = PointToCanvas(P);

            TextBlock TextBlock = new TextBlock()
            {
                Text = Text,
                FontSize = Size * Scale,
                Foreground = Color
            };

            TextBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            Canvas.SetLeft(TextBlock, P.X - TextBlock.DesiredSize.Width * 0.5);
            Canvas.SetTop(TextBlock, P.Y - TextBlock.DesiredSize.Height * 0.5);

            CanvasMain.Children.Add(TextBlock);
        }

        public void DrawLine(Point A, Point B, Brush Color, double Thickness)
        {
            A = PointToCanvas(A);
            B = PointToCanvas(B);

            Line Linie = new Line()
            {
                Stroke = Color,
                X1 = A.X,
                Y1 = A.Y,
                X2 = B.X,
                Y2 = B.Y,
                StrokeThickness = Scale * Thickness
            };

            CanvasMain.Children.Add(Linie);
        }

        private void DrawCircle(Point P, double Radius, Brush Farbe)
        {
            P = PointToCanvas(P);
            Radius *= Scale;

            Ellipse E = new Ellipse()
            {
                Width = Radius,
                Height = Radius,
                Fill = Farbe
            };

            CanvasMain.Children.Add(E);

            Canvas.SetLeft(E, P.X - Radius * 0.5);
            Canvas.SetTop(E, P.Y - Radius * 0.5);
        }

        private Point PointToCanvas(Point P)
        {
            return new Point((P.X - MinX) * Scale + OffsetX / 2, CanvasMain.ActualHeight - P.Y * Scale - OffsetY / 2);
        }

        // ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private bool OpenFile()
        {
            // Open file dialog
            Microsoft.Win32.OpenFileDialog Dlg = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = "\"Abbiegen\"-Datei (*.txt)|*.txt|Alle Dateien (*.*)|*.*",
                FilterIndex = 0
            };

            // Return if user cancels selection
            if (Dlg.ShowDialog() != true)
            {
                return false;
            }

            // Read file contents
            FileStream File = new FileStream(Dlg.FileName, FileMode.Open);
            string FileString = String.Empty;
            StreamReader Reader = new StreamReader(File);

            try
            {
                FileString = Reader.ReadToEnd();
            }
            catch
            {
                MessageBox.Show("Die Datei konnte nicht eingelesen werden.");
                return false;
            }
            finally
            {
                File.Close();
                Reader.Close();
            }

            // Split file into rows
            string[] Rows = FileString.Split('\n');

            try
            {
                // Parse file
                int StreetCount = int.Parse(Rows[0]);
                Startpoint = PointFromString(Rows[1]);
                Endpoint = PointFromString(Rows[2]);
                Streets = new Point[StreetCount, 2];

                for(int I = 0; I < StreetCount; I++)
                {
                    string[] Substrings = Rows[3 + I].Split(' ');
                    Streets[I, 0] = PointFromString(Substrings[0]);
                    Streets[I, 1] = PointFromString(Substrings[1]);
                }

                // Generate streetmap
                Streetmap = new TStreetmap(Streets, Startpoint, Endpoint);

                MessageBox.Show("Die Datei wurde erfolgreich eingelesen.", "Operation erfolgreich", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch
            {
                MessageBox.Show("Die Datei konnte nicht eingelesen werden!", "Operation fehlgeschlagen", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }

        private Point PointFromString(string S)
        {
            S = S.Replace("(", "").Replace(")", "").Trim();
            string[] Coordinates = S.Split(',');
            Point P = new Point();
            P.X = int.Parse(Coordinates[0]);
            P.Y = int.Parse(Coordinates[1]);
            return P;
        }

        // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        // HANDLERS
        // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Update UI
            Draw();
        }

        // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private void ButtonOpen_Click(object sender, RoutedEventArgs e)
        {
            // Update UI
            ButtonCalculate.IsEnabled = false;
            StackPanelPath.Visibility = Visibility.Collapsed;
            
            // Read file
            if (!OpenFile())
            {
                return;
            }

            // Calculate path
            if (!Streetmap.CalculateShortestPath())
            {
                MessageBox.Show("Es konnte kein Weg zum Ziel gefunden werden!", "Operation fehlgeschlagen", MessageBoxButton.OK, MessageBoxImage.Error);
                ButtonCalculate.IsEnabled = false;
                return;
            }

            // Check if there is a path with less turns
            CalculatePath(0);

            // Update UI
            StackPanelPath.Visibility = Visibility.Visible;
            ButtonCalculate.IsEnabled = true;
            Draw();
        }

        // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private void ButtonCalculate_Click(object sender, RoutedEventArgs e)
        {
            // Parse input percentage
            double Percentage;
            try
            {
                Percentage = double.Parse(TextBoxPercentage.Text);
            }
            catch
            {
                MessageBox.Show("Ungültige Eingabe", "Operation fehlgeschlagen", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Calculate path
            CalculatePath(Percentage);

            // Update UI
            Draw();
        }

        // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private void ButtonExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
    }

    // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
}

// +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++