namespace RevitUtils.Geometry.NavisGeometryListener.Views
{
    public class GeometryPoint
    {
        public GeometryPoint(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public double X { get; }

        public double Y { get; }

        public double Z { get; }
    }
}