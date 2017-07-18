using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;

namespace kinectBackground
{
	class CognitiveCall
	{
		//public MainWindow main;
		HttpClient client = new HttpClient();
		string name;
		public async Task<Token> MakeRequest(byte[] ms, Token _token) {
			try
			{
				StreamContent scontent = new StreamContent(new MemoryStream(ms));
				scontent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
				{
					FileName = "kinectImage",	//this is the name you want to give the file
					Name = "userImage"			//this is the key
				};
				scontent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
				var client = new HttpClient();
				var multi = new MultipartFormDataContent();
				multi.Add(scontent);
				var uri = new Uri("https://fypadminconsoletest.azurewebsites.net/API/cognitive/deidentify/");
				var result = await client.PostAsync(uri, multi);
				Debug.WriteLine(result);
				var obj = await result.Content.ReadAsStringAsync();
				Debug.WriteLine(obj);
				var person = JsonConvert.DeserializeObject<dynamic>(obj);
				if(person.status == "identified")
				{
					_token.username = person.name;
					_token.serToken = person.token;
					_token.userId = person.userId;
				}
				


				return _token;
			} catch(Exception e)
			{
				Debug.WriteLine(e);
				return _token;
			}
		}

		static byte[] ImageToBinary(string imagePath) {
			FileStream fileStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read);
			byte[] buffer = new byte[fileStream.Length];
			fileStream.Read(buffer, 0, (int)fileStream.Length);
			fileStream.Close();
			return buffer;
		}

		//change image to byte array v1
		public async Task<Token> ImageToBinary(WriteableBitmap image, Token _token) {
			byte[] data;
			JpegBitmapEncoder encoder = new JpegBitmapEncoder();
			encoder.Frames.Add(BitmapFrame.Create(image));
			using (MemoryStream ms = new MemoryStream())
			{
				encoder.Save(ms);
				data = ms.ToArray();
			}
			_token =  await MakeRequest(data,_token);
			return _token;
		}
	}
}
