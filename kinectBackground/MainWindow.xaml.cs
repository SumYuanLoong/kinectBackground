using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using Microsoft.Kinect.Wpf.Controls;
using LightBuzz.Vitruvius;
using System.Diagnostics;

namespace kinectBackground
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		//normal kinect shite
		KinectSensor kinect;
		MultiSourceFrameReader _reader;

		//Background removal stuff
		BackGroundRemovalTool _back;
		IList<Body> _bodies;
		double distance;

		//Identity stuff
		Token _token;
		CognitiveCall cc = new CognitiveCall();
		bool userAuthenticated, dataReceived;
		WriteableBitmap bitmap;

		public MainWindow() {
			InitializeComponent();
			_token = new Token();
			_token.skeletonID = 254;//255 is default null and 254 is unreachable
			userAuthenticated = false;
			distance = 9;//max distance of detection is 8m
		}
		private void Window_Loaded(object sender, RoutedEventArgs e) {
			kinect = KinectSensor.GetDefault();

			if (kinect != null)
			{
				kinect.Open();

				// 2) Initialize the background removal tool.
				_back = new BackGroundRemovalTool(kinect.CoordinateMapper);

				_reader = kinect.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.BodyIndex | FrameSourceTypes.Body);
				_reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;
			}
		}

		private void Window_Closed(object sender, EventArgs e) {
			if (_reader != null)
			{
				_reader.Dispose();
			}

			if (kinect != null)
			{
				kinect.Close();
			}
		}

		async void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e) {
			//init the frames
			var reference = e.FrameReference.AcquireFrame();
			using (var colorFrame = reference.ColorFrameReference.AcquireFrame())
			using (var depthFrame = reference.DepthFrameReference.AcquireFrame())
			using (var bodyIndexFrame = reference.BodyIndexFrameReference.AcquireFrame())
			using (var bodyFrame = reference.BodyFrameReference.AcquireFrame())
			{
				dataReceived = false;
				//repopultate the body List
				if (bodyFrame != null)
				{
					if (this._bodies == null)
					{
						_bodies = new Body[bodyFrame.BodyFrameSource.BodyCount];
					}

					bodyFrame.GetAndRefreshBodyData(_bodies);
					dataReceived = true;
				}

				if (!userAuthenticated && dataReceived && _bodies.Any(body => body.IsTracked == true))
				{
					_token = new Token();
					Body num = _bodies.Closest();
					//Trace.WriteLine(num.TrackingId);
					Trace.WriteLine(Length(num.Joints[JointType.SpineBase].Position));
					if (Length(num.Joints[JointType.SpineBase].Position) < 2.3)
					{
						_token.skeletonID = _bodies.IndexOf(num);
						Trace.WriteLine(_token.skeletonID + "from outer");
						try
						{
							bitmap = _back.GreenScreen(colorFrame, depthFrame, bodyIndexFrame, _token.skeletonID);
							camera.Source = bitmap;
							_token = await cc.ImageToBinary(bitmap, _token);
							if (_token.serToken != null)
							{
								Trace.WriteLine("user found");
								userAuthenticated = true;
								_token.bod = num;
							}
								
						} catch {	}
					}


				} else if (userAuthenticated && dataReceived)
				{
					if (_bodies.All(b => b.IsTracked == false))
					{
						Trace.WriteLine("tracking released");
						invalidToken();
						userAuthenticated = false;
					} else if(_bodies[_token.skeletonID].TrackingId==_token.bod.TrackingId)
					{
						Trace.WriteLine("tracking released");
						invalidToken();
						userAuthenticated = false;
					}
					if (Length(_bodies[_token.skeletonID].Joints[JointType.SpineBase].Position) > 3)
					{
						Trace.WriteLine("tracking released");
						invalidToken();
						userAuthenticated = false;
					}else
					removeBG(colorFrame, depthFrame, bodyIndexFrame, _token.skeletonID);
				} 
				//else
				//{
				//	Trace.WriteLine("tracking released");
				//	invalidToken();
				//	userAuthenticated = false;
				//}





				//try
				//{
				//	colorFrame.Dispose();
				//	depthFrame.Dispose();
				//	bodyIndexFrame.Dispose();
				//	bodyCountFrame.Dispose();
				//} catch
				//{
				//}
			}
		}


		

		/// <summary>
		/// removes the background and updates the colorFrame
		/// </summary>
		/// <param name="colorFrame"></param>
		/// <param name="depthFrame"></param>
		/// <param name="bodyIndexFrame"></param>
		/// <param name="skeleID"></param>
		private void removeBG(ColorFrame colorFrame, DepthFrame depthFrame, BodyIndexFrame bodyIndexFrame, int i) {
			if (colorFrame != null && depthFrame != null && bodyIndexFrame != null)
			{
				Console.WriteLine(_token.skeletonID);
				// 3) Update the image source.
				camera.Source = _back.GreenScreen(colorFrame, depthFrame, bodyIndexFrame, i);
			}
		}


		/// <summary>
		/// Calculates the distance the user is away from the kinect
		/// </summary>
		/// <param name="point">the point at which the spine base of the user</param>
		/// <returns></returns>
		private double Length(CameraSpacePoint point) {
			return Math.Sqrt(
				point.X * point.X +
				point.Y * point.Y +
				point.Z * point.Z
			);
		}


		/// <summary>
		/// invalidates the token when user has left
		/// </summary>
		private void invalidToken() {
			_token = null;
		}
	}
}



