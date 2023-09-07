﻿using System.Net.Sockets;
using Assessment;
using System.IO;
using System.Net;


namespace NetworkSecurityScanner
{
    internal class Program
    {
        private static readonly HttpClient client = new HttpClient();

        private static bool IsTargetReachable(string ipAddress, int port = 80, int timeout = 5000)
        {
            using (var client = new TcpClient())
            {
                try
                {
                    var task = client.ConnectAsync(ipAddress, port);
                    return Task.WhenAny(task, Task.Delay(timeout)).Result == task;
                }
                catch
                {
                    return false;
                }
            }
        }

        public static async Task StartScan()
        {
            Console.WriteLine("Enter target IP address:");
            string targetIpAddress = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(targetIpAddress) || !IPAddress.TryParse(targetIpAddress, out _))
            {
                Console.WriteLine("You didn't enter a valid IP address. Exiting...");
                return;
            }

            if (!IsTargetReachable(targetIpAddress))
            {
                Console.WriteLine($"Unable to connect to target IP: {targetIpAddress}. Exiting...");
                return;
            }

            try
            {
                HttpResponseMessage rootResponse = await client.GetAsync($"http://{targetIpAddress}/");
                if (!rootResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Unable to get a positive HTTP response from target IP: {targetIpAddress}. Exiting...");
                    return;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error making an HTTP request to {targetIpAddress}. Message: {e.Message}");
                return;
            }

            Console.WriteLine($"Successfully connected to {targetIpAddress}. Proceeding with scan...");

            Console.WriteLine("Enter the name of the file (including its .extension) to scan for:");
            string filename = Console.ReadLine();

            if (string.IsNullOrEmpty(filename) || !filename.Contains("."))
            {
                Console.WriteLine("You didn't enter a valid filename or its extension. (For example enter password.txt instead of password");
                return;
            }

            // Check the local directories first
            CheckLocalDirectories(filename);

            // Then check the remote server
            //await CheckForFile("", targetIpAddress, filename);

            // Create an instance of the Words class
            IWordsProvider wordProvider = new Words();
            List<string> wordList = wordProvider.GetWordList();

            // Check for the file in each of the paths in the word list
            foreach (var path in wordList)
            {
                await CheckForFile(path, targetIpAddress, filename);
            }
        }

        private static void CheckLocalDirectories(string filename)
        {
            string desktopDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            SearchAndPrintFiles(desktopDirectory, filename);
        }

        private static void SearchAndPrintFiles(string directory, string filename)
        {
            try
            {
                foreach (var file in Directory.GetFiles(directory, filename))
                {
                    Console.WriteLine($"Found {filename} at {file}");
                    Console.WriteLine($"Content of {filename}:");
                    Console.WriteLine(File.ReadAllText(file));
                }

                // Recursively search in subdirectories
                foreach (var dir in Directory.GetDirectories(directory))
                {
                    SearchAndPrintFiles(dir, filename);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // We don't have access to this directory, log a message and skip it.
                Console.WriteLine($"Access denied to directory: {directory}. Skipping...");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error accessing {directory}: {e.Message}");
            }
        }


        private static async Task CheckForFile(string path, string ipAddress, string filename)
        {
            string targetUrl = $"http://{ipAddress}{path}/{filename}";
            try
            {
                HttpResponseMessage response = await client.GetAsync(targetUrl);
                if (response.IsSuccessStatusCode)
                {
                    string fileContent = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(fileContent))
                    {
                        Console.WriteLine($"password.txt found at: {targetUrl}\nContent of password.txt:\n{fileContent}");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error fetching {targetUrl}. Message: {e.Message}");
            }
        }
    }
}



