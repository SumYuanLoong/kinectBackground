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
using LightBuzz.Vitruvius;

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
		double distance=9;
		Token _token;
		bool userActive;

		public MainWindow() {
			InitializeComponent();
			_token = new Token();
			_token.skeletonID = 0xff;
			userActive = false;
		}
		private void Window_Loaded(object sender, RoutedEventArgs e) {
			kinect = KinectSensor.GetDefault();

			if (kinect != null)
			{
				kinect.Open();

				// 2) Initialize the background removal tool.
				_back = new BackGroundRemovalTool(kinect.CoordinateMapper);

				_reader = kinect.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.BodyIndex);
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

		void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e) {
			//init the frames
			var reference = e.FrameReference.AcquireFrame();
			using (var colorFrame = reference.ColorFrameReference.AcquireFrame())
			using (var depthFrame = reference.DepthFrameReference.AcquireFrame())
			using (var bodyIndexFrame = reference.BodyIndexFrameReference.AcquireFrame())
			using (var bodyCountFrame = reference.BodyFrameReference.AcquireFrame())
			{

				//repopultate the body List
				if (bodyCountFrame != null)
				{
					_bodies = new Body[bodyCountFrame.BodyFrameSource.BodyCount];
					bodyCountFrame.GetAndRefreshBodyData(_bodies);



					//when there is no user using the system
					if (!userActive)
					{
						//find a user

						foreach (var body in _bodies)
						{
							var point = body.Joints[JointType.SpineBase].Position;
							var distance = Length(point);
							if (distance < 2.3)
							{
								// authenticate him
								_token.skeletonID = lowestDist(_bodies);
								userActive = true;
								//fkbtn(); //checks with server that the user is registered
								break;
							}
							Console.WriteLine("nouser close enough")
						}
					}

					if (userActive && _token.compareSkeleton(lowestDist(_bodies)))
					{
						removeBG(colorFrame, depthFrame, bodyIndexFrame, _token.skeletonID);

					} else
					{
						Console.WriteLine("token is invalidated");
						invalidToken();
						userActive = false;
					}
				}
			}

			//try
			//{
			//    colorFrame.Dispose();
			//    depthFrame.Dispose();
			//    bodyIndexFrame.Dispose();
			//}
			//catch
			//{
			//}
		}
		

		/// <summary>
		/// removes the background and updates the colorFrame
		/// </summary>
		/// <param name="colorFrame"></param>
		/// <param name="depthFrame"></param>
		/// <param name="bodyIndexFrame"></param>
		/// <param name="skeleID"></param>
		private void removeBG(ColorFrame colorFrame, DepthFrame depthFrame, BodyIndexFrame bodyIndexFrame, ulong skeleID) {
			if (colorFrame != null && depthFrame != null && bodyIndexFrame != null)
			{
				Console.WriteLine(_token.skeletonID);
				// 3) Update the image source.
				camera.Source = _back.GreenScreen(colorFrame, depthFrame, bodyIndexFrame, _token.skeletonID);
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
			distance = 9;
		}


		/// <summary>
		/// finds the cloest person to the kinect
		/// </summary>
		/// <param name="bodies">IList<Body> from BodyFrameSource.BodyCount</param>
		/// <returns></returns>
		private ulong lowestDist(IList<Body> bodies) {
			ulong ID= 0xff;

			foreach (var body in bodies)
			{
				var point = body.Joints[JointType.SpineBase].Position;
				if (Length(point) < distance)
				{
					distance = Length(point);
					ID = body.TrackingId;
				}
			}
			return ID;
			//return _token.skeletonID;
		}
	}
}
