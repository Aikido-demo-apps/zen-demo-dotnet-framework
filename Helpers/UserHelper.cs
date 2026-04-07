using System;

namespace ZenDemo.DotNetFramework.Helpers
{
    public static class UserHelper
    {
        public static string GetName(int number)
        {
            var names = new[]
            {
                "Hans",
                "Pablo",
                "Samuel",
                "Timo",
                "Tudor",
                "Willem",
                "Wout",
                "Yannis",
            };

            var index = Math.Abs(number) % names.Length;
            return names[index];
        }
    }
}
