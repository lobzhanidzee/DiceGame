using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

class Program
{
    static void Main(string[] args)
    {
        // Validate command-line arguments
        if (args.Length < 3)
        {
            Console.WriteLine("Error: At least three dice configurations must be specified as command-line arguments.");
            Console.WriteLine("Example: dotnet run \"2,2,4,4,9,9\" \"6,8,1,1,8,6\" \"7,5,3,7,5,3\"");
            return;
        }

        List<int[]> dice = ParseDice(args);

        Console.WriteLine("Let's determine who makes the first move.");
        int computerChoice = FairRandom(0, 1, out string hmac, out byte[] key);
        Console.WriteLine($"I selected a random value in the range 0..1 (HMAC={hmac}).");
        Console.WriteLine("Try to guess my selection.");
        Console.WriteLine("0 - 0\n1 - 1\nX - exit\n? - help");

        int userGuess = GetUserChoice(0, 1);
        if (userGuess == -1) return;

        Console.WriteLine($"My selection: {computerChoice} (KEY={BitConverter.ToString(key).Replace("-", "")}).");

        bool userGoesFirst = userGuess == computerChoice;
        int userDiceIndex, computerDiceIndex;

        if (userGoesFirst)
        {
            Console.WriteLine("You make the first move. Choose your dice:");
            userDiceIndex = ChooseDice(dice);
            if (userDiceIndex == -1) return;
            Console.WriteLine($"You chose the [{string.Join(",", dice[userDiceIndex])}] dice.");

            computerDiceIndex = 0;
            Console.WriteLine($"I choose the [{string.Join(",", dice[computerDiceIndex])}] dice.");
        }
        else
        {
            Console.WriteLine("I make the first move and choose the dice.");
            computerDiceIndex = 0;
            Console.WriteLine($"I choose the [{string.Join(",", dice[computerDiceIndex])}] dice.");

            Console.WriteLine("Now, choose your dice:");
            userDiceIndex = ChooseDice(dice, exclude: computerDiceIndex);
            if (userDiceIndex == -1) return;
            Console.WriteLine($"You chose the [{string.Join(",", dice[userDiceIndex])}] dice.");
        }

        int[] userDice = dice[userDiceIndex];
        int[] computerDice = dice[computerDiceIndex];

        // Computer's throw
        Console.WriteLine("It's time for my throw.");
        int computerThrow = FairThrow(computerDice.Length, out hmac, out key);
        Console.WriteLine($"I selected a random value in the range 0..{computerDice.Length - 1} (HMAC={hmac}).");

        Console.WriteLine("Add your number modulo 6.");
        int userModNumber = GetUserChoice(0, 5);
        if (userModNumber == -1) return;

        int computerModNumber = computerThrow % 6;
        int result = (userModNumber + computerModNumber) % 6;
        Console.WriteLine($"My number is {computerModNumber} (KEY={BitConverter.ToString(key).Replace("-", "")}).");
        Console.WriteLine($"The result is {userModNumber} + {computerModNumber} = {result} (mod 6).");
        Console.WriteLine($"My throw is {computerDice[computerThrow]}.");

        // User's throw
        Console.WriteLine("It's time for your throw.");
        int userThrow = FairThrow(userDice.Length, out hmac, out key);
        Console.WriteLine($"I selected a random value in the range 0..{userDice.Length - 1} (HMAC={hmac}).");

        Console.WriteLine("Add your number modulo 6.");
        userModNumber = GetUserChoice(0, 5);
        if (userModNumber == -1) return;

        computerModNumber = userThrow % 6;
        result = (userModNumber + computerModNumber) % 6;
        Console.WriteLine($"My number is {computerModNumber} (KEY={BitConverter.ToString(key).Replace("-", "")}).");
        Console.WriteLine($"The result is {computerModNumber} + {userModNumber} = {result} (mod 6).");
        Console.WriteLine($"Your throw is {userDice[userThrow]}.");

        // Determine winner
        if (userDice[userThrow] > computerDice[computerThrow])
            Console.WriteLine($"You win ({userDice[userThrow]} > {computerDice[computerThrow]})!");
        else if (userDice[userThrow] < computerDice[computerThrow])
            Console.WriteLine($"I win ({computerDice[computerThrow]} > {userDice[userThrow]})!");
        else
            Console.WriteLine($"It's a tie ({userDice[userThrow]} == {computerDice[computerThrow]})!");
    }

    static List<int[]> ParseDice(string[] args)
    {
        var dice = new List<int[]>();
        foreach (var arg in args)
        {
            try
            {
                var values = arg.Split(',').Select(int.Parse).ToArray();
                if (values.Length < 2)
                    throw new ArgumentException($"Each dice must have at least two sides: \"{arg}\".");
                dice.Add(values);
            }
            catch
            {
                throw new ArgumentException($"Invalid dice configuration: \"{arg}\". Use a comma-separated list of integers.");
            }
        }
        return dice;
    }

    static int FairRandom(int min, int max, out string hmac, out byte[] key)
    {
        key = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(key);

        int value;
        using (rng)
        {
            value = RandomUniform(rng, min, max);
        }

        using var hmacSha256 = new HMACSHA256(key);
        byte[] hash = hmacSha256.ComputeHash(Encoding.UTF8.GetBytes(value.ToString()));
        hmac = BitConverter.ToString(hash).Replace("-", "");
        return value;
    }

    static int RandomUniform(RandomNumberGenerator rng, int min, int max)
    {
        byte[] buffer = new byte[4];
        int range = max - min + 1;
        int limit = int.MaxValue / range * range;

        int value;
        do
        {
            rng.GetBytes(buffer);
            value = BitConverter.ToInt32(buffer, 0) & int.MaxValue;
        } while (value >= limit);

        return value % range + min;
    }

    static int FairThrow(int range, out string hmac, out byte[] key)
    {
        return FairRandom(0, range - 1, out hmac, out key);
    }

    static int GetUserChoice(int min, int max)
    {
        while (true)
        {
            string? input = Console.ReadLine()?.Trim().ToUpper();
            if (input == "X") return -1;
            if (input == "?")
            {
                Console.WriteLine("Help is not yet implemented.");
                continue;
            }
            if (int.TryParse(input, out int choice) && choice >= min && choice <= max)
                return choice;

            Console.WriteLine($"Invalid selection. Enter a number between {min} and {max}, X to exit, or ? for help.");
        }
    }

    static int ChooseDice(List<int[]> dice, int exclude = -1)
    {
        for (int i = 0; i < dice.Count; i++)
        {
            if (i == exclude) continue;
            Console.WriteLine($"{i} - {string.Join(",", dice[i])}");
        }
        return GetUserChoice(0, dice.Count - 1);
    }
}
