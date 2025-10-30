using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace laba2
{
    internal interface IFunction
    {
        double Calc(double x);
        string Name { get; } 
    }
}
