using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UPnPTest.Utils
{
    using System.Threading;
    using System.Threading.Tasks;

    public class PeriodicTask
    {
        public static async Task Run(Action action, TimeSpan period, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                action();
                await Task.Delay(period, cancellationToken);
            }
        }

        public static Task Run(Action action, TimeSpan period)
        {
            return Run(action, period, CancellationToken.None);
        }
    }
}
