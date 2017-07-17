using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Diagnostics;

namespace kinectBackground
{
	class BackGroundRemovalTool
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

		#endregion

		#region Constructor

		/// <summary>
		/// Creates a new instance of BackgroundRemovalTool.
		/// </summary>
		/// <param name="mapper">The coordinate mapper used for the background removal.</param>
		public BackGroundRemovalTool(CoordinateMapper mapper) {
			_coordinateMapper = mapper;
		}

		#endregion


		public BitmapSource GreenScreen(ColorFrame colorFrame, DepthFrame depthFrame, BodyIndexFrame bodyIndexFrame, int indexID) {
			int colorWidth = colorFrame.FrameDescription.Width;
			int colorHeight = colorFrame.FrameDescription.Height;

			int depthWidth = depthFrame.FrameDescription.Width;
			int depthHeight = depthFrame.FrameDescription.Height;

			int bodyIndexWidth = bodyIndexFrame.FrameDescription.Width;
			int bodyIndexHeight = bodyIndexFrame.FrameDescription.Height;

			//get list of detected bodies into array
			//IList<Body> bodies = new Body[bodyFrame.BodyFrameSource.BodyCount];
			//bodyFrame.GetAndRefreshBodyData(bodies);

			//foreach(var body in bodies)
			//{
			//	Console.WriteLine(body.TrackingId+"ID from body frame");
				
			//}


			if (_displayPixels == null)
			{
				_depthData = new ushort[depthWidth * depthHeight];
				_bodyData = new byte[depthWidth * depthHeight];
				_colorData = new byte[colorWidth * colorHeight * BYTES_PER_PIXEL];
				_displayPixels = new byte[depthWidth * depthHeight * BYTES_PER_PIXEL];
				_colorPoints = new ColorSpacePoint[depthWidth * depthHeight];
				_bitmap = new WriteableBitmap(depthWidth, depthHeight, DPI, DPI, FORMAT, null);
			}

			if (((depthWidth * depthHeight) == _depthData.Length) && ((colorWidth * colorHeight * BYTES_PER_PIXEL) == _colorData.Length) && ((bodyIndexWidth * bodyIndexHeight) == _bodyData.Length))
			{
				depthFrame.CopyFrameDataToArray(_depthData);

				if (colorFrame.RawColorImageFormat == ColorImageFormat.Bgra)
				{
					colorFrame.CopyRawFrameDataToArray(_colorData);
				} else
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
						int depthIndex = (y * depthWidth) + x;  //each pixel in a frame
						//Trace.WriteLine(depthIndex+" depthIndex");
						byte player = _bodyData[depthIndex]; //255 is null player
						//Trace.WriteLine(_bodyData[depthIndex].ToString() + "ID from bodyIndex");


						if (player != 0xff && player == indexID)//&& player== bodyID
						{
							ColorSpacePoint colorPoint = _colorPoints[depthIndex];

							int colorX = (int)Math.Floor(colorPoint.X + 0.5);
							int colorY = (int)Math.Floor(colorPoint.Y + 0.5);

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

				_bitmap.Lock();

				Marshal.Copy(_displayPixels, 0, _bitmap.BackBuffer, _displayPixels.Length);
				_bitmap.AddDirtyRect(new Int32Rect(0, 0, depthWidth, depthHeight));

				_bitmap.Unlock();
			}

			return _bitmap;
		}
	}
}
