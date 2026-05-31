using Microsoft.Kinect;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KinectBackgroundR
{
    /// <summary>
    /// Provides extension methods for removing the background of a Kinect frame.
    /// </summary>
    public class BackgroundRemoval
    {
        #region Constants

        /// <summary>
        /// The DPI.
        /// </summary>
        readonly double DPI = 96.0;

        /// <summary>
        /// Default format.
        /// </summary>
        readonly PixelFormat FORMAT = PixelFormats.Bgra32;

        /// <summary>
        /// Bytes per pixel.
        /// </summary>
        readonly int BYTES_PER_PIXEL = (PixelFormats.Bgr32.BitsPerPixel + 7) / 8;

        #endregion

        #region Members

        /// <summary>
        /// The bitmap source.
        /// </summary>
        WriteableBitmap _bitmap = null;

        /// <summary>
        /// The depth values.
        /// </summary>
        ushort[] _depthData = null;

        /// <summary>
        /// The body index values.
        /// </summary>
        byte[] _bodyData = null;

        /// <summary>
        /// The RGB pixel values.
        /// </summary>
        byte[] _colorData = null;

        /// <summary>
        /// The RGB pixel values used for the background removal (green-screen) effect.
        /// </summary>
        byte[] _displayPixels = null;

        /// <summary>
        /// The color points used for the background removal (green-screen) effect.
        /// </summary>
        ColorSpacePoint[] _colorPoints = null;

        /// <summary>
        /// The coordinate mapper for the background removal (green-screen) effect.
        /// </summary>
        CoordinateMapper _coordinateMapper = null;
        private KinectLeap.MainWindow update;
        public string position = "";

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new instance of BackgroundRemovalTool.
        /// </summary>
        /// <param name="mapper">The coordinate mapper used for the background removal.</param>
        public BackgroundRemoval(CoordinateMapper mapper)
        {
            _coordinateMapper = mapper;
        }

        public BackgroundRemoval(KinectLeap.MainWindow update)
        {
            // TODO: Complete member initialization
            this.update = update;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Converts a depth frame to the corresponding System.Windows.Media.Imaging.BitmapSource and removes the background (green-screen effect).
        /// </summary>
        /// <param name="depthFrame">The specified depth frame.</param>
        /// <param name="colorFrame">The specified color frame.</param>
        /// <param name="bodyIndexFrame">The specified body index frame.</param>
        /// <returns>The corresponding System.Windows.Media.Imaging.BitmapSource representation of image.</returns>
        public BitmapSource GreenScreen(ColorFrame colorFrame, DepthFrame depthFrame, BodyIndexFrame bodyIndexFrame)
        {
            int colorWidth = colorFrame.FrameDescription.Width;
            int colorHeight = colorFrame.FrameDescription.Height;

            int depthWidth = depthFrame.FrameDescription.Width;
            int depthHeight = depthFrame.FrameDescription.Height;

            int bodyIndexWidth = bodyIndexFrame.FrameDescription.Width;
            int bodyIndexHeight = bodyIndexFrame.FrameDescription.Height;
            double pixels= 0.0;
            double pixels2 = 0.0;
            int count = 0;

            if (_displayPixels == null)
            {
                _depthData = new ushort[depthWidth * depthHeight];
                _bodyData = new byte[depthWidth * depthHeight];
                _colorData = new byte[colorWidth * colorHeight * BYTES_PER_PIXEL];
                _displayPixels = new byte[depthWidth * depthHeight * BYTES_PER_PIXEL];
                _colorPoints = new ColorSpacePoint[depthWidth * depthHeight];
                _bitmap = new WriteableBitmap(depthWidth, depthHeight, DPI, DPI, FORMAT, null);
            }

            if (((depthWidth * depthHeight) == _depthData.Length) && ((colorWidth * colorHeight * BYTES_PER_PIXEL) 
                == _colorData.Length) && ((bodyIndexWidth * bodyIndexHeight) == _bodyData.Length))
            {
                depthFrame.CopyFrameDataToArray(_depthData);

                if (colorFrame.RawColorImageFormat == ColorImageFormat.Bgra)
                {
                    colorFrame.CopyRawFrameDataToArray(_colorData);
                }
                else
                {
                    colorFrame.CopyConvertedFrameDataToArray(_colorData, ColorImageFormat.Bgra);
                }

                bodyIndexFrame.CopyFrameDataToArray(_bodyData);

                _coordinateMapper.MapDepthFrameToColorSpace(_depthData, _colorPoints);

                Array.Clear(_displayPixels, 0, _displayPixels.Length);

                for (int y = 0; y < depthHeight; ++y)
                {
                    for (int x = 0; x < depthWidth; ++x)
                    {
                        int depthIndex = (y * depthWidth) + x;
                        byte player = _bodyData[depthIndex];
                        
                        if (player != 0xff)
                        {
                            ColorSpacePoint colorPoint = _colorPoints[depthIndex];

                            int colorX = (int)Math.Floor(colorPoint.X + 0.5);
                            int colorY = (int)Math.Floor(colorPoint.Y + 0.5);

                            pixels2 += (colorPoint.Y / colorPoint.X);
                            pixels += (y / x);
                            count++;

                            if ((colorX >= 0) && (colorX < colorWidth) && (colorY >= 0) && (colorY < colorHeight))
                            {
                                int colorIndex = ((colorY * colorWidth) + colorX) * BYTES_PER_PIXEL;
                                int displayIndex = depthIndex * BYTES_PER_PIXEL;

                                _displayPixels[displayIndex + 0] = _colorData[colorIndex];
                                _displayPixels[displayIndex + 1] = _colorData[colorIndex + 1];
                                _displayPixels[displayIndex + 2] = _colorData[colorIndex + 2];
                                _displayPixels[displayIndex + 3] = 0xff;
                            }
                        }
                    }
                }

                var distance1 = pixels/count;
                var distance2 = (pixels2 / count);
                double ov = distance1 / distance2;

                 if((int)ov == 0 && distance1 < 0.7 && distance2 < 0.7)
                {
                   // Console.WriteLine("MOVE MORE TO THE LEFT PLEASE");
                    position = "MOVE MORE TO THE LEFT PLEASE";
                }
                else if ((int)ov == 1 && (distance1 > 0.9 && (distance2 > 0.7 && distance2 < 0.9)) && ov > 1.3)
                {
                    //Console.WriteLine("MOVE BACKWARDS");
                    position = "MOVE BACKWARDS";
                }
                else if ((int)ov > 1 && distance2 > 0.9)
                {
                   //Console.WriteLine("MOVE MORE TO THE RIGHT PLEASE");
                    position = "MOVE MORE TO THE RIGHT PLEASE";
                }
                else if (distance1 == distance2 && (ov<1.3 && ov>=1.0))
                 {
                     position = "CENTER OF BODY FOCUS";
                 }
                else
                {
                   // Console.WriteLine("BODY CENTERED");
                    position = "BODY CENTERED";
                }

                _bitmap.Lock();

                Marshal.Copy(_displayPixels, 0, _bitmap.BackBuffer, _displayPixels.Length);
                _bitmap.AddDirtyRect(new Int32Rect(0, 0, depthWidth, depthHeight));

                _bitmap.Unlock();
            }    
   
            return _bitmap;
        }


        public String Positioning(ColorFrame colorFrame, DepthFrame depthFrame, BodyIndexFrame bodyIndexFrame)
        {
            int colorWidth = colorFrame.FrameDescription.Width;
            int colorHeight = colorFrame.FrameDescription.Height;

            int depthWidth = depthFrame.FrameDescription.Width;
            int depthHeight = depthFrame.FrameDescription.Height;

            int bodyIndexWidth = bodyIndexFrame.FrameDescription.Width;
            int bodyIndexHeight = bodyIndexFrame.FrameDescription.Height;

            double pixels = 0.0;
            double pixels2 = 0.0;
            int count = 0;

            if (_displayPixels == null)
            {
                _depthData = new ushort[depthWidth * depthHeight];
                _bodyData = new byte[depthWidth * depthHeight];
                _colorData = new byte[colorWidth * colorHeight * BYTES_PER_PIXEL];
                _displayPixels = new byte[depthWidth * depthHeight * BYTES_PER_PIXEL];
                _colorPoints = new ColorSpacePoint[depthWidth * depthHeight];
                _bitmap = new WriteableBitmap(depthWidth, depthHeight, DPI, DPI, FORMAT, null);
            }

            if (((depthWidth * depthHeight) == _depthData.Length) && ((colorWidth * colorHeight * BYTES_PER_PIXEL)
                == _colorData.Length) && ((bodyIndexWidth * bodyIndexHeight) == _bodyData.Length))
            {
                depthFrame.CopyFrameDataToArray(_depthData);

                _coordinateMapper.MapDepthFrameToColorSpace(_depthData, _colorPoints);
                Array.Clear(_displayPixels, 0, _displayPixels.Length);

                for (int y = 0; y < depthHeight; ++y)
                {
                    for (int x = 0; x < depthWidth; ++x)
                    {
                        int depthIndex = (y * depthWidth) + x;

                        if (_bodyData[depthIndex] != 0xff)
                        {
                            ColorSpacePoint colorPoint = _colorPoints[depthIndex];

                            int colorX = (int)Math.Floor(colorPoint.X + 0.5);
                            int colorY = (int)Math.Floor(colorPoint.Y + 0.5);

                            pixels2 += (colorPoint.Y / colorPoint.X);
                            pixels += (y / x);
                            count++;

                            if ((colorX >= 0) && (colorX < colorWidth) && (colorY >= 0) && (colorY < colorHeight))
                            {
                                int colorIndex = ((colorY * colorWidth) + colorX) * BYTES_PER_PIXEL;
                                int displayIndex = depthIndex * BYTES_PER_PIXEL;
                            }
                        }
                    }
                }

                var distance1 = pixels / count;
                var distance2 = (pixels2 / count);
                double ov = distance1 / distance2;

                if ((int)ov == 0 && distance1 < 0.7 && distance2 < 0.7)
                {
                    // Console.WriteLine("MOVE MORE TO THE LEFT PLEASE");
                    position = "MOVE MORE TO THE LEFT PLEASE";
                }
                else if ((int)ov == 1 && (distance1 > 0.9 && (distance2 > 0.7 && distance2 < 0.9)) && ov > 1.3)
                {
                    //Console.WriteLine("MOVE BACKWARDS");
                    position = "MOVE BACKWARDS";
                }
                else if ((int)ov > 1 && distance2 > 0.9)
                {
                    //Console.WriteLine("MOVE MORE TO THE RIGHT PLEASE");
                    position = "MOVE MORE TO THE RIGHT PLEASE";
                }
                else
                {
                    // Console.WriteLine("BODY CENTERED");
                    position = "BODY CENTERED";
                }
            }

            return null;
        }
        #endregion
    }
}
