using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entities;
using BibliotecaAPITests.Utils;
using System.Net;
using System.Security.Claims;
using System.Text.Json;

namespace BibliotecaAPITests.PruebasIntegracion.Controller.V1
{
    [TestClass]
    public class AutoresControllerPruebas: BasePruebas
    {
        private static readonly string url = "/api/v1/autores";
        private string nombreBD = Guid.NewGuid().ToString();

        [TestMethod]
        public async Task Get_Devuelve404_CuandoAutorNoExiste()
        {

            // Arrangement
            var factory = ConstruirWebApplicationFactory(nombreBD);

            var cliente = factory.CreateClient();

            // Act
            var respuesta = await cliente.GetAsync($"{url}/1");

            // Assert
            var statusCode = respuesta.StatusCode;
            Assert.AreEqual(expected: HttpStatusCode.NotFound, actual: statusCode);

        }

        [TestMethod]
        public async Task Get_DevuelveAutor_CuandoAutorExiste()
        {
            // Arrangement
            var context = ConstruirContext(nombreBD);
            context.Autores.Add(new Autor { Nombres = "Camilo", Apellidos = "Penagos" });
            context.Autores.Add(new Autor { Nombres = "Angela", Apellidos = "Sanchez" });
            await context.SaveChangesAsync();

            var factory = ConstruirWebApplicationFactory(nombreBD);
            var cliente = factory.CreateClient();

            // Act
            var respuesta = await cliente.GetAsync($"{url}/1");

            // Assert
            respuesta.EnsureSuccessStatusCode();

            var autor = JsonSerializer.Deserialize<AutorConLibrosDTO>(
                await respuesta.Content.ReadAsStringAsync(), jsonSerializerOptions)!;

            Assert.AreEqual(expected: 1, autor.Id);

        }

        [TestMethod]
        public async Task Post_Devuelde401_CUandoUsuarioNoAutenticado()
        {
            // Arrangement
            var factory = ConstruirWebApplicationFactory(nombreBD, ignorarSeguridad: false);

            var cliente = factory.CreateClient();
            var autorCreacionDTO = new AutorCreacionDTO
            {
                Nombres = "Braian",
                Apellidos = "Penagos",
                Identificacion = "123"
            };

            // Act
            var respuesta = await cliente.PostAsJsonAsync(url, autorCreacionDTO);

            // Assert
            Assert.AreEqual(expected: HttpStatusCode.Unauthorized, actual: respuesta.StatusCode);
        }

        [TestMethod]
        public async Task Post_Devuelde403_CuandoUsuarioNoAdmin()
        {
            // Arrangement
            var factory = ConstruirWebApplicationFactory(nombreBD, ignorarSeguridad: false);
            var token = await CrearUsuario(nombreBD, factory);

            var cliente = factory.CreateClient();

            cliente.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var autorCreacionDTO = new AutorCreacionDTO
            {
                Nombres = "Braian",
                Apellidos = "Penagos",
                Identificacion = "123"
            };

            // Act
            var respuesta = await cliente.PostAsJsonAsync(url, autorCreacionDTO);

            // Assert
            Assert.AreEqual(expected: HttpStatusCode.Forbidden, actual: respuesta.StatusCode);
        }

        [TestMethod]
        public async Task Post_Devuelde403_CuandoUsuarioAdmin()
        {
            // Arrangement
            var factory = ConstruirWebApplicationFactory(nombreBD, ignorarSeguridad: false);

            var claims = new List<Claim> { adminClaim };

            var token = await CrearUsuario(nombreBD, factory, claims);

            var cliente = factory.CreateClient();

            cliente.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var autorCreacionDTO = new AutorCreacionDTO
            {
                Nombres = "Braian",
                Apellidos = "Penagos",
                Identificacion = "123"
            };

            // Act
            var respuesta = await cliente.PostAsJsonAsync(url, autorCreacionDTO);

            // Assert

            respuesta.EnsureSuccessStatusCode();
            Assert.AreEqual(expected: HttpStatusCode.Created, actual: respuesta.StatusCode);
        }

    }
}
