using BibliotecaAPI.Entities;
using Microsoft.AspNetCore.Identity;

namespace BibliotecaAPI.Servicios
{
    public interface IServiciosUsuarios
    {
        Task<Usuario?> ObtenerUsuario();
    }
}