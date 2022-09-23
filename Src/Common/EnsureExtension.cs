namespace Common
{
    namespace EnsureExtension
    {
        public static class EnsureExtension
        {
            #region Public members

            public static void EnsureEmpty<T>(this IReadOnlyCollection<T> collection, string name = "")
            {
                if (collection.Count != 0)
                {
                    throw new InvalidOperationException($"Expected collection {name} to be empty.");
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

            public static void EnsureTrue(this bool value)
            {
                if (!value)
                {
                    throw new InvalidOperationException("Expected value to be true.");
                }
            }

            #endregion
        }
    }
}
