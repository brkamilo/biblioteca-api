namespace BibliotecaAPI.Entities
{
    public class Error
    {
        public Guid Id { get; set; }
        public required string MensajeError { get; set; }
        public string? StrackTrace { get;set; }
        public DateTime Fecha { get; set; }
    }
}
