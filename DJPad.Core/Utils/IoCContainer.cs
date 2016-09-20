namespace DJPad.Core.Utils
{
    using System;
    using System.Collections.Generic;

    public static class IoCContainer
    {
        private static Dictionary<Type, Tuple<Type, object>> IocMapping = new Dictionary<Type, Tuple<Type, object>>();

        public static void AddMapping<T1, T2>(bool singleton = false)
        {
            IocMapping[typeof (T1)] = Tuple.Create(typeof (T2), singleton ? Activator.CreateInstance(typeof(T2)) : null);
        }

        public static T Get<T>()
        {
            var typeofT = typeof(T);

            if (IocMapping.ContainsKey(typeofT))
            {
                var mappingData = IocMapping[typeofT];

                return mappingData.Item2 != null 
                                            ? (T) mappingData.Item2
                                            : (T)Activator.CreateInstance(IocMapping[typeofT].Item1);
            }
         
            throw new InvalidOperationException(string.Format("The type '{0}' was not registered with the container.", typeofT.Name)); 
        }
    }
}
