using System;
using Tracker.Shared.Auth;

Console.WriteLine("Testing Tracker.Shared.Auth reference...");

try
{
    var test = TestAuthReference.Test();
    Console.WriteLine($"Success! {test}");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}

Console.WriteLine("Press any key to exit...");
Console.ReadKey();
