using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SqrtBabylon {
    /// <summary>
    /// Thanks to Rod Stephens for tutorial for drawing a graph with rotated text in WPF
    /// http://www.csharphelper.com/
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
        }

        private double epsilon = 0.00001f;//margin of error for square root result
        private List<double> partialResults = new List<double>();//list of partial results
        private List<Color> colors = new List<Color>();//list of colors for results
        private Random r = new Random();
        private static int iter = 0;
        List<PointCollection> pointsColection = new List<PointCollection>();//list of collections (lines corresponding with partial results)

        // Babylonian sqrt formula
        private double sqrtB(double number, double guess) {
            if (guess != 0)
                return (guess + number / guess) / 2;
            else
                return 0;
        }

        private void sqrtBabylon(double number, double guess) {
            axisStart = axisStop = 0;
            partialResults = new List<double>();
            double prev = 0, next = guess;

            //add initial guess
            partialResults.Add(guess);
            colors.Add(Color.FromRgb((byte)r.Next(1, 255), (byte)r.Next(1, 255), (byte)r.Next(1, 255)));

            //add each guess with new color
            do {
                prev = next;
                next = sqrtB(number, prev);


                partialResults.Add(next);
                colors.Add(Color.FromRgb((byte)r.Next(1, 255), (byte)r.Next(1, 255), (byte)r.Next(1, 255)));


            } while (Math.Abs(prev - next) > epsilon && next != 0);

            //calculate min&max for x axis
            axisStart = partialResults.Min();
            axisStop = partialResults.Max();
            double distance = axisStop - axisStart;
            axisStep = distance / 10;

            //add extra space
            axisStart -= axisStep;
            axisStop += axisStep;

        }


        private Matrix WtoDMatrix, DtoWMatrix;
        private double axisStart, axisStop, axisStep;

        private void DrawGraph(Canvas can) {

            //world(graph) dimensions

            double wxmin = axisStart - axisStep;//dimensions from computed partial results
            double wxmax = axisStop + axisStep;

            double wymin = -1;
            double wymax = 10;
            double xstep = axisStep;
            double xstart = axisStart;

            //device dimensions
            double dmargin = 10;
            double dxmin = dmargin;
            double dxmax = can.ActualWidth - dmargin;
            double dymin = dmargin;
            double dymax = can.ActualHeight - dmargin;

            // Prepare the transformation matrices.
            PrepareTransformations(wxmin, wxmax, wymin, wymax, dxmin, dxmax, dymax, dymin);

            // Get the tic mark lengths.
            Point p0 = DtoW(new Point(0, 0));
            Point p1 = DtoW(new Point(5, 5));
            double xtic = p1.X - p0.X;
            double ytic = p1.Y - p0.Y;

            // Make the X axis.
            GeometryGroup xaxis_geom = new GeometryGroup();
            p0 = new Point(wxmin, 0);
            p1 = new Point(wxmax, 0);
            xaxis_geom.Children.Add(new LineGeometry(WtoD(p0), WtoD(p1)));

            for (double x = xstart; x <= wxmax - xstep; x += xstep) {
                // Add the tic mark.
                Point tic0 = WtoD(new Point(x, -ytic));
                Point tic1 = WtoD(new Point(x, ytic));
                xaxis_geom.Children.Add(new LineGeometry(tic0, tic1));

                // Label the tic mark's X coordinate.
                DrawText(can, x.ToString(),
                    new Point(tic0.X, tic0.Y + 5), 12,
                    HorizontalAlignment.Left,
                    VerticalAlignment.Top);
            }

            Path xaxis_path = new Path();
            xaxis_path.StrokeThickness = 1;
            xaxis_path.Stroke = Brushes.Black;
            xaxis_path.Data = xaxis_geom;

            can.Children.Add(xaxis_path);

        }

        private Point WtoD(Point point) {
            return WtoDMatrix.Transform(point);
        }
        private Point DtoW(Point point) {
            return DtoWMatrix.Transform(point);
        }
        private void PrepareTransformations(
            double wxmin, double wxmax, double wymin, double wymax,
            double dxmin, double dxmax, double dymin, double dymax) {
            // Make WtoD.
            WtoDMatrix = Matrix.Identity;
            WtoDMatrix.Translate(-wxmin, -wymin);

            double xscale = (dxmax - dxmin) / (wxmax - wxmin);
            double yscale = (dymax - dymin) / (wymax - wymin);
            WtoDMatrix.Scale(xscale, yscale);

            WtoDMatrix.Translate(dxmin, dymin);

            // Make DtoW.
            DtoWMatrix = WtoDMatrix;
            DtoWMatrix.Invert();
        }

        private void DrawText(Canvas can, string text, Point location, double font_size, HorizontalAlignment halign, VerticalAlignment valign) {
            // Make the label.
            Label label = new Label();
            label.Content = text;
            label.FontSize = font_size;


            can.Children.Add(label);

            label.LayoutTransform = new RotateTransform(30);

            // Position the label.
            label.Measure(new Size(double.MaxValue, double.MaxValue));

            double x = location.X;
            if (halign == HorizontalAlignment.Center)
                x -= label.DesiredSize.Width / 2;
            else if (halign == HorizontalAlignment.Right)
                x -= label.DesiredSize.Width;
            else
                x -= label.DesiredSize.Width / 10;
            Canvas.SetLeft(label, x);

            double y = location.Y;
            if (valign == VerticalAlignment.Center)
                y -= label.DesiredSize.Height / 2;
            else if (valign == VerticalAlignment.Bottom)
                y -= label.DesiredSize.Height;
            Canvas.SetTop(label, y);
        }

        //draw existing lines after window resize
        private void DrawLines(Canvas can, List<double> guesses) {
            //Create temporary collection for points after scaling
            var tmpCollection = new PointCollection();
            for (int i = 0; i < iter; i++) {

                //set appearance
                Polyline polyline = new Polyline();
                polyline.StrokeThickness = guesses.Count - i;
                polyline.Stroke = new SolidColorBrush(colors[i]);

                //scale points (world to device) and add them do temporary collection
                foreach (Point p in pointsColection[i]) {
                    tmpCollection.Add(WtoD(p));
                }
                polyline.Points = tmpCollection;

                //draw on canvas
                can.Children.Add(polyline);
                tmpCollection = new PointCollection();//clear collection for next
            }

        }

        /// <summary>
        /// Draw next partial result
        /// </summary>
        /// <param name="can"></param>
        /// <param name="guesses"></param>
        private void DrawLine(Canvas can, List<double> guesses) {
            //Create temporary collection for points after scaling
            var tmpCollection = new PointCollection();

            //add new collection for points (points for partial results before scaling)
            pointsColection.Add(new PointCollection());

            pointsColection[iter].Add(new Point((axisStart + axisStop) / 2, 8));
            pointsColection[iter].Add(new Point(guesses[iter], 0));

            //set appearance
            Polyline polyline = new Polyline();
            polyline.StrokeThickness = guesses.Count - iter;
            polyline.Stroke = new SolidColorBrush(colors[iter]);

            //scale points (world to device) and add them do temporary collection
            foreach (Point p in pointsColection[iter]) {
                tmpCollection.Add(WtoD(p));
            }
            polyline.Points = tmpCollection;

            //draw on canvas
            can.Children.Add(polyline);

            iter++;
        }

        /// <summary>
        /// Draw next partial result or start by drawing x axis
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Draw_Click(object sender, RoutedEventArgs e) {
            if (partialResults.Count <= 0) {
                try {
                    double number = Convert.ToDouble(NumberTextBox.Text);
                    double guess = Convert.ToDouble(GuessTextBox.Text);
                    if (guess == 0) {
                        MessageBox.Show("Initial guess can't be zero", "Warning", MessageBoxButton.OK);
                        return;
                    }
                    graph.Children.Clear();

                    sqrtBabylon(number, guess);

                    DrawGraph(graph);
                }
                catch {
                    MessageBox.Show(
                    "You must provide number for square root and initial guess",
                    "Warning", MessageBoxButton.OK);
                };

            }
            else {
                StringBuilder sb = new StringBuilder();
                if (iter < partialResults.Count)
                    DrawLine(graph, partialResults);
                else
                    MessageBox.Show(
                        sb.AppendFormat("All square root guesses already drawn.\nBest guess is equal to {0} with epsilon at {1}", partialResults[partialResults.Count - 1], epsilon).ToString(),
                        "Warning", MessageBoxButton.OK);
            }
        }

        /// <summary>
        /// Remove partial results and clear canvas
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Clear_Click(object sender, RoutedEventArgs e) {
            pointsColection = new List<PointCollection>();
            partialResults = new List<double>();
            colors = new List<Color>();
            iter = 0;
            graph.Children.Clear();
        }


        private void Window_Loaded(object sender, RoutedEventArgs e) {

        }

        /// <summary>
        /// Redraw graph and scale it each time window changes size
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void graph_SizeChanged(object sender, SizeChangedEventArgs e) {
            if (partialResults.Count > 0)
                try {
                    double number = Convert.ToDouble(NumberTextBox.Text);
                    double guess = Convert.ToDouble(GuessTextBox.Text);
                    if (guess == 0) {
                        MessageBox.Show("Initial guess can't be zero", "Warning", MessageBoxButton.OK);
                        return;
                    }
                    graph.Children.Clear();

                    sqrtBabylon(number, guess);

                    DrawGraph(graph);
                    DrawLines(graph, partialResults);
                }
                catch {
                    NumberTextBox.Text = "";
                    GuessTextBox.Text = "";
                    graph.Children.Clear();
                };
        }
    }
}
