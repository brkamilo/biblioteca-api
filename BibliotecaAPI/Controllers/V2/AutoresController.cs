﻿using AutoMapper;
using Azure;
using BibliotecaAPI.Data;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entities;
using BibliotecaAPI.Servicios;
using BibliotecaAPI.Servicios.V1;
using BibliotecaAPI.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Linq.Dynamic.Core;

namespace BibliotecaAPI.Controllers.V2
{
    [ApiController]
    [Route("api/v2/autores")]
    [Authorize(Policy = "esadmin")]
    //[FiltroAgregarCabeceras("controlador", "autores")]
    public class AutoresController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;
        private readonly IAlmacenadorArchivos almacenadorArchivos;
        private readonly ILogger<AutoresController> logger;
        private readonly IOutputCacheStore outputCacheStore;
        private readonly IServicioAutores servicioAutoresV1;
        private const string contenedor = "autores";
        private const string cache = "autores-obtener";

        public AutoresController(ApplicationDbContext context, IMapper mapper,
            IAlmacenadorArchivos almacenadorArchivos, ILogger<AutoresController> logger,
            IOutputCacheStore outputCacheStore, IServicioAutores servicioAutoresV1)
        {
            this.context = context;
            this.mapper = mapper;
            this.almacenadorArchivos = almacenadorArchivos;
            this.logger = logger;
            this.outputCacheStore = outputCacheStore;
            this.servicioAutoresV1 = servicioAutoresV1;
        }

        [HttpGet]
        [AllowAnonymous]
        //[OutputCache(Tags = [cache])]
        //[ServiceFilter<MiFiltroDeAccion>()]
        //[FiltroAgregarCabeceras("accion", "obtener-autores")]
        public async Task<IEnumerable<AutorDTO>> Get([FromQuery] PaginacionDTO paginacionDTO)
        {
            return await servicioAutoresV1.Get(paginacionDTO);
        }

        //[HttpGet("{parametro1}/{parametro2?}")]
        //public IActionResult Get(string parametro1, string parametro2 = "Valor por defecto")
        //{
        //    return Ok(new { parametro1, parametro2 });
        //}

        //[HttpGet("{nombre:alpha}")]
        //public async Task<IEnumerable<Autor>> Get(string nombre)
        //{
        //    return await context.Autores.Where(x => x.Nombre.Contains(nombre)).ToListAsync();
        //}

        [HttpGet("primero")] // api/autores/primero
        public async Task<Autor> GetPrimerAutor()
        {
            return await context.Autores.FirstAsync();
        }

        [HttpGet("{id:int}", Name = "ObtenerAutorV2")] // api/autores/id?llave1=valor&llave2=valor2
        [AllowAnonymous]
        [EndpointSummary("Obtiene Autor por Id")]
        [EndpointDescription("Obtiene Autor por Id, incluye sus libros")]
        [ProducesResponseType<AutorConLibrosDTO>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [OutputCache(Tags = [cache])]
        public async Task<ActionResult<AutorConLibrosDTO>> Get([Description("Id autor")] int id,
            bool incluirLibros = false)
        {
            var queryable = context.Autores.AsQueryable();

            if (incluirLibros)
            {
                queryable = queryable.Include(x => x.Libros)
                .ThenInclude(x => x.Libro);
            }

            var autor = await queryable.FirstOrDefaultAsync(x => x.Id == id);

            if (autor is null)
                return NotFound();

            var autorDTO = mapper.Map<AutorConLibrosDTO>(autor);

            return autorDTO;
        }

        [HttpGet("filtrar")]
        [AllowAnonymous]
        public async Task<ActionResult> Filtrar([FromQuery] AutorFiltroDTO autorFiltroDTO)
        {
            var queryable = context.Autores.AsQueryable();

            if (!string.IsNullOrEmpty(autorFiltroDTO.Nombres))
                queryable = queryable.Where(x => x.Nombres.Contains(autorFiltroDTO.Nombres));

            if (!string.IsNullOrEmpty(autorFiltroDTO.Apellidos))
                queryable = queryable.Where(x => x.Apellidos.Contains(autorFiltroDTO.Apellidos));

            if (autorFiltroDTO.IncluirLibros)
                queryable = queryable.Include(x => x.Libros).ThenInclude(x => x.Libro);

            if (autorFiltroDTO.TieneFoto.HasValue)
            {
                if (autorFiltroDTO.TieneFoto.Value)
                    queryable = queryable.Where(x => x.Foto != null);
                else
                    queryable = queryable.Where(x => x.Foto == null);
            }

            if (autorFiltroDTO.TieneLibros.HasValue)
            {
                if (autorFiltroDTO.TieneLibros.Value)
                    queryable = queryable.Where(x => x.Libros.Any());
                else
                    queryable = queryable.Where(x => !x.Libros.Any());

            }

            if (!string.IsNullOrEmpty(autorFiltroDTO.TituloLibro))
                queryable = queryable
                    .Where(x => x.Libros.Any(y => y.Libro!.Titulo.Contains(autorFiltroDTO.TituloLibro)));

            if (!string.IsNullOrEmpty(autorFiltroDTO.CampoOrdenar))
            {
                var tipoOrden = autorFiltroDTO.OrdenAscendente ? "ascending" : "descending";

                try
                {
                    queryable = queryable.OrderBy($"{autorFiltroDTO.CampoOrdenar} {tipoOrden}");
                }
                catch (Exception e)
                {
                    queryable = queryable.OrderBy(x => x.Nombres);
                    logger.LogError(e.Message, e);
                }
            }
            else
            {
                queryable = queryable.OrderBy(x => x.Nombres);
            }

            var autores = await queryable
                        .Paginar(autorFiltroDTO.PaginacionDTO).ToListAsync();

            if (autorFiltroDTO.IncluirLibros)
            {
                var autoresDTO = mapper.Map<IEnumerable<AutorConLibrosDTO>>(autores);
                return Ok(autoresDTO);
            }
            else
            {
                var autoresDTO = mapper.Map<IEnumerable<AutorDTO>>(autores);
                return Ok(autoresDTO);
            }

        }


        [HttpPost]
        public async Task<ActionResult> Post(AutorCreacionDTO autorCreacionDTO)
        {
            var autor = mapper.Map<Autor>(autorCreacionDTO);
            context.Add(autor);
            await context.SaveChangesAsync();
            await outputCacheStore.EvictByTagAsync(cache, default);
            var autorDTO = mapper.Map<AutorDTO>(autor);
            return CreatedAtRoute("ObtenerAutorV2", new { id = autor.Id }, autorDTO);

        }


        [HttpPost("con-foto")]
        public async Task<ActionResult> PostConFoto([FromForm] AutorCreacionDTOFoto autorCreacionDTO)
        {
            var autor = mapper.Map<Autor>(autorCreacionDTO);

            if (autorCreacionDTO.Foto is not null)
            {
                var url = await almacenadorArchivos.Almacenar(contenedor, autorCreacionDTO.Foto);
                autor.Foto = url;
            }

            context.Add(autor);
            await context.SaveChangesAsync();
            var autorDTO = mapper.Map<AutorDTO>(autor);
            return CreatedAtRoute("ObtenerAutorV2", new { id = autor.Id }, autorDTO);

        }


        [HttpPut("{id:int}")]
        public async Task<ActionResult> Put(int id,
            [FromForm] AutorCreacionDTOFoto autorCreacionDTO)
        {
            var existeAutor = await context.Autores.AnyAsync(x => x.Id == id);

            if (!existeAutor)
                return NotFound();

            var autor = mapper.Map<Autor>(autorCreacionDTO);
            autor.Id = id;

            if (autorCreacionDTO.Foto is not null)
            {
                var fotoActual = await context
                    .Autores.Where(x => x.Id == id)
                    .Select(x => x.Foto).FirstAsync();

                var url = await almacenadorArchivos.Editar(fotoActual, contenedor,
                    autorCreacionDTO.Foto);
                autor.Foto = url;

            }

            context.Update(autor);
            await context.SaveChangesAsync();
            return NoContent();

        }

        [HttpPatch("{id:int}")]
        public async Task<ActionResult> Patch(int id, JsonPatchDocument<AutorPatchDTO> patchDoc)
        {
            if (patchDoc is null)
                return BadRequest();

            var autorDB = await context.Autores.FirstOrDefaultAsync(x => x.Id == id);

            if (autorDB is null)
                NotFound();

            var autorpatchDTO = mapper.Map<AutorPatchDTO>(autorDB);

            patchDoc.ApplyTo(autorpatchDTO, ModelState);

            var isValid = TryValidateModel(autorpatchDTO);

            if (!isValid)
                return ValidationProblem();

            mapper.Map(autorpatchDTO, autorDB);

            await context.SaveChangesAsync();

            return NoContent();

        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            //var registroBorrado = await context.Autores.Where(x => x.Id == id).ExecuteDeleteAsync();

            //if (registroBorrado == 0)
            //    return NotFound();

            var autor = await context.Autores.FirstOrDefaultAsync(x => x.Id == id);

            if (autor is null)
                return NotFound();

            context.Remove(autor);
            await context.SaveChangesAsync();
            await almacenadorArchivos.Borrar(autor.Foto, contenedor);

            return NoContent();

        }



    }
}
