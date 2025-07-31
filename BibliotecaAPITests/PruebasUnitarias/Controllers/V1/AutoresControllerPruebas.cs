using BibliotecaAPI.Controllers.V1;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entities;
using BibliotecaAPI.Servicios;
using BibliotecaAPI.Servicios.V1;
using BibliotecaAPITests.Utils;
using BibliotecaAPITests.Utils.Dobles;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Services;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace BibliotecaAPITests.PruebasUnitarias.Controllers.V1
{
    [TestClass]
    public class AutoresControllerPruebas : BasePruebas
    {
        IAlmacenadorArchivos almacenadorArchivos = null!;
        ILogger<AutoresController> logger = null!;
        IOutputCacheStore outputCacheStore = new OutputCacheStoreFalse();
        IServicioAutores servicioAutores = null!;
        private string nombreBD = Guid.NewGuid().ToString();
        private AutoresController controller = null!;

        [TestInitialize]
        public void Setup()
        {
            var context = ConstruirContext(nombreBD);
            var mapper = ConfigurarAutoMapper();
            almacenadorArchivos = Substitute.For<IAlmacenadorArchivos>();
            logger = Substitute.For<ILogger<AutoresController>>();
            outputCacheStore = Substitute.For<IOutputCacheStore>();
            servicioAutores = Substitute.For<IServicioAutores>();


            controller = new AutoresController(context, mapper, almacenadorArchivos,
                logger, outputCacheStore, servicioAutores);
        }
        [TestMethod]
        public async Task Get_Retornar404_CuandoAutorNoExiste()
        {
            // Arragement


            // Act
            var respuesta = await controller.Get(1);

            // Assert
            var resultado = respuesta.Result as StatusCodeResult;
            Assert.AreEqual(expected: 404, actual: resultado!.StatusCode);

        }

        [TestMethod]
        public async Task Get_RetornarAutor_CuandoAutorExiste()
        {
            // Arragement          
            var context = ConstruirContext(nombreBD);
            context.Autores.Add(new Autor { Nombres = "Camilo", Apellidos = "Penagos" });
            context.Autores.Add(new Autor { Nombres = "Angela", Apellidos = "Sanchez" });

            await context.SaveChangesAsync();

            // Act
            var respuesta = await controller.Get(1);

            // Assert
            var resultado = respuesta.Value;
            Assert.AreEqual(expected: 1, actual: resultado!.Id);

        }

        [TestMethod]
        public async Task Get_RetornarAutorConLibros_CuandoAutorTieneLibros()
        {
            // Arragement          
            var context = ConstruirContext(nombreBD);
            var libro1 = new Libro { Titulo = "Libro 1" };
            var libro2 = new Libro { Titulo = "Libro 2" };

            var autor = new Autor()
            {
                Nombres = "Braian",
                Apellidos = "Penagos",
                Libros = new List<AutorLibro>
                {
                    new AutorLibro{Libro= libro1},
                    new AutorLibro{Libro= libro2}
                }
            };

            context.Add(autor);

            await context.SaveChangesAsync();

            var context2 = ConstruirContext(nombreBD);

            // Act
            var respuesta = await controller.Get(1);

            // Assert
            var resultado = respuesta.Value;
            Assert.AreEqual(expected: 1, actual: resultado!.Id);
            Assert.AreEqual(expected: 2, actual: resultado.Libros.Count);

        }

        [TestMethod]
        public async Task Get_ServicioAutores()
        {
            // Arrangement
            var paginacionDTO = new PaginacionDTO(2, 3);

            // Act
            await controller.Get(paginacionDTO);

            // Assert
            await servicioAutores.Received(1).Get(paginacionDTO);

        }

        [TestMethod]
        public async Task Post_DebeCrearAutor()
        {
            // Arrangement           
            var context = ConstruirContext(nombreBD);
            var newAutor = new AutorCreacionDTO { Nombres = "nuevo", Apellidos = "autor" };

            // Act
            var respuesta = await controller.Post(newAutor);

            // Assert
            var resultado = respuesta as CreatedAtRouteResult;
            Assert.IsNotNull(resultado);

            var context2 = ConstruirContext(nombreBD);
            var cantidad = await context2.Autores.CountAsync();
            Assert.AreEqual(expected: 1, actual: cantidad);

        }

        [TestMethod]
        public async Task Put_Retornar404_CuandoAutorNoExiste()
        {
            // Act
            var respuesta = await controller.Put(1, autorCreacionDTO: null!);

            // Assert
            var resultado = respuesta as StatusCodeResult;
            Assert.AreEqual(404, resultado!.StatusCode);
        }

        private const string contenedor = "autores";
        private const string cache = "autores-obtener";

        [TestMethod]
        public async Task Put_ActualizarAutor_CuandoAutorSinFoto()
        {
            //Arrangement
            var context = ConstruirContext(nombreBD);
            context.Autores.Add(new Autor
            {
                Nombres = "Braian",
                Apellidos = "Moncada",
                Identificacion = "Id"
            });

            await context.SaveChangesAsync();

            var autorCreacionDTO = new AutorCreacionDTOFoto
            {
                Nombres = "Braian2",
                Apellidos = "Moncada2",
                Identificacion = "Id2"
            };

            // Act
            var respuesta = await controller.Put(1, autorCreacionDTO);

            // Assert
            var resultado = respuesta as StatusCodeResult;
            Assert.AreEqual(204, resultado!.StatusCode);

            var context3 = ConstruirContext(nombreBD);
            var autorActualizado = await context3.Autores.SingleAsync();

            Assert.AreEqual(expected: "Braian2", actual: autorActualizado.Nombres);
            Assert.AreEqual(expected: "Moncada2", actual: autorActualizado.Apellidos);
            Assert.AreEqual(expected: "Id2", actual: autorActualizado.Identificacion);
            await outputCacheStore.Received(1).EvictByTagAsync(cache, default);
            await almacenadorArchivos.DidNotReceiveWithAnyArgs().Editar(default, default!, default!);

        }

        [TestMethod]
        public async Task Put_ActualizarAutor_CuandoAutorConFoto()
        {
            //Arrangement
            var context = ConstruirContext(nombreBD);

            var urlOld = "Url-1";
            var urlNew = "Url-2";
            almacenadorArchivos.Editar(default, default!, default!).ReturnsForAnyArgs(urlNew);

            context.Autores.Add(new Autor
            {
                Nombres = "Braian",
                Apellidos = "Moncada",
                Identificacion = "Id",
                Foto = urlOld
            });

            await context.SaveChangesAsync();

            var formFile = Substitute.For<IFormFile>();

            var autorCreacionDTO = new AutorCreacionDTOFoto
            {
                Nombres = "Braian2",
                Apellidos = "Moncada2",
                Identificacion = "Id2",
                Foto = formFile
            };

            // Act
            var respuesta = await controller.Put(1, autorCreacionDTO);

            // Assert
            var resultado = respuesta as StatusCodeResult;
            Assert.AreEqual(204, resultado!.StatusCode);

            var context3 = ConstruirContext(nombreBD);
            var autorActualizado = await context3.Autores.SingleAsync();

            Assert.AreEqual(expected: "Braian2", actual: autorActualizado.Nombres);
            Assert.AreEqual(expected: "Moncada2", actual: autorActualizado.Apellidos);
            Assert.AreEqual(expected: "Id2", actual: autorActualizado.Identificacion);
            Assert.AreEqual(expected: urlNew, actual: autorActualizado.Foto);
            await outputCacheStore.Received(1).EvictByTagAsync(cache, default);
            await almacenadorArchivos.DidNotReceiveWithAnyArgs().Editar(urlOld, contenedor, formFile);

        }

        [TestMethod]
        public async Task Patch_Retorna400_CuandoEsNulo()
        {
            // Act
            var respuesta = await controller.Patch(1, patchDoc: null!);

            // Assert 
            var resultado = respuesta as StatusCodeResult;
            Assert.AreEqual(400, resultado!.StatusCode);

        }

        [TestMethod]
        public async Task Patch_Retorna404_CuandoAutorNoExiste()
        {
            // Arrangement
            var patchDoc = new JsonPatchDocument<AutorPatchDTO>();

            // Act
            var respuesta = await controller.Patch(1, patchDoc: null!);

            // Assert 
            var resultado = respuesta as StatusCodeResult;
            Assert.AreEqual(404, resultado!.StatusCode);

        }

        [TestMethod]
        public async Task Patch_RetornaValidationProblem()
        {
            // Arrangement
            var patchDoc = new JsonPatchDocument<AutorPatchDTO>();

            // Act
            var respuesta = await controller.Patch(1, patchDoc: null!);

            // Assert 
            var resultado = respuesta as StatusCodeResult;
            Assert.AreEqual(404, resultado!.StatusCode);

        }

        [TestMethod]
        public async Task Delete_Retorna404_CuandoAutorNoExiste()
        {
            // act
            var respuesta = await controller.Delete(1);

            // assert
            var resultado = respuesta as StatusCodeResult;
            Assert.AreEqual(404, resultado!.StatusCode);
        }

        [TestMethod]
        public async Task Delete_BorraAutor_CuandoAutorNxiste()
        {
            // Arrangement
            var urlFoto = "Url-1";

            var context = ConstruirContext(nombreBD);

            context.Autores.Add(new Autor { Nombres = "Autor1", Apellidos = "Autor1", Foto = urlFoto });
            context.Autores.Add(new Autor { Nombres = "Autor2", Apellidos = "Autor2" });

            await context.SaveChangesAsync();

            // act
            var respuesta = await controller.Delete(1);

            // assert
            var resultado = respuesta as StatusCodeResult;
            Assert.AreEqual(204, resultado!.StatusCode);

            var context2 = ConstruirContext(nombreBD);
            var cantAutores = await context2.Autores.CountAsync();
            Assert.AreEqual(expected: 1, actual: cantAutores);

            var autorExiste = await context2.Autores.AnyAsync(x => x.Nombres == "Autor2");
            Assert.IsTrue(autorExiste);

            
        }

    }
}
