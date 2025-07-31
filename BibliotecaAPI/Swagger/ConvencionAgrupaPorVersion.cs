using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace BibliotecaAPI.Swagger
{
    public class ConvencionAgrupaPorVersion : IControllerModelConvention
    {
        public void Apply(ControllerModel controller)
        {
            //Ejemplo : Controllers.V1"
            var namespaceController = controller.ControllerType.Namespace;
            var version = namespaceController!.Split(".").Last().ToLower();
            controller.ApiExplorer.GroupName = version;
           
        }
    }
}
