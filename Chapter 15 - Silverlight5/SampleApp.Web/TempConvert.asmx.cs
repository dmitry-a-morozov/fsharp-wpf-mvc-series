using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;

namespace SampleApp.Web
{
    /// <summary>
    /// Summary description for tempconvert
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class TempConvert : System.Web.Services.WebService
    {
        [WebMethod]
        public string CelsiusToFahrenheit(string celsius)
        {
            return ((double.Parse(celsius) * 9 / 5) + 32).ToString();
        }

        [WebMethod]
        public string FahrenheitToCelsius(string fahrenheit)
        {
            return ((double.Parse(fahrenheit) - 32) * 5 / 9).ToString();
        }
    }
}
