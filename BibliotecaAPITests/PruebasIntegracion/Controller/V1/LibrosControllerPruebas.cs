using BibliotecaAPI.DTOs;
using BibliotecaAPITests.Utils;
using System.Net;

namespace BibliotecaAPITests.PruebasIntegracion.Controller.V1
{
    [TestClass]
    public class LibrosControllerPruebas: BasePruebas
    {
        private readonly string url = "/api/v1/libros";
        private string nombreBD = Guid.NewGuid().ToString();


        [TestMethod]
        public async Task Post_Devuelve400_CuandoAutoresIdsVacio()
        {
            // Arrangement
            var factory = ConstruirWebApplicationFactory(nombreBD);
            var cliente = factory.CreateClient();
            var libroCracionDTO = new LibroCreacionDTO { Titulo = "Titulo" };
            // Act
            var respuesta = await cliente.PostAsJsonAsync(url, libroCracionDTO);

            // Assert
            Assert.AreEqual(expected: HttpStatusCode.BadRequest, actual: respuesta.StatusCode);
        }
    }
}
