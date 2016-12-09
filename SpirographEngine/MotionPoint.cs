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
using System.Drawing;
using System.Linq;
using System.Text;

namespace RainstormStudios.Drawing.Spirograph
{
    public class MotionPoint : ICloneable, IDisposable
    {
        #region Nested Types
        //***************************************************************************
        // Enumerations
        // 
        public enum MotionDirection
        {
            None = -1,
            Bounce = 0,
            Zoom,
            Rotate,
            Snow,
            FromLeft,
            FromRight,
            FromTop,
            FromBottom
        }
        public enum MotionSpeed
        {
            VerySlow = 2,
            Slow = 5,
            Default = 10,
            Fast = 20,
            VeryFast = 30,
            Warp = 40
        }
        public enum AlphaFade
        {
            None = 0,
            Linear,
            Logarythmic
        }
        #endregion

        #region Declarations
        //***************************************************************************
        // Private Fields
        // 
        private MotionPointCollection _owner;
        private float _xPos, _yPos;         // The physical position of the point in coordinate (X/Y) space.
        private float _xSpd, _ySpd;         // The speed of the point in coordinate (X/Y) space.
        private float _xCen, _yCen;         // The center of the origin rectangle in coordinate (X/Y) space.
        private float _rad;                 // For the rotation type, this stores the radius.
        private int _offset;                // For the rotation type, provides a compuational offset for the position.
        private MotionDirection _mDir;      // The type of movement this point should calculate.
        private MotionSpeed _mSpd;          // The speed of movement for this point's calculations.
        private RectangleF _orgBounds;      // The origin coorinate bounds for this point.
        private RectangleF _drwBounds;      // The draw bounds for this point.
        private Color _clr;                 // This point's color.
        private int _steps;                 // The number of times this point's position has been calculated.
        private bool _cycClr;               // Whether or not this point should cycle its color.
        private bool _bubble;               // Whether or not this point exhibits "bubble motion"
        private AlphaFade _aFade;           // The type of alpha-blend fading to calculate.
        private int _life;                  // Number of iterations for which the point should remain visible if it doesn't leave the draw area.
        private int _lifeVar;               // Number of iterations variance in point lifespan.
        private int _fadeIn;                // The number of steps to fade in for.
        private int _fadeOut;               // The number of steps to fade out for, if the point is going to 'die'.
        private int _rotPrec;               // The base precision with which to calculate the rotation equation.
        Random rnd;                         // A random number generator.
        #endregion

        #region Properties
        //***************************************************************************
        // Public Properties
        // 
        public MotionPointCollection Owner
        {
            get { return _owner; }
            set { _owner = value; }
        }
        public MotionDirection MovementType
        {
            get { return _mDir; }
            set { _mDir = value; }
        }
        public MotionSpeed MovementSpeed
        {
            get { return _mSpd; }
            set { _mSpd = value; }
        }
        public Rectangle DrawBounds
        {
            get { return new Rectangle((int)_drwBounds.Left, (int)_drwBounds.Top, (int)_drwBounds.Width, (int)_drwBounds.Height); }
            set { _drwBounds = new RectangleF((float)value.Left, (float)value.Top, (float)value.Width, (float)value.Height); }
        }
        public RectangleF DrawBoundsF
        {
            get { return _drwBounds; }
            set { _drwBounds = value; }
        }
        public Rectangle OriginBounds
        {
            get { return new Rectangle((int)_orgBounds.Left, (int)_orgBounds.Top, (int)_orgBounds.Width, (int)_orgBounds.Height); }
            //set { _orgBounds = new RectangleF((float)value.Left, (float)value.Top, (float)value.Width, (float)value.Height); }
        }
        public RectangleF OriginBoundsF
        {
            get { return _orgBounds; }
            //set { _orgBounds = value; }
        }
        public int xPosition
        {
            get { return (int)_xPos; }
        }
        public int yPosition
        {
            get { return (int)_yPos; }
        }
        public float xPositionF
        {
            get { return _xPos; }
        }
        public float yPositionF
        {
            get { return _yPos; }
        }
        public float xSpeed
        {
            get { return _xSpd; }
            set { _xSpd = value; }
        }
        public float ySpeed
        {
            get { return _ySpd; }
            set { _ySpd = value; }
        }
        public float xCenter
        {
            get { return _xCen; }
        }
        public float yCenter
        {
            get { return _yCen; }
        }
        public int FadeIn
        {
            get { return _fadeIn; }
            set { _fadeIn = value; }
        }
        public int FadeOut
        {
            get { return _fadeOut; }
            set { _fadeOut = value; }
        }
        public int Iterations
        {
            get { return _steps; }
        }
        public Color PointColor
        {
            get { return _clr; }
            set { _clr = value; }
        }
        public bool CycleColor
        {
            get { return _cycClr; }
            set { _cycClr = value; }
        }
        public bool BubbleMotion
        {
            get { return _bubble; }
            set { _bubble = value; }
        }
        public int LifeSpan
        {
            get { return _life; }
            set { _life = value; }
        }
        public int LifeSpaceVariance
        {
            get { return _lifeVar; }
            set
            {
                _lifeVar = (value > _life) ? _life : value;
                _life += rnd.Next(_lifeVar * 2) - _lifeVar;
            }
        }
        public AlphaFade AlphaFadeMode
        {
            get { return _aFade; }
            set { _aFade = value; }
        }
        public bool isVisible
        {
            get { return (this.xPositionF > this.DrawBoundsF.Left && this.xPosition < this.DrawBoundsF.Right && this.yPosition > this.DrawBoundsF.Top && this.yPosition < this.DrawBoundsF.Bottom) ? true : false; }
        }
        public bool isAlive
        {
            get { return (this._life > 0 && this._steps > _life) ? false : true; }
        }
        public int RotationPrecesion
        {
            get { return _rotPrec / 30; }
            set
            {
                if (value < 1)
                    _rotPrec = (30 * 1);
                else if (value > 10)
                    _rotPrec = (30 * 10);
                else
                    _rotPrec = (30 * value);
                this._offset = rnd.Next(_rotPrec);
            }
        }
        public int RotationOffset
        {
            get { return _offset; }
            set { _offset = value; }
        }
        public float RotationRadius
        {
            get { return _rad; }
            set { _rad = value; }
        }
        public int AlphaLevel
        {
            get
            {
                int retValue = 128;
                if (this._aFade != AlphaFade.None)
                {
                    // Some alpha's are specific to motion type.
                    if (_mDir == MotionDirection.Zoom)
                        retValue = _steps * 3;
                    else if (_mDir == MotionDirection.Snow)
                        retValue = rnd.Next(50, 255);
                    else if (_mDir == MotionDirection.FromTop || _mDir == MotionDirection.FromBottom)
                        retValue = ((Convert.ToInt32(System.Math.Abs(_ySpd)) * 255) / 10);
                    else if (_mDir == MotionDirection.FromLeft || _mDir == MotionDirection.FromRight)
                        retValue = ((Convert.ToInt32(System.Math.Abs(_xSpd)) * 255) / 10);

                    // If they setup the fade in/out times, add those values.
                    if (this._steps < this._fadeIn)
                        retValue = 0 + ((_steps * 255) / _fadeIn);
                    else if (this._life > 0 && this._steps > (this._life - this._fadeOut))
                        retValue = 255 - (((_steps - (_life - _fadeOut)) * 255) / _fadeOut);
                }
                else
                    retValue = 255;

                return (retValue > -1) ? ((retValue < 256) ? retValue : 255) : 0;
            }
        }
        #endregion

        #region Class Constructors
        //***************************************************************************
        // Class Constructors
        // 
        public MotionPoint(Rectangle drawBounds, MotionDirection direction, MotionSpeed speed, Color color, MotionPointCollection owner)
            : this(new RectangleF((float)drawBounds.Left, (float)drawBounds.Top, (float)drawBounds.Width, (float)drawBounds.Height), direction, speed, color, owner)
        { }
        public MotionPoint(RectangleF originBounds, MotionDirection direction, MotionSpeed speed, Color color, MotionPointCollection owner)
        {
            this._owner = owner;
            this._orgBounds = originBounds;
            this._mDir = direction;
            this._mSpd = speed;
            this._clr = color;
            this._bubble = false;
            this._life = 0;
            this._aFade = AlphaFade.None;
            this._drwBounds = originBounds;
            this._xCen = originBounds.Width / 2;
            this._yCen = originBounds.Height / 2;

            if (owner != null)
                rnd = this._owner.rnd;
            else
                rnd = new Random(Convert.ToInt32((DateTime.Now.Ticks / 20) + (DateTime.Now.Millisecond * (DateTime.Now.TimeOfDay.Ticks / 10))));

            if (direction > MotionDirection.FromLeft)
            {
                // Calculate for Left/Right/Top/Bottom movement types.

                if (direction > MotionDirection.FromTop)
                {
                    // Top/Bottom
                    _xPos = rnd.Next((int)originBounds.Left, (int)originBounds.Right);
                    if (direction == MotionDirection.FromTop)
                        _yPos = originBounds.Top;
                    else
                        _yPos = originBounds.Bottom;
                }
                else
                {
                    // Left/Right
                    if (direction == MotionDirection.FromLeft)
                        _xPos = originBounds.Left;
                    else
                        _xPos = originBounds.Right;
                    _yPos = rnd.Next((int)originBounds.Top, (int)originBounds.Bottom);
                }
                _xSpd = rnd.Next(1, 10);
                _ySpd = rnd.Next(1, 10);
            }
            else
            {
                // For free-random origin types

                _xPos = rnd.Next((int)originBounds.Left, (int)originBounds.Right);
                _yPos = rnd.Next((int)originBounds.Top, (int)originBounds.Bottom);

                if (direction == MotionDirection.Zoom)
                {
                    _xSpd = (_xPos - _xCen) / 2;
                    _ySpd = (_yPos - _yCen) / 2;
                }
                else if (direction == MotionDirection.Bounce)
                {
                    _xSpd = rnd.Next(10) - 5;
                    if (_xSpd == 0)
                        _xSpd = 1 * System.Math.Sign((originBounds.Width / 2) - _xPos);

                    _ySpd = rnd.Next(10) - 5;
                    if (_ySpd == 0)
                        _ySpd = 1 * System.Math.Sign((originBounds.Height / 2) - _yPos);
                }
                else if (direction == MotionDirection.Snow)
                {
                    _xSpd = rnd.Next(1, 5);
                    _ySpd = rnd.Next(1, 5);
                }
                else if (direction == MotionDirection.Rotate)
                {
                    this.RotationPrecesion = 2;
                    _rad = rnd.Next(2, (int)(originBounds.Width / 2));
                    _offset = rnd.Next(_rotPrec * 2);
                }
            }
        }
        #endregion

        #region Public Methods
        //***************************************************************************
        // Public Methods
        // 
        public void Dispose()
        {
            this._drwBounds = RectangleF.Empty;
            this._orgBounds = RectangleF.Empty;
            this._clr = Color.Empty;
            _xPos = 0;
            _xSpd = 0;
            _yPos = 0;
            _ySpd = 0;
            _steps = 0;
        }
        public object Clone()
        {
            return this.MemberwiseClone();
        }
        public void MovePoint()
        {
            _steps++;
            switch (this._mDir)
            {
                case MotionDirection.None:
                    // Nothing to do here.
                    break;
                case MotionDirection.Bounce:
                    _xPos += _xSpd;
                    if (_xPos >= _drwBounds.Right || _xPos <= _drwBounds.Left)
                        _xSpd *= (System.Math.Sign(_xSpd) * -1);

                    _yPos += _ySpd;
                    if (_yPos >= _drwBounds.Bottom || _yPos <= _drwBounds.Top)
                        _ySpd *= (System.Math.Sign(_ySpd) * -1);
                    break;
                case MotionDirection.Snow:
                    _xPos += _xSpd;
                    if (_xPos >= _drwBounds.Right)
                        _xSpd *= System.Math.Sign(_xSpd);

                    _yPos += _ySpd;
                    if (_yPos >= _drwBounds.Right || _yPos <= _drwBounds.Left)
                        _ySpd *= System.Math.Sign(_ySpd);
                    break;
                case MotionDirection.Zoom:
                    //_xPos += (_xSpd * (((int)_mSpd) / 10)) + (_steps / 10);
                    //_yPos += (_ySpd * (((int)_mSpd) / 10)) + (_steps / 10);
                    this._xPos += (((_xSpd + ((int)_mSpd)) / 10) * ((((_steps / 10) > 0) ? (_steps / 10) : 1) * System.Math.Sign(_xSpd))) - 1;
                    this._yPos += (((_ySpd + ((int)_mSpd)) / 10) * ((((_steps / 10) > 0) ? (_steps / 10) : 1) * System.Math.Sign(_ySpd))) - 1;
                    break;
                case MotionDirection.Rotate:
                    int t = _steps + _offset;
                    while (t > (_rotPrec + _offset))
                        t -= (_rotPrec + _offset);
                    double a = System.Math.PI * t / ((_rotPrec + _offset) / 2);
                    _xPos = (float)((DrawBoundsF.Width / 2) + _rad * System.Math.Sin(a));
                    _yPos = (float)((DrawBoundsF.Height / 2) + _rad * System.Math.Cos(a));
                    break;
                case MotionDirection.FromLeft:
                    _xPos += _xSpd;
                    break;
                case MotionDirection.FromRight:
                    _xSpd -= _xSpd;
                    break;
                case MotionDirection.FromTop:
                    _yPos += _ySpd;
                    break;
                case MotionDirection.FromBottom:
                    _yPos -= _ySpd;
                    break;
            }
        }
        public RectangleF GetEllipseF()
        {
            return GetEllipseF(2);
        }
        public RectangleF GetEllipseF(int Radius)
        {
            return new Rectangle((int)(this._xPos - (Radius / 2)), (int)(this._yPos - (Radius / 2)), Radius, Radius);
        }
        //***************************************************************************
        // Static Methods
        // 
        public static int RandVect()
        {
            return Rand(5, 10);
        }
        public static int Rand(int max)
        {
            return Rand(0, max);
        }
        public static int Rand(int min, int max)
        {
            return rndVect.Next(min, max);
        }
        #endregion

        #region Private Methods
        //***************************************************************************
        // Static Methods
        // 
        private static Random rndVect = new Random();
        #endregion
    }
    public class MotionPointCollection : ICollection<MotionPoint>
    {
        #region Declarations
        //***************************************************************************
        // Private Fields
        // 
        public readonly Random rnd;
        System.Collections.ArrayList
            _innerList;
        #endregion

        #region Properties
        //***************************************************************************
        // Public Properties
        // 
        public MotionPoint this[int index]
        {
            get { return (MotionPoint)this._innerList[index]; }
            set { this._innerList[index] = value; }
        }
        public int Count
        { get { return this._innerList.Count; } }
        public bool IsReadOnly
        { get { return false; } }
        #endregion

        #region Class Constructors
        //***************************************************************************
        // Class Constructors
        // 
        public MotionPointCollection()
        {
            this.rnd = new Random();
            this._innerList = new System.Collections.ArrayList();
        }
        #endregion

        #region Public Methods
        //***************************************************************************
        // Public Methods
        // 
        public void Add(MotionPoint value)
        {
            this._innerList.Add(value);
        }
        public bool Remove(MotionPoint value)
        {
            if (this._innerList.Contains(value)) {
                this._innerList.Remove(value);
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool Contains(MotionPoint value)
        {
            return this._innerList.Contains(value);
        }
        public int IndexOf(MotionPoint value)
        {
            return this._innerList.IndexOf(value);
        }
        public void Clear()
        {
            this._innerList.Clear();
        }
        public void CopyTo(MotionPoint[] ar, int idx)
        {
            throw new NotImplementedException();
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            // Because IEnumerable<T> inherits IEnumerable, we have to explicitly implement this interface.
            return this.GetEnumerator();
        }
        public IEnumerator<MotionPoint> GetEnumerator()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
