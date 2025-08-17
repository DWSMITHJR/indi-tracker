using System;
using Tracker.Shared.Auth;

namespace Tracker.Client
{
    public static class TestAuthAccess
    {
        public static void Test()
        {
            try
            {
                var test = TestAuthReference.Test();
                Console.WriteLine($"TestAuthReference.Test() returned: {test}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accessing Tracker.Shared.Auth: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}
