using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//package com.qunhe.util.nest.util;
namespace NestingLibPort.Util
{
    public class Config
    {
        public static int CLIIPER_SCALE = 10000;
        public static double CURVE_TOLERANCE = 0.02;
        public double SPACING;
        public int POPULATION_SIZE;
        public int MUTATION_RATE;
        private bool CONCAVE;
        public bool USE_HOLE;


        public Config()
        {
            CLIIPER_SCALE = 10000;
            CURVE_TOLERANCE = 0.3;//贝塞尔曲线路径和圆弧的线性近似所允许的最大误差，以SVG单位或“像素”为单位。如果弯曲部分看起来略微重叠，则减小此值。
            SPACING = 0;//在套料过程中，所有板件两两之间的距离
            POPULATION_SIZE = 10;//利用遗传算法时所生成的族群个体数量    
            MUTATION_RATE = 10;//利用遗传算法时，套料顺序的变异几率
            CONCAVE = false;//是否允许以一些性能和放置鲁棒性为代价解决凹面情况：
            USE_HOLE = false;//当板件中存在空心板件时，是否允许将板件放在空心板件当中
        }

        public bool isCONCAVE()
        {
            return CONCAVE;
        }

        public bool isUSE_HOLE()
        {
            return USE_HOLE;
        }
    }
}
