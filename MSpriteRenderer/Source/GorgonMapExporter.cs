using sspack;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace MSpriteRenderer.Source
{
    class GorgonMapExporter : IMapExporter
    {
        public float atlasHeight;
        public float atlasWidth;
        string IMapExporter.MapExtension
        {
            get { return ".TAI";  }
        }

        void IMapExporter.Save(string filename, Dictionary<string, System.Drawing.Rectangle> map)
        {
            // Original filename                    New filename,     ?, 2D, X         Y,        MAGIC     Width     Height
            //.\Animations\player_fall_ne_9.png		0_Animations.png, 0, 2D, 0.000000, 0.000000, 0.000000, 0.058594, 0.064453
            
			// copy the files list and sort alphabetically
			string[] keys = new string[map.Count];
			map.Keys.CopyTo(keys, 0);
			List<string> outputFiles = new List<string>(keys);
			outputFiles.Sort();

			using (StreamWriter writer = new StreamWriter(filename))
			{
				foreach (var image in outputFiles)
				{
					// get the destination rectangle
					Rectangle destination = map[image];

					// write out the destination rectangle for this bitmap
                    //line = item.SourceName + "\t\t" + item.Destination + ", 0, 2D, " + item.ScaledRect.X.ToString("0.000000") + ", " +
                    //item.ScaledRect.Y.ToString("0.000000") + ", 0.000000, " + item.ScaledRect.Width.ToString("0.000000") + ", " + item.ScaledRect.Height.ToString("0.000000");
                    
					writer.WriteLine(string.Format(
                        "{0}\t\t{1}, 0, 2D, {2:F5}, {3:F5}, 0.00000, {4:F5}, {5:F5}", 
	                 	image, 
                        Path.GetFileNameWithoutExtension(filename)+".png",
	                 	(float)destination.X / atlasWidth,
                        (float)destination.Y / atlasHeight,
                        (float)destination.Width / atlasWidth,
                        (float)destination.Height / atlasHeight));
				}
			}
        }
    }
}
