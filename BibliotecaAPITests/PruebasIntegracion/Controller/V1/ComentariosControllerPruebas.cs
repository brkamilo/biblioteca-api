using BibliotecaAPI.Entities;
using BibliotecaAPITests.Utils;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Headers;

namespace BibliotecaAPITests.PruebasIntegracion.Controller.V1
{
    [TestClass]
    public class ComentariosControllerPruebas: BasePruebas
    {
        private readonly string url = "/api/v1/libros/1/comentarios";
        private string nombreBD = Guid.NewGuid().ToString();
        private async Task CrearDataPrueba()
        {
            var context = ConstruirContext(nombreBD);
            var autor = new Autor { Nombres = "Braian", Apellidos = "Penagos" };
            context.Add(autor);
            await context.SaveChangesAsync();

            var libro = new Libro { Titulo = "titulo" };
            libro.Autores.Add(new AutorLibro { Autor = autor });
            context.Add(libro);
            await context.SaveChangesAsync();

         }

        [TestMethod]
        public async Task Delete_Devuelve404_CuandoUsuarioBorraPropioComentario()
        {
            // Arrangement
            await CrearDataPrueba();

            var factory = ConstruirWebApplicationFactory(nombreBD, ignorarSeguridad: false);

            var token = await CrearUsuario(nombreBD, factory);

            var context = ConstruirContext(nombreBD);
            var usuario = await context.Users.FirstAsync();

            var comentario = new Comentario
            {
                Cuerpo = "contenido",
                UsuarioId = usuario!.Id,
                LibroId = 1
            };

            context.Add(comentario);
            await context.SaveChangesAsync();

            var cliente = factory.CreateClient();
            cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            var respuesta = await cliente.DeleteAsync($"{url}/{comentario.Id}");

            // Assert
            Assert.AreEqual(expected: HttpStatusCode.NoContent, actual: respuesta.StatusCode);
        }

        [TestMethod]
        public async Task Delete_Devuelve403_CuandoUsuarioBorraOtroComentario()
        {
            // Arrangement
            await CrearDataPrueba();

            var factory = ConstruirWebApplicationFactory(nombreBD, ignorarSeguridad: false);
            var emailCreadorComentario = "creado-comentario@hotmail.com";
            await CrearUsuario(nombreBD, factory, [], emailCreadorComentario);

            var context = ConstruirContext(nombreBD);
            var usuarioCreador = await context.Users.FirstAsync();

            var comentario = new Comentario
            {
                Cuerpo = "contenido",
                UsuarioId = usuarioCreador!.Id,
                LibroId = 1
            };

            context.Add(comentario);
            await context.SaveChangesAsync();

            var tokenUsuarioDistinto = await CrearUsuario(nombreBD, factory, [], "usuario-distinto@hotmail.com");

            var cliente = factory.CreateClient();
            cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenUsuarioDistinto);

            // Act
            var respuesta = await cliente.DeleteAsync($"{url}/{comentario.Id}");

            // Assert
            Assert.AreEqual(expected: HttpStatusCode.Forbidden, actual: respuesta.StatusCode);
        }
    }
}
