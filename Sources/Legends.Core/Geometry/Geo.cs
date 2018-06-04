﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Legends.Core.Geometry
{
    public class Geo
    {
        public static float GetDistance(Vector2 p1, Vector2 p2)
        {
            return (float)Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
        }
        public static float GetAngle(Vector2 p1, Vector2 p2)
        {
            return (float)Math.Atan2(p2.Y - p1.Y, p2.X - p1.X);
        }
        public static Vector2 GetPointOnCircle(Vector2 centerPoint, float angle, float distance)
        {
            return new Vector2((float)(centerPoint.X + (distance * Math.Cos(angle))), (float)(centerPoint.Y + (distance * Math.Sin(angle))));
        }
    }
}