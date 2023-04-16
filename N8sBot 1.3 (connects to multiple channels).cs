using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using N8sBot.GambleFunctionLibrary;

namespace N8sBotApp
{
    class N8sBot
    {
        static void Main(string[] args)
        {
            // Server, port, and channel information
            string server = "irc.gamesurge.net";
            int port = 6667;
            string[] channels = { "", "", "" }; // Add your channel names here
            string nickname = "";
            string password = "";
            string message = "";

            // Connect to the IRC server
            TcpClient tcpClient = new TcpClient(server, port);
            NetworkStream stream = tcpClient.GetStream();
            StreamReader reader = new StreamReader(stream);
            StreamWriter writer = new StreamWriter(stream);

            // Send authentication information
            writer.WriteLine("PASS " + password);
            writer.WriteLine("NICK " + nickname);
            writer.WriteLine("USER " + nickname + " 0 * :" + nickname);
            writer.Flush();

            Timer timer = null;

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                Console.WriteLine(line);

                // Respond to PING messages from the server
                if (line.StartsWith("PING"))
                {
                    writer.WriteLine("PONG" + line.Substring(4));
                    writer.Flush();
                }
                // Join channels once authenticated and identified
                else if (line.Contains("001"))
                {
                    writer.WriteLine("PRIVMSG authserv@services.gamesurge.net :AUTH " + nickname + " " + password);
                    writer.Flush();

                    // Join each channel with a 1-second delay between them
                    foreach (string channel in channels)
                    {
                        writer.WriteLine("JOIN " + channel);
                        writer.Flush();
                        Thread.Sleep(1000);

                        if (!string.IsNullOrEmpty(message))
                        {
                            writer.WriteLine("PRIVMSG " + channel + " :" + message);
                            writer.Flush();
                        }
                    }

                    // Initialize the timer for hourly messages if not already initialized
                    if (timer == null)
                    {
                        timer = new Timer(PostHourlyMessage, (writer, channels), TimeSpan.Zero, TimeSpan.FromHours(1));
                    }
                }
                // Process PRIVMSG messages
                else if (line.Contains("PRIVMSG"))
                {
                    (bool isGambleCommand, string response) = GambleFunction.ProcessLineForGambleCommand(line);
                    if (isGambleCommand)
                    {
                        string target = GetTarget(line);
                        writer.WriteLine("PRIVMSG " + target + " :" + response);
                        writer.Flush();
                    }
                }
            }

            tcpClient.Close();
        }

        // Post an hourly message in all channels
        static void PostHourlyMessage(object state)
        {
            var (writer, channels) = ((StreamWriter, string[]))state;
            string message = ""; // Add your hourly message here

            foreach (string channel in channels)
            {
                writer.WriteLine("PRIVMSG " + channel + " :" + message);
                writer.Flush();
            }
        }

        // Get the target of a PRIVMSG message (either a channel or a user)
        static string GetTarget(string line)
        {
            int startIndex = line.IndexOf("PRIVMSG") + 8;
            int endIndex = line.IndexOf(" :", startIndex);
            return line.Substring(startIndex, endIndex - startIndex);
        }
    }
}
