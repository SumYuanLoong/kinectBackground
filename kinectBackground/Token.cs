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
		public ulong skeletonID {
			get { return this.skeletonID; }
			set	{ this.skeletonID = value; }
		}
		public int serToken
		{
			get { return this.serToken; }
			set { this.serToken = value; }
		}
		public string Username
		{
			get { return this.Username; }
			set { this.Username = value; }
		}

		public bool compareSkeleton(ulong Id) {
			return Id.Equals(skeletonID);
		}


	}
}
