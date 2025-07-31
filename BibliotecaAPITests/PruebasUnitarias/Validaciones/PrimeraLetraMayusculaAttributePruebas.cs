using BibliotecaAPI.Validaciones;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BibliotecaAPITests.PruebasUnitarias.Validaciones
{
    [TestClass]
    public  class PrimeraLetraMayusculaAttributePruebas
    {
        [TestMethod]
        [DataRow("")]
        [DataRow(null)]
        [DataRow("Felipe")]
        public void IsValid_RetornarExitoso_SiValueNoTienePrimeraLetraMinuscula(string value)
        {
            // Arragement
            var primetaletra = new PrimeraLetraMayusculaAttribute();
            var validationContext = new ValidationContext(new object());
            //var value = string.Empty;

            // Act
            var resultado = primetaletra.GetValidationResult(value, validationContext);

            // Assert
            Assert.AreEqual(expected: ValidationResult.Success, actual: resultado);
        }

        [TestMethod]
        [DataRow("felipe")]
        public void IsValid_RetornarError_SiValueTienePrimeraLetraMinuscula(string value)
        {
            // Arragement
            var primetaletra = new PrimeraLetraMayusculaAttribute();
            var validationContext = new ValidationContext(new object());

            // Act
            var resultado = primetaletra.GetValidationResult(value, validationContext);

            // Assert
            Assert.AreEqual(expected: "La primera letra debe ser mayuscula", actual: resultado!.ErrorMessage);
        }

    }
}
