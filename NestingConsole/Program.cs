using NestingLibPort;
using NestingLibPort.Data;
using NestingLibPort.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NestingConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            NestPath bin = new NestPath();
            double binWidth =75;
            double binHeight = 41;
            bin.add(0, 0);
            bin.add(binWidth, 0);
            bin.add(binWidth, binHeight);
            bin.add(0, binHeight);
            Console.WriteLine("Bin Size : Width = " + binWidth + " Height=" + binHeight);
            //将多边形转换为坐标形式
            var nestPaths = SvgUtil.transferSvgIntoPolygons("test3.xml");
            Console.WriteLine("Reading File = test1.xml");
            Console.WriteLine("No of parts = " + nestPaths.Count);
            Config config = new Config();
            Console.WriteLine("Configuring Nest");
            Nest nest = new Nest(bin, nestPaths, config, 2);
            Console.WriteLine("Performing Nest");
            List<List<Placement>> appliedPlacement = nest.startNest();
            Console.WriteLine("Nesting Completed");
            var svgPolygons =  SvgUtil.svgGenerator(nestPaths, appliedPlacement, binWidth, binHeight);
            Console.WriteLine("Converted to SVG format");
            SvgUtil.saveSvgFile(svgPolygons, "output.svg");
            Console.WriteLine("Saved svg file..Opening File");
            Process.Start("output.svg");
            Console.ReadLine();
        }
    }
}
