using System;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.IO;
using System.Collections.Generic;

class Program
{
    static string FilePath = "parkingData.json";
    static string ConfigFilePath = "config.json";
    static ConfigData config = new ConfigData();
    static Dictionary<string, DateTime> parkingTimes = new Dictionary<string, DateTime>();

    static void Main()
    {
        // Ladda in konfigurationsdata från JSON
        config = LoadConfigData();

        // Ladda in parkeringsinformation från JSON
        string[] parkingGarage = LoadParkingData();

        while (true)
        {
            Console.Clear();

            // Skriver ut ASCII text för rubriken
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(@"
  _____  _____            _____ _    _ ______    _____        _____  _  _______ _   _  _____         __ 
 |  __ \|  __ \     /\   / ____| |  | |  ____|  |  __ \ /\   |  __ \| |/ /_   _| \ | |/ ____|       /_ |
 | |__) | |__) |   /  \ | |  __| |  | | |__     | |__) /  \  | |__) | ' /  | | |  \| | |  __   __   _| |
 |  ___/|  _  /   / /\ \| | |_ | |  | |  __|    |  ___/ /\ \ |  _  /|  <   | | | . ` | | |_ |  \ \ / / |
 | |    | | \ \  / ____ \ |__| | |__| | |____   | |  / ____ \| | \ \| . \ _| |_| |\  | |__| |   \ V /| |
 |_|    |_|  \_\/_/    \_\_____|\____/|______|  |_| /_/    \_\_|  \_\_|\_\_____|_| \_|\_____|    \_/ |_|
                                                                                                      
                                                                                                      
                                                                                                  
                                                                                                  
            ");
            Console.ResetColor();

            // Grundläggande parkering system information
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Välkommen till Prague Parking ");
            Console.WriteLine("Antal parkeringsplatser: " + config.TotalParkingSpaces);
            Console.WriteLine("Timpris för bil: " + config.Prices["BIL"] + " kr");
            Console.WriteLine("Timpris för motorcykel: " + config.Prices["MC"] + " kr");
            Console.WriteLine("Timpris för buss: " + config.Prices["BUSS"] + " kr");
            Console.WriteLine("Timpris för cykel: " + config.Prices["CYKEL"] + " kr");
            Console.WriteLine(config.FreeMinutes + " första minuterna är gratis för alla fordon.");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("Prague Parking System");
            Console.WriteLine("1. Parkera Fordon");
            Console.WriteLine("2. Hämta Fordon");
            Console.WriteLine("3. Flytta Fordon");
            Console.WriteLine("4. Sök Fordon på Reg nr");
            Console.WriteLine("5. Visa aktuell parkerings vy");
            Console.WriteLine("6. Läs in konfigurationsfilen på nytt");
            Console.WriteLine("7. Avsluta");
            Console.Write("Välj ett alternativ: ");
            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    ParkVehicle(parkingGarage);
                    break;
                case "2":
                    RetrieveVehicle(parkingGarage);
                    break;
                case "3":
                    MoveVehicle(parkingGarage);
                    break;
                case "4":
                    SearchVehicle(parkingGarage);
                    break;
                case "5":
                    ViewParkingMap(parkingGarage);
                    break;
                case "6":
                    config = LoadConfigData();
                    Console.WriteLine("Konfigurationsfilen har lästs in på nytt.");
                    break;
                case "7":
                    SaveParkingData(parkingGarage);
                    return; // Avsluta programmet
                default:
                    Console.WriteLine("Ogiltigt val, Försök igen.");
                    break;
            }

            // Spara data efter varje operation  
            SaveParkingData(parkingGarage);

            Console.WriteLine("Tryck enter för att fortsätta...");
            Console.ReadLine();
        }
    }

    // Metod för att ladda in parkeringsdata från JSON-fil
    static string[] LoadParkingData()
    {
        if (File.Exists(FilePath))
        {
            string jsonString = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<string[]>(jsonString);
        }
        return new string[config.TotalParkingSpaces]; // Om filen inte finns, returnera tomt garage
    }

    // Metod för att spara parkeringsdata till JSON-fil
    static void SaveParkingData(string[] garage)
    {
        string jsonString = JsonSerializer.Serialize(garage);
        File.WriteAllText(FilePath, jsonString);
    }

    // Metod för att ladda in konfigurationsdata från JSON-fil
    static ConfigData LoadConfigData()
    {
        if (File.Exists(ConfigFilePath))
        {
            string jsonString = File.ReadAllText(ConfigFilePath);
            ConfigData loadedConfig = JsonSerializer.Deserialize<ConfigData>(jsonString);

            // Sätt standardvärden för saknade delar av konfigurationsfilen
            if (loadedConfig.Prices == null)
            {
                loadedConfig.Prices = new Dictionary<string, decimal>
                {
                    { "BIL", 20.00m },
                    { "MC", 10.00m },
                    { "BUSS", 50.00m },
                    { "CYKEL", 5.00m }
                };
            }
            if (loadedConfig.TotalParkingSpaces == 0)
            {
                loadedConfig.TotalParkingSpaces = 100;
            }
            if (loadedConfig.FreeMinutes == 0)
            {
                loadedConfig.FreeMinutes = 10;
            }
             if (loadedConfig.MaxVehiclesPerSlot == null)
            {
                loadedConfig.MaxVehiclesPerSlot = new Dictionary<string, int>
                {
                    { "BIL", 1 },
                    { "MC", 2 },
                    { "BUSS", 1 },
                    { "CYKEL", 4 }
                };
            }

            return loadedConfig;
        }
        // Standardvärden om filen inte finns
        return new ConfigData
        {
            TotalParkingSpaces = 100,
            FreeMinutes = 10,
            Prices = new Dictionary<string, decimal>
            {
                { "BIL", 20.00m },
                { "MC", 10.00m },
                { "BUSS", 50.00m },
                { "CYKEL", 5.00m }
            }
        };
    }

    static void ParkVehicle(string[] garage)
    {
        Console.Write("Ange fordonstyp (BIL/MC/BUSS/CYKEL): ");
        string vehicleType = Console.ReadLine().ToUpper();

        if (!config.Prices.ContainsKey(vehicleType))
        {
            Console.WriteLine("Fel Fordonstyp.");
            return;
        }

        Console.WriteLine("Det ska vara 1-2 bokstäver, 1-4 siffror, följt av 0-2 bokstäver  ");
        Console.Write("Ange registreringsnummer: ");
        string registration = Console.ReadLine();

        // Regex för att kontrollera formatet
        string pattern = @"^[A-Za-z]{1,2}[0-9]{1,4}[A-Za-z]{0,2}$";

        if (!string.IsNullOrWhiteSpace(registration) && registration.Length <= 10 && Regex.IsMatch(registration, pattern))
        {
            Console.WriteLine("Registreringsnumret är giltigt.");
        }
        else
        {
            Console.WriteLine("Fel format på registreringsnumret. Det ska vara 1-2 bokstäver, 1-4 siffror, följt av 0-2 bokstäver.");
            return;
        }

        // hitta en tom parkeringsplats
        for (int i = 0; i < garage.Length; i++)
        {
            if (string.IsNullOrEmpty(garage[i]))
            {
                garage[i] = $"{vehicleType}#{registration}";
                parkingTimes[registration] = DateTime.Now;
                Console.WriteLine($"Fordon parkerat på ruta {i + 1}.");
                return;
            }
            else if (vehicleType == "MC" && garage[i].StartsWith("MC#"))
            {
                // kontrollera om 2 mc på samma ruta
                if (garage[i].Split('|').Length < 2)
                {
                    garage[i] += $"|{vehicleType}#{registration}";
                    parkingTimes[registration] = DateTime.Now;
                    Console.WriteLine($"Motorcykel dubbelparkerad i ruta {i + 1}.");
                    return;
                }
            }
        }

        Console.WriteLine("inga tillgängliga parkeringsplatser.");
    }

    static void RetrieveVehicle(string[] garage)
    {
        Console.Write("ange reg nr för att hämta fordon: ");
        string registration = Console.ReadLine();

        // sök efter fordon med reg nr
        for (int i = 0; i < garage.Length; i++)
        {
            if (!string.IsNullOrEmpty(garage[i]) && garage[i].Contains(registration))
            {
                DateTime startTime;
                if (parkingTimes.TryGetValue(registration, out startTime))
                {
                    DateTime endTime = DateTime.Now;
                    TimeSpan parkedDuration = endTime - startTime;
                    decimal cost = CalculateParkingCost(parkedDuration, registration);
                    Console.WriteLine($"Total parkeringstid: {parkedDuration.TotalMinutes:F2} minuter. Kostnad: {cost:F2} kr.");
                    parkingTimes.Remove(registration);
                }

                if (garage[i].StartsWith("MC#"))
                {
                    // om parkeringsrutan innehåller dubbla motorcyklar, dela
                    string[] vehicles = garage[i].Split('|');
                    for (int j = 0; j < vehicles.Length; j++)
                    {
                        if (vehicles[j].Contains(registration))
                        {
                            vehicles[j] = ""; //markera specifik motorcykel för att hämta
                            Console.WriteLine($"Hämtar motorcykel med reg nr {registration} från ruta {i + 1}.");
                            // uppdatera parkeringslistan
                            garage[i] = string.Join("|", vehicles).Trim('|'); // ta bort tomma inlägg
                            if (string.IsNullOrEmpty(garage[i]))
                            {
                                garage[i] = ""; // Rensa utrymmet om det nu är tomt
                            }
                            return;
                        }
                    }
                }
                else // det är ett fordon
                {
                    Console.WriteLine($"hämta fordon: {garage[i]} från ruta {i + 1}.");
                    garage[i] = ""; // rensa parkeringsruta
                    return;
                }
            }
        }

        Console.WriteLine("hittar ej fordon.");
    }

    static decimal CalculateParkingCost(TimeSpan duration, string registration)
    {
        decimal hourlyRate = 0;
        string vehicleType = registration.Split('#')[0];

        if (config.Prices.ContainsKey(vehicleType))
        {
            hourlyRate = config.Prices[vehicleType];
        }

        double totalMinutes = duration.TotalMinutes;
        if (totalMinutes <= config.FreeMinutes)
        {
            return 0;
        }

        double totalHours = Math.Ceiling((totalMinutes - config.FreeMinutes) / 60);
        return (decimal)totalHours * hourlyRate;
    }

    static void MoveVehicle(string[] garage)
    {
        Console.Write("Ange aktuellt nummer på parkeringsplats (1-" + config.TotalParkingSpaces + "): ");
        if (int.TryParse(Console.ReadLine(), out int currentSpace) && currentSpace >= 1 && currentSpace <= config.TotalParkingSpaces)
        {
            int currentIndex = currentSpace - 1;

            if (!string.IsNullOrEmpty(garage[currentIndex]))
            {
                Console.Write("Ange nytt nummer på parkeringsplatsen (1-" + config.TotalParkingSpaces + "): ");
                if (int.TryParse(Console.ReadLine(), out int newSpace) && newSpace >= 1 && newSpace <= config.TotalParkingSpaces)
                {
                    int newIndex = newSpace - 1;

                    if (string.IsNullOrEmpty(garage[newIndex]))
                    {
                        garage[newIndex] = garage[currentIndex]; // flytta fordon
                        garage[currentIndex] = ""; // rensa gammal ruta
                        Console.WriteLine($"Fordon flyttad från ruta {currentSpace} Till {newSpace}.");
                    }
                    else
                    {
                        Console.WriteLine("Det angivna rutan är upptagen.");
                    }
                }
                else
                {
                    Console.WriteLine("Ogiltig ruta.");
                }
            }
            else
            {
                Console.WriteLine("Inget fordon hittades i det aktuella rutan.");
            }
        }
        else
        {
            Console.WriteLine("Ogiltig ruta.");
        }
    }

    static void SearchVehicle(string[] garage)
    {
        Console.Write("Ange reg nr för att söka: ");
        string registration = Console.ReadLine();

        for (int i = 0; i < garage.Length; i++)
        {
            if (garage[i].Contains(registration))
            {
                Console.WriteLine($"Fordon hittad på ruta {i + 1}: {garage[i]}.");
                return;
            }
        }

        Console.WriteLine("Fordonet hittades inte.");
    }

    static void ViewParkingMapWithVisual(string[] garage)
    {
        Console.WriteLine("Aktuell parkeringsvy:");
        for (int i = 0; i < garage.Length; i++)
        {
            string status = string.IsNullOrEmpty(garage[i]) ? "[ ]" : "[X]";
            Console.Write(status);
            if ((i + 1) % 10 == 0) Console.WriteLine(); // Byt rad efter varje 10 rutor för bättre översikt
        }
        Console.WriteLine();
    }
}

class ConfigData
{
    public int TotalParkingSpaces { get; set; }
    public int FreeMinutes { get; set; }
    public Dictionary<string, decimal> Prices { get; set; }
}
