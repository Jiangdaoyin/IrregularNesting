﻿using ClipperLib;
using NestingLibPort.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NestingLibPort.Util
{
    using Path = List<IntPoint>;
    using Paths = List<List<IntPoint>>;

    public class GeometryUtil
    {


        private static double TOL = Math.Pow(10, -2);

        public static bool almostEqual(double a, double b)
        {
            return Math.Abs(a - b) < TOL;
        }

        public static bool almostEqual(double a, double b, double tolerance)
        {
            return Math.Abs(a - b) < tolerance;
        }

        /**
         * 计算多边形面积
         * @param polygon
         * @return
         */
        public static double polygonArea(NestPath polygon)
        {
            double area = 0;
            for (int i = 0, j = polygon.size() - 1; i < polygon.size(); j = i++)
            {
                Segment si = polygon.getSegments()[i];
                Segment sj = polygon.getSegments()[j];
                area += (sj.getX() + si.getX()) * (sj.getY() - si.getY());
            }
            return 0.5 * area;
        }

        /**
         * 判断点P是否在边AB上
         * @param A
         * @param B
         * @param p
         * @return
         */
        public static bool onSegment(Segment A, Segment B, Segment p)
        {
            // vertical line
            if (almostEqual(A.x, B.x) && almostEqual(p.x, A.x))
            {
                if (!almostEqual(p.y, B.y) && !almostEqual(p.y, A.y) && p.y < Math.Max(B.y, A.y) && p.y > Math.Min(B.y, A.y))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            // horizontal line
            if (almostEqual(A.y, B.y) && almostEqual(p.y, A.y))
            {
                if (!almostEqual(p.x, B.x) && !almostEqual(p.x, A.x) && p.x < Math.Max(B.x, A.x) && p.x > Math.Min(B.x, A.x))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            //range check
            if ((p.x < A.x && p.x < B.x) || (p.x > A.x && p.x > B.x) || (p.y < A.y && p.y < B.y) || (p.y > A.y && p.y > B.y))
            {
                return false;
            }


            // exclude end points
            if ((almostEqual(p.x, A.x) && almostEqual(p.y, A.y)) || (almostEqual(p.x, B.x) && almostEqual(p.y, B.y)))
            {
                return false;
            }

            double cross = (p.y - A.y) * (B.x - A.x) - (p.x - A.x) * (B.y - A.y);

            if (Math.Abs(cross) > TOL)
            {
                return false;
            }

            double dot = (p.x - A.x) * (B.x - A.x) + (p.y - A.y) * (B.y - A.y);



            if (dot < 0 || almostEqual(dot, 0))
            {
                return false;
            }

            double len2 = (B.x - A.x) * (B.x - A.x) + (B.y - A.y) * (B.y - A.y);



            if (dot > len2 || almostEqual(dot, len2))
            {
                return false;
            }

            return true;

        }

        /**
         * 判断点P是否在多边形polygon上
         * @param point
         * @param polygon
         * @return
         */
        public static Boolean? pointInPolygon(Segment point, NestPath polygon)
        {
            bool inside = false;
            double offsetx = polygon.offsetX;
            double offsety = polygon.offsetY;

            for (int i = 0, j = polygon.size() - 1; i < polygon.size(); j = i++)
            {
                double xi = polygon.get(i).x + offsetx;
                double yi = polygon.get(i).y + offsety;
                double xj = polygon.get(j).x + offsetx;
                double yj = polygon.get(j).y + offsety;

                if (almostEqual(xi, point.x) && almostEqual(yi, point.y))
                {
                    return null; // no result
                }

                if (onSegment(new Segment(xi, yi), new Segment(xj, yj), point))
                {
                    return null; // exactly on the segment
                }

                if (almostEqual(xi, xj) && almostEqual(yi, yj))
                { // ignore very small lines
                    continue;
                }

                bool intersect = ((yi > point.y) != (yj > point.y)) && (point.x < (xj - xi) * (point.y - yi) / (yj - yi) + xi);
                if (intersect) inside = !inside;
            }

            return inside;
        }

        /**
         * 获取多边形边界
         * @param polygon
         * @return
         */
        public static Bound getPolygonBounds(NestPath polygon)
        {

            double xmin = polygon.getSegments()[0].getX();
            double xmax = polygon.getSegments()[0].getX();
            double ymin = polygon.getSegments()[0].getY();
            double ymax = polygon.getSegments()[0].getY();

            for (int i = 1; i < polygon.getSegments().Count; i++)
            {
                double x = polygon.getSegments()[i].getX();
                double y = polygon.getSegments()[i].getY();
                if (x > xmax)
                {
                    xmax = x;
                }
                else if (x < xmin)
                {
                    xmin = x;
                }

                if (y > ymax)
                {
                    ymax = y;
                }
                else if (y < ymin)
                {
                    ymin = y;
                }
            }
            return new Bound(xmin, ymin, xmax - xmin, ymax - ymin);
        }

        /**
         * 将多边形旋转一定角度后，返回旋转后多边形的边界
         * @param polygon
         * @param angle
         * @return
         */
        public static Bound rotatePolygon(NestPath polygon, double angle)
        {
            if (angle == 0)
            {
                return getPolygonBounds(polygon);
            }
            double Fangle = angle * Math.PI / 180;
            NestPath rotated = new NestPath();
            for (int i = 0; i < polygon.size(); i++)
            {
                double x = polygon.get(i).x;
                double y = polygon.get(i).y;
                double x1 = x * Math.Cos(Fangle) - y * Math.Sin(Fangle);
                double y1 = x * Math.Sin(Fangle) + y * Math.Cos(Fangle);
                rotated.add(x1, y1);
            }
            Bound bounds = getPolygonBounds(rotated);
            return bounds;
        }

        /**
         * 将多边形旋转一定角度后，返回该旋转后的多边形
         * @param polygon
         * @param degrees
         * @return
         */
        public static NestPath rotatePolygon2Polygon(NestPath polygon, double degrees)
        {
            NestPath rotated = new NestPath();
            double angle = degrees * Math.PI / 180;
            for (int i = 0; i < polygon.size(); i++)
            {
                double x = polygon.get(i).x;
                double y = polygon.get(i).y;
                double x1 = x * Math.Cos(angle) - y * Math.Sin(angle);
                double y1 = x * Math.Sin(angle) + y * Math.Cos(angle);
                rotated.add(new Segment(x1, y1));
            }
            rotated.bid = polygon.bid;
            rotated.setId(polygon.getId());
            rotated.setSource(polygon.getSource());
            if (polygon.getChildren().Count > 0)
            {
                for (int j = 0; j < polygon.getChildren().Count; j++)
                {
                    rotated.getChildren().Add(rotatePolygon2Polygon(polygon.getChildren()[j], degrees));
                }
            }
            return rotated;
        }

        /**
         * 判断是否是矩形
         * @param poly
         * @param tolerance
         * @return
         */
        public static bool isRectangle(NestPath poly, double tolerance)
        {
            Bound bb = getPolygonBounds(poly);

            for (int i = 0; i < poly.size(); i++)
            {
                if (!almostEqual(poly.get(i).x, bb.getXmin(), tolerance) && !almostEqual(poly.get(i).x, bb.getXmin() + bb.getWidth(), tolerance))
                {
                    return false;
                }
                if (!almostEqual(poly.get(i).y, bb.getYmin(), tolerance) && !almostEqual(poly.get(i).y, bb.getYmin() + bb.getHeight(), tolerance))
                {
                    return false;
                }
            }
            return true;
        }

        /**
         * 构建NFP
         * @param A
         * @param B
         * @param inside
         * @param searchEdges
         * @return
         */
        public static List<NestPath> noFitPolygon( NestPath A ,  NestPath B , bool inside, bool searchEdges)
        {
            A.setOffsetX(0);
            A.setOffsetY(0);

            double minA = A.get(0).y;
            int minAIndex = 0;
            double currentAX = A.get(0).x;
            double maxB = B.get(0).y;
            int maxBIndex = 0;

            for (int i = 1; i < A.size(); i++)
            {
                A.get(i).marked = false;
                if (almostEqual(A.get(i).y, minA) && A.get(i).x < currentAX)
                {
                    minA = A.get(i).y;
                    minAIndex = i;
                    currentAX = A.get(i).x;
                }
                else if (A.get(i).y < minA)
                {
                    minA = A.get(i).y;
                    minAIndex = i;
                    currentAX = A.get(i).x;
                }
            }
            for (int i = 1; i < B.size(); i++)
            {
                B.get(i).marked = false;
                if (B.get(i).y > maxB)
                {
                    maxB = B.get(i).y;
                    maxBIndex = i;
                }
            }
            Segment startPoint = null;
            if (!inside)
            {
                startPoint = new Segment(A.get(minAIndex).x - B.get(maxBIndex).x,
                                         A.get(minAIndex).y - B.get(maxBIndex).y);

            }
            else
            {
                //TODO heuristic for inside
                startPoint = searchStartPoint(A, B, true, null);

            }

            List<NestPath> NFPlist = new List<NestPath>();

            while (startPoint != null)
            {
                Segment prevvector = null;
                B.setOffsetX(startPoint.x);
                B.setOffsetY(startPoint.y);


                List<SegmentRelation> touching;
                NestPath NFP = new NestPath();
                NFP.add(new Segment(B.get(0).x + B.getOffsetX(),
                                    B.get(0).y + B.getOffsetY()));

                double referenceX = B.get(0).x + B.getOffsetX();
                double referenceY = B.get(0).y + B.getOffsetY();
                double startX = referenceX;
                double startY = referenceY;
                int counter = 0;

                // sanity check  , prevent infinite loop
                while (counter < 10 * (A.size() + B.size()))
                {
                    touching = new List<SegmentRelation>();


                    for (int i = 0; i < A.size(); i++)
                    {
                        int nexti = (i == A.size() - 1) ? 0 : i + 1;
                        for (int j = 0; j < B.size(); j++)
                        {
                            int nextj = (j == B.size() - 1) ? 0 : j + 1;
                            if (almostEqual(A.get(i).x, B.get(j).x + B.offsetX) && almostEqual(A.get(i).y, B.get(j).y + B.offsetY))
                            {
                                touching.Add(new SegmentRelation(0, i, j));
                            }
                            else if (onSegment(A.get(i), A.get(nexti), new Segment(B.get(j).x + B.offsetX, B.get(j).y + B.offsetY)))
                            {
                                touching.Add(new SegmentRelation(1, nexti, j));
                            }
                            else if (onSegment(new Segment(B.get(j).x + B.offsetX, B.get(j).y + B.offsetY),
                                               new Segment(B.get(nextj).x + B.offsetX, B.get(nextj).y + B.offsetY),
                                                A.get(i)))
                            {
                                touching.Add(new SegmentRelation(2, i, nextj));
                            }
                        }
                    }


                    NestPath vectors = new NestPath();
                    for (int i = 0; i < touching.Count; i++)
                    {
                        Segment vertexA = A.get(touching[i].A);
                        vertexA.marked = true;

                        int prevAIndex = touching[i].A - 1;
                        int nextAIndex = touching[i].A + 1;

                        prevAIndex = (prevAIndex < 0) ? A.size() - 1 : prevAIndex; // loop
                        nextAIndex = (nextAIndex >= A.size()) ? 0 : nextAIndex; // loop

                        Segment prevA = A.get(prevAIndex);
                        Segment nextA = A.get(nextAIndex);

                        Segment vertexB = B.get(touching[i].B);

                        int prevBIndex = touching[i].B - 1;
                        int nextBIndex = touching[i].B + 1;

                        prevBIndex = (prevBIndex < 0) ? B.size() - 1 : prevBIndex; // loop
                        nextBIndex = (nextBIndex >= B.size()) ? 0 : nextBIndex; // loop

                        Segment prevB = B.get(prevBIndex);
                        Segment nextB = B.get(nextBIndex);

                        if (touching[i].type == 0)
                        {
                            Segment vA1 = new Segment(prevA.x - vertexA.x, prevA.y - vertexA.y);
                            vA1.start = vertexA; vA1.end = prevA;

                            Segment vA2 = new Segment(nextA.x - vertexA.x, nextA.y - vertexA.y);
                            vA2.start = vertexA; vA2.end = nextA;

                            Segment vB1 = new Segment(vertexB.x - prevB.x, vertexB.y - prevB.y);
                            vB1.start = prevB; vB1.end = vertexB;

                            Segment vB2 = new Segment(vertexB.x - nextB.x, vertexB.y - nextB.y);
                            vB2.start = nextB; vB2.end = vertexB;

                            vectors.add(vA1);
                            vectors.add(vA2);
                            vectors.add(vB1);
                            vectors.add(vB2);
                        }
                        else if (touching[i].type == 1)
                        {

                            Segment tmp = new Segment(vertexA.x - (vertexB.x + B.offsetX),
                                                        vertexA.y - (vertexB.y + B.offsetY));

                            tmp.start = prevA;
                            tmp.end = vertexA;

                            Segment tmp2 = new Segment(prevA.x - (vertexB.x + B.offsetX), prevA.y - (vertexB.y + B.offsetY));
                            tmp2.start = vertexA; tmp2.end = prevA;
                            vectors.add(tmp);
                            vectors.add(tmp2);

                        }
                        else if (touching[i].type == 2)
                        {
                            Segment tmp1 = new Segment(vertexA.x - (vertexB.x + B.offsetX),
                                                        vertexA.y - (vertexB.y + B.offsetY));
                            tmp1.start = prevB;
                            tmp1.end = vertexB;
                            Segment tmp2 = new Segment(vertexA.x - (prevB.x + B.offsetX),
                                                       vertexA.y - (prevB.y + B.offsetY));
                            tmp2.start = vertexB;
                            tmp2.end = prevB;

                            vectors.add(tmp1); vectors.add(tmp2);
                        }
                    }

                    Segment translate = null;

                    Double maxd = 0.0;
                    for (int i = 0; i < vectors.size(); i++)
                    {
                        if (almostEqual(vectors.get(i).x, 0) && almostEqual(vectors.get(i).y, 0))
                        {
                            continue;
                        }

                        if (prevvector != null && vectors.get(i).y * prevvector.y + vectors.get(i).x * prevvector.x < 0)
                        {

                            double vectorlength = Math.Sqrt(vectors.get(i).x * vectors.get(i).x + vectors.get(i).y * vectors.get(i).y);
                            Segment unitv = new Segment(vectors.get(i).x / vectorlength, vectors.get(i).y / vectorlength);


                            double prevlength = Math.Sqrt(prevvector.x * prevvector.x + prevvector.y * prevvector.y);
                            Segment prevunit = new Segment(prevvector.x / prevlength, prevvector.y / prevlength);


                            // we need to scale down to unit vectors to normalize vector length. Could also just do a tan here
                            if (Math.Abs(unitv.y * prevunit.x - unitv.x * prevunit.y) < 0.0001)
                            {

                                continue;
                            }
                        }
                        //todo polygonSlideDistance
                        Double? d = polygonSlideDistance(A, B, vectors.get(i), true);

                        double vecd2 = vectors.get(i).x * vectors.get(i).x + vectors.get(i).y * vectors.get(i).y;

                        if (d == null || d * d > vecd2)
                        {
                            double vecd = Math.Sqrt(vectors.get(i).x * vectors.get(i).x + vectors.get(i).y * vectors.get(i).y);
                            d = vecd;
                        }

                        if (d != null && d > maxd)
                        {
                            maxd =(double) d;
                            translate = vectors.get(i);
                        }

                    }

                    if (translate == null || almostEqual(maxd, 0))
                    {
                        // didn't close the loop, something went wrong here
                        if (translate == null)
                        {

                        }
                        if (almostEqual(maxd, 0))
                        {
                        }
                        NFP = null;
                        break;
                    }

                    translate.start.marked = true;
                    translate.end.marked = true;

                    prevvector = translate;


                    // trim
                    double vlength2 = translate.x * translate.x + translate.y * translate.y;
                    if (maxd * maxd < vlength2 && !almostEqual(maxd * maxd, vlength2))
                    {
                        double scale = Math.Sqrt((maxd * maxd) / vlength2);
                        translate.x *= scale;
                        translate.y *= scale;
                    }

                    referenceX += translate.x;
                    referenceY += translate.y;


                    if (almostEqual(referenceX, startX) && almostEqual(referenceY, startY))
                    {
                        // we've made a full loop
                        break;
                    }

                    // if A and B start on a touching horizontal line, the end point may not be the start point
                    bool looped = false;
                    if (NFP.size() > 0)
                    {
                        for (int i = 0; i < NFP.size() - 1; i++)
                        {
                            if (almostEqual(referenceX, NFP.get(i).x) && almostEqual(referenceY, NFP.get(i).y))
                            {
                                looped = true;
                            }
                        }
                    }

                    if (looped)
                    {
                        // we've made a full loop
                        break;
                    }

                    NFP.add(new Segment(referenceX, referenceY));

                    B.offsetX += translate.x;
                    B.offsetY += translate.y;
                    counter++;
                }

                if (NFP != null && NFP.size() > 0)
                {
                    NFPlist.Add(NFP);
                }

                if (!searchEdges)
                {
                    // only get outer NFP or first inner NFP
                    break;
                }
                startPoint = searchStartPoint(A, B, inside, NFPlist);
            }
            return NFPlist;
        }

        public static Segment searchStartPoint(NestPath CA, NestPath CB, bool inside, List<NestPath> NFP)
        {

            NestPath A = new NestPath(CA);
            NestPath B = new NestPath(CB);

            if (A.get(0) != A.get(A.size() - 1))
            {
                A.add(A.get(0));
            }

            if (B.get(0) != B.get(B.size() - 1))
            {
                B.add(B.get(0));
            }

            for (int i = 0; i < A.size() - 1; i++)
            {
                if (!A.get(i).marked)
                {
                    A.get(i).marked = true;
                    for (int j = 0; j < B.size(); j++)
                    {
                        B.offsetX = A.get(i).x - B.get(j).x;
                        B.offsetY = A.get(i).y - B.get(j).y;
                        Boolean? Binside = null;
                        for (int k = 0; k < B.size(); k++)
                        {
                            Boolean? inpoly = pointInPolygon(new Segment(B.get(k).x + B.offsetX, B.get(k).y + B.offsetY), A);
                            if (inpoly != null)
                            {
                                Binside = inpoly;
                                break;
                            }
                        }

                        if (Binside == null)
                        { // A and B are the same
                            return null;
                        }

                        Segment startPoint = new Segment(B.offsetX, B.offsetY);

                        if (((Binside != null && inside) || (Binside == null && !inside)) && !intersect(A, B) && !inNfp(startPoint, NFP))
                        {
                            return startPoint;
                        }

                        // slide B along vector
                        double vx = A.get(i + 1).x - A.get(i).x;
                        double vy = A.get(i + 1).y - A.get(i).y;

                        Double? d1 = polygonProjectionDistance(A, B, new Segment(vx, vy));
                        Double? d2 = polygonProjectionDistance(B, A, new Segment(-vx, -vy));

                        Double? d = null;

                        // todo: clean this up
                        if (d1 == null && d2 == null)
                        {
                            // nothin
                        }
                        else if (d1 == null)
                        {
                            d = d2;
                        }
                        else if (d2 == null)
                        {
                            d = d1;
                        }
                        else
                        {
                            d = Math.Min((double)d1,(double) d2);
                        }

                        // only slide until no longer negative
                        // todo: clean this up
                        if (d != null && !almostEqual((double)d, 0) && d > 0)
                        {

                        }
                        else
                        {
                            continue;
                        }

                        double vd2 = vx * vx + vy * vy;

                        if (d * d < vd2 && !almostEqual((double)d *(double) d, vd2))
                        {
                            double vd = Math.Sqrt(vx * vx + vy * vy);
                            vx *= (double)d / vd;
                            vy *=(double) d / vd;
                        }

                        B.offsetX += vx;
                        B.offsetY += vy;

                        for (int k = 0; k < B.size(); k++)
                        {
                            Boolean? inpoly = pointInPolygon(new Segment(B.get(k).x + B.offsetX, B.get(k).y + B.offsetY), A);
                            if (inpoly != null)
                            {
                                Binside = inpoly;
                                break;
                            }
                        }
                        startPoint = new Segment(B.offsetX, B.offsetY);
                        if ((((bool)Binside && inside) || (!(bool)Binside && !inside)) && !intersect(A, B) && !inNfp(startPoint, NFP))
                        {
                            return startPoint;
                        }
                    }
                }
            }
            return null;
        }


        /**
         *
         * @param p
         * @param nfp
         * @return
         */
        public static bool inNfp(Segment p, List<NestPath> nfp)
        {
            if (nfp == null)
            {
                return false;
            }
            for (int i = 0; i < nfp.Count; i++)
            {
                for (int j = 0; j < nfp[i].size(); j++)
                {
                    if (almostEqual(p.x, nfp[i].get(j).x) && almostEqual(p.y, nfp[i].get(j).y))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static Double? polygonProjectionDistance(NestPath CA, NestPath CB, Segment direction)
        {
            double Boffsetx = CB.offsetX;
            double Boffsety = CB.offsetY;

            double Aoffsetx = CA.offsetX;
            double Aoffsety = CA.offsetY;

            NestPath A = new NestPath(CA);
            NestPath B = new NestPath(CB);

            if (A.get(0) != A.get(A.size() - 1))
            {
                A.add(A.get(0));
            }

            if (B.get(0) != B.get(B.size() - 1))
            {
                B.add(B.get(0));
            }

            NestPath edgeA = A;
            NestPath edgeB = B;

            Double? distance = null;
            Segment p, s1, s2 = null;
            Double? d = null;
            for (int i = 0; i < edgeB.size(); i++)
            {
                // the shortest/most negative projection of B onto A
                Double? minprojection = null;
                Segment minp = null;
                for (int j = 0; j < edgeA.size() - 1; j++)
                {
                    p = new Segment(edgeB.get(i).x + Boffsetx, edgeB.get(i).y + Boffsety);
                    s1 = new Segment(edgeA.get(j).x + Aoffsetx, edgeA.get(j).y + Aoffsety);
                    s2 = new Segment(edgeA.get(j + 1).x + Aoffsetx, edgeA.get(j + 1).y + Aoffsety);
                    if (Math.Abs((s2.y - s1.y) * direction.x - (s2.x - s1.x) * direction.y) < TOL)
                    {
                        continue;
                    }

                    // project point, ignore edge boundaries
                    d = pointDistance(p, s1, s2, direction, null);

                    if (d != null && (minprojection == null || d < minprojection))
                    {
                        minprojection = d;
                        minp = p;
                    }
                }
                if (minprojection != null && (distance == null || minprojection > distance))
                {
                    distance = minprojection;
                }
            }
            return distance;
        }

        public static bool intersect(NestPath CA,NestPath CB)
        {
            double Aoffsetx = CA.offsetX;
            double Aoffsety = CA.offsetY;

            double Boffsetx = CB.offsetX;
            double Boffsety = CB.offsetY;

            NestPath A = new NestPath(CA);
            NestPath B = new NestPath(CB);

            for (int i = 0; i < A.size() - 1; i++)
            {
                for (int j = 0; j < B.size() - 1; j++)
                {
                    Segment a1 = new Segment(A.get(i).x + Aoffsetx, A.get(i).y + Aoffsety);
                    Segment a2 = new Segment(A.get(i + 1).x + Aoffsetx, A.get(i + 1).y + Aoffsety);
                    Segment b1 = new Segment(B.get(j).x + Boffsetx, B.get(j).y + Boffsety);
                    Segment b2 = new Segment(B.get(j + 1).x + Boffsetx, B.get(j + 1).y + Boffsety);


                    int prevbindex = (j == 0) ? B.size() - 1 : j - 1;
                    int prevaindex = (i == 0) ? A.size() - 1 : i - 1;
                    int nextbindex = (j + 1 == B.size() - 1) ? 0 : j + 2;
                    int nextaindex = (i + 1 == A.size() - 1) ? 0 : i + 2;

                    // go even further back if we happen to hit on a loop end point
                    if (B.get(prevbindex) == B.get(j) || (almostEqual(B.get(prevbindex).x, B.get(j).x) && almostEqual(B.get(prevbindex).y, B.get(j).y)))
                    {
                        prevbindex = (prevbindex == 0) ? B.size() - 1 : prevbindex - 1;
                    }

                    if (A.get(prevaindex) == A.get(i) || (almostEqual(A.get(prevaindex).x, A.get(i).x) && almostEqual(A.get(prevaindex).y, A.get(i).y)))
                    {
                        prevaindex = (prevaindex == 0) ? A.size() - 1 : prevaindex - 1;
                    }

                    // go even further forward if we happen to hit on a loop end point
                    if (B.get(nextbindex) == B.get(j + 1) || (almostEqual(B.get(nextbindex).x, B.get(j + 1).x) && almostEqual(B.get(nextbindex).y, B.get(j + 1).y)))
                    {
                        nextbindex = (nextbindex == B.size() - 1) ? 0 : nextbindex + 1;
                    }

                    if (A.get(nextaindex) == A.get(i + 1) || (almostEqual(A.get(nextaindex).x, A.get(i + 1).x) && almostEqual(A.get(nextaindex).y, A.get(i + 1).y)))
                    {
                        nextaindex = (nextaindex == A.size() - 1) ? 0 : nextaindex + 1;
                    }

                    Segment a0 = new Segment(A.get(prevaindex).x + Aoffsetx, A.get(prevaindex).y + Aoffsety);
                    Segment b0 = new Segment(B.get(prevbindex).x + Boffsetx, B.get(prevbindex).y + Boffsety);

                    Segment a3 = new Segment(A.get(nextaindex).x + Aoffsetx, A.get(nextaindex).y + Aoffsety);
                    Segment b3 = new Segment(B.get(nextbindex).x + Boffsetx, B.get(nextbindex).y + Boffsety);

                    if (onSegment(a1, a2, b1) || (almostEqual(a1.x, b1.x, 0.01) && almostEqual(a1.y, b1.y, 0.01)))
                    {
                        // if a point is on a segment, it could intersect or it could not. Check via the neighboring points
                        Boolean? b0in = pointInPolygon(b0, A);
                        Boolean? b2in = pointInPolygon(b2, A);
                        if (b0in == null || b2in == null)
                        {
                            continue;
                        }
                        if ((b0in == true && b2in == false) || (b0in == false && b2in == true))
                        {

                            return true;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    if (onSegment(a1, a2, b2) || (almostEqual(a2.x, b2.x) && almostEqual(a2.y, b2.y)))
                    {
                        // if a point is on a segment, it could intersect or it could not. Check via the neighboring points
                        Boolean? b1in = pointInPolygon(b1, A);
                        Boolean? b3in = pointInPolygon(b3, A);
                        if (b1in == null || b3in == null)
                        {
                            continue;
                        }
                        if ((b1in == true && b3in == false) || (b1in == false && b3in == true))
                        {

                            return true;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    if (onSegment(b1, b2, a1) || (almostEqual(a1.x, b2.x) && almostEqual(a1.y, b2.y)))
                    {
                        // if a point is on a segment, it could intersect or it could not. Check via the neighboring points
                        Boolean? a0in = pointInPolygon(a0, B);
                        Boolean? a2in = pointInPolygon(a2, B);
                        if (a0in == null || a2in == null)
                        {
                            continue;
                        }
                        if ((a0in == true && a2in == false) || (a0in == false && a2in == true))
                        {

                            return true;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    if (onSegment(b1, b2, a2) || (almostEqual(a2.x, b1.x) && almostEqual(a2.y, b1.y)))
                    {
                        // if a point is on a segment, it could intersect or it could not. Check via the neighboring points
                        Boolean? a1in = pointInPolygon(a1, B);
                        Boolean? a3in = pointInPolygon(a3, B);
                        if (a1in == null || a3in == null)
                        {
                            continue;
                        }

                        if ((a1in == true && a3in == false) || (a1in == false && a3in == true))
                        {

                            return true;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    Segment p = lineIntersect(b1, b2, a1, a2, null);

                    if (p != null)
                    {

                        return true;
                    }
                }
            }

            return false;
        }

        public static Segment lineIntersect(Segment A, Segment B, Segment E, Segment F, bool? infinite)
        {
            double a1, a2, b1, b2, c1, c2, x, y;

            a1 = B.y - A.y;
            b1 = A.x - B.x;
            c1 = B.x * A.y - A.x * B.y;
            a2 = F.y - E.y;
            b2 = E.x - F.x;
            c2 = F.x * E.y - E.x * F.y;

            double denom = a1 * b2 - a2 * b1;

            x = (b1 * c2 - b2 * c1) / denom;
            y = (a2 * c1 - a1 * c2) / denom;

            if (Double.IsInfinity(x) || Double.IsInfinity(y))
            {
                //            System.out.println(" not infi ");
                return null;
            }

            if (infinite == null || !(bool)infinite)
            {
                // coincident points do not count as intersecting
                if (Math.Abs(A.x - B.x) > TOL && ((A.x < B.x) ? x < A.x || x > B.x : x > A.x || x < B.x)) return null;
                if (Math.Abs(A.y - B.y) > TOL && ((A.y < B.y) ? y < A.y || y > B.y : y > A.y || y < B.y)) return null;

                if (Math.Abs(E.x - F.x) > TOL && ((E.x < F.x) ? x < E.x || x > F.x : x > E.x || x < F.x)) return null;
                if (Math.Abs(E.y - F.y) > TOL && ((E.y < F.y) ? y < E.y || y > F.y : y > E.y || y < F.y)) return null;
            }
            return new Segment(x, y);
        }

        public static Double? polygonSlideDistance(NestPath TA , NestPath TB , Segment direction, bool ignoreNegative)
        {
            double Aoffsetx = TA.offsetX;
            double Aoffsety = TA.offsetY;

            double Boffsetx = TB.offsetX;
            double BoffsetY = TB.offsetY;

            NestPath A = new NestPath(TA);
            NestPath B = new NestPath(TB);

            if (A.get(0) != A.get(A.size() - 1))
            {
                A.add(A.get(0));
            }
            if (B.get(0) != B.get(B.size() - 1))
            {
                B.add(B.get(0));
            }

            NestPath edgeA = A;
            NestPath edgeB = B;

            Double? distance = null;


            Segment dir = normalizeVector(direction);

            Segment normal = new Segment(dir.y, -dir.x);

            Segment reverse = new Segment(-dir.x, -dir.y);

            Segment A1, A2, B1, B2 = null;
            for (int i = 0; i < edgeB.size() - 1; i++)
            {
                for (int j = 0; j < edgeA.size() - 1; j++)
                {
                    A1 = new Segment(edgeA.get(j).x + Aoffsetx, edgeA.get(j).y + Aoffsety);
                    A2 = new Segment(edgeA.get(j + 1).x + Aoffsetx, edgeA.get(j + 1).y + Aoffsety);
                    B1 = new Segment(edgeB.get(i).x + Boffsetx, edgeB.get(i).y + BoffsetY);
                    B2 = new Segment(edgeB.get(i + 1).x + Boffsetx, edgeB.get(i + 1).y + BoffsetY);

                    if ((almostEqual(A1.x, A2.x) && almostEqual(A1.y, A2.y)) || (almostEqual(B1.x, B2.x) && almostEqual(B1.y, B2.y)))
                    {
                        continue;
                    }
                    Double? d = segmentDistance(A1, A2, B1, B2, dir);

                    if (d != null && (distance == null || d < distance))
                    {
                        if (!ignoreNegative || d > 0 || almostEqual((double)d, 0))
                        {
                            distance = d;
                        }
                    }
                }
            }
            return distance;
        }

        public static Segment normalizeVector(Segment v)
        {
            if (almostEqual(v.x * v.x + v.y * v.y, 1))
            {
                return v;
            }
            double len = Math.Sqrt(v.x * v.x + v.y * v.y);
            double inverse = 1 / len;

            return new Segment(v.x * inverse, v.y * inverse);
        }

        public static Double? segmentDistance(Segment A, Segment B, Segment E, Segment F, Segment direction)
        {
            double SEGTOL = 10E-4;
            Segment normal = new Segment(direction.y, -direction.x);

            Segment reverse = new Segment(-direction.x, -direction.y);

            double dotA = A.x * normal.x + A.y * normal.y;
            double dotB = B.x * normal.x + B.y * normal.y;
            double dotE = E.x * normal.x + E.y * normal.y;
            double dotF = F.x * normal.x + F.y * normal.y;

            double crossA = A.x * direction.x + A.y * direction.y;
            double crossB = B.x * direction.x + B.y * direction.y;
            double crossE = E.x * direction.x + E.y * direction.y;
            double crossF = F.x * direction.x + F.y * direction.y;
            double crossABmin = Math.Min(crossA, crossB);
            double crossABmax = Math.Max(crossA, crossB);

            double crossEFmax = Math.Max(crossE, crossF);
            double crossEFmin = Math.Min(crossE, crossF);

            double ABmin = Math.Min(dotA, dotB);
            double ABmax = Math.Max(dotA, dotB);

            double EFmax = Math.Max(dotE, dotF);
            double EFmin = Math.Min(dotE, dotF);

            if (almostEqual(ABmax, EFmin, SEGTOL) || almostEqual(ABmin, EFmax, SEGTOL))
            {
                return null;
            }
            // segments miss eachother completely
            if (ABmax < EFmin || ABmin > EFmax)
            {
                return null;
            }
            double overlap;
            if ((ABmax > EFmax && ABmin < EFmin) || (EFmax > ABmax && EFmin < ABmin))
            {
                overlap = 1;
            }
            else
            {
                double minMax = Math.Min(ABmax, EFmax);
                double maxMin = Math.Max(ABmin, EFmin);

                double maxMax = Math.Max(ABmax, EFmax);
                double minMin = Math.Min(ABmin, EFmin);

                overlap = (minMax - maxMin) / (maxMax - minMin);
            }
            double crossABE = (E.y - A.y) * (B.x - A.x) - (E.x - A.x) * (B.y - A.y);
            double crossABF = (F.y - A.y) * (B.x - A.x) - (F.x - A.x) * (B.y - A.y);

            if (almostEqual(crossABE, 0) && almostEqual(crossABF, 0))
            {

                Segment ABnorm = new Segment(B.y - A.y, A.x - B.x);
                Segment EFnorm = new Segment(F.y - E.y, E.x - F.x);

                double ABnormlength = Math.Sqrt(ABnorm.x * ABnorm.x + ABnorm.y * ABnorm.y);
                ABnorm.x /= ABnormlength;
                ABnorm.y /= ABnormlength;

                double EFnormlength = Math.Sqrt(EFnorm.x * EFnorm.x + EFnorm.y * EFnorm.y);
                EFnorm.x /= EFnormlength;
                EFnorm.y /= EFnormlength;

                // segment normals must point in opposite directions
                if (Math.Abs(ABnorm.y * EFnorm.x - ABnorm.x * EFnorm.y) < SEGTOL && ABnorm.y * EFnorm.y + ABnorm.x * EFnorm.x < 0)
                {
                    // normal of AB segment must point in same direction as given direction vector
                    double normdot = ABnorm.y * direction.y + ABnorm.x * direction.x;
                    // the segments merely slide along eachother
                    if (almostEqual(normdot, 0, SEGTOL))
                    {
                        return null;
                    }
                    if (normdot < 0)
                    {
                        return (double)0;
                    }
                }
                return null;
            }
            List<Double?> distances = new List<Double?>();

            // coincident points
            if (almostEqual(dotA, dotE))
            {
                distances.Add(crossA - crossE);
            }
            else if (almostEqual(dotA, dotF))
            {
                distances.Add(crossA - crossF);
            }
            else if (dotA > EFmin && dotA < EFmax)
            {
                Double? d = pointDistance(A, E, F, reverse, false);
                if (d != null && almostEqual((double)d, 0))
                { //  A currently touches EF, but AB is moving away from EF
                    Double? dB = pointDistance(B, E, F, reverse, true);
                    if (dB < 0 || almostEqual((double)dB * overlap, 0))
                    {
                        d = null;
                    }
                }
                if (d != null)
                {
                    distances.Add(d);
                }
            }

            if (almostEqual(dotB, dotE))
            {
                distances.Add(crossB - crossE);
            }
            else if (almostEqual(dotB, dotF))
            {
                distances.Add(crossB - crossF);
            }
            else if (dotB > EFmin && dotB < EFmax)
            {
                Double? d = pointDistance(B, E, F, reverse, false);

                if (d != null && almostEqual((double)d, 0))
                { // crossA>crossB A currently touches EF, but AB is moving away from EF
                    Double? dA = pointDistance(A, E, F, reverse, true);
                    if (dA < 0 || almostEqual((double)dA * overlap, 0))
                    {
                        d = null;
                    }
                }
                if (d != null)
                {
                    distances.Add(d);
                }
            }

            if (dotE > ABmin && dotE < ABmax)
            {
                Double? d = pointDistance(E, A, B, direction, false);
                if (d != null && almostEqual((double)d, 0))
                { // crossF<crossE A currently touches EF, but AB is moving away from EF
                    Double? dF = pointDistance(F, A, B, direction, true);
                    if (dF < 0 || almostEqual((double)dF * overlap, 0))
                    {
                        d = null;
                    }
                }
                if (d != null)
                {
                    distances.Add(d);
                }
            }

            if (dotF > ABmin && dotF < ABmax)
            {
                Double? d = pointDistance(F, A, B, direction, false);
                if (d != null && almostEqual((double)d, 0))
                { // && crossE<crossF A currently touches EF, but AB is moving away from EF
                    Double? dE = pointDistance(E, A, B, direction, true);
                    if (dE < 0 || almostEqual((double)dE * overlap, 0))
                    {
                        d = null;
                    }
                }
                if (d != null)
                {
                    distances.Add(d);
                }
            }

            if (distances.Count == 0)
            {
                return null;
            }

            Double minElement = Double.MaxValue;
            foreach (Double d in distances)
            {
                if (d < minElement)
                {
                    minElement = d;
                }
            }
            return minElement;
        }

        public static Double? pointDistance(Segment p, Segment s1, Segment s2, Segment normal, Boolean? infinite)
        {
            normal = normalizeVector(normal);
            Segment dir = new Segment(normal.y, -normal.x);

            double pdot = p.x * dir.x + p.y * dir.y;
            double s1dot = s1.x * dir.x + s1.y * dir.y;
            double s2dot = s2.x * dir.x + s2.y * dir.y;

            double pdotnorm = p.x * normal.x + p.y * normal.y;
            double s1dotnorm = s1.x * normal.x + s1.y * normal.y;
            double s2dotnorm = s2.x * normal.x + s2.y * normal.y;


            if (infinite == null || !(bool)infinite)
            {
                if (((pdot < s1dot || almostEqual(pdot, s1dot)) && (pdot < s2dot || almostEqual(pdot, s2dot))) || ((pdot > s1dot || almostEqual(pdot, s1dot)) && (pdot > s2dot || almostEqual(pdot, s2dot))))
                {
                    return null; // dot doesn't collide with segment, or lies directly on the vertex
                }
                if ((almostEqual(pdot, s1dot) && almostEqual(pdot, s2dot)) && (pdotnorm > s1dotnorm && pdotnorm > s2dotnorm))
                {
                    return Math.Min(pdotnorm - s1dotnorm, pdotnorm - s2dotnorm);
                }
                if ((almostEqual(pdot, s1dot) && almostEqual(pdot, s2dot)) && (pdotnorm < s1dotnorm && pdotnorm < s2dotnorm))
                {
                    return -Math.Min(s1dotnorm - pdotnorm, s2dotnorm - pdotnorm);
                }
            }

            return -(pdotnorm - s1dotnorm + (s1dotnorm - s2dotnorm) * (s1dot - pdot) / (s1dot - s2dot));
        }

        /**
         * 专门为环绕矩形生成的nfp
         * @param A
         * @param B
         * @return
         */
        public static List<NestPath> noFitPolygonRectangle(NestPath A, NestPath B)
        {
            double minAx = A.get(0).x;
            double minAy = A.get(0).y;
            double maxAx = A.get(0).x;
            double maxAy = A.get(0).y;

            for (int i = 1; i < A.size(); i++)
            {
                if (A.get(i).x < minAx)
                {
                    minAx = A.get(i).x;
                }
                if (A.get(i).y < minAy)
                {
                    minAy = A.get(i).y;
                }
                if (A.get(i).x > maxAx)
                {
                    maxAx = A.get(i).x;
                }
                if (A.get(i).y > maxAy)
                {
                    maxAy = A.get(i).y;
                }
            }

            double minBx = B.get(0).x;
            double minBy = B.get(0).y;
            double maxBx = B.get(0).x;
            double maxBy = B.get(0).y;
            for (int i = 1; i < B.size(); i++)
            {
                if (B.get(i).x < minBx)
                {
                    minBx = B.get(i).x;
                }
                if (B.get(i).y < minBy)
                {
                    minBy = B.get(i).y;
                }
                if (B.get(i).x > maxBx)
                {
                    maxBx = B.get(i).x;
                }
                if (B.get(i).y > maxBy)
                {
                    maxBy = B.get(i).y;
                }
            }



            if (maxBx - minBx > maxAx - minAx)
            {

                return null;
            }
            double diffBy = maxBy - minBy;
            double diffAy = maxAy - minAy;

            if (diffBy > diffAy)
            {
                return null;
            }


            List<NestPath> nfpRect = new List<NestPath>();
            NestPath res = new NestPath();
            res.add(minAx - minBx + B.get(0).x, minAy - minBy + B.get(0).y);
            res.add(maxAx - maxBx + B.get(0).x, minAy - minBy + B.get(0).y);
            res.add(maxAx - maxBx + B.get(0).x, maxAy - maxBy + B.get(0).y);
            res.add(minAx - minBx + B.get(0).x, maxAy - maxBy + B.get(0).y);
            nfpRect.Add(res);
            return nfpRect;
        }

        /**
         *
         * @param A
         * @param B
         * @return
         */
        public static List<NestPath> minkowskiDifference(NestPath A, NestPath B)
        {
            Path Ac = PlacementWorker.scaleUp2ClipperCoordinates(A);
            Path Bc = PlacementWorker.scaleUp2ClipperCoordinates(B);
            for (int i = 0; i < Bc.Count; i++)
            {
                long X = Bc[i].X;
                long Y = Bc[i].Y;
                IntPoint intPoint = Bc[i];
                intPoint.X= (-1 * X);
                intPoint.Y= (-1 * Y);
                Bc[i] = intPoint;
            }
            Paths solution = Clipper.MinkowskiSum(Ac, Bc, true);
            double largestArea = Double.MaxValue;
            NestPath clipperNfp = null;
            for (int i = 0; i < solution.Count; i++)
            {

                NestPath n = PlacementWorker.toNestCoordinates(solution[i]);
                double sarea = GeometryUtil.polygonArea(n);
                if (largestArea > sarea)
                {
                    clipperNfp = n;
                    largestArea = sarea;
                }
            }

            for (int i = 0; i < clipperNfp.size(); i++)
            {
                clipperNfp.get(i).x += B.get(0).x;
                clipperNfp.get(i).y += B.get(0).y;
            }
            List<NestPath> nfp = new List<NestPath>();
            nfp.Add(clipperNfp);
            return nfp;
        }

        public static NestPath linearize(Segment p1, Segment p2, double rx, double ry, double angle, int laregearc, int sweep, double tol)
        {
            NestPath finished = new NestPath();
            finished.add(p2);
            DataExchange arc = ConvertToCenter(p1, p2, rx, ry, angle, laregearc, sweep);
            Deque<DataExchange> list = new Deque<DataExchange>();
            list.AddToBack(arc);
            while (list.Count > 0)
            {
                arc = list.First();
                DataExchange fullarc = ConvertToSvg(arc.center, arc.rx, arc.ry, arc.theta, arc.extent, arc.angle);
                DataExchange subarc = ConvertToSvg(arc.center, arc.rx, arc.ry, arc.theta, 0.5 * arc.extent, arc.angle);
                Segment arcmid = subarc.p2;
                Segment mid = new Segment(0.5 * (fullarc.p1.x + fullarc.p2.x), 0.5 * (fullarc.p1.y + fullarc.p2.y));
                if (withinDistance(mid, arcmid, tol))
                {
                    finished.reverse(); finished.add(new Segment(fullarc.p2)); finished.reverse();
                    list.RemoveFromFront();
                }
                else
                {
                    DataExchange arc1 = new DataExchange(new Segment(arc.center), arc.rx, arc.ry, arc.theta, 0.5 * arc.extent, arc.angle, false);
                    DataExchange arc2 = new DataExchange(new Segment(arc.center), arc.rx, arc.ry, arc.theta + 0.5 * arc.extent, 0.5 * arc.extent, arc.angle, false);
                    list.RemoveFromFront();
                    list.AddToFront(arc2); list.AddToFront(arc1);
                }
            }
            return finished;
        }

        public static DataExchange ConvertToSvg(Segment center, double rx, double ry, double theta1, double extent, double angleDegrees)
        {
            double theta2 = theta1 + extent;

            theta1 = degreesToRadians(theta1);
            theta2 = degreesToRadians(theta2);
            double angle = degreesToRadians(angleDegrees);

            double cos = Math.Cos(angle);
            double sin = Math.Sin(angle);

            double t1cos = Math.Cos(theta1);
            double t1sin = Math.Sin(theta1);

            double t2cos = Math.Cos(theta2);
            double t2sin = Math.Sin(theta2);

            double x0 = center.x + cos * rx * t1cos + (-sin) * ry * t1sin;
            double y0 = center.y + sin * rx * t1cos + cos * ry * t1sin;

            double x1 = center.x + cos * rx * t2cos + (-sin) * ry * t2sin;
            double y1 = center.y + sin * rx * t2cos + cos * ry * t2sin;

            int largearc = (extent > 180) ? 1 : 0;
            int sweep = (extent > 0) ? 1 : 0;
            List<Segment> list = new List<Segment>();
            list.Add(new Segment(x0, y0)); list.Add(new Segment(x1, y1));
            return new DataExchange(new Segment(x0, y0), new Segment(x1, y1), rx, ry, angle, largearc, sweep, true);
        }

        public static DataExchange ConvertToCenter(Segment p1, Segment p2, double rx, double ry, double angleDgrees, int largearc, int sweep)
        {
            Segment mid = new Segment(0.5 * (p1.x + p2.x), 0.5 * (p1.y + p2.y));
            Segment diff = new Segment(0.5 * (p2.x - p1.x), 0.5 * (p2.y - p1.y));

            double angle = degreesToRadians(angleDgrees);
            double cos = Math.Cos(angle);
            double sin = Math.Sin(angle);

            double x1 = cos * diff.x + sin * diff.y;
            double y1 = -sin * diff.x + cos * diff.y;

            rx = Math.Abs(rx);
            ry = Math.Abs(ry);
            double Prx = rx * rx;
            double Pry = ry * ry;
            double Px1 = x1 * x1;
            double Py1 = y1 * y1;

            double radiiCheck = Px1 / Prx + Py1 / Pry;
            double radiiSqrt = Math.Sqrt(radiiCheck);
            if (radiiCheck > 1)
            {
                rx = radiiSqrt * rx;
                ry = radiiSqrt * ry;
                Prx = rx * rx;
                Pry = ry * ry;
            }

            double sign = (largearc != sweep) ? -1 : 1;
            double sq = ((Prx * Pry) - (Prx * Py1) - (Pry * Px1)) / ((Prx * Py1) + (Pry * Px1));

            sq = (sq < 0) ? 0 : sq;

            double coef = sign * Math.Sqrt(sq);
            double cx1 = coef * ((rx * y1) / ry);
            double cy1 = coef * -((ry * x1) / rx);

            double cx = mid.x + (cos * cx1 - sin * cy1);
            double cy = mid.y + (sin * cx1 + cos * cy1);

            double ux = (x1 - cx1) / rx;
            double uy = (y1 - cy1) / ry;
            double vx = (-x1 - cx1) / rx;
            double vy = (-y1 - cy1) / ry;
            double n = Math.Sqrt((ux * ux) + (uy * uy));
            double p = ux;
            sign = (uy < 0) ? -1 : 1;

            double theta = sign * Math.Acos(p / n);
            theta = radiansToDegree(theta);

            n = Math.Sqrt((ux * ux + uy * uy) * (vx * vx + vy * vy));
            p = ux * vx + uy * vy;
            sign = ((ux * vy - uy * vx) < 0) ? -1 : 1;
            double delta = sign * Math.Acos(p / n);
            delta = radiansToDegree(delta);

            if (sweep == 1 && delta > 0)
            {
                delta -= 360;
            }
            else if (sweep == 0 && delta < 0)
            {
                delta += 360;
            }
            delta %= 360;
            theta %= 360;
            List<Segment> list = new List<Segment>();
            list.Add(new Segment(cx, cy));
            return new DataExchange(new Segment(cx, cy), rx, ry, theta, delta, angleDgrees, false);

        }

        public static double degreesToRadians(double angle)
        {
            return angle * (Math.PI / 180);
        }

        public static double radiansToDegree(double angle)
        {
            return angle * (180 / Math.PI);
        }

       public   class DataExchange
        {
          internal  Segment p1;
            internal Segment p2;
            internal Segment center;
            internal double rx;
            internal double ry;
            internal double theta;
            internal double extent;
            internal double angle;
            internal double largearc;
            internal int sweep;
            internal bool flag;

            public DataExchange(Segment p1, Segment p2, double rx, double ry, double angle, double largearc, int sweep, bool flag)
            {
                this.p1 = p1;
                this.p2 = p2;
                this.rx = rx;
                this.ry = ry;
                this.angle = angle;
                this.largearc = largearc;
                this.sweep = sweep;
                this.flag = flag;
            }


            public DataExchange(Segment center, double rx, double ry, double theta, double extent, double angle, bool flag)
            {
                this.center = center;
                this.rx = rx;
                this.ry = ry;
                this.theta = theta;
                this.extent = extent;
                this.angle = angle;
                this.flag = flag;
            }

            
            public override String ToString()
            {
                String s = "";
                if (flag)
                {
                    s += " p1 = " + p1.ToString() + " p2 = " + p2.ToString() + "\n rx = " + rx + " ry = " + ry + " angle = " + angle + " largearc = " + largearc + " sweep = " + sweep;
                }
                else
                {
                    s += " center = " + center + "\n rx = " + rx + " ry = " + ry + " theta = " + theta + " extent = " + extent + " angle = " + angle;
                }
                return s;
            }
        }

        public static bool withinDistance(Segment p1, Segment p2, double distance)
        {
            double dx = p1.x - p2.x;
            double dy = p1.y - p2.y;
            return ((dx * dx + dy * dy) < distance * distance);
        }

        #region QuadraticBezier

        // Roger Willcocks bezier flatness criterion
        public static bool QuadraticBezierIsFlat(Segment p1, Segment p2, Segment c1, double tolerance)
        {
            var tol = 4 * tolerance * tolerance;

            var ux = 2 * c1.x - p1.x - p2.x;
            ux *= ux;

            var uy = 2 * c1.y - p1.y - p2.y;
            uy *= uy;

            return (ux + uy <= tol);
        }

        // turn Bezier into line segments via de Casteljau, returns an array of points
        public static List<Segment> QuadraticBezierLinearize(Segment p1, Segment p2, Segment c1, double tolerance)
        {
            var finished = new List<Segment> { p1 }; // list of points to return
            var todo = new List<List<Segment>> { new List<Segment> { p1, p2, c1 } }; // list of Beziers to divide

            // recursion could stack overflow, loop instead
            while (todo.Count > 0)
            {
                var segments = todo[0];

                if (QuadraticBezierIsFlat(segments[0], segments[1], segments[2], tolerance))
                { // reached subdivision limit
                    finished.Add(new Segment(segments[1].x, segments[1].y));
                    todo.Remove(todo[0]);
                }
                else
                {
                    var divided = QuadraticBezierSubdivide(segments[0], segments[1], segments[2], 0.5);
                    todo.Remove(todo[0]);
                    todo.Add(divided[0]);
                    todo.Add(divided[1]);
                }
            }
            return finished;
        }

        // subdivide a single Bezier
        // t is the percent along the Bezier to divide at. eg. 0.5
        public static List<List<Segment>> QuadraticBezierSubdivide(Segment p1, Segment p2, Segment c1, double t)
        {
            var mid1 = new Segment(x: p1.x + (c1.x - p1.x) * t, y: p1.y + (c1.y - p1.y) * t);
            var mid2 = new Segment(x: c1.x + (p2.x - c1.x) * t, y: c1.y + (p2.y - c1.y) * t);
            var mid3 = new Segment(x: mid1.x + (mid2.x - mid1.x) * t, y: mid1.y + (mid2.y - mid1.y) * t);

            var seg1 = new List<Segment> { p1, mid3, mid1 };
            var seg2 = new List<Segment> { mid3, p2, mid2 };
            return new List<List<Segment>> { seg1, seg2 };
        }

        #endregion

        #region CubicBezier

        // Roger Willcocks bezier flatness criterion
        public static bool CubicBezierIsFlat(Segment p1, Segment p2, Segment c1, Segment c2, double tolerance)
        {
            var tol = 16 * tolerance * tolerance;

            var ux = 3 * c1.x - 2 * p1.x - p2.x;
            ux *= ux;

            var uy = 3 * c1.y - 2 * p1.y - p2.y;
            uy *= uy;

            var vx = 3 * c2.x - 2 * p2.x - p1.x;
            vx *= vx;

            var vy = 3 * c2.y - 2 * p2.y - p1.y;
            vy *= vy;

            if (ux < vx)
            {
                ux = vx;
            }
            if (uy < vy)
            {
                uy = vy;
            }

            return (ux + uy <= tol);
        }

        // turn Bezier into line segments via de Casteljau, returns an array of points
        public static List<Segment> CubicBezierLinearize(Segment p1, Segment p2, Segment c1, Segment c2, double tolerance)
        {
            var finished = new List<Segment> { p1 }; // list of points to return
            var todo = new List<List<Segment>> { new List<Segment> { p1, p2, c1, c2}}; // list of Beziers to divide

            // recursion could stack overflow, loop instead
            while (todo.Count > 0)
            {
                var segments = todo[0];

                if (CubicBezierIsFlat(segments[0], segments[1], segments[2], segments[3], tolerance))
                { // reached subdivision limit
                    finished.Add(new Segment(segments[1].x, segments[1].y));
                    todo.Remove(todo[0]);
                }
                else
                {
                    var divided = CubicBezierSubdivide(segments[0], segments[1], segments[2], segments[3], 0.5);
                    todo.Remove(todo[0]);
                    todo.Add(divided[0]);
                    todo.Add(divided[1]);
                }
            }
            return finished;
        }

        // subdivide a single Bezier
        // t is the percent along the Bezier to divide at. eg. 0.5
        public static List<List<Segment>> CubicBezierSubdivide(Segment p1, Segment p2, Segment c1, Segment c2, double t)
        {
            var mid1 = new Segment(x: p1.x + (c1.x - p1.x) * t, y: p1.y + (c1.y - p1.y) * t);
            var mid2 = new Segment(x: c2.x + (p2.x - c2.x) * t, y: c2.y + (p2.y - c2.y) * t);
            var mid3 = new Segment(x: c1.x + (c2.x - c1.x) * t, y: c1.y + (c2.y - c1.y) * t);
            var mida = new Segment(x: mid1.x + (mid3.x - mid1.x) * t, y: mid1.y + (mid3.y - mid1.y) * t);
            var midb = new Segment(x: mid3.x + (mid2.x - mid3.x) * t, y: mid3.y + (mid2.y - mid3.y) * t);
            var midx = new Segment(x: mida.x + (midb.x - mida.x) * t, y: mida.y + (midb.y - mida.y) * t);

            var seg1 = new List<Segment> { p1, midx, mid1, mida };
            var seg2 = new List<Segment> { midx, p2, midb, mid2 };
            return new List<List<Segment>> { seg1, seg2 };
        }

        #endregion

    }
}
