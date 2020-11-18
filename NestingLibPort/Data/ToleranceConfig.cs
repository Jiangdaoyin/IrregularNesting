using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NestingLibPort.Data
{
    public class ToleranceConfig
    {
        public static int tolerance = 2; // max bound for bezier->line segment conversion, in native SVG units
        public static double toleranceSvg = 0.005;// fudge factor for browser inaccuracy in SVG unit handling
    }
}
