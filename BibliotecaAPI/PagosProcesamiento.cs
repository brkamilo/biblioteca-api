using Microsoft.Extensions.Options;

namespace BibliotecaAPI
{
    public class PagosProcesamiento
    {
        private TarifasOpciones _tarifasOpciones;

        public PagosProcesamiento(IOptionsMonitor<TarifasOpciones> optionsMonitor)
        {
            _tarifasOpciones = optionsMonitor.CurrentValue;

            optionsMonitor.OnChange(nuevaTarifa =>
            {
                Console.WriteLine("Tarifa actualizada");
                _tarifasOpciones = nuevaTarifa;
            });
        }

        public void ProcesarPago()
        {

        }

        public TarifasOpciones ObtenerTarfias()
        {
            return _tarifasOpciones;
        }

    }
}
