using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DJPad.UI.Common
{
    public class WatchableProperty<T>
    {
        public delegate void ValueChanged(T newValue);

        public event ValueChanged Changed;

        private T value;

        public T Change
        {
            get { return this.value; }
            set
            {
                this.value = value;
                if (this.Changed != null)
                {
                    this.Changed(value);
                }
            }
        }
    }
}
