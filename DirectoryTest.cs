namespace Fact.BatchCleaner.Tests
{
    public class DirectoryTest
    {
        public static void TestDirectoryAccess()
        {
            Console.WriteLine("=== Directory Access Test ===");

            var logDirectory = @"E:\moe\CycleClean\logs";
            Console.WriteLine($"Testing directory: {logDirectory}");

            try
            {
                // Test if we can create the directory
                if (!Directory.Exists(logDirectory))
                {
                    Console.WriteLine("Directory doesn't exist, attempting to create...");
                    Directory.CreateDirectory(logDirectory);
                    Console.WriteLine("Directory created successfully!");
                }
                else
                {
                    Console.WriteLine("Directory already exists.");
                }

                // Test if we can write a file
                var testFile = Path.Combine(logDirectory, "test.txt");
                Console.WriteLine($"Testing file write to: {testFile}");
                
                File.WriteAllText(testFile, $"Test written at {DateTime.Now}");
                Console.WriteLine("File write successful!");
                
                // Read it back
                var content = File.ReadAllText(testFile);
                Console.WriteLine($"File content: {content}");
                
                // Clean up
                File.Delete(testFile);
                Console.WriteLine("Test file deleted.");
                
                Console.WriteLine("=== All tests passed! ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                Console.WriteLine($"Full exception: {ex}");
            }
        }
    }
}