using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace N8sBot.GambleFunctionLibrary
{
    public static class GambleFunction
    {
        // Process the line for the gamble command and return a boolean and a response string
        public static (bool, string) ProcessLineForGambleCommand(string line)
        {
            // Define a regular expression to match the gamble command pattern (should work in all IRC client types)
            Regex regex = new Regex(@"^:.*? PRIVMSG (?<target>.*?) :!Gamble(?:\s+(?<message>.*))?$", RegexOptions.IgnoreCase);

            // Check if the line matches the regular expression pattern
            Match match = regex.Match(line);
            if (match.Success)
            {
                // Extract the message from the line
                string message = match.Groups["message"].Value.Trim();

                // Call the Gamble function with the message
                string result = Gamble(message);

                // Return true and the gamble result as a response
                return (true, "Gamble result: " + result);
            }

            // If the line does not match the pattern, return false and null
            return (false, null);
        }

        // Perform the gamble operation by randomly selecting a word from the input message
        public static string Gamble(string message)
        {
            // Split the message into words
            string[] words = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // If there are no words, return an error message
            if (words.Length == 0)
            {
                return "No words provided.";
            }

            // Initialize a random number generator
            Random random = new Random();

            // Generate a random index within the range of words
            int randomIndex = random.Next(words.Length);

            // Return the randomly selected word as the gamble result
            return words[randomIndex];
        }
    }
}
