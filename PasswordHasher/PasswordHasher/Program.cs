using System;
using BCrypt.Net;

class Program
{
    static void Main()
    {
        Console.Write("Enter new password: ");
        string password = Console.ReadLine();

        string hash = BCrypt.Net.BCrypt.HashPassword(password);
        Console.WriteLine($"Hashed password:\n{hash}");
    }
}
