namespace Calculator;

public class CalculatorService
{
    public int Add(int a, int b) => a + b;

    public int Subtract(int a, int b) => a - b;

    public int Multiply(int a, int b) => a * b;

    public double Divide(int a, int b)
    {
        if (b == 0)
            throw new DivideByZeroException("Cannot divide by zero");

        return (double)a / b;
    }

    public bool IsEven(int number) => number % 2 == 0;

    public bool IsPrime(int number)
    {
        if (number <= 1) return false;
        if (number == 2) return true;
        if (number % 2 == 0) return false;

        for (int i = 3; i <= Math.Sqrt(number); i += 2)
        {
            if (number % i == 0)
                return false;
        }
        return true;
    }

    public int Factorial(int n)
    {
        if (n < 0)
            throw new ArgumentException("Factorial is not defined for negative numbers", nameof(n));

        if (n <= 1) return 1;

        return n * Factorial(n - 1);
    }

    public int Abs(int number) => Math.Abs(number);

    public int Max(int a, int b) => Math.Max(a, b);

    public int Min(int a, int b) => Math.Min(a, b);
}

