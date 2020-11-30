using NestingLibPort.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;


//package com.qunhe.util.nest.util;

//import com.qunhe.util.nest.data.NestPath;
//import com.qunhe.util.nest.data.Placement;
//import com.qunhe.util.nest.data.Segment;

//import java.io.File;
//import java.util.ArrayList;
//import java.util.List;
namespace NestingLibPort.Util
{
    public static class SvgUtil
    {
        public static void saveSvgFile(List<String> strings, string path)
        {
            StreamWriter f = null;
            if (!File.Exists(path))

            {
                f = File.CreateText(path);
            }
            else
            {
                File.Delete(path);
                f = File.CreateText(path);

            }

            f.Write("<?xml version=\"1.0\" standalone=\"no\"?>\n" +
                "\n" +
                "<!DOCTYPE svg PUBLIC \"-//W3C//DTD SVG 1.1//EN\" \n" +
                "\"http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd\">\n" +
                " \n" +
                "<svg width=\"100%\" height=\"100%\" version=\"1.1\"\n" +
                "xmlns=\"http://www.w3.org/2000/svg\">\n");
            foreach (String s in strings)
            {
                f.Write(s);
            }
            f.Write("</svg>");
            f.Close();
        }
        public static List<String> svgGenerator(List<NestPath> list, List<List<Placement>> applied, double binwidth, double binHeight)
        {
            List<String> strings = new List<String>();
            int x = 0;//代表离y轴初始边的距离
            int y = 0;//代表离轴初始边的距离
            foreach (List<Placement> binlist in applied)
            {
                String s = " <g transform=\"translate(" + x + "  " + y + ")\">" + "\n";
                s += "    <rect x=\"0\" y=\"0\" width=\"" + binwidth + "\" height=\"" + binHeight + "\"  fill=\"none\" stroke=\"#010101\" stroke-width=\"1\" />\n";
                foreach (Placement placement in binlist)
                {
                    int bid = placement.bid;
                    NestPath nestPath = getNestPathByBid(bid, list);
                    double ox = placement.translate.x;
                    double oy = placement.translate.y;
                    double rotate = placement.rotate;

                    var translateX = ox + x;
                    var translateY = oy + y;

                    s += "<g transform=\"translate(" + translateX + " " + translateY + ") rotate(" + rotate + ")\"> \n";
                    //s += "<path d=\"";
                    //for (int i = 0; i < nestPath.getSegments().Count; i++)
                    //{
                    //    if (i == 0)
                    //    {
                    //        s += "M";
                    //    }
                    //    else
                    //    {
                    //        s += "L";
                    //    }
                    //    Segment segment = nestPath.get(i);
                    //    s += segment.x + " " + segment.y + " ";
                    //}
                    //s += "Z\" fill=\"#8498d1\" stroke=\"#010101\" stroke-width=\"1\" />" + " \n";
                    s += nestPath.getElement();
                    s += "</g> \n";
                }
                s += "</g> \n";
                y += (int)(binHeight + 0);
                strings.Add(s);
            }
            return strings;
        }

        public static NestPath getNestPathByBid(int bid, List<NestPath> list)
        {
            foreach (NestPath nestPath in list)
            {
                if (nestPath.bid == bid)
                {
                    return nestPath;
                }
            }
            return null;
        }


        public static List<NestPath> transferSvgIntoPolygons(string xmlFilePath)
        {
            List<NestPath> nestPaths = new List<NestPath>();

            XDocument document = XDocument.Load(xmlFilePath);
            List<XElement> elementList = document.Root.DescendantNodes().OfType<XElement>().ToList();
            //对于测试库的数据，需要做一下筛选，如果是自己弄得测试数据，这句可以不要
            var elements = elementList.Where(p => p.Name == "polygon");
            int count = 0;
            int index = 0;
            foreach (XElement element in elements)
            {
                count++;
                //对于测试库的数据要加上这一句
                var elementFirstNode = (XElement)element.FirstNode;
                var rotation = int.Parse(element.Attributes((XName)"nVertices").ToList()[0].Value.ToString());

                switch (elementFirstNode.Name.ToString())
                {
                    case "polyline":
                    case "polygon":
                        {
                            String datalist = element.Attributes((XName)"points").ToList()[0].Value.ToString();
                            NestPath polygon = new NestPath();
                            polygon.setElement(element.ToString());
                            foreach (String s in datalist.Split(' '))
                            {
                                var temp = s.Trim();
                                if (temp.IndexOf(",") == -1)
                                {
                                    continue;
                                }
                                String[] value = s.Split(',');
                                double x = Double.Parse(value[0]);
                                double y = Double.Parse(value[1]);
                                polygon.add(x, y);//点的坐标
                            }
                            polygon.bid = count;//多边形的序号
                            polygon.setRotation(4);//旋转角度，值设为4时代表角度可以旋转90、180、270，该值一般为360的倍数
                            nestPaths.Add(polygon);
                            break;
                        }
                    case "lines":
                        {
                            index++;
                            var piesCount = int.Parse(elementFirstNode.Attributes((XName)"count").ToList()[0].Value.ToString());
                            var dataList = elementFirstNode.DescendantNodes().OfType<XElement>().ToList();
                            NestPath polygon = new NestPath();
                            string point = null;
                            foreach (var data in dataList)
                            {
                                if (data.Name == "segment")
                                {
                                    //为了保证在排样前各个图形的坐标不重合，所以后面增加2000 * index
                                    var x0 = Double.Parse(data.Attributes((XName)"x0").ToList()[0].Value.ToString()) + 20 * index;
                                    var y0 = Double.Parse(data.Attributes((XName)"y0").ToList()[0].Value.ToString()) + 20 * index;
                                    var x1 = Double.Parse(data.Attributes((XName)"x1").ToList()[0].Value.ToString()) + 20 * index;
                                    var y1 = Double.Parse(data.Attributes((XName)"y1").ToList()[0].Value.ToString()) + 20 * index;
                                    point += x0 + "," + y0 + " ";
                                    polygon.add(x0, y0);
                                    polygon.add(x1, y1);
                                }
                            }

                            var listTemp = polygon.getSegments();
                            var newList = new List<Segment>();
                            for (int i = 0; i < listTemp.Count; i++)
                            {
                                if (i % 2 == 0)
                                    newList.Add(listTemp[i]);
                            }
                            polygon.setSegments(newList);
                            polygon.bid = count;//多边形的序号
                            polygon.setRotation(rotation);//旋转角度，值设为4时代表角度可以旋转90、180、270，该值一般为360的倍数
                            var elementTemp = " <polygon fill=\"none\" stroke=\"#010101\" stroke-miterlimit=\"10\" points= \"" + point + " \"></polygon>";
                            for (int i = 0; i < piesCount; i++)
                            {
                                polygon.setElement(elementTemp);
                                nestPaths.Add(polygon);
                            }
                            break;
                        }
                    case "rect":
                        {
                            double width = Double.Parse(element.Attributes((XName)"width").ToList()[0].Value.ToString());
                            double height = Double.Parse(element.Attributes((XName)"height").ToList()[0].Value.ToString());
                            double x = Double.Parse(element.Attributes((XName)"x").ToList()[0].Value.ToString());
                            double y = Double.Parse(element.Attributes((XName)"y").ToList()[0].Value.ToString());
                            NestPath rect = new NestPath();
                            rect.setElement(element.ToString());
                            rect.add(x, y);
                            rect.add(x + width, y);
                            rect.add(x + width, y + height);
                            rect.add(x, y + height);
                            rect.bid = count;
                            rect.setRotation(4);
                            nestPaths.Add(rect);
                            break;
                        }
                    case "circle"://对于圆形，给转换成坐标形式
                        {
                            double cx = Double.Parse(element.Attributes((XName)"cx").ToList()[0].Value.ToString());
                            double cy = Double.Parse(element.Attributes((XName)"cy").ToList()[0].Value.ToString());
                            double radius = Double.Parse(element.Attributes((XName)"r").ToList()[0].Value.ToString());

                            // num is the smallest number of segments required to approximate the circle to the given tolerance
                            var num = Math.Ceiling((2 * Math.PI) / Math.Acos(1 - (ToleranceConfig.tolerance / radius)));

                            if (num < 3)
                            {
                                num = 3;
                            }

                            NestPath circle = new NestPath();
                            circle.setElement(element.ToString());
                            circle.bid = count;
                            circle.setRotation(4);

                            for (var i = 0; i < num; i++)
                            {
                                var theta = i * ((2 * Math.PI) / num);
                                double x = radius * Math.Cos(theta) + cx;
                                double y = radius * Math.Sin(theta) + cy;
                                circle.add(x, y);
                            }
                            nestPaths.Add(circle);

                            break;
                        }
                    case "ellipse"://对于椭圆，给转换成坐标形式
                        {
                            // same as circle case. There is probably a way to reduce points but for convenience we will just flatten the equivalent circular polygon
                            var rx = Double.Parse(element.Attributes((XName)"rx").ToList()[0].Value.ToString());

                            var ry = Double.Parse(element.Attributes((XName)"ry").ToList()[0].Value.ToString());
                            var maxradius = Math.Max(rx, ry);

                            var cx = Double.Parse(element.Attributes((XName)"cx").ToList()[0].Value.ToString());
                            var cy = Double.Parse(element.Attributes((XName)"cy").ToList()[0].Value.ToString());

                            var num = Math.Ceiling((2 * Math.PI) / Math.Acos(1 - (ToleranceConfig.tolerance / maxradius)));

                            if (num < 3)
                            {
                                num = 3;
                            }

                            NestPath ellipse = new NestPath();
                            ellipse.setElement(element.ToString());
                            ellipse.bid = count;
                            ellipse.setRotation(4);
                            for (var i = 0; i < num; i++)
                            {
                                var theta = i * ((2 * Math.PI) / num);
                                double x = maxradius * Math.Cos(theta) + cx;
                                double y = maxradius * Math.Sin(theta) + cy;
                                ellipse.add(x, y);
                            }
                            nestPaths.Add(ellipse);
                            break;
                        }
                    case "path"://对于带弧形的多边形，给转换成坐标形式
                        {

                            var path = element.Attributes((XName)"d").ToList()[0].Value.ToString();
                            var pathNumbers = transferPathToNumber(path);
                            string pathNumberType = "MLHVCSQTA";
                            NestPath pathPloy = new NestPath();
                            pathPloy.setElement(element.ToString());
                            pathPloy.bid = count;
                            pathPloy.setRotation(4);

                            double x, y, x0, y0, x1, y1, x2, y2, prevx, prevy, prevx1, prevy1, prevx2, prevy2;
                            x = y = x0 = y0 = x1 = y1 = x2 = y2 = prevx = prevy = prevx1 = prevy1 = prevx2 = prevy2 = 0;

                            for (var i = 0; i < pathNumbers.Count; i++)
                            {
                                var s = pathNumbers[i].Numbers;////对应C#中的pathNumber的Numbers
                                var command = pathNumbers[i].Type;//对应C#中的pathNumber的path type

                                prevx = x;
                                prevy = y;

                                prevx1 = x1;
                                prevy1 = y1;

                                prevx2 = x2;
                                prevy2 = y2;

                                if (pathNumberType.Contains(command))
                                {
                                    switch (command)
                                    {
                                        case "M":
                                        case "L":
                                        case "T":
                                            {
                                                x = s[0];
                                                y = s[1];
                                                break;
                                            }
                                        case "H":
                                            {
                                                x = s[0];
                                                break;
                                            }
                                        case "V":
                                            {
                                                y = s[0];
                                                break;
                                            }
                                        case "Q":
                                            {
                                                x1 = s[0];
                                                y1 = s[1];
                                                x = s[2];
                                                y = s[3];
                                                break;
                                            }
                                        case "S":
                                            {
                                                x2 = s[0];
                                                y2 = s[1];
                                                x = s[2];
                                                y = s[3];
                                                break;
                                            }
                                        case "C":
                                            {
                                                x1 = s[0];
                                                y1 = s[1];
                                                x2 = s[2];
                                                y2 = s[3];
                                                x = s[4];
                                                y = s[5];
                                                break;
                                            }
                                    }
                                }
                                else
                                {
                                    switch (command)
                                    {
                                        case "m":
                                        case "l":
                                        case "t":
                                            {
                                                x += s[0];
                                                y += s[1];
                                                break;
                                            }
                                        case "h":
                                            {
                                                x += s[0];
                                                break;
                                            }
                                        case "v":
                                            {
                                                y += s[0];
                                                break;
                                            }
                                        case "q":
                                            {
                                                x1 = x + s[0];
                                                y1 = y + s[1];
                                                x += s[2];
                                                y += s[3];
                                                break;
                                            }
                                        case "s":
                                            {
                                                x2 = x + s[0];
                                                y2 = y + s[1];
                                                x += s[2];
                                                y += s[3];
                                                break;
                                            }
                                        case "c":
                                            {
                                                x1 = x + s[0];
                                                y1 = y + s[1];
                                                x2 = x + s[2];
                                                y2 = y + s[3];
                                                x += s[4];
                                                y += s[5];
                                                break;
                                            }
                                    }
                                }

                                switch (command)
                                {
                                    // linear line types
                                    case "m":
                                    case "M":
                                    case "l":
                                    case "L":
                                    case "h":
                                    case "H":
                                    case "v":
                                    case "V":
                                        pathPloy.add(x, y);
                                        break;
                                    // Quadratic Beziers
                                    case "t":
                                    case "T":
                                        {
                                            // implicit control point
                                            var tPathNumberType = "QqTt";
                                            if (i > 0 && tPathNumberType.Contains(pathNumbers[i - 1].Type))
                                            {
                                                x1 = prevx + (prevx - prevx1);
                                                y1 = prevy + (prevy - prevy1);
                                            }
                                            else
                                            {
                                                x1 = prevx;
                                                y1 = prevy;
                                            }
                                            break;
                                        }

                                    case "q":
                                    case "Q":
                                        {
                                            var pointlist = GeometryUtil.QuadraticBezierLinearize(new Segment(x: prevx, y: prevy), new Segment(x: x, y: y), new Segment(x: x1, y: y1), ToleranceConfig.tolerance);
                                            pointlist.Remove(pointlist[0]); // firstpoint would already be in the poly
                                            for (var j = 0; j < pointlist.Count; j++)
                                            {
                                                pathPloy.add(pointlist[j].x, pointlist[j].y);
                                            }
                                            break;
                                        }

                                    case "s":
                                    case "S":
                                        {
                                            var sPathNumberType = "CcSs";
                                            if (i > 0 && sPathNumberType.Contains(pathNumbers[i - 1].Type))
                                            {
                                                x1 = prevx + (prevx - prevx2);
                                                y1 = prevy + (prevy - prevy2);
                                            }
                                            else
                                            {
                                                x1 = prevx;
                                                y1 = prevy;
                                            }
                                            break;
                                        }

                                    case "c":
                                    case "C":
                                        {
                                            var pointlist = GeometryUtil.CubicBezierLinearize(new Segment(x: prevx, y: prevy), new Segment(x: x, y: y), new Segment(x: x1, y: y1), new Segment(x: x2, y: y2), ToleranceConfig.tolerance);
                                            pointlist.Remove(pointlist[0]); // firstpoint would already be in the poly
                                            for (var j = 0; j < pointlist.Count; j++)
                                            {
                                                pathPloy.add(pointlist[j].x, pointlist[j].y);
                                            }
                                            break;
                                        }

                                    case "a":
                                    case "A":
                                        {
                                            //var pointlist = GeometryUtil.Arc.linearize({ x: prevx, y: prevy}, { x: x, y: y}, s.r1, s.r2, s.angle, s.largeArcFlag,s.sweepFlag, this.conf.tolerance
                                            //pointlist.shift();

                                            //for (var j = 0; j < pointlist.length; j++)
                                            //{
                                            //    var point = { };
                                            //    point.x = pointlist[j].x;
                                            //    point.y = pointlist[j].y;
                                            //    poly.push(point);
                                            //}
                                            break;
                                        }
                                    case "z":
                                    case "Z":
                                        {
                                            x = x0;
                                            y = y0;
                                            break;
                                        }
                                }
                                // Record the start of a subpath
                                if (command == "M" || command == "m")
                                {
                                    x0 = x;
                                    y0 = y;
                                }
                            }


                            // 判断最后一个点是不是和第一个点一样，如果一样，就去除最后一个点
                            while (pathPloy.getSegments().Count > 0 && GeometryUtil.almostEqual(pathPloy.getSegments()[0].x, pathPloy.getSegments()[pathPloy.getSegments().Count - 1].x, ToleranceConfig.toleranceSvg) && GeometryUtil.almostEqual(pathPloy.getSegments()[0].y, pathPloy.getSegments()[pathPloy.getSegments().Count - 1].y, ToleranceConfig.toleranceSvg))
                            {
                                pathPloy.getSegments().RemoveAt(pathPloy.getSegments().Count - 1);
                            }

                            nestPaths.Add(pathPloy);
                            break;
                        }
                }
            }
            return nestPaths;
        }

        #region SVG Parser

        /// <summary>
        /// Parse a number from an SVG path.  path值对应的只有一个M/m，如果有多个，数据
        /// 处理不了，需要手动转换
        /// </summary>
        /// <param name="path">path 对应的element中d所对应的值</param>
        /// <returns></returns>
        public static List<PathNumber> transferPathToNumber(string path)
        {

            var currentIndex = 1;

            var startIndex = currentIndex;
            var endIndex = path.Length;

            List<PathNumber> pathNumbers = new List<PathNumber>();
            //跳过字符串中的空格位置
            skipOptionalSpaces(path, ref currentIndex, endIndex);

            while (currentIndex < endIndex)
            {
                PathNumber pathNumber = new PathNumber();
                pathNumber.Numbers = new List<double>();
                pathNumber.Type = path[currentIndex - 1].ToString();


                do
                {
                    var exponent = 0;//指数
                    var integer = 0;//整数
                    var frac = 1;
                    decimal decimalNumber = 0;//小数
                    var sign = 1;//符号
                    var expsign = 1;
                    // Read the sign.
                    if (currentIndex < endIndex && path[currentIndex] == '+')
                        currentIndex++;
                    else if (currentIndex < endIndex && path[currentIndex] == '-')
                    {
                        currentIndex++;
                        sign = -1;
                    }

                    if (currentIndex == endIndex || ((path[currentIndex] < '0' || path[currentIndex] > '9') && path[currentIndex] != '.'))
                        // The first character of a number must be one of [0-9+-.].
                        return null;

                    // Read the integer part, build right-to-left.
                    var startIntPartIndex = currentIndex;
                    while (currentIndex < endIndex && path[currentIndex] >= '0' && path[currentIndex] <= '9')
                        currentIndex++; // Advance to first non-digit.

                    if (currentIndex != startIntPartIndex)
                    {
                        var scanIntPartIndex = currentIndex - 1;
                        var multiplier = 1;
                        while (scanIntPartIndex >= startIntPartIndex)
                        {
                            integer += multiplier * (path[scanIntPartIndex--] - '0');
                            multiplier *= 10;
                        }
                    }

                    // Read the decimals.
                    if (currentIndex < endIndex && path[currentIndex] == '.')
                    {
                        currentIndex++;

                        // There must be a least one digit following the .
                        if (currentIndex >= endIndex || path[currentIndex] < '0' || path[currentIndex] > '9')
                            return null;
                        while (currentIndex < endIndex && path[currentIndex] >= '0' && path[currentIndex] <= '9')
                        {
                            frac *= 10;
                            decimalNumber += (decimal)(path[currentIndex] - '0') / frac;
                            currentIndex += 1;
                        }
                    }

                    // Read the exponent part.
                    if (currentIndex != startIndex && currentIndex + 1 < endIndex && (path[currentIndex] == 'e' || path[currentIndex] == 'E') && (path[currentIndex + 1] != 'x' && path[currentIndex + 1] != 'm'))
                    {
                        currentIndex++;

                        // Read the sign of the exponent.
                        if (path[currentIndex] == '+')
                        {
                            currentIndex++;
                        }
                        else if (path[currentIndex] == '-')
                        {
                            currentIndex++;
                            expsign = -1;
                        }

                        // There must be an exponent.
                        if (currentIndex >= endIndex || path[currentIndex] < '0' || path[currentIndex] > '9')
                            return null;

                        while (currentIndex < endIndex && path[currentIndex] >= '0' && path[currentIndex] <= '9')
                        {
                            exponent *= 10;
                            exponent += (path[currentIndex] - '0');
                            currentIndex++;
                        }
                    }

                    var number = integer + decimalNumber;
                    number *= sign;

                    if (exponent > 0)
                        number *= (decimal)Math.Pow(10, expsign * exponent);

                    if (startIndex == currentIndex)
                        return null;
                    pathNumber.Numbers.Add((double)number);
                    skipOptionalSpacesOrDelimiter(path, ref currentIndex, endIndex);
                } while (!isEndOfOneTypeNumber(pathNumber, ref currentIndex));
                pathNumbers.Add(pathNumber);

            }

            if (path[endIndex - 1] == 'Z' || path[endIndex - 1] == 'z')
            {
                PathNumber pathNumber = new PathNumber();
                pathNumber.Type = path[endIndex - 1].ToString();
                pathNumbers.Add(pathNumber);
            }
            return pathNumbers;
        }

        private static bool isCurrentSpace(string path, int currentIndex)
        {
            var character = path[currentIndex];
            return character <= ' ' && (character == ' ' || character == '\n' || character == '\t' || character == '\r' || character == '\f');
        }

        private static bool skipOptionalSpaces(string path, ref int currentIndex, int endIndex)
        {
            while (currentIndex < endIndex && isCurrentSpace(path, currentIndex))
                currentIndex++;
            return currentIndex < endIndex;
        }
        private static bool skipOptionalSpacesOrDelimiter(string path, ref int currentIndex, int endIndex)
        {
            if (currentIndex < endIndex && !isCurrentSpace(path, currentIndex) && path[currentIndex] != ',')
                return false;
            if (skipOptionalSpaces(path, ref currentIndex, endIndex))
            {
                if (currentIndex < endIndex && path[currentIndex] == ',')
                {
                    currentIndex++;
                    skipOptionalSpaces(path, ref currentIndex, endIndex);
                }
            }
            return currentIndex < endIndex;
        }

        //判断一种类型的数据是不是读取完了，如对于M类型，后面跟两个数据
        private static bool isEndOfOneTypeNumber(PathNumber pathNumber, ref int currentIndex)
        {
            bool isEnd = false;
            switch (pathNumber.Type)
            {
                case "M":
                case "m":
                case "L":
                case "l":
                    {
                        if (pathNumber.Numbers.Count() == 2)
                        {
                            currentIndex++;
                            isEnd = true;
                        }
                        break;
                    }
                case "H":
                case "h":
                case "V":
                case "v":
                    {
                        if (pathNumber.Numbers.Count() == 1)
                        {
                            currentIndex++;
                            isEnd = true;
                        }
                        break;
                    }
                case "C":
                case "c":
                    {
                        if (pathNumber.Numbers.Count() == 6)
                        {
                            currentIndex++;
                            isEnd = true;
                        }
                        break;
                    }
                case "S":
                case "s":
                case "Q":
                case "q":
                case "T":
                case "t":
                    {
                        if (pathNumber.Numbers.Count() == 4)
                        {
                            currentIndex++;
                            isEnd = true;
                        }
                        break;
                    }
                case "A":
                case "a":
                    {
                        if (pathNumber.Numbers.Count() == 5)
                        {
                            currentIndex++;
                            isEnd = true;
                        }
                        break;
                    }
            }

            return isEnd;
        }

        public class PathNumber
        {
            public string Type { get; set; }

            public List<double> Numbers { get; set; }
        }


        #endregion



    }
}
