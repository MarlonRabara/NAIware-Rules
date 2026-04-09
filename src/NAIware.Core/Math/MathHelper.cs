namespace NAIware.Core.Math;

/// <summary>
/// Provides mathematical helper methods including GCF, LCM, and rounding.
/// </summary>
public static class MathHelper
{
    /// <summary>
    /// Rounds a number up (away from zero) to the specified number of digits.
    /// </summary>
    /// <param name="number">The number to round.</param>
    /// <param name="digits">The number of decimal digits.</param>
    /// <returns>The rounded number.</returns>
    public static double RoundUp(double number, int digits) =>
        System.Math.Ceiling(number * System.Math.Pow(10, digits)) / System.Math.Pow(10, digits);

    /// <summary>
    /// Computes the greatest common factor of two numbers using the Euclidean algorithm.
    /// </summary>
    /// <param name="num1">The first number.</param>
    /// <param name="num2">The second number.</param>
    /// <returns>The greatest common factor.</returns>
    public static uint GCF(uint num1, uint num2)
    {
        if (num1 <= num2 && num1 != 0 && num2 % num1 == 0) return num1;
        if (num2 <= num1 && num2 != 0 && num1 % num2 == 0) return num2;

        while (num1 != num2)
        {
            if (num1 > num2)
                num1 -= num2;
            else
                num2 -= num1;
        }

        return num1;
    }

    /// <summary>
    /// Computes the least common multiple of two numbers.
    /// </summary>
    /// <param name="num1">The first number.</param>
    /// <param name="num2">The second number.</param>
    /// <returns>The least common multiple.</returns>
    public static uint LCM(uint num1, uint num2)
    {
        uint multiple1 = num1;
        uint multiple2 = num2;

        while (num1 != num2)
        {
            if (num1 < num2)
                num1 += multiple1;
            else
                num2 += multiple2;
        }

        return num1;
    }
}
