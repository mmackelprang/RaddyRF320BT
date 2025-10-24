# RadioProtocol.Tests - Test Execution Guide

This directory contains a comprehensive test suite for the RadioProtocol.Core library.

## Test Structure

### Unit Tests
- **RadioCommandBuilderTests.cs** - Tests for command building and validation
- **RadioProtocolParserTests.cs** - Tests for protocol message parsing  
- **Messages/MessageTests.cs** - Tests for message types and constants
- **Protocol/ProtocolUtilsTests.cs** - Tests for protocol utility functions
- **Mocks/MockImplementationTests.cs** - Tests for mock implementations

### Integration Tests  
- **RadioManagerIntegrationTests.cs** - End-to-end integration tests
- **EndToEnd/EndToEndTests.cs** - Complete workflow tests

### Test Utilities
- **Mocks/MockImplementations.cs** - Mock Bluetooth and logging implementations
- **Utilities/TestUtilities.cs** - Test helpers and utilities

## Running Tests

### Visual Studio
1. Open the solution in Visual Studio
2. Build the solution (Ctrl+Shift+B)
3. Open Test Explorer (Test > Test Explorer)
4. Click "Run All Tests"

### Command Line (.NET CLI)
```bash
# Navigate to the test project directory
cd tests\RadioProtocol.Tests

# Run all tests
dotnet test

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run with coverage (requires coverlet)
dotnet test --collect:"XPlat Code Coverage"

# Run specific test class
dotnet test --filter "ClassName=RadioManagerIntegrationTests"

# Run specific test method
dotnet test --filter "MethodName=SendButtonPress_ShouldSucceed"
```

### PowerShell Test Runner
```powershell
# Run from solution root
& dotnet test .\tests\RadioProtocol.Tests\RadioProtocol.Tests.csproj --logger "trx;LogFileName=TestResults.trx" --logger "console;verbosity=normal"
```

## Test Categories

### 1. Protocol Validation Tests
- Message format validation
- Checksum calculation and verification
- Protocol constant verification
- Message type enumeration tests

### 2. Command Building Tests
- Button press command generation
- Channel command generation  
- Sync and status request generation
- Payload handling and validation

### 3. Mock Implementation Tests
- MockBluetoothConnection functionality
- MockRadioLogger functionality
- Event handling and state management
- Error simulation and recovery

### 4. Integration Tests
- Complete command-response cycles
- Connection management
- Event propagation
- Error handling workflows

### 5. End-to-End Tests
- Full protocol workflows
- Performance testing
- Concurrency testing
- Resource cleanup testing

## Test Data

Tests use documented message sequences from:
- `COMMAND_RESPONSE_SEQUENCES.md`
- `ADDITIONAL_MESSAGES.md` 
- `PROTOCOL_REFERENCE.md`

### Sample Test Messages
```
AB0120AE - Sync Request (RadioID: 0x01)
AB0220B1 - Sync Response  
AB0320C2 - Status Request Response
AB040120010B7 - Button Press (PTT)
AB050120050BC - Channel Command (Channel 5)
```

## Expected Test Results

### Test Coverage
- **Target Coverage**: >90% line coverage
- **Critical Paths**: 100% coverage for protocol parsing and command building
- **Edge Cases**: Comprehensive error condition testing

### Performance Benchmarks
- **Individual Commands**: <10ms average execution time
- **100 Rapid Commands**: <5 seconds total execution time
- **Concurrent Operations**: No deadlocks or race conditions

### Test Counts (Approximate)
- Unit Tests: ~60 tests
- Integration Tests: ~15 tests  
- End-to-End Tests: ~6 tests
- **Total**: ~80+ test methods

## Debugging Tests

### Common Issues
1. **Connection Timeouts**: Check MockBluetoothConnection setup
2. **Invalid Messages**: Verify checksum calculations
3. **Event Handling**: Ensure proper async/await usage
4. **Resource Disposal**: Check using statements and cleanup

### Logging During Tests
Tests use `MockRadioLogger` which captures all log entries:
```csharp
var logger = new MockRadioLogger();
// ... run tests ...
foreach(var entry in logger.LogEntries)
{
    output.WriteLine(entry);
}
```

### Test Output
Use `ITestOutputHelper` for debugging:
```csharp
public class MyTests
{
    private readonly ITestOutputHelper _output;
    
    public MyTests(ITestOutputHelper output)
    {
        _output = output;
    }
    
    [Fact]
    public void MyTest()
    {
        _output.WriteLine("Debug information");
    }
}
```

## Continuous Integration

The test suite is designed to run in CI/CD environments:
- No external dependencies
- Mock implementations for hardware interfaces
- Deterministic test execution
- Cross-platform compatibility

### CI Commands
```yaml
# Example CI pipeline steps
- run: dotnet restore
- run: dotnet build --no-restore
- run: dotnet test --no-build --logger trx --collect:"XPlat Code Coverage"
```

## Contributing Tests

When adding new tests:
1. Follow existing naming conventions
2. Use FluentAssertions for readable assertions
3. Include both positive and negative test cases
4. Add appropriate test documentation
5. Ensure tests are isolated and repeatable
6. Use TestUtilities helpers when applicable

### Test Naming Convention
- `MethodName_Scenario_ExpectedResult`
- Example: `SendButtonPress_WhenConnected_ShouldSucceed`

### Test Structure
```csharp
[Fact]
public void Method_Scenario_ExpectedResult()
{
    // Arrange
    var setup = CreateTestSetup();
    
    // Act  
    var result = setup.ExecuteAction();
    
    // Assert
    result.Should().BeExpectedValue();
}
```