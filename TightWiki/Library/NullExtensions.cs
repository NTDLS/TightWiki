using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace TightWiki.Library
{
    public static class NullExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T EnsureNotNull<T>([NotNull] this T? value, string? message = null, [CallerArgumentExpression(nameof(value))] string paramName = "")
        {
            if (value == null)
            {
                if (message == null)
                {
                    throw new ArgumentNullException(paramName, "Value should not be null.");
                }

                throw new ArgumentException(message, paramName);
            }
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Guid EnsureNotNullOrEmpty([NotNull] this Guid? value, [CallerArgumentExpression(nameof(value))] string strName = "")
        {
            if (!value.HasValue || value == Guid.Empty)
            {
                throw new ArgumentNullException("Value should not be null or empty: '" + strName + "'.");
            }
            return (Guid)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string EnsureNotNullOrEmpty([NotNull] this string? value, [CallerArgumentExpression(nameof(value))] string strName = "")
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException("Value should not be null or empty: '" + strName + "'.");
            }
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureNotNullOrWhiteSpace([NotNull] this string? value, [CallerArgumentExpression(nameof(value))] string strName = "")
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentNullException("Value should not be null or empty: '" + strName + "'.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T DefaultWhenNull<T>(this T? value, T defaultValue)
            => value == null ? defaultValue : value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string DefaultWhenNullOrEmpty(this string? value, string defaultValue)
            => string.IsNullOrEmpty(value) == true ? defaultValue : value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNull<T>(this T? value) where T : class
            => value == null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNullOrEmpty(this string value)
            => string.IsNullOrEmpty(value);
    }
}
