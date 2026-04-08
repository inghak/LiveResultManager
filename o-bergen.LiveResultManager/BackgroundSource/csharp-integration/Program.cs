using System;
using System.Threading.Tasks;

namespace OBergen.LiveResults
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("===========================================");
            Console.WriteLine("  O-Bergen Live Resultater - Upload til Supabase");
            Console.WriteLine("===========================================");
            Console.WriteLine();

            try
            {
                var service = new LiveResultsService();

                Console.WriteLine("Initialiserer tilkobling til Supabase...");
                await service.InitializeAsync();

                Console.WriteLine();
                Console.WriteLine("Velg modus:");
                Console.WriteLine("1. Last opp alle resultater én gang");
                Console.WriteLine("2. Kontinuerlig overvåking (anbefalt)");
                Console.Write("Valg (1/2): ");

                var choice = Console.ReadLine();

                if (choice == "1")
                {
                    await service.UploadAllResultsAsync();
                    Console.WriteLine();
                    Console.WriteLine("✅ Upload fullført. Trykk en tast for å avslutte...");
                    Console.ReadKey();
                }
                else if (choice == "2")
                {
                    Console.WriteLine();
                    await service.StartWatchingResultsFileAsync();
                }
                else
                {
                    Console.WriteLine("❌ Ugyldig valg. Avslutter...");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine($"❌ FATAL FEIL: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine();
                Console.WriteLine("Trykk en tast for å avslutte...");
                Console.ReadKey();
            }
        }
    }
}
