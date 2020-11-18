﻿using NestingLibPort.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NestingLibPort.Data
{

    public class Segment
    {
        public double x;
        public double y;

        public bool marked = false;
        public Segment start;//多边形的起点
        public Segment end;//多边形的终点


       
        public override bool Equals(Object obj)
        {
            Segment s = (Segment)obj;
            if (x == s.x && y == s.y)
            {
                return true;
            }
            return false;
        }

        public Segment()
        {
        }


        public Segment(Segment srcSeg)
        {
            this.x = srcSeg.x;
            this.y = srcSeg.y;
        }

        public Segment(int x, int y)
        {
            this.x = (double)x;
            this.y = (double)y;
        }

        //点的坐标
        public Segment(double x, double y)
        {
            int Ix = (int)(x * Config.CLIIPER_SCALE);
            int Iy = (int)(y * Config.CLIIPER_SCALE);

            this.x = (double)Ix * 1.0 / Config.CLIIPER_SCALE;
            this.y = (double)Iy * 1.0 / Config.CLIIPER_SCALE;
        }

      
        public override String ToString()
        {
            return "x = " + x + ", y = " + y;
        }

        public bool isMarked()
        {
            return marked;
        }

        public void setMarked(bool marked)
        {
            this.marked = marked;
        }

        public Segment getStart()
        {
            return start;
        }

        public void setStart(Segment start)
        {
            this.start = start;
        }

        public Segment getEnd()
        {
            return end;
        }

        public void setEnd(Segment end)
        {
            this.end = end;
        }

        public double getX()
        {
            return x;
        }

        public void setX(double x)
        {
            int lx = (int)(x * Config.CLIIPER_SCALE);
            this.x = lx * 1.0 / Config.CLIIPER_SCALE;
        }

        public double getY()
        {
            return y;
        }

        public void setY(double y)
        {
            int ly = (int)(y * Config.CLIIPER_SCALE);
            this.y = ly * 1.0 / Config.CLIIPER_SCALE;
        }
    }

}