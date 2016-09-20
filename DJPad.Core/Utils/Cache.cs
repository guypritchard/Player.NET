namespace DJPad.Core.Utils
{
    using System;
    using System.Collections.Generic;

    public class Cache<T>
    {
        private Func<T,T> generator;
        public Cache(Func<T,T> generatorFunc)
        {
            this.generator = generatorFunc;
        }
    
        private Dictionary<T, T> values = new Dictionary<T,T>();

        public T this[T key]
        {
            get
            {
                if (!values.ContainsKey(key))
                {
                    this.values.Add(key, this.generator(key));
                }

                return this.values[key];
            }
        }

    }
}