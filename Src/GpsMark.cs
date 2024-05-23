using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public struct GpsMark
        {

            public static bool IsGpsMark(string stringifiedMark)
            {
                return stringifiedMark.StartsWith("GPS");
            }
            public static GpsMark FromString(string stringifiedMark)
            {
                var parts = stringifiedMark.Split(':');
                return new GpsMark(
                    name: parts[1],
                    coords: new Vector3D(double.Parse(parts[2]), double.Parse(parts[3]), double.Parse(parts[4]))
                );
            }

            public string Name { get; }
            public Vector3D Coords { get; }

            public GpsMark(string name, Vector3D coords)
            {
                Name = name;
                Coords = coords;
            }
        }
    }
}
