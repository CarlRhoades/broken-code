using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System;
using System.Threading;
using System.Threading.Tasks;
using File = System.IO.File;
using static System.Runtime.InteropServices.JavaScript.JSType;


// ...
namespace DeleteFilesRebuild
{
    internal class Program
    {
        private static IConfiguration? _config;
        private static DateTime currentTime = DateTime.Now;
        private static string authUrl = "";
        private static Token token = new Token();

        // Settings for Main()
        public static string statusCode = "";
        public static int rowIndex = 0;
        public static int counterDisplay = 0;
        public static int delayNextRow = 0; //in milliseconds - changed at the loop-level depending on errors
        public static int modeSwitch = 0;
        public static string baseUrl = "";

        // Setting files
        public static string settingsFilename = "Settings.txt";

        // Settings for GetToken() and NICE connection
        public static int connectionRetryCount = 0;
        public static int connectionRetryDelay = 0;
        public static int connectionRetryCounter = 0;

        // Connection Files
        public static string connectionLogFilename = "Connection_Log.txt";

        // Settings for LookupFiles()
        public static int lookupRetryCount = 0;
        public static int lookupRetryDelay = 0;
        public static int lookupNumberOfRows = 0;
        public static int lookupStartingRow = 0;
        public static int lookupStartingColumn = 0;
        public static int lookupOverallCount = 0;

        // Lookup Files
        public static string lookupListFilename = "468232 MCIDs Original List.txt";
        public static string lookupLogFilename = "Lookup_Log.txt";
        public static string lookupFailedRequestFilename = "Lookup_Failed_Requests.txt";
        public static string lookupCallLogPathsFilename = "Lookup_CallLog_Paths.txt";

        // Settings for DeleteFiles()
        public static int deleteRetryCount = 0;
        public static int deleteRetryDelay = 0;
        public static int deleteNumberOfRows = 0;
        public static int deleteStartingRow = 0;
        public static int deleteStartingColumn = 0;
        public static int deleteOverallCount = 0;

        // Delete Files
        public static string deleteListFilename = "Delete_List.csv";
        public static string deleteLogFilename = "Delete_Log.txt";
        public static string deleteFailedRequestFilename = "Delete_Failed_Requests.txt";

        public static class Settings
        {
            public static int ConnectionRetryCount { get; set; }
            public static int ConnectionRetryDelay { get; set; }
            public static int LookupRetryCount { get; set; }
            public static int LookupRetryDelay { get; set; }
            public static int LookupNumberOfRows { get; set; }
            public static int LookupStartingRow { get; set; }
            public static int LookupOverallCount { get; set; }
            public static int DeleteRetryCount { get; set; }
            public static int DeleteRetryDelay { get; set; }
            public static int DeleteNumberOfRows { get; set; }
            public static int DeleteStartingRow { get; set; }
            public static int DeleteOverallCount { get; set; }
        }

        public static async Task Main()
        {
            //Global Exception Handler
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(GlobalExceptionHandler);
            // Load the configuration file
            _config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile($"Config/appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

            ///////////////////////////////////////////////////
            // Process settings file
            string[] settingsKeys = new string[]
            {
                "deleteRetryCount",
                "deleteRetryDelay",
                "deleteNumberOfRows",
                "deleteStartingRow",
                "deleteOverallCount",
                "lookupRetryCount",
                "lookupRetryDelay",
                "lookupNumberOfRows",
                "lookupStartingRow",
                "lookupOverallCount",
                "connectionRetryCount",
                "connectionRetryDelay"
            };
            Dictionary<string, int> settingsValues = new Dictionary<string, int>();
            foreach (var key in settingsKeys)
            {
                string line = GetLineByFirstWord(settingsFilename, key);
                if (line != null)
                {
                    string[] parts = line.Split(' ');
                    if (parts.Length > 1 && int.TryParse(parts[1], out int value))
                    {
                        settingsValues[key] = value;
                    }
                }
            }
            // Try to get the values from the dictionary and set the properties
            if (settingsValues.TryGetValue("connectionRetryCount", out int connectionRetryCount))
            {
                Settings.ConnectionRetryCount = connectionRetryCount;
            }
            if (settingsValues.TryGetValue("connectionRetryDelay", out int connectionRetryDelay))
            {
                Settings.ConnectionRetryDelay = connectionRetryDelay;
            }
            if (settingsValues.TryGetValue("lookupOverallCount", out int lookupOverallCount))
            {
                Settings.LookupOverallCount = lookupOverallCount;
            }
            if (settingsValues.TryGetValue("lookupRetryCount", out int lookupRetryCount))
            {
                Settings.LookupRetryCount = lookupRetryCount;
            }
            if (settingsValues.TryGetValue("lookupRetryDelay", out int lookupRetryDelay))
            {
                Settings.LookupRetryDelay = lookupRetryDelay;
            }
            if (settingsValues.TryGetValue("lookupNumberOfRows", out int lookupNumberOfRows))
            {
                Settings.LookupNumberOfRows = lookupNumberOfRows;
            }
            if (settingsValues.TryGetValue("lookupStartingRow", out int lookupStartingRow))
            {
                Settings.LookupStartingRow = lookupStartingRow;
            }
            if (settingsValues.TryGetValue("deleteOverallCount", out int deleteOverallCount))
            {
                Settings.DeleteOverallCount = deleteOverallCount;
            }
            if (settingsValues.TryGetValue("deleteRetryCount", out int deleteRetryCount))
            {
                Settings.DeleteRetryCount = deleteRetryCount;
            }
            if (settingsValues.TryGetValue("deleteRetryDelay", out int deleteRetryDelay))
            {
                Settings.DeleteRetryDelay = deleteRetryDelay;
            }
            if (settingsValues.TryGetValue("deleteNumberOfRows", out int deleteNumberOfRows))
            {
                Settings.DeleteNumberOfRows = deleteNumberOfRows;
            }
            if (settingsValues.TryGetValue("deleteStartingRow", out int deleteStartingRow))
            {
                Settings.DeleteStartingRow = deleteStartingRow;
            }



            // Display our Console UI
            Console.WriteLine("/**************************************************************************/");
            Console.WriteLine("/* Welcome to the NICE File Manipulation Program!                         */");
            Console.WriteLine("/* This program gives the option to look up file locations based on MCID  */");
            Console.WriteLine("/* or delete files from NICE storage using, API commands.                 */");
            Console.WriteLine("/*                                                                        */");
            Console.WriteLine("/* Copyright 2024                            */");
            Console.WriteLine("/* For technical support, please contact          */");
            Console.WriteLine("/**************************************************************************/");
            Console.WriteLine("/* Options [Enter your choice and press Enter - Invalid options Exit!]    */");
            Console.WriteLine("/* 1 - Look up file locations based on MCID                               */");
            Console.WriteLine("/* 2 - Delete files from NICE storage                                     */");
            Console.WriteLine("/* 3 - Exit immediately [or exits to Batch file]                          */");
            Console.WriteLine("/**************************************************************************/");
            Console.Write("Select an option: ");

            // Get the user's choice
            string choice = Console.ReadLine();
            if (choice != null)
            {
                Console.WriteLine(choice);
                switch (choice)
                {
                    case "1":
                        modeSwitch = 1;
                        int lookupNewStartingRow = (int)Settings.LookupStartingRow + (int)Settings.LookupNumberOfRows;
                        Console.WriteLine("/**************************************************************************/");
                        Console.WriteLine("/* 1 - Look up File - Details & Current Settings                          */");
                        Console.WriteLine("/**************************************************************************/");
                        Console.WriteLine("      Overall Processed: " + Settings.LookupOverallCount);
                        Console.WriteLine(" Connection Retry Count: " + Settings.ConnectionRetryCount);
                        Console.WriteLine(" Connection Retry Delay: " + Settings.ConnectionRetryDelay);
                        Console.WriteLine("         Retry Attempts: " + Settings.LookupRetryCount);
                        Console.WriteLine("            Retry Delay: " + Settings.LookupRetryDelay);
                        Console.WriteLine("         Number of Rows: " + Settings.LookupNumberOfRows);
                        Console.WriteLine("           Starting Row: " + lookupNewStartingRow);
                        Console.WriteLine("/**************************************************************************/");
                        Console.WriteLine("/* New Settings                                                           */");
                        Console.WriteLine("/*                                                                        */");
                        Console.WriteLine("/* To use the existing value above, press Enter.                          */");
                        Console.WriteLine("/* To change the value, enter a new value and press Enter.                */");
                        Console.WriteLine("/* New values will be saved for the next time the app is ran.             */");
                        Console.WriteLine("/**************************************************************************/");
                        Settings.ConnectionRetryCount = GetNewValue("How many times do you want to retry a failed connection?: ", Settings.ConnectionRetryCount);
                        Settings.ConnectionRetryDelay = GetNewValue("How long do you want to wait between connection retries (in milliseconds)?: ", Settings.ConnectionRetryDelay);
                        Settings.LookupRetryCount = GetNewValue("How many times do you want to retry a failed lookup?: ", Settings.LookupRetryCount);
                        Settings.LookupRetryDelay = GetNewValue("How long do you want to wait between look up retries (in milliseconds)?: ", Settings.LookupRetryDelay);
                        Settings.LookupNumberOfRows = GetNewValue("How many rows do you want to look up?:", Settings.LookupNumberOfRows);
                        Settings.LookupStartingRow = GetNewValue("What row do you want to start the look up on?: ", lookupNewStartingRow);
                        break;
                    case "2":
                        modeSwitch = 2;
                        int deleteNewStartingRow = (int)Settings.DeleteStartingRow + (int)Settings.DeleteNumberOfRows;
                        Console.WriteLine("/**************************************************************************/");
                        Console.WriteLine("/* 2 - Delete Files - Details & Current Settings                          */");
                        Console.WriteLine("/**************************************************************************/");
                        Console.WriteLine("      Overall Processed: " + Settings.DeleteOverallCount);
                        Console.WriteLine(" Connection Retry Count: " + Settings.ConnectionRetryCount);
                        Console.WriteLine(" Connection Retry Delay: " + Settings.ConnectionRetryDelay);
                        Console.WriteLine("         Retry Attempts: " + Settings.DeleteRetryCount);
                        Console.WriteLine("            Retry Delay: " + Settings.DeleteRetryDelay);
                        Console.WriteLine("         Number of Rows: " + Settings.DeleteNumberOfRows);
                        Console.WriteLine("           Starting Row: " + deleteNewStartingRow);
                        Console.WriteLine("/**************************************************************************/");
                        Console.WriteLine("/* New Settings                                                           */");
                        Console.WriteLine("/*                                                                        */");
                        Console.WriteLine("/* To use the existing value above, press Enter.                          */");
                        Console.WriteLine("/* To change the value, enter a new value and press Enter.                */");
                        Console.WriteLine("/* New values will be saved for the next time the app is ran.             */");
                        Console.WriteLine("/**************************************************************************/");
                        Settings.ConnectionRetryCount = GetNewValue("How many times do you want to retry a failed connection?: ", Settings.ConnectionRetryCount);
                        Settings.ConnectionRetryDelay = GetNewValue("How long do you want to wait between connection retries (in milliseconds)?: ", Settings.ConnectionRetryDelay);
                        Settings.DeleteRetryCount = GetNewValue("How many times do you want to retry a failed delete?: ", Settings.DeleteRetryCount);
                        Settings.DeleteRetryDelay = GetNewValue("How long do you want to wait between retries (in milliseconds)?: ", Settings.DeleteRetryDelay);
                        Settings.DeleteNumberOfRows = GetNewValue("How many rows do you want to delete?:", Settings.DeleteNumberOfRows);
                        Settings.DeleteStartingRow = GetNewValue("What row do you want to start on?: ", deleteNewStartingRow);
                        break;
                    case "3":
                        Exit();
                        return;
                    default:
                        Console.WriteLine("Invalid selection. Exiting!...");
                        Exit();
                        return;
                }
            }



            /////////////////////////////////////////////////
            // Prompt to continue or exit
            Console.WriteLine("/**************************************************************************/");
            Console.WriteLine("/* Continue?                      [Y to continue; Anything else to exit!] */");
            Console.WriteLine("/**************************************************************************/");
            Console.Write("Continue?: ");
            string confirm = Console.ReadLine();

            /////////////////////////////////////////////////
            // Y will continue the program
            // Anything else will exit the program
            if (confirm.ToLower() == "y")
            {
                foreach (var key in settingsKeys)
                {
                    if (settingsValues.TryGetValue(key, out int value))
                    {
                        Console.WriteLine($"Key: {key}, Value: {value}");
                    }
                }
                System.Threading.Thread.Sleep(1000);
                // Update the settings file with the new values
                SaveSettings(settingsFilename, settingsKeys, settingsValues);
                Console.WriteLine();
                Console.WriteLine("/**************************************************************************/");
                Console.WriteLine("/* Updating Settings & Starting!                                          */");
                Console.WriteLine("/**************************************************************************/");
                System.Threading.Thread.Sleep(10000);
                // Animate cursor to indicate processing...
                Console.CursorVisible = false; // Hide the cursor                
                var cts = new CancellationTokenSource();
                var spinnerToken = cts.Token;
                var spinner = Task.Run(() => Spin(spinnerToken));
                // Loop! Loop! Loop!
                try
                {
                    //////////////////////////////////////////////////////////////////////////
                    // Connect to and retrieve token for NICE inContact API
                    currentTime = DateTime.Now;
                    Console.WriteLine("Current Time :" + DateTime.Now);
                    authUrl = TokenDiscoveryService();
                    Console.WriteLine("Received Auth URL");
                    token = GetToken(authUrl);
                    Console.WriteLine("Obtained Token");
                    string tenantId = GetTenantId(token.access_token);
                    Console.WriteLine("Retrieved Tenant ID");
                    string api_endpoint = getBaseUrl(tenantId);
                    Console.WriteLine("Retrieved API Endpoint");
                    baseUrl = api_endpoint;

                    //////////////////////////////////////////////////////////////////////////
                    // Switch based on the user's selection... crude, but works for now
                    switch (modeSwitch)
                    {
                        case 1:
                            //SaveSettings(settingsFilename, settingsKeys, settingsValues);
                            // Look up file locations based on MCID
                            Console.WriteLine("Attempt to Load " + lookupListFilename + " File");
                            //Read the CSV file and process each row
                            using (var reader = new StreamReader(lookupListFilename))
                            {
                                Console.WriteLine("Successfully Loaded " + lookupListFilename + " File!");
                                string record;
                                for (int i = 0; i < Settings.LookupStartingRow; i++)
                                {
                                    if (reader.ReadLine() == null)
                                    {
                                        Console.WriteLine("### NOTICE ### Reached end of file while skipping rows.");
                                        return;
                                    }
                                    rowIndex++;
                                }
                                while ((record = reader.ReadLine()) != null)
                                {
                                    // Make sure we don't have a runaway test by limiting our row count
                                    if (rowIndex >= Settings.LookupStartingRow + Settings.LookupNumberOfRows)
                                    {
                                        break;
                                    }
                                    await FullFilePathLookup(token.access_token, record);
                                    counterDisplay++;
                                    // Display the final result in console
                                    Console.WriteLine("### Final Result ### Row: " + rowIndex + " - Datetime: " + DateTime.Now + " - Counter: " + counterDisplay + " - Status Code: " + statusCode);
                                    // Log the processed row index
                                    File.AppendAllText(lookupLogFilename, rowIndex + " - File: " + record + " - Datetime: " + DateTime.Now + " - Counter: " + counterDisplay + " - Status Code: " + statusCode + Environment.NewLine);
                                    rowIndex++;
                                }
                            }
                            break;
                        case 2:
                            // Update Settings.txt with new 1]\\\\values
                            //SaveSettings(settingsFilename, settingsKeys, settingsValues);
                            // Delete files from NICE storage
                            Console.WriteLine("Attempt to Load " + deleteListFilename + " File");
                            //Read the CSV file and process each row
                            using (var reader = new StreamReader(deleteListFilename))
                            {
                                Console.WriteLine("Successfully Loaded " + deleteListFilename + " File!");
                                string record;
                                for (int i = 0; i < Settings.DeleteStartingRow; i++)
                                {
                                    if (reader.ReadLine() == null)
                                    {
                                        Console.WriteLine("### NOTICE ### Reached end of file while skipping rows.");
                                        return;
                                    }
                                    rowIndex++;
                                }
                                while ((record = reader.ReadLine()) != null)
                                {
                                    // Make sure we don't have a runaway test by limiting our row count
                                    if (rowIndex >= Settings.DeleteStartingRow + Settings.DeleteNumberOfRows)
                                    {
                                        break;
                                    }
                                    DeleteFile(token.access_token, record.TrimEnd(','));
                                    counterDisplay++;
                                    // Display the final result in console
                                    Console.WriteLine("### Final Result ### Row: " + rowIndex + " - Datetime: " + DateTime.Now + " - Counter: " + counterDisplay + " - Status Code: " + statusCode);

                                    // Log the processed row index
                                    File.AppendAllText(deleteLogFilename, rowIndex + " - File: " + record.TrimEnd(',') + " - Datetime: " + DateTime.Now + " - Counter: " + counterDisplay + " - Status Code: " + statusCode + Environment.NewLine);
                                    rowIndex++;
                                }
                            }
                            break;
                        default:
                            break;
                    }

                    // close out our spinner
                    cts.Cancel();
                    await spinner; // Ensure the spinner task has completed
                    Console.CursorVisible = true; // Show the cursor
                    // show final messaging
                    Console.WriteLine("/**************************************************************************/");
                    Console.WriteLine("/* Finished Processing!                                                   */");
                    Console.WriteLine("/*                                                                        */");
                    Console.WriteLine("/* See Status_Log.txt for full details.                                   */");
                    Console.WriteLine("/**************************************************************************/");
                    Console.WriteLine("Total Processed So Far: " + rowIndex);
                    Console.WriteLine("Total Processed This Run: " + counterDisplay);
                    Console.WriteLine("/**************************************************************************/");

                }
                catch (WebException ex) when (ex.Status == WebExceptionStatus.SecureChannelFailure)
                {
                    Console.WriteLine("A 443 error occurred: " + ex.Message);
                    File.AppendAllText(deleteLogFilename, "Row: " + rowIndex + " - Datetime: " + DateTime.Now + " - Counter: " + counterDisplay + " - Status Code: " + statusCode + " - " + ex.Message + Environment.NewLine);
                    // If we've reached the maximum number of retries, rethrow the exception
                    if (connectionRetryCounter >= Settings.ConnectionRetryCount)
                    {
                        throw;
                    }
                    // Increment the retry counter
                    connectionRetryCounter++;
                    // Optionally, wait a bit before retrying
                    Console.WriteLine("Waiting for connection " + Settings.ConnectionRetryDelay + " attempt: " + connectionRetryCounter);
                    System.Threading.Thread.Sleep(Settings.ConnectionRetryDelay);
                }
                catch (WebException ex)
                {
                    if (ex.Response == null)
                    {
                        Console.WriteLine("An error occurred: " + ex.Message);
                        File.AppendAllText(deleteLogFilename, "Row: " + rowIndex + " - Datetime: " + DateTime.Now + " - Counter: " + counterDisplay + " - Status Code: " + statusCode + " - " + ex.Message + Environment.NewLine);
                        // If we've reached the maximum number of retries, rethrow the exception
                        if (connectionRetryCounter >= Settings.ConnectionRetryDelay)
                        {
                            throw;
                        }
                        // Increment the retry counter
                        connectionRetryCounter++;
                        // Optionally, wait a bit before retrying
                        Console.WriteLine("Waiting for connection " + Settings.ConnectionRetryDelay + " attempt: " + connectionRetryCounter);
                        System.Threading.Thread.Sleep(Settings.ConnectionRetryDelay);
                    }
                    else
                    {
                        Console.WriteLine("An error occurred: " + ex.Message);
                        File.AppendAllText(deleteLogFilename, "Row: " + rowIndex + " - Datetime: " + DateTime.Now + " - Counter: " + counterDisplay + " - Status Code: " + statusCode + " - " + ex.Message + Environment.NewLine);
                        // If we've reached the maximum number of retries, rethrow the exception
                        if (connectionRetryCounter >= Settings.ConnectionRetryDelay)
                        {
                            throw;
                        }
                        // Increment the retry counter
                        connectionRetryCounter++;
                        // Optionally, wait a bit before retrying
                        Console.WriteLine("Waiting for connection " + Settings.ConnectionRetryDelay + " attempt: " + connectionRetryCounter);
                        System.Threading.Thread.Sleep(Settings.ConnectionRetryDelay);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("An error occurred: " + ex.Message);
                    File.AppendAllText(deleteLogFilename, "Row: " + rowIndex + " - Datetime: " + DateTime.Now + " - Counter: " + counterDisplay + " - Status Code: " + statusCode + " - " + ex.Message + Environment.NewLine);
                    // If we've reached the maximum number of retries, rethrow the exception
                    if (connectionRetryCounter >= Settings.ConnectionRetryDelay)
                    {
                        throw;
                    }
                    // Increment the retry counter
                    connectionRetryCounter++;
                    Console.WriteLine("Waiting for connection " + Settings.ConnectionRetryDelay + " attempt: " + connectionRetryCounter);
                    System.Threading.Thread.Sleep(Settings.ConnectionRetryDelay);
                }
            }
            else
            {
                Exit();
            }
        }

        /////////////////////////////////////////////////
        // Process the User's input
        static int GetNewValue(string prompt, int currentValue)
        {
            Console.Write(prompt);
            string input = Console.ReadLine();
            if (string.IsNullOrEmpty(input))
            {
                return currentValue;
            }
            else if (int.TryParse(input, out int newValue))
            {
                return newValue;
            }
            else
            {
                Console.WriteLine("Please enter a valid integer.");
                return GetNewValue(prompt, currentValue);
            }
        }


        static string GetLineByFirstWord(string filePath, string keyword)
        {
            using (var reader = new StreamReader(filePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string firstWord = line.Split(' ')[0];
                    if (string.Equals(firstWord, keyword, StringComparison.OrdinalIgnoreCase))
                    {
                        return line;
                    }
                }
                return null;
            }
        }

        private static void SaveSettings(string filename, string[] settingsKeys, Dictionary<string, int> settingsValues)
        {
            List<string> lines = new List<string>();
            foreach (var key in settingsKeys)
            {
                if (settingsValues.TryGetValue(key, out int value))
                {
                    lines.Add($"{key} {value}");
                }
            }
            File.WriteAllLines(filename, lines);
        }


        private static async Task FullFilePathLookup(string access_token, string mcid)
        {
            //string mcid = "16507436a4445";
            string fileUri = "https://api-c30.incontact.com/incontactapi/services/v30.0/contacts/" + mcid + "/files?fields=isDeleted%2CfullFileName";
            using var client = new HttpClient();

            try
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access_token);
                client.DefaultRequestHeaders.Add("Accept", "application/json");

                var response = await client.GetAsync(fileUri);

                Console.WriteLine("GET Result: " + (int)response.StatusCode);
                Console.WriteLine("GET Result: " + response.StatusCode);
                Console.WriteLine("GET Result: " + response.ReasonPhrase);

                // if the response is successful, parse the JSON and log the results
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("Response Content: " + result);

                    var jArray = ParseJson(result);

                    foreach (var item in jArray)
                    {
                        JObject jsonObject = item as JObject;
                        if (jsonObject != null)
                        {
                            string key = "fullFileName";
                            JToken value;
                            if (jsonObject.TryGetValue(key, out value))
                            {
                                Console.WriteLine("Key: " + key);
                                Console.WriteLine("Value: " + value.ToString());
                                // Log successful lookups to the Lookup_CallLog_Paths.txt file
                                File.AppendAllText(lookupCallLogPathsFilename, value.ToString() + Environment.NewLine);
                            }
                        }
                    }
                }
                else
                {
                    // Log errors to the Failed_Requests.txt file
                    File.AppendAllText(lookupFailedRequestFilename, "Row: " + rowIndex + " - Datetime: " + DateTime.Now + " - Counter: " + counterDisplay + " - Status Code: " + statusCode + " - " + response.ReasonPhrase + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static void DeleteFile(string access_token, string fileName)
        {
            Console.WriteLine("DELETE Started");
            string fileUri = "https://api-c30.incontact.com/inContactAPI/services/v29.0/files?fileName=" + fileName;
            //   string fileURI = "https://api-b2.incontact.com/incontactapi/services/v24.0/files?fileName=" + fileName;
            using var client = new HttpClient();
            var statusCodeInt = 0;

            try
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access_token);
                client.DefaultRequestHeaders.Add("Accept", "application/json");

                var response = Task.Run(() => client.DeleteAsync(fileUri));
                response.Wait();
                statusCodeInt = (int)response.Result.StatusCode;
                statusCode = statusCodeInt.ToString();
                Console.WriteLine("DELETE Result: " + (int)response.Result.StatusCode);
                Console.WriteLine("DELETE Result: " + response.Result.StatusCode);
                Console.WriteLine("DELETE Result: " + response.Result.ReasonPhrase);
                counterDisplay++;
                Console.WriteLine("DELETE Result Counter: " + counterDisplay);
                var result = Task.Run(() => response.Result.Content.ToString());
                result.Wait();

                // Record any errors to the Failed_Requests.txt file as 200 should be the only status code.
                if ((int)response.Result.StatusCode != 200)
                {
                    delayNextRow = 5000;
                    Console.WriteLine("DELETE ERROR --- WAIT: " + delayNextRow);
                    File.AppendAllText(deleteFailedRequestFilename, "Row: " + rowIndex + " - Datetime: " + DateTime.Now + " - Counter: " + counterDisplay + " - Status Code: " + statusCode + " - " + response.Result.ReasonPhrase + Environment.NewLine);
                }
                else
                {
                    delayNextRow = 500;
                    Console.WriteLine("DELETE SUCCESS - WAIT: " + delayNextRow);
                }
                Task.Delay(delayNextRow).Wait();
                Console.WriteLine("DELETE Ended");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error During DeleteFile Function: " + ex.Message);
                // If we've reached the maximum number of retries, rethrow the exception
                if (deleteRetryCount >= Settings.DeleteRetryCount)
                {
                    throw;
                }
                // Increment the retry counter
                deleteRetryCount++;
                Console.WriteLine("Waiting for connection 5000ms; attempt: " + deleteRetryCount);
                System.Threading.Thread.Sleep(5000);
            }

        }

        private static JArray ParseJson(string json)
        {
            var jObject = JObject.Parse(json);
            var jToken = jObject.SelectToken("files");
            var jArray = JArray.Parse(jToken?.ToString() ?? "[]");
            foreach (var item in jArray)
            {
                Console.WriteLine(item);
            }
            return jArray;
        }

        /////////////////////////////////////////////////
        // Quick exit function
        static void Exit()
        {
            Console.WriteLine("Exited program, press any key to close Batch file...");
            Console.ReadKey();
            Environment.Exit(0); // This will close the application
        }

        /////////////////////////////////////////////////
        // Spinner Function
        static async Task Spin(CancellationToken spinnerToken)
        {
            string spinner = "-\\|/";
            int i = 0;
            while (!spinnerToken.IsCancellationRequested)
            {
                Console.SetCursorPosition(0, Console.BufferHeight - 1);
                Console.Write(spinner[i++ % spinner.Length]);
                await Task.Delay(100);
            }
        }

        private static string TokenDiscoveryService()
        {
            string tokenDiscoveryUrl = "https://cxone.niceincontact.com/.well-known/openid-configuration";
            using var client = new HttpClient();
            string authUrl = "";
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            var response = client.GetAsync(tokenDiscoveryUrl);
            var result = response.Result.Content.ReadAsStringAsync();
            if (result.Result != "")
            {
                JObject ds = JsonConvert.DeserializeObject<JObject>(result.Result.ToString());
                //   var jo = JObject.Parse(result.Result.ToString());
                var token_endpoint = ds.ContainsKey("token_endpoint");
                //  string test = token_endpoint;
                if (token_endpoint == true)
                {
                    authUrl = ds["token_endpoint"].ToString();
                }
                else
                {
                    authUrl = "https://cxone.niceincontact.com/auth/token";
                }
            }
            else
            {
                authUrl = "https://cxone.niceincontact.com/auth/token";
            }
            return authUrl;
        }

        private static Token GetToken(string tokenURI)
        {
            // string tokenURI = _config["Settings:token_endpoint"];//_config["Settings:tokenURI"];//"https://na1.nice-incontact.com/authentication/v1/token/access-key";
            TokenCreds creds = new TokenCreds();

            creds.username = _config["Settings:accessKeyId"];
            creds.password = _config["Settings:accessKeySecret"];
            creds.grant_type = "password";
            string basicAuth = _config["Settings:basicAuth"];
            var payload = $"grant_type=password&username={creds.username}&password={creds.password}";
            var json = JsonConvert.SerializeObject(creds);
            var data = new StringContent(payload, Encoding.UTF8, "application/x-www-form-urlencoded");

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", basicAuth);
            var response = client.PostAsync(tokenURI, data);

            var result = response.Result.Content.ReadAsStringAsync();
            Token token = JsonConvert.DeserializeObject<Token>(result.Result.ToString());
            //   Console.WriteLine(result);
            return token;
        }

        private static string GetTenantId(string token)
        {
            string tenantUrl = "https://api-na1.niceincontact.com/incontactAPI/services/v25.0/business-unit";
            using var client = new HttpClient();
            string tenantId = "";
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            //   string baseUrl = "";
            var response = client.GetAsync(tenantUrl);
            var result = response.Result.Content.ReadAsStringAsync();
            if (result.Result != "")
            {
                JObject ds = JsonConvert.DeserializeObject<JObject>(result.Result.ToString());
                tenantId = ds["businessUnits"][0]["tenantId"].ToString();
            }
            else
            {
                tenantId = _config["Settings:tenantId"];
            }
            return tenantId;
        }

        private static string getBaseUrl(string tenantId)
        {
            //   string tenantId = _config["Settings:tenantId"];
            string newBaseUrl = "";
            string cxoneUrl = "https://cxone.niceincontact.com/.well-known/cxone-configuration?tenantId=" + tenantId;
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            var response = client.GetAsync(cxoneUrl);
            var result = response.Result.Content.ReadAsStringAsync();
            if (result.Result != "")
            {
                JObject ds = JsonConvert.DeserializeObject<JObject>(result.Result.ToString());
                var api_endpoint = ds.ContainsKey("api_endpoint");

                //  string test = token_endpoint;
                if (api_endpoint == true)
                {
                    newBaseUrl = ds["api_endpoint"].ToString();
                }
                else
                {
                    newBaseUrl = _config["Settings:baseUrl"];
                }
            }
            else
            {
                newBaseUrl = _config["Settings:baseUrl"];
            }
            return newBaseUrl;
        }

        static void GlobalExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            Console.WriteLine("Unhandled exception: " + e.Message);
            Console.WriteLine(e.StackTrace);
            Console.ReadKey();
        }
    }
}

public class Token
{
    public string? id_token { get; set; }
    public string? token_type { get; set; }
    public string? refresh_token { get; set; }
    public string? access_token { get; set; }
    public string? expires_in { get; set; }
}

public class TokenCreds
{
    public string? username { get; set; }
    public string? password { get; set; }
    public string? grant_type { get; set; }
}
