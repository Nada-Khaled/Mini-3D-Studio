using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphics
{
    public class KeyFrame
    {
        public Dictionary<string, List<float>> verticesDict;
        public int numOfInterpolatedFrames;

        public KeyFrame(Dictionary<string, List<float>> V, int numFrames)
        {
            verticesDict = new Dictionary<string, List<float>>(V);
            numOfInterpolatedFrames = numFrames;
        }
    }
}
