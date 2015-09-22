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
            try
            {
                var result = LocationService.Connection.Service.ValidateAPIKey((string)value);
                return result
               ? ValidationResult.ValidResult
               : new ValidationResult(false, "");
            }
            catch (Exception)
            {
                LocationService.ResetChannel(this, EventArgs.Empty);
            }
            return new ValidationResult(false, "Connection error");
        }
    }
}
