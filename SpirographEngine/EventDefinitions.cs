//  Copyright (c) 2008, Michael unfried
//  Email:  serbius3@gmail.com
//  All rights reserved.

//  Redistribution and use in source and binary forms, with or without modification, 
//  are permitted provided that the following conditions are met:

//  Redistributions of source code must retain the above copyright notice, 
//  this list of conditions and the following disclaimer. 
//  Redistributions in binary form must reproduce the above copyright notice, 
//  this list of conditions and the following disclaimer in the documentation 
//  and/or other materials provided with the distribution. 

//  THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
//  KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//  IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR
//  PURPOSE. IT CAN BE DISTRIBUTED FREE OF CHARGE AS LONG AS THIS HEADER 
//  REMAINS UNCHANGED.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RainstormStudios.Drawing.Spirograph
{
    public delegate void PaintEventHandler(object sender, PaintEventArgs e);
    public class PaintEventArgs : EventArgs
    {
        public readonly IntPtr
            WindowHandle;
        public readonly double
            X1, X2, Y1, Y2, H;
        public readonly double
            RadiusA, RadiusB;
        public readonly double
            Theta;
        public readonly int
            Angle;

        public PaintEventArgs(IntPtr hwnd, double x1, double y1, double x2, double y2, int a, double t, double ra, double rb, double h)
        {
            this.WindowHandle = hwnd;
            this.X1 = x1;
            this.X2 = x2;
            this.Y1 = y1;
            this.Y2 = y2;
            this.Angle = a;
            this.Theta = t;
            this.RadiusA = ra;
            this.RadiusB = rb;
            this.H = h;
        }
    }
}
