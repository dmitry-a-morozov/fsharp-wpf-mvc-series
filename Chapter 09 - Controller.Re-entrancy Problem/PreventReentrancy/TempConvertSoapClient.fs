[<AutoOpen>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module FSharp.Windows.Sample.TempConvertSoapClient

open CSharpWindow.TempConverter

type TempConvertSoapClient with
    
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

