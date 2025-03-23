// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    // Polyfill CoreLib internal interfaces and methods
    // Define necessary members only

    internal static partial class Number
    {
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = nameof(AllowHyphenDuringParsing))]
        internal static bool AllowHyphenDuringParsing(this NumberFormatInfo info);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = nameof(PositiveSignTChar))]
        internal static extern ReadOnlySpan<TChar> PositiveSignTChar<TChar>(this NumberFormatInfo info)
            where TChar : unmanaged, IBinaryInteger<TChar>;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = nameof(NegativeSignTChar))]
        internal static extern ReadOnlySpan<TChar> NegativeSignTChar<TChar>(this NumberFormatInfo info)
            where TChar : unmanaged, IBinaryInteger<TChar>;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = nameof(CurrencySymbolTChar))]
        internal static extern ReadOnlySpan<TChar> CurrencySymbolTChar<TChar>(this NumberFormatInfo info)
            where TChar : unmanaged, IBinaryInteger<TChar>;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = nameof(PercentSymbolTChar))]
        internal static extern ReadOnlySpan<TChar> PercentSymbolTChar<TChar>(this NumberFormatInfo info)
            where TChar : unmanaged, IBinaryInteger<TChar>;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = nameof(PerMilleSymbolTChar))]
        internal static extern ReadOnlySpan<TChar> PerMilleSymbolTChar<TChar>(this NumberFormatInfo info)
            where TChar : unmanaged, IBinaryInteger<TChar>;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = nameof(CurrencyDecimalSeparatorTChar))]
        internal static extern ReadOnlySpan<TChar> CurrencyDecimalSeparatorTChar<TChar>(this NumberFormatInfo info)
            where TChar : unmanaged, IBinaryInteger<TChar>;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = nameof(CurrencyGroupSeparatorTChar))]
        internal static extern ReadOnlySpan<TChar> CurrencyGroupSeparatorTChar<TChar>(this NumberFormatInfo info)
            where TChar : unmanaged, IBinaryInteger<TChar>;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = nameof(NumberDecimalSeparatorTChar))]
        internal static extern ReadOnlySpan<TChar> NumberDecimalSeparatorTChar<TChar>(this NumberFormatInfo info)
            where TChar : unmanaged, IBinaryInteger<TChar>;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = nameof(NumberGroupSeparatorTChar))]
        internal static extern ReadOnlySpan<TChar> NumberGroupSeparatorTChar<TChar>(this NumberFormatInfo info)
            where TChar : unmanaged, IBinaryInteger<TChar>;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = nameof(PercentDecimalSeparatorTChar))]
        internal static extern ReadOnlySpan<TChar> PercentDecimalSeparatorTChar<TChar>(this NumberFormatInfo info)
            where TChar : unmanaged, IBinaryInteger<TChar>;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = nameof(PercentGroupSeparatorTChar))]
        internal static extern ReadOnlySpan<TChar> PercentGroupSeparatorTChar<TChar>(this NumberFormatInfo info)
            where TChar : unmanaged, IBinaryInteger<TChar>;
    }
}
