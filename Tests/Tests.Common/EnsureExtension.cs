namespace Tests.Common
{
    using System;
    using System.Linq;

    namespace EnsureExtension
    {
        public static class EnsureExtension
        {
            public static void EnsureNull<T>(this T? src) where T : class
            {
                if (src != null)
                {
                    throw new InvalidOperationException("Expected value to be null.");
                }
            }
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
