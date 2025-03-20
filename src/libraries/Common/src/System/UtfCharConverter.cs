// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace System
{
    // NOTE: This is a workaround for current inlining limitations of some backend code generators.
    // We would prefer to not have this class at all and instead just use TChar.CreateTruncuating.
    // Once inlining is improved on these hot code paths in formatting, we can remove this class.

    /// <summary>Internal class used to unify char and byte in formatting operations.</summary>
    internal static class UtfCharConverter
    {
        /// <summary>Casts the specified value to this type.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TChar CastFrom<TChar>(byte value) where TChar : unmanaged, IBinaryInteger<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(byte) || typeof(TChar) == typeof(char));
            return typeof(TChar) == typeof(byte)
                ? Unsafe.BitCast<byte, TChar>((byte)value)
                : Unsafe.BitCast<char, TChar>((char)value);
        }

        /// <summary>Casts the specified value to this type.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TChar CastFrom<TChar>(char value) where TChar : unmanaged, IBinaryInteger<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(byte) || typeof(TChar) == typeof(char));
            return typeof(TChar) == typeof(byte)
                ? Unsafe.BitCast<byte, TChar>((byte)value)
                : Unsafe.BitCast<char, TChar>((char)value);
        }

        /// <summary>Casts the specified value to this type.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TChar CastFrom<TChar>(int value) where TChar : unmanaged, IBinaryInteger<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(byte) || typeof(TChar) == typeof(char));
            return typeof(TChar) == typeof(byte)
                ? Unsafe.BitCast<byte, TChar>((byte)value)
                : Unsafe.BitCast<char, TChar>((char)value);
        }

        /// <summary>Casts the specified value to this type.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TChar CastFrom<TChar>(uint value) where TChar : unmanaged, IBinaryInteger<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(byte) || typeof(TChar) == typeof(char));
            return typeof(TChar) == typeof(byte)
                ? Unsafe.BitCast<byte, TChar>((byte)value)
                : Unsafe.BitCast<char, TChar>((char)value);
        }

        /// <summary>Casts the specified value to this type.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TChar CastFrom<TChar>(ulong value) where TChar : unmanaged, IBinaryInteger<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(byte) || typeof(TChar) == typeof(char));
            return typeof(TChar) == typeof(byte)
                ? Unsafe.BitCast<byte, TChar>((byte)value)
                : Unsafe.BitCast<char, TChar>((char)value);
        }

        /// <summary>Casts a value of this type to an UInt32.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint CastToUInt32<TChar>(TChar value) where TChar : unmanaged, IBinaryInteger<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(byte) || typeof(TChar) == typeof(char));
            return typeof(TChar) == typeof(byte)
                ? Unsafe.BitCast<TChar, byte>(value)
                : Unsafe.BitCast<TChar, char>(value);
        }
    }
}
