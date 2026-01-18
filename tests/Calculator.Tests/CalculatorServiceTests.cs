using Xunit;

namespace Calculator.Tests;

public class CalculatorServiceTests
{
    private readonly CalculatorService _calculator = new();

    [Theory]
    [InlineData(2, 3, 5)]
    [InlineData(-1, 1, 0)]
    [InlineData(0, 0, 0)]
    [InlineData(-5, -3, -8)]
    public void Add_ReturnsCorrectSum(int a, int b, int expected)
    {
        var result = _calculator.Add(a, b);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(5, 3, 2)]
    [InlineData(10, 10, 0)]
    [InlineData(-5, -3, -2)]
    [InlineData(0, 5, -5)]
    public void Subtract_ReturnsCorrectDifference(int a, int b, int expected)
    {
        var result = _calculator.Subtract(a, b);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(3, 4, 12)]
    [InlineData(-2, 3, -6)]
    [InlineData(0, 100, 0)]
    [InlineData(-3, -3, 9)]
    public void Multiply_ReturnsCorrectProduct(int a, int b, int expected)
    {
        var result = _calculator.Multiply(a, b);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(10, 2, 5.0)]
    [InlineData(7, 2, 3.5)]
    [InlineData(-10, 2, -5.0)]
    public void Divide_ReturnsCorrectQuotient(int a, int b, double expected)
    {
        var result = _calculator.Divide(a, b);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Divide_ByZero_ThrowsDivideByZeroException()
    {
        Assert.Throws<DivideByZeroException>(() => _calculator.Divide(10, 0));
    }

    [Theory]
    [InlineData(2, true)]
    [InlineData(4, true)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(7, false)]
    [InlineData(-2, true)]
    public void IsEven_ReturnsCorrectResult(int number, bool expected)
    {
        var result = _calculator.IsEven(number);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(5, true)]
    [InlineData(7, true)]
    [InlineData(11, true)]
    [InlineData(1, false)]
    [InlineData(4, false)]
    [InlineData(9, false)]
    [InlineData(0, false)]
    [InlineData(-5, false)]
    public void IsPrime_ReturnsCorrectResult(int number, bool expected)
    {
        var result = _calculator.IsPrime(number);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(5, 120)]
    [InlineData(6, 720)]
    [InlineData(10, 3628800)]
    public void Factorial_ReturnsCorrectResult(int n, int expected)
    {
        var result = _calculator.Factorial(n);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Factorial_NegativeNumber_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _calculator.Factorial(-1));
    }
}
