using Lugares.Infrastructure.Data;
using Lugares.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lugares.UnitTests;

public class ControlCuotaServiceTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task PuedeRealizarLlamadaAsync_BajoLimite_RetornaTrue()
    {
        using var context = CreateInMemoryDbContext();
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c.GetSection(It.IsAny<string>())).Returns(new Mock<IConfigurationSection>().Object);
        var loggerMock = new Mock<ILogger<ControlCuotaService>>();

        var service = new ControlCuotaService(context, configMock.Object, loggerMock.Object);

        var puede = await service.PuedeRealizarLlamadaAsync();

        Assert.True(puede);
    }

    [Fact]
    public async Task RegistrarLlamadaAsync_IncrementaContador()
    {
        using var context = CreateInMemoryDbContext();
        var configMock = new Mock<IConfiguration>();
        var loggerMock = new Mock<ILogger<ControlCuotaService>>();

        var service = new ControlCuotaService(context, configMock.Object, loggerMock.Object);

        await service.RegistrarLlamadaAsync();
        var estado = await service.ObtenerEstadoHoyAsync();

        Assert.Equal(1, estado.LlamadasRealizadas);
        Assert.False(estado.CircuitoAbierto);
    }

    [Fact]
    public async Task PuedeRealizarLlamadaAsync_AlAlcanzarLimite_AbreCircuitoYRetornaFalse()
    {
        using var context = CreateInMemoryDbContext();
        var configMock = new Mock<IConfiguration>();
        // Límite de prueba: 2 llamadas
        configMock.Setup(c => c["GooglePlaces:LimiteDiario"]).Returns("2");
        var loggerMock = new Mock<ILogger<ControlCuotaService>>();

        var service = new ControlCuotaService(context, configMock.Object, loggerMock.Object);

        // Simulamos 2 llamadas
        await service.RegistrarLlamadaAsync();
        await service.RegistrarLlamadaAsync();

        var estado = await service.ObtenerEstadoHoyAsync();
        var puedeTercera = await service.PuedeRealizarLlamadaAsync();

        Assert.True(estado.CircuitoAbierto);
        Assert.False(puedeTercera);
    }
}
