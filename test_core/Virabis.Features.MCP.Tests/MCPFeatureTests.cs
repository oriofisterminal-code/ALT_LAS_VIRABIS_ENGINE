using Virabis.Core.Nexus;
using Virabis.Features.MCP;
using Xunit;

namespace Virabis.Features.MCP.Tests;

public class MCPFeatureTests : FeatureTestBase<MCPFeature>
{
    [Fact]
    public void Constructor_CreatesFeature_WithCorrectName()
    {
        Assert.Equal("MCP", Feature.Name);
    }

    [Fact]
    public void IsHealthy_WithoutServer_ReturnsFalse()
    {
        // Before initialization, should be unhealthy
        Assert.False(Feature.IsHealthy());
    }

    [Fact]
    public void GetHealthStatus_WithoutServer_ReturnsNotFoundMessage()
    {
        var status = Feature.GetHealthStatus();
        
        Assert.Contains("Initialized=false", status);
        Assert.Contains("MCP", status);
    }

    [Fact]
    public void GetServerPort_WithoutServer_ReturnsNegativeOne()
    {
        var port = Feature.GetServerPort();
        
        Assert.Equal(-1, port);
    }

    [Fact]
    public void Update_DoesNotThrow()
    {
        Feature.Initialize();
        var exception = Record.Exception(() => Feature.Update(0.016f));
        
        Assert.Null(exception);
    }

    [Fact]
    public void Initialize_PrintsExpectedMessages()
    {
        var originalOut = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);

        try
        {
            Feature.Initialize();
            var output = writer.ToString();
            
            Assert.Contains("[MCP] Initialized", output);
            Assert.Contains("mcp_interaction_server.gd", output);
            Assert.Contains("9090", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void Shutdown_PrintsExpectedMessage()
    {
        Feature.Initialize();
        var originalOut = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);

        try
        {
            Feature.Shutdown();
            var output = writer.ToString();
            
            Assert.Contains("[MCP] Shutdown", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void MultipleUpdates_DoNotThrow()
    {
        Feature.Initialize();
        
        for (int i = 0; i < 10; i++)
        {
            var exception = Record.Exception(() => Feature.Update(0.016f));
            Assert.Null(exception);
        }
    }

    [Fact]
    public void GetHealthStatus_ContainsMCPPrefix()
    {
        Feature.Initialize();
        var status = Feature.GetHealthStatus();
        
        Assert.StartsWith("MCP:", status);
    }
}
