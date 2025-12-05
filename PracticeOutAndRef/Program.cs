/*
 * The ref keyword indicates that an argument 
 * is being passed by reference and must be 
 * initialized before it is passed to the method.
 * Purpose: To allow a method to read and modify the value of an argument, and have that modification reflected in the calling code.

Requirement: The variable must have an initial value before being passed to the method.
 * */

void IncrementValue(ref int number)
{
    number = number + 10;
}
void IncrementValueWithoutRef(int number)
{
    number = number + 10;
}

// example
/*
int a = 5;
IncrementValueWithoutRef(a);
Console.WriteLine(a);

IncrementValue(ref a);
Console.WriteLine(a);
*/


/* 
 * The out keyword indicates that an argument 
 * is being passed by reference, but the method 
 * is responsible for assigning a value to the parameter 
 * before the method returns.

Purpose: To allow a method to return multiple values 
(in addition to its normal return type).

Requirement: The variable does not need to be initialized 
before being passed. The method must assign 
a value to the out parameter before it exits.
*/

bool Divide(int numerator, int denominator, out int result)
{
    if (denominator != 0)
    {
        result = numerator / denominator;
        return true;
    }
    else
    {
        result = 0;
        return false;
    }
}

// Usage (since C# 7, you can declare the out variable inline):
if (Divide(10, 2, out int quotient))
{
    Console.WriteLine(quotient);
}
// Note: 'quotient' is now scoped to the outer block.
// It means: The variable DOES NOT disappear after the if statement!


void TestingRefOutProcessor()
{
    int x = 2, y = 6;
    Console.WriteLine($"x {x} y {y} ");
    RefOutProcessor.SwapIntegers(ref x, ref y);

    int[] tableOfData = { 4, 2, 3, 25, 74, 3, 2, 454, 23, 4, 534, 3 };
    RefOutProcessor.CalculateStatistics(tableOfData, out int sum, out double average);
    Console.WriteLine($"Sum: {sum} average: {average}");

}

TestingRefOutProcessor();

class RefOutProcessor
{
    public static void SwapIntegers(ref int x1, ref int x2)
    {
        int temp = x1;
        x1 = x2;
        x2 = temp;
    }

    public static bool CalculateStatistics(int[] numbers, out int sum, out double avarage)
    {
        sum = 0;
        avarage = 0;

        foreach (int number in numbers)
        {
            sum += number;
            avarage += number;
        }

        if (numbers.Length != 0)
            avarage /= numbers.Length;
        else
            avarage = 0;

        return true;
    }   
}
