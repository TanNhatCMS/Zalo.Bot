using System.Runtime.CompilerServices;

#pragma warning disable IDE0130, MA0003

namespace Zalo.Bot.Extensions
{
    internal static class ObjectExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T ThrowIfNull<T>(this T? value, [CallerArgumentExpression(nameof(value))] string? parameterName = default)
            => value ?? throw new ArgumentNullException(parameterName);
    }
}
