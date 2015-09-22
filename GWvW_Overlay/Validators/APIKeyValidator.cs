using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using GWvW_Overlay.Service;

namespace GWvW_Overlay.Validators
{
    public class APIKeyValidator : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            return LocationService.Connection.Service.ValidateAPIKey((string)value)
               ? ValidationResult.ValidResult
               : new ValidationResult(false, "");
        }
    }
}
