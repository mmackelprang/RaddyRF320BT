using System.Reflection;
using Xunit.Abstractions;

namespace RadioProtocol.Tests.Utilities;

/// <summary>
/// Test utilities and helpers for the test suite
/// </summary>
public static class TestUtilities
{
    /// <summary>
    /// Gets all test classes in the current assembly
    /// </summary>
    public static IEnumerable<Type> GetTestClasses()
    {
        return Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.GetMethods().Any(m => m.GetCustomAttributes<Xunit.FactAttribute>().Any() ||
                                                m.GetCustomAttributes<Xunit.TheoryAttribute>().Any()));
    }

    /// <summary>
    /// Gets count of test methods in a class
    /// </summary>
    public static int GetTestMethodCount(Type testClass)
    {
        return testClass.GetMethods()
            .Count(m => m.GetCustomAttributes<Xunit.FactAttribute>().Any() ||
                        m.GetCustomAttributes<Xunit.TheoryAttribute>().Any());
    }

    /// <summary>
    /// Creates test data for various protocol scenarios
    /// </summary>
    public static class TestData
    {
        public static readonly byte[] ValidSyncRequest = { 0xAB, 0x01, 0x01, 0xAD };
        public static readonly byte[] ValidSyncResponse = { 0xAB, 0x02, 0x20, 0xCD };
        public static readonly byte[] ValidStatusRequest = { 0xAB, 0x03, 0x01, 0xAF };
        public static readonly byte[] ValidButtonPress = { 0xAB, 0x04, 0x01, 0x01, 0xB1 };
        public static readonly byte[] ValidChannelCommand = { 0xAB, 0x05, 0x01, 0x05, 0xB6 };
        
        public static readonly byte[] InvalidStartByte = { 0xAA, 0x01, 0x01, 0xAC };
        public static readonly byte[] InvalidChecksum = { 0xAB, 0x01, 0x01, 0xFF };
        public static readonly byte[] TooShort = { 0xAB, 0x01 };
        public static readonly byte[] TooLong = new byte[300]; // Exceeds max length

        static TestData()
        {
            // Initialize too long array with valid start
            TooLong[0] = 0xAB;
            for (int i = 1; i < TooLong.Length; i++)
            {
                TooLong[i] = 0x01;
            }
        }

        /// <summary>
        /// Gets test messages documented in the protocol files
        /// </summary>
        public static IEnumerable<(string Description, byte[] Data)> GetDocumentedMessages()
        {
            yield return ("Sync Request", ValidSyncRequest);
            yield return ("Sync Response", ValidSyncResponse);
            yield return ("Status Request", ValidStatusRequest);
            yield return ("Button Press", ValidButtonPress);
            yield return ("Channel Command", ValidChannelCommand);
        }

        /// <summary>
        /// Gets invalid test messages for error testing
        /// </summary>
        public static IEnumerable<(string Description, byte[] Data)> GetInvalidMessages()
        {
            yield return ("Invalid Start Byte", InvalidStartByte);
            yield return ("Invalid Checksum", InvalidChecksum);
            yield return ("Too Short", TooShort);
            yield return ("Too Long", TooLong);
        }

        /// <summary>
        /// Gets button types for testing
        /// </summary>
        public static IEnumerable<RadioProtocol.Core.Constants.ButtonType> GetAllButtonTypes()
        {
            return Enum.GetValues<RadioProtocol.Core.Constants.ButtonType>();
        }

        /// <summary>
        /// Gets message types for testing
        /// </summary>
        public static IEnumerable<RadioProtocol.Core.Constants.MessageType> GetAllMessageTypes()
        {
            return Enum.GetValues<RadioProtocol.Core.Constants.MessageType>();
        }
    }

    /// <summary>
    /// Assertion helpers for common test scenarios
    /// </summary>
    public static class Assertions
    {
        public static void AssertValidProtocolMessage(byte[] message, string? context = null)
        {
            var contextMsg = context != null ? $" (Context: {context})" : "";
            
            if (message.Length < 4)
                throw new Xunit.Sdk.XunitException($"Message too short{contextMsg}");
            
            if (message[0] != 0xAB)
                throw new Xunit.Sdk.XunitException($"Invalid start byte{contextMsg}");
            
            // Verify checksum
            var dataForChecksum = message.Take(message.Length - 1).ToArray();
            var expectedChecksum = dataForChecksum.Sum(b => (int)b) & 0xFF;
            var actualChecksum = message[^1];
            
            if (expectedChecksum != actualChecksum)
                throw new Xunit.Sdk.XunitException($"Invalid checksum. Expected: {expectedChecksum:X2}, Actual: {actualChecksum:X2}{contextMsg}");
        }

        public static void AssertMessageType(byte[] message, RadioProtocol.Core.Constants.MessageType expectedType, string? context = null)
        {
            AssertValidProtocolMessage(message, context);
            
            var actualType = (RadioProtocol.Core.Constants.MessageType)message[1];
            if (actualType != expectedType)
            {
                var contextMsg = context != null ? $" (Context: {context})" : "";
                throw new Xunit.Sdk.XunitException($"Wrong message type. Expected: {expectedType}, Actual: {actualType}{contextMsg}");
            }
        }

        public static void AssertRadioId(byte[] message, byte expectedRadioId, string? context = null)
        {
            AssertValidProtocolMessage(message, context);
            
            var actualRadioId = message[2];
            if (actualRadioId != expectedRadioId)
            {
                var contextMsg = context != null ? $" (Context: {context})" : "";
                throw new Xunit.Sdk.XunitException($"Wrong radio ID. Expected: {expectedRadioId:X2}, Actual: {actualRadioId:X2}{contextMsg}");
            }
        }
    }

    /// <summary>
    /// Helper methods for test setup and teardown
    /// </summary>
    public static class Setup
    {
        public static (RadioProtocol.Tests.Mocks.MockBluetoothConnection bluetooth, 
                      RadioProtocol.Tests.Mocks.MockRadioLogger logger, 
                      RadioProtocol.Core.RadioManager manager) CreateTestRadioManager()
        {
            var bluetooth = new RadioProtocol.Tests.Mocks.MockBluetoothConnection();
            var logger = new RadioProtocol.Tests.Mocks.MockRadioLogger();
            var manager = new RadioProtocol.Core.RadioManager(bluetooth, logger);
            
            return (bluetooth, logger, manager);
        }

        public static async Task<(RadioProtocol.Tests.Mocks.MockBluetoothConnection bluetooth, 
                                 RadioProtocol.Tests.Mocks.MockRadioLogger logger, 
                                 RadioProtocol.Core.RadioManager manager)> CreateConnectedTestRadioManager(
            string deviceAddress = "00:11:22:33:44:55")
        {
            var (bluetooth, logger, manager) = CreateTestRadioManager();
            await manager.ConnectAsync(deviceAddress);
            return (bluetooth, logger, manager);
        }

        public static void QueueStandardResponses(RadioProtocol.Tests.Mocks.MockBluetoothConnection bluetooth, int count = 10)
        {
            for (int i = 0; i < count; i++)
            {
                bluetooth.QueueResponse("AB0720D6"); // Standard general response
            }
        }

        public static void DisposeTestObjects(params IDisposable[] disposables)
        {
            foreach (var disposable in disposables)
            {
                try
                {
                    disposable?.Dispose();
                }
                catch
                {
                    // Ignore disposal errors in tests
                }
            }
        }
    }
}

/// <summary>
/// Custom test collection for managing shared test resources
/// </summary>
[Xunit.CollectionDefinition("RadioProtocol Tests")]
public class RadioProtocolTestCollection : Xunit.ICollectionFixture<RadioProtocolTestFixture>
{
}

/// <summary>
/// Test fixture for shared test resources
/// </summary>
public class RadioProtocolTestFixture : IDisposable
{
    public RadioProtocolTestFixture()
    {
        // Initialize any shared test resources
        TestStartTime = DateTime.UtcNow;
    }

    public DateTime TestStartTime { get; }

    public void Dispose()
    {
        // Clean up any shared test resources
        GC.SuppressFinalize(this);
    }
}