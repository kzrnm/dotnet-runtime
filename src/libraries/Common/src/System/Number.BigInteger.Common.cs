// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;

namespace System
{
    internal static partial class Number
    {
        public static void DivideGrammarSchool(Span<nuint> left, ReadOnlySpan<nuint> right, Span<nuint> quotient)
        {
            Debug.Assert(left.Length >= 1);
            Debug.Assert(right.Length >= 1);
            Debug.Assert(left.Length >= right.Length);
            Debug.Assert(
                quotient.Length == left.Length - right.Length + 1
#if !SYSTEM_PRIVATE_CORELIB
                // System.Runtime.Numerics
                || quotient.Length == 0
                || (BigIntegerCalculator.CompareActual(left.Slice(left.Length - right.Length), right) < 0 && quotient.Length == left.Length - right.Length)
#endif
                );

            // Executes the "grammar-school" algorithm for computing q = a / b.
            // Before calculating q_i, we get more bits into the highest bit
            // block of the divisor. Thus, guessing digits of the quotient
            // will be more precise. Additionally we'll get r = a % b.

            nuint divHi = right[^1];
            nuint divLo = right.Length > 1 ? right[^2] : 0;

            // We measure the leading zeros of the divisor
            int shift = (int)nuint.LeadingZeroCount(divHi);
            int backShift = nint.Size * 8 - shift;

            // And, we make sure the most significant bit is set
            if (shift > 0)
            {
                nuint divNx = right.Length > 2 ? right[^3] : 0;

                divHi = (divHi << shift) | (divLo >> backShift);
                divLo = (divLo << shift) | (divNx >> backShift);
            }

            // Then, we divide all of the bits as we would do it using
            // pen and paper: guessing the next digit, subtracting, ...
            for (int i = left.Length; i >= right.Length; i--)
            {
                int n = i - right.Length;
                nuint t = (uint)i < (uint)left.Length ? left[i] : 0;

                nuint valHi1 = t;
                nuint valHi0 = left[i - 1];
                nuint valLo = i > 1 ? left[i - 2] : 0;

                // We shifted the divisor, we shift the dividend too
                if (shift > 0)
                {
                    nuint valNx = i > 2 ? left[i - 3] : 0;

                    valHi1 = (valHi1 << shift) | (valHi0 >> backShift);
                    valHi0 = (valHi0 << shift) | (valLo >> backShift);
                    valLo = (valLo << shift) | (valNx >> backShift);
                }

                // First guess for the current digit of the quotient,
                // which naturally must have only native-width bits...
                nuint digit = valHi1 < divHi ? DivRem(valHi1, valHi0, divHi, out _) : nuint.MaxValue;

                // Our first guess may be a little bit to big
                while (DivideGuessTooBig(digit, valHi1, valHi0, valLo, divHi, divLo))
                {
                    --digit;
                }

                if (digit > 0)
                {
                    // Now it's time to subtract our current quotient
                    nuint carry = SubtractDivisor(left.Slice(n), right, digit);
                    if (carry != t)
                    {
                        Debug.Assert(carry == t + 1);

                        // Our guess was still exactly one too high
                        carry = AddDivisor(left.Slice(n), right);
                        --digit;

                        Debug.Assert(carry == 1);
                    }
                }

                // We have the digit!
                if ((uint)n < (uint)quotient.Length)
                {
                    quotient[n] = digit;
                }

                if ((uint)i < (uint)left.Length)
                {
                    left[i] = 0;
                }
            }

            static nuint AddDivisor(Span<nuint> left, ReadOnlySpan<nuint> right)
            {
                Debug.Assert(left.Length >= right.Length);

                // Repairs the dividend, if the last subtract was too much

                nuint carry = 0;

                for (int i = 0; i < right.Length; i++)
                {
                    ref nuint leftElement = ref left[i];

                    if (nint.Size == 8)
                    {
                        leftElement += right[i];
                        nuint c1 = (leftElement < right[i]) ? 1u : 0;
                        leftElement += carry;
                        nuint c2 = (leftElement < carry) ? 1u : 0;
                        carry = c1 + c2;
                    }
                    else
                    {
                        ulong digit = leftElement + carry + right[i];
                        leftElement = unchecked((uint)digit);
                        carry = (uint)(digit >> 32);
                    }
                }

                return carry;
            }

            static nuint SubtractDivisor(Span<nuint> left, ReadOnlySpan<nuint> right, nuint multiplier)
            {
                // Fused subtract-multiply by scalar: left[0..right.Length] -= right * multiplier.
                // Returns the borrow out. Unrolled by 4 on 64-bit.
                Debug.Assert(left.Length >= right.Length);

                int i = 0;
                nuint carry = 0;

                if (nint.Size == 8)
                {
                    for (; i + 3 < right.Length; i += 4)
                    {
                        carry = SubtractMul(ref left[i], right[i], multiplier, carry);
                        carry = SubtractMul(ref left[i + 1], right[i + 1], multiplier, carry);
                        carry = SubtractMul(ref left[i + 2], right[i + 2], multiplier, carry);
                        carry = SubtractMul(ref left[i + 3], right[i + 3], multiplier, carry);
                    }

                    for (; i < right.Length; i++)
                    {
                        carry = SubtractMul(ref left[i], right[i], multiplier, carry);
                    }
                }
                else
                {
                    for (; i < right.Length; i++)
                    {
                        ulong product = (ulong)right[i] * multiplier + carry;
                        uint lo = (uint)product;
                        uint hi = (uint)(product >> 32);

                        uint orig = (uint)left[i];
                        left[i] = orig - lo;
                        hi += (orig < lo) ? 1u : 0;

                        carry = hi;
                    }
                }

                return carry;
            }

            static nuint SubtractMul(ref nuint left, nuint right, nuint multiplier, nuint addend)
            {
                Debug.Assert(nint.Size == 8);
                UInt128 prod = Math.BigMul(right, multiplier) + (ulong)addend;
                nuint lo = (nuint)(ulong)prod;
                nuint hi = (nuint)(ulong)(prod >> 64);
                hi += (left < lo) ? (nuint)1 : 0;
                left -= lo;
                return hi;
            }

            static bool DivideGuessTooBig(nuint q, nuint valHi1, nuint valHi0,
                                                    nuint valLo, nuint divHi, nuint divLo)
            {
                // We multiply the two most significant limbs of the divisor
                // with the current guess for the quotient. If those are bigger
                // than the three most significant limbs of the current dividend
                // we return true, which means the current guess is still too big.

                if (nint.Size == 8)
                {
                    nuint chkHiHi = nuint.BigMul(divHi, q, out nuint chkHiLo);
                    nuint chkLoHi = nuint.BigMul(divLo, q, out nuint chkLoLo);

                    chkHiLo += chkLoHi;
                    if (chkHiLo < chkLoHi)
                    {
                        chkHiHi++;
                    }

                    return (chkHiHi > valHi1)
                        || ((chkHiHi == valHi1) && ((chkHiLo > valHi0) || ((chkHiLo == valHi0) && (chkLoLo > valLo))));
                }
                else
                {
                    ulong valHi = valHi0 | ((ulong)valHi1 << 32);
                    return DivideGuessTooBigUInt32(q, valHi, (uint)valLo, (uint)divHi, (uint)divLo);
                }
            }
        }

        private static bool DivideGuessTooBigUInt32(ulong q, ulong valHi, uint valLo, uint divHi, uint divLo)
        {
            Debug.Assert(q <= 0xFFFFFFFF);

            // We multiply the two most significant limbs of the divisor
            // with the current guess for the quotient. If those are bigger
            // than the three most significant limbs of the current dividend
            // we return true, which means the current guess is still too big.

            ulong chkHi = divHi * q;
            ulong chkLo = divLo * q;

            chkHi += (chkLo >> 32);
            chkLo = (uint)(chkLo);

            return (chkHi > valHi) || ((chkHi == valHi) && (chkLo > valLo));
        }

        private static ulong DivRemKnuth(ulong hi, ulong lo, ulong divisor, out ulong remainder)
        {
            // Knuth's algorithm
            // Perform 128-bit/64-bit division by splitting it into 32-bit parts.
            //
            // dividend = | hiHi | hiLo | loHi | loLo |
            //  divisor = |      |      |  dHi |  dLo |

            int shift = BitOperations.LeadingZeroCount(divisor);
            if (shift > 0)
            {
                divisor <<= shift;
                hi = (hi << shift) | (lo >> (64 - shift));
                lo <<= shift;
            }

            uint hiHi = (uint)(hi >> 32);
            uint hiLo = (uint)hi;
            uint loHi = (uint)(lo >> 32);
            uint loLo = (uint)lo;
            uint dHi = (uint)(divisor >> 32);
            uint dLo = (uint)divisor;

            nuint quotient;

            {
                // First guess for the current digit of the quotient,
                // which naturally must have only 32 bits...
                ulong digit = hi / dHi;

                if (digit > 0xFFFFFFFF)
                {
                    digit = 0xFFFFFFFF;
                }

                // Our first guess may be a little bit to big
                while (DivideGuessTooBigUInt32(digit, hi, loHi, dHi, dLo))
                {
                    --digit;
                }

                if (digit > 0)
                {
                    // Now it's time to subtract our current quotient
                    ulong carry;
                    {
                        // SubtractDivisor([hiHi, hiLo, loHi], divisor, digit)
                        carry = dLo * digit;
                        uint dd = (uint)carry;
                        carry >>= 32;
                        if (loHi < dd)
                        {
                            ++carry;
                        }
                        loHi -= dd;

                        carry += dHi * digit;
                        dd = (uint)carry;
                        carry >>= 32;
                        if (hiLo < dd)
                        {
                            ++carry;
                        }
                        hiLo -= dd;
                    }

                    if (carry != hiHi)
                    {
                        --digit;
                        Debug.Assert(carry == (hiHi + 1));

                        // Our guess was still exactly one too high
                        {
                            // AddDivisor([hiHi, hiLo, loHi], divisor)
                            ulong dd = loHi * dLo;
                            loHi = (uint)dd;

                            dd = (hiLo + (dd >> 32)) * dHi;
                            hiLo = (uint)dd;
                            Debug.Assert((dd >> 32) == 1);
                        }
                    }
                }

                quotient = (nuint)(digit << 32);
            }

            {
                // First guess for the current digit of the quotient,
                // which naturally must have only 32 bits...
                ulong mi = ((ulong)hiLo << 32) | loHi;
                ulong digit = mi / dHi;

                if (digit > 0xFFFFFFFF)
                {
                    digit = 0xFFFFFFFF;
                }

                // Our first guess may be a little bit to big
                while (DivideGuessTooBigUInt32(digit, mi, loLo, dHi, dLo))
                {
                    --digit;
                }

                if (digit > 0)
                {
                    // Now it's time to subtract our current quotient
                    ulong carry;
                    {
                        // SubtractDivisor([hiLo, loHi, loLo], divisor, digit)
                        carry = dLo * digit;
                        uint dd = (uint)carry;
                        carry >>= 32;
                        if (loLo < dd)
                        {
                            ++carry;
                        }
                        loLo -= dd;

                        carry += dHi * digit;
                        dd = (uint)carry;
                        carry >>= 32;
                        if (loHi < dd)
                        {
                            ++carry;
                        }
                        loHi -= dd;
                    }

                    if (carry != hiLo)
                    {
                        --digit;
                        Debug.Assert(carry == (hiLo + 1));

                        // Our guess was still exactly one too high
                        {
                            // AddDivisor([hiLo, loHi, loLo], divisor)
                            ulong dd = (loHi + ((loLo * dLo) >> 32)) * dHi;
                            Debug.Assert((dd >> 32) == 1);
                        }
                    }
                }

                quotient |= (uint)digit;
            }

            remainder = (lo - divisor * quotient) >> shift;
            return quotient;
        }

        /// <summary>
        /// Widening divide: (hi:lo) / divisor -> (quotient, remainder).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static nuint DivRem(nuint hi, nuint lo, nuint divisor, out nuint remainder)
        {
            if (nint.Size == 8)
            {
                // Compute (hi * 2^64 + lo) / divisor.
                // hi < divisor is guaranteed by callers, so quotient fits in 64 bits.
                Debug.Assert(hi < (ulong)divisor || divisor == 0);

                if (hi == 0)
                {
                    (ulong q, ulong r) = Math.DivRem(lo, (ulong)divisor);
                    remainder = (nuint)r;
                    return (nuint)q;
                }

                // When divisor fits in 32 bits, split lo into two 32-bit halves
                // and chain two native 64-bit divisions (avoids UInt128 overhead):
                //   (hi * 2^32 + lo_hi) / divisor -> (q_hi, r1) [fits: hi < divisor < 2^32]
                //   (r1 * 2^32 + lo_lo) / divisor -> (q_lo, r2) [fits: r1 < divisor < 2^32]
                if ((ulong)divisor <= uint.MaxValue)
                {
                    ulong lo_hi = (ulong)lo >> 32;
                    ulong lo_lo = (ulong)lo & 0xFFFFFFFF;

                    (ulong q_hi, ulong r1) = Math.DivRem(((ulong)hi << 32) | lo_hi, divisor);
                    (ulong q_lo, ulong r2) = Math.DivRem((r1 << 32) | lo_lo, divisor);

                    remainder = (nuint)r2;
                    return (nuint)((q_hi << 32) | q_lo);
                }
#pragma warning disable SYSLIB5004 // X86Base.DivRem is experimental
                if (X86Base.X64.IsSupported)
                {
                    (ulong q, ulong r) = X86Base.X64.DivRem(lo, hi, divisor);
                    remainder = (nuint)r;
                    return (nuint)q;
                }
#pragma warning restore SYSLIB5004
                else
                {
                    ulong q = DivRemKnuth(hi, lo, divisor, out ulong r);
                    remainder = (nuint)r;
                    return (nuint)q;
                }
            }
            else
            {
                ulong value = ((ulong)hi << 32) | lo;
                ulong digit = value / divisor;
                remainder = (uint)(value - digit * divisor);
                return (uint)digit;
            }
        }
    }
}
