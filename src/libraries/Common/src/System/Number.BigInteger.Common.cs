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
        /// <summary>
        /// Performs widening addition of two limbs plus a carry-in, returning the sum and carry-out.
        /// On 64-bit: uses 128-bit arithmetic. On 32-bit: uses 64-bit arithmetic.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static nuint AddWithCarry(nuint a, nuint b, nuint carryIn, out nuint carryOut)
        {
            if (nint.Size == 8)
            {
                nuint sum1 = a + b;
                nuint c1 = (sum1 < a) ? 1 : (nuint)0;
                nuint sum2 = sum1 + carryIn;
                nuint c2 = (sum2 < sum1) ? 1 : (nuint)0;
                carryOut = c1 + c2;
                return sum2;
            }
            else
            {
                ulong sum = (ulong)a + b + carryIn;
                carryOut = (uint)(sum >> 32);
                return (uint)sum;
            }
        }

        /// <summary>
        /// Update `left` to compute `left += right`. The range `left[(right.Length)..]` remains unchanged, and the carry-out is returned.
        /// </summary>
        public static nuint AddBigIntegerCarryOut(Span<nuint> left, ReadOnlySpan<nuint> right)
        {
            Debug.Assert(left.Length >= right.Length);

            if (right.Length == 0)
            {
                return 0;
            }

            nuint carry = 0;
            _ = left[right.Length - 1];

            for (int i = 0; i < right.Length; i++)
            {
                left[i] = AddWithCarry(left[i], right[i], carry, out carry);
            }

            // To fully update `left`, the remaining elements need to be processed as shown below.
            //   for (; carry != 0 && i < left.Length; i++)
            //   {
            //       nuint sum = left[i] + carry;
            //       carry = (sum < carry) ? 1 : (nuint)0;
            //       left[i] = sum;
            //   }

            return carry;
        }

        /// <summary>
        /// Update `left` to compute `left += right`.
        /// </summary>
        public static void AddBigInteger(Span<nuint> left, ReadOnlySpan<nuint> right)
        {
            Debug.Assert(left.Length >= right.Length);

            nuint carry = AddBigIntegerCarryOut(left, right);

            for (int i = right.Length; carry != 0 && i < left.Length; i++)
            {
                nuint sum = left[i] + carry;
                carry = (sum < carry) ? 1 : (nuint)0;
                left[i] = sum;
            }

            Debug.Assert(carry == 0);
        }

        /// <summary>
        /// quotient = left / right, and left is mutated to left % right.
        /// </summary>
        public static void DivideBigInteger(Span<nuint> left, ReadOnlySpan<nuint> right, Span<nuint> quotient)
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

            // Executes the "grammar-school" algorithm a.k.a. Knuth's algorithm
            // for computing q = a / b.
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
                nuint digit = valHi1 < divHi
                    ? (nint.Size == 8
                        ? (nuint)DivRem(valHi1, valHi0, divHi).Quotient
                        : (nuint)((((ulong)valHi1 << 32) | valHi0) / divHi))
                    : nuint.MaxValue;

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
                        carry = AddBigIntegerCarryOut(left.Slice(n), right);
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
        }

        /// <summary>
        /// Return: (divHi:divLo) * q > (valHi:valMi:valLo)
        /// </summary>
        private static bool DivideGuessTooBig(
            nuint q,
            nuint valHi, nuint valMi, nuint valLo,
            nuint divHi, nuint divLo)
        {
            // We multiply the two most significant limbs of the divisor
            // with the current guess for the quotient. If those are bigger
            // than the three most significant limbs of the current dividend
            // we return true, which means the current guess is still too big.

            if (nint.Size == 8)
            {
                ulong chkHi = Math.BigMul(divHi, q, out ulong chkHiLo);
                ulong chkLoHi = Math.BigMul(divLo, q, out ulong chkLo);
                ulong chkMi = chkHiLo + chkLoHi;

                if (chkMi < chkLoHi)
                {
                    chkHi++;
                }

                return (chkHi > valHi)
                    || ((chkHi == valHi) && ((chkMi > valMi) || ((chkMi == valMi) && (chkLo > valLo))));
            }
            else
            {
                ulong valMiLo = ((ulong)valMi << 32) | valLo;
                return DivideGuessTooBig(q, (uint)valMi, valMiLo, (uint)divHi, (uint)divLo, out _);
            }
        }

        /// <summary>
        /// Return: (divHi:divLo) * q > (valHi:valLo) and remainder = (valHi:valLo) - (divHi:divLo) * q if result is false.
        /// </summary>
        private static bool DivideGuessTooBig(
            ulong q,
            uint valHi, ulong valLo,
            uint divHi, uint divLo,
            out ulong remainder)
        {
            Debug.Assert(q <= 0xFFFFFFFF);

            // We multiply the two most significant limbs of the divisor
            // with the current guess for the quotient. If those are bigger
            // than the three most significant limbs of the current dividend
            // we return true, which means the current guess is still too big.

            ulong hi = divHi * q;
            ulong chkLo = divLo * q + (hi << 32);
            uint chkHi = (uint)(hi >> 32);

            if ((chkHi > valHi) || ((chkHi == valHi) && (chkLo > valLo)))
            {
                remainder = 0;
                return true;
            }

            remainder = valLo - chkLo;

            Debug.Assert(valHi == chkHi || (valHi == chkHi + 1 && chkLo < valLo));
            Debug.Assert(remainder < (divLo | ((ulong)divHi << 32)));

            return false;
        }

        /// <summary>
        /// Widening divide: (hi:lo) / divisor -> (quotient, remainder).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (ulong Quotient, ulong Remainder) DivRem(ulong hi, ulong lo, ulong divisor)
        {
            // Compute (hi * 2^64 + lo) / divisor.
            // hi < divisor is guaranteed by callers, so quotient fits in 64 bits.
            Debug.Assert(hi < divisor);

            if (hi == 0)
            {
                return ulong.DivRem(lo, divisor);
            }

            // When divisor fits in 32 bits, split lo into two 32-bit halves
            // and chain two native 64-bit divisions (avoids UInt128 overhead):
            //   (hi * 2^32 + lo_hi) / divisor -> (q_hi, r1) [fits: hi < divisor < 2^32]
            //   (r1 * 2^32 + lo_lo) / divisor -> (q_lo, r2) [fits: r1 < divisor < 2^32]
            if (divisor <= uint.MaxValue)
            {
                ulong lo_hi = lo >> 32;
                ulong lo_lo = lo & 0xFFFFFFFF;

                (ulong q_hi, ulong r1) = Math.DivRem((hi << 32) | lo_hi, divisor);
                (ulong q_lo, ulong r2) = Math.DivRem((r1 << 32) | lo_lo, divisor);

                ulong q = (q_hi << 32) | q_lo;
                return (q, r2);
            }
#pragma warning disable SYSLIB5004 // X86Base.DivRem is experimental
            if (X86Base.X64.IsSupported)
            {
                return X86Base.X64.DivRem(lo, hi, divisor);
            }
#pragma warning restore SYSLIB5004
            else
            {
                ulong q = Div128BitsBy64Bits(hi, lo, divisor, out ulong r);
                Debug.Assert(r < divisor);
                Debug.Assert(Math.BigMul(q, divisor) + r == new UInt128(hi, lo));
                return (q, r);
            }

            static ulong Div128BitsBy64Bits(ulong hi, ulong lo, ulong divisor, out ulong remainder)
            {
                // Knuth's algorithm
                // Perform 128-bit/64-bit division by splitting it into 32-bit parts.
                //
                // dividend = |      hi     |      lo     |
                //  divisor = |             |   divisor   |

                int shift = BitOperations.LeadingZeroCount(divisor);
                if (shift > 0)
                {
                    divisor <<= shift;
                    hi = (hi << shift) | (lo >> (64 - shift));
                    lo <<= shift;
                }

                uint dHi = (uint)(divisor >> 32);
                uint dLo = (uint)divisor;

                ulong quotient = Div96BitsBy64Bits(hi, (uint)(lo >> 32), dHi, dLo, out ulong rem) << 32;
                quotient |= Div96BitsBy64Bits(rem, (uint)lo, dHi, dLo, out remainder);

                Debug.Assert((remainder & (~0ul << shift)) == 0);
                remainder >>= shift;

                return quotient;
            }

            static ulong Div96BitsBy64Bits(ulong hi, uint lo, uint dHi, uint dLo, out ulong remainder)
            {
                // dividend = |      hi     |  lo  |
                // dividend = |  mHi |      mLo    |
                //  divisor = |      |  dHi |  dLo |
                ulong mLo = (hi << 32) | lo;
                uint mHi = (uint)(hi >> 32);

                // First guess for the current digit of the quotient,
                // which naturally must have only 32 bits...
                ulong q = hi / dHi;

                if (q > 0xFFFFFFFF)
                {
                    q = 0xFFFFFFFF;
                }

                // Our first guess may be a little bit to big
                // and subtract our current quotient -> (hi:lm) -= divisor * q
                // In the original Knuth's algorithm, `DivideGuessTooBig` is
                // determined using only the high bits of the left and right
                // values, which can lead to cases where `left < q * right`.
                // However, in this 96-bit by 64-bit division, all bits are
                // used, so that concern does not apply.
                //
                //  current  |  mHi |      mLo    |
                //    -      |      |   dLo * q   |
                //    -      |   dHi * q   |      |
                // ---------------------------------
                // remainder |      |      |  mi  |
                while (DivideGuessTooBig(q, mHi, mLo, dHi, dLo, out remainder))
                {
                    --q;
                }

                Debug.Assert(q <= uint.MaxValue);
                return q;
            }
        }
    }
}
