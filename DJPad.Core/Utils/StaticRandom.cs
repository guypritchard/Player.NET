using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DJPad.Core.Utils
{
    public static class StaticRandom
    {
        private static Random random = new Random();

        public static int Next(int max)
        {
            lock (random)
            {
                return random.Next(max);
            }
        }
    }
}
