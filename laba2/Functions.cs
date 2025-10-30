using System;

namespace laba2
{
    public class SinFunction : IFunction { public string Name => "sin(x)"; public double Calc(double x) => Math.Sin(x); }
    public class SquareFunction : IFunction { public string Name => "x*x"; public double Calc(double x) => x * x; }
    public class TanFunction : IFunction { public string Name => "tan(x)"; public double Calc(double x) => Math.Tan(x); }
    public class CubeFunction : IFunction { public string Name => "x^3"; public double Calc(double x) => x * x * x; }
    public class LinearFunction : IFunction { public string Name => "2x + 5"; public double Calc(double x) => 2 * x + 5; }
}