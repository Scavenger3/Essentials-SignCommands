using System;
using System.IO;
using Newtonsoft.Json;

namespace Essentials
{
	public class esConfig
	{
		public bool ShowBackMessageOnDeath = true;
		public bool UsePermissonsForTeams = false;
		public string RedPassword = "";
		public string GreenPassword = "";
		public string BluePassword = "";
		public string YellowPassword = "";


		public static esConfig Read(string path)
		{
			if (!File.Exists(path))
				return new esConfig();
			using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				return Read(fs);
			}
		}

		public static esConfig Read(Stream stream)
		{
			using (var sr = new StreamReader(stream))
			{
				var cf = JsonConvert.DeserializeObject<esConfig>(sr.ReadToEnd());
				if (ConfigRead != null)
					ConfigRead(cf);
				return cf;
			}
		}

		public void Write(string path)
		{
			using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Write))
			{
				Write(fs);
			}
		}

		public void Write(Stream stream)
		{
			var str = JsonConvert.SerializeObject(this, Formatting.Indented);
			using (var sw = new StreamWriter(stream))
			{
				sw.Write(str);
			}
		}

		public static Action<esConfig> ConfigRead;
	}
}