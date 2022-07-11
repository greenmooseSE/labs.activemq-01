namespace Tests.Common
{
    using System;
    using System.Linq;

    namespace EnsureExtension
    {
        public static class EnsureExtension
        {
            public static T EnsureNotNull<T>(this T? src, string name = "") where T : class
            {
                if (src == null)
                {
                    throw new ArgumentNullException(name);
                }

                return src;
            }
        }
    }
}
