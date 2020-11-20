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
            //var test = "M746.843,60.679c-2.465,1.232-7.395,2.465-13.711,2.465c-14.635,0-25.65-9.243-25.65-26.266c0-16.252,11.016-27.268,27.113-27.268c6.471,0,10.553,1.387,12.324,2.311l-1.617,5.469c-2.542-1.232-6.162-2.157-10.476-2.157c-12.17,0-20.258,7.78-20.258,21.414c0,12.709,7.317,20.874,19.95,20.874c4.082,0,8.241-0.848,10.938-2.157L746.843,60.679z";
            //var testNumbers = SvgUtil.transferPathToNumber(test);


            NestPath bin = new NestPath();
            double binWidth =1000;
            double binHeight = 1000;
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
