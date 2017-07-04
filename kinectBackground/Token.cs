using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kinectBackground
{
	class Token
	{
		public ulong skeletonID;
		public int serToken;

		public string Username;


		public bool compareSkeleton(ulong Id) {
			return Id.Equals(skeletonID);
		}


	}
}
