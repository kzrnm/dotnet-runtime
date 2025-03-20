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
        internal static bool AllowHyphenDuringParsing(this NumberFormatInfo info)
        {
            string negativeSign = info.NegativeSign;
            return negativeSign.Length == 1 &&
                   negativeSign[0] switch
                   {
                       '\u2012' or         // Figure Dash
                       '\u207B' or         // Superscript Minus
                       '\u208B' or         // Subscript Minus
                       '\u2212' or         // Minus Sign
                       '\u2796' or         // Heavy Minus Sign
                       '\uFE63' or         // Small Hyphen-Minus
                       '\uFF0D' => true,   // Fullwidth Hyphen-Minus
                       _ => false
                   };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlySpan<TChar> PositiveSignTChar<TChar>(this NumberFormatInfo info)
            where TChar : unmanaged, IBinaryInteger<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(char));
            return MemoryMarshal.Cast<char, TChar>(info.PositiveSign);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlySpan<TChar> NegativeSignTChar<TChar>(this NumberFormatInfo info)
            where TChar : unmanaged, IBinaryInteger<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(char));
            return MemoryMarshal.Cast<char, TChar>(info.NegativeSign);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlySpan<TChar> CurrencySymbolTChar<TChar>(this NumberFormatInfo info)
            where TChar : unmanaged, IBinaryInteger<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(char));
            return MemoryMarshal.Cast<char, TChar>(info.CurrencySymbol);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlySpan<TChar> PercentSymbolTChar<TChar>(this NumberFormatInfo info)
            where TChar : unmanaged, IBinaryInteger<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(char));
            return MemoryMarshal.Cast<char, TChar>(info.PercentSymbol);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlySpan<TChar> PerMilleSymbolTChar<TChar>(this NumberFormatInfo info)
            where TChar : unmanaged, IBinaryInteger<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(char));
            return MemoryMarshal.Cast<char, TChar>(info.PerMilleSymbol);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlySpan<TChar> CurrencyDecimalSeparatorTChar<TChar>(this NumberFormatInfo info)
            where TChar : unmanaged, IBinaryInteger<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(char));
            return MemoryMarshal.Cast<char, TChar>(info.CurrencyDecimalSeparator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlySpan<TChar> CurrencyGroupSeparatorTChar<TChar>(this NumberFormatInfo info)
            where TChar : unmanaged, IBinaryInteger<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(char));
            return MemoryMarshal.Cast<char, TChar>(info.CurrencyGroupSeparator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlySpan<TChar> NumberDecimalSeparatorTChar<TChar>(this NumberFormatInfo info)
            where TChar : unmanaged, IBinaryInteger<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(char));
            return MemoryMarshal.Cast<char, TChar>(info.NumberDecimalSeparator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlySpan<TChar> NumberGroupSeparatorTChar<TChar>(this NumberFormatInfo info)
            where TChar : unmanaged, IBinaryInteger<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(char));
            return MemoryMarshal.Cast<char, TChar>(info.NumberGroupSeparator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlySpan<TChar> PercentDecimalSeparatorTChar<TChar>(this NumberFormatInfo info)
            where TChar : unmanaged, IBinaryInteger<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(char));
            return MemoryMarshal.Cast<char, TChar>(info.PercentDecimalSeparator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlySpan<TChar> PercentGroupSeparatorTChar<TChar>(this NumberFormatInfo info)
            where TChar : unmanaged, IBinaryInteger<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(char));
            return MemoryMarshal.Cast<char, TChar>(info.PercentGroupSeparator);
        }
    }
}
