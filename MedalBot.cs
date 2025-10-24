using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;

namespace MedalBot
{
    class Program
    {
        private static TcpClient irc;
        private static StreamReader reader;
        private static StreamWriter writer;

        private static readonly string server = "irc.gamesurge.net";
        private static readonly int port = 6667;
        private static readonly string nick = "CO2";
        private static readonly string user = "CO2";
        private static readonly string pass = "1FpyDmQe";
        private static readonly string channel = "#cncnet-ra";
        private static readonly string channelPass = "ra1-derp";

        private static readonly string adminsFile = "admins.txt";
        private static readonly string voicedFile = "voiced.txt";

        private static HashSet<string> admins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, int> voicedUsers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        static void Main()
        {
            LoadAdmins();
            LoadVoiced();

            irc = new TcpClient(server, port);
            reader = new StreamReader(irc.GetStream());
            writer = new StreamWriter(irc.GetStream()) { AutoFlush = true };

            Console.WriteLine("Connected to IRC server.");

            writer.WriteLine($"NICK {nick}");
            writer.WriteLine($"USER {user} 8 * :{user}");

            bool authed = false;
            bool joined = false;

            while (true)
            {
                string line = reader.ReadLine();
                if (line == null) continue;

                Console.WriteLine(line);

                if (line.StartsWith("PING"))
                {
                    writer.WriteLine($"PONG {line.Split(' ')[1]}");
                    continue;
                }

                // Wait for 001 (Welcome message)
                if (line.Contains(" 001 "))
                {
                    Thread.Sleep(1000);
                    writer.WriteLine($"PRIVMSG AuthServ@Services.GameSurge.net :AUTH {nick} {pass}");
                    Console.WriteLine("Sent AuthServ authentication...");
                    authed = true;
                    continue;
                }

                // After auth, join channel
                if (authed && !joined && line.Contains("is now your hidden host"))
                {
                    Thread.Sleep(1500);
                    writer.WriteLine($"JOIN {channel} {channelPass}");
                    Console.WriteLine($"Joined channel {channel}.");
                    joined = true;
                }

                HandleLine(line);
            }
        }

        private static void HandleLine(string line)
        {
            // Auto voice on join
            if (line.Contains("JOIN :") && line.Contains(channel))
            {
                string joinedNick = GetNick(line);
                if (voicedUsers.ContainsKey(joinedNick))
                {
                    VoiceUser(joinedNick);
                }
                return;
            }

            // Detect messages
            if (line.Contains("PRIVMSG"))
            {
                string nick = GetNick(line);
                string message = GetMessage(line);

                if (message.StartsWith("!medal ", StringComparison.OrdinalIgnoreCase))
                {
                    HandleMedalCommand(nick, message);
                }
            }
        }

        private static void HandleMedalCommand(string sender, string message)
        {
            if (!admins.Contains(sender))
            {
                writer.WriteLine($"PRIVMSG {channel} :{sender}: You are not authorized to use this command.");
                return;
            }

            string[] parts = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3)
            {
                writer.WriteLine($"PRIVMSG {channel} :Usage: !medal <nick> <Platinum|Gold|Silver>");
                return;
            }

            string targetNick = parts[1];
            string medalType = parts[2].ToLower();

            int medalCode = medalType switch
            {
                "platinum" => 1,
                "gold" => 2,
                "silver" => 3,
                _ => 0
            };

            if (medalCode == 0)
            {
                writer.WriteLine($"PRIVMSG {channel} :Unknown medal type. Use Platinum, Gold, or Silver.");
                return;
            }

            voicedUsers[targetNick] = medalCode;
            SaveVoiced();

            VoiceUser(targetNick);
            writer.WriteLine($"PRIVMSG {channel} :{targetNick} has been awarded the {medalType.ToUpper()} medal!");
        }

        private static void VoiceUser(string nick)
        {
            writer.WriteLine($"PRIVMSG ChanServ :voice {channel} {nick}");
            Console.WriteLine($"Gave voice to {nick}");
        }

        private static string GetNick(string line)
        {
            if (line.StartsWith(":"))
            {
                int end = line.IndexOf('!');
                if (end > 1) return line.Substring(1, end - 1);
            }
            return "Unknown";
        }

        private static string GetMessage(string line)
        {
            int idx = line.IndexOf("PRIVMSG");
            if (idx == -1) return "";
            int msgStart = line.IndexOf(':', idx);
            if (msgStart == -1) return "";
            return line.Substring(msgStart + 1).Trim();
        }

        private static void LoadAdmins()
        {
            if (File.Exists(adminsFile))
                admins = new HashSet<string>(File.ReadAllLines(adminsFile).Where(l => !string.IsNullOrWhiteSpace(l)), StringComparer.OrdinalIgnoreCase);
        }

        private static void LoadVoiced()
        {
            if (!File.Exists(voicedFile)) return;

            foreach (string line in File.ReadAllLines(voicedFile))
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2 && int.TryParse(parts[1], out int medal))
                {
                    voicedUsers[parts[0]] = medal;
                }
            }
        }

        private static void SaveVoiced()
        {
            File.WriteAllLines(voicedFile, voicedUsers.Select(kv => $"{kv.Key} {kv.Value}"));
        }
    }
}