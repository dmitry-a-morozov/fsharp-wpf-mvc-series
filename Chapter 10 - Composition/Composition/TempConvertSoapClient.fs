[<AutoOpen>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Mvc.Wpf.Sample.TempConvertSoapClient

type Mvc.Wpf.UIElements.Sample.TempConvertSoapClient with
    
    member this.AsyncCelsiusToFahrenheit(celsius : float) = 
        async {
            let! result = Async.FromBeginEnd(string celsius, this.BeginCelsiusToFahrenheit, this.EndCelsiusToFahrenheit) 
            return float result
        }

    member this.AsyncFahrenheitToCelsius(fahrenheit : float) = 
        async {
            let! result = Async.FromBeginEnd(string fahrenheit, this.BeginFahrenheitToCelsius, this.EndFahrenheitToCelsius) 
            return float result
        }

