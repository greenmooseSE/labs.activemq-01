namespace RestApi.Common
{
    namespace EnsureExtension
    {
        public static class EnsureExtension
        {
            #region Public members

            public static T EnsureNotNull<T>(this T? src, string name = "") where T : class
            {
                if (src == null)
                {
                    throw new ArgumentNullException(name);
                }

                return src;
            }

            public static Guid EnsureNotNull(this Guid? src, string name = "")
            {
                if (src == null)
                {
                    throw new ArgumentNullException(name);
                }

                return src.Value;
            }

            public static void EnsureNull<T>(this T? src)
            {
                if (src != null)
                {
                    throw new InvalidOperationException("Expected value to be null.");
                }
            }

            #endregion
        }
    }
}
