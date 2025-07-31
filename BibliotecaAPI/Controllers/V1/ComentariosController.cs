using AutoMapper;
using BibliotecaAPI.Data;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entities;
using BibliotecaAPI.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaAPI.Controllers.V1
{
    [ApiController]
    [Route("api/v1/libros/{libroId:int}/comentarios")]
    [Authorize]
    public class ComentariosController: ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;
        private readonly IServiciosUsuarios serviciosUsuarios;

        public ComentariosController(ApplicationDbContext context, IMapper mapper, IServiciosUsuarios serviciosUsuarios)
        {
            this.context = context;
            this.mapper = mapper;
            this.serviciosUsuarios = serviciosUsuarios;
        }

        [HttpGet(Name = "ObtenerComentariosV1")]
        [AllowAnonymous]
        [OutputCache]
        public async Task<ActionResult<List<ComentarioDTO>>> Get (int libroId)
        {
            var existeLibro = await context.Libros.AnyAsync(x => x.Id == libroId);

            if (!existeLibro)
                return NotFound();

            var comentarios = await context.Comentarios
                .Include(x => x.Usuario)
                .Where(x => x.LibroId == libroId)
                .OrderByDescending(x => x.FechaPublicacion)
                .ToListAsync();

            return mapper.Map<List<ComentarioDTO>>(comentarios);

        }

        [HttpGet("{id}", Name = "ObtenerComentarioV1")]
        [AllowAnonymous]
        [OutputCache]
        public async Task<ActionResult<ComentarioDTO>> Get(Guid id)
        {
            var comentario = await context.Comentarios
                .Include(x => x.Usuario)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (comentario is null)
                return NotFound();

            return mapper.Map<ComentarioDTO>(comentario);
        }

        [HttpPost(Name = "CrearComentarioV1")]
        public async Task<ActionResult> Post (int libroId, ComentarioCreacionDTO comentarioCreacionDTO)
        {
            var existeLibro = await context.Libros.AnyAsync(x => x.Id == libroId);

            if (!existeLibro)
                return NotFound();

            var usuario = await serviciosUsuarios.ObtenerUsuario();

            if(usuario is null)
                return NotFound();

            var comentario = mapper.Map<Comentario>(comentarioCreacionDTO);
            comentario.LibroId = libroId;
            comentario.FechaPublicacion = DateTime.UtcNow;
            comentario.UsuarioId = usuario.Id;
            context.Add(comentario);
            await context.SaveChangesAsync();

            var comentarioDTO = mapper.Map<ComentarioDTO>(comentario);
            return CreatedAtRoute("ObtenerComentarioV1", new { id = comentario.Id, libroId }, comentarioDTO);
        }

        [HttpPatch("{id}", Name = "PatchComentarioV1")]
        public async Task<ActionResult> Patch(Guid id, int libroId, JsonPatchDocument<ComentarioPatchDTO> patchDoc)
        {
            if (patchDoc is null)
                return BadRequest();

            var existeLibro = await context.Libros.AnyAsync(x => x.Id == libroId);

            if (!existeLibro)
                return NotFound();

            var usuario = await serviciosUsuarios.ObtenerUsuario();

            if (usuario is null)
                return NotFound();

            var comentarioDB = await context.Comentarios.FirstOrDefaultAsync(x => x.Id == id);

            if (comentarioDB is null)
                NotFound();

            if (comentarioDB.UsuarioId != usuario.Id)
                return Forbid();

            var comentarioPatchDTO = mapper.Map<ComentarioPatchDTO>(comentarioDB);

            patchDoc.ApplyTo(comentarioPatchDTO, ModelState);

            var isValid = TryValidateModel(comentarioPatchDTO);

            if (!isValid)
                return ValidationProblem();

            mapper.Map(comentarioPatchDTO, comentarioDB);

            await context.SaveChangesAsync();

            return NoContent();

        }

        [HttpDelete("{id}", Name = "BorrarComentarioV1")]
        public async Task<ActionResult> Delete (Guid id, int libroId)
        {
            var existeLibro = await context.Libros.AnyAsync(x => x.Id == libroId);

            if (!existeLibro)
                return NotFound();

            var usuario = await serviciosUsuarios.ObtenerUsuario();

            if (usuario is null)
                return NotFound();

            var comentarioDB = await context.Comentarios.FirstOrDefaultAsync(x => x.Id == id);

            if (comentarioDB is null)
                return NotFound();

            if(comentarioDB.UsuarioId != usuario.Id)
                return Forbid();

            comentarioDB.EstaBorrado = true;
            context.Update(comentarioDB);
            // context.Remove(comentarioDB);
            await context.SaveChangesAsync();
            //var registrosBorrados = await context.Comentarios.Where(X => X.Id == id).ExecuteDeleteAsync();

            //if (registrosBorrados == 0)
            //    return NotFound();

            return NoContent();

        }

    }
}
