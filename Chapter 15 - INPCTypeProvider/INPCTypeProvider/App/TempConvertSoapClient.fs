[<AutoOpen>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module FSharp.Windows.Sample

open Microsoft.FSharp.Data.TypeProviders

type TempConvert = WsdlService<"http://www.w3schools.com/webservices/tempconvert.asmx?WSDL">

type TempConvert.ServiceTypes.SimpleDataContextTypes.TempConvertSoapClient with

    member this.AsyncCelsiusToFahrenheit(value : float (*<celsius>*) ) = 
        async {
            let! response = value |> string |> this.CelsiusToFahrenheitAsync |> Async.AwaitTask 
            return response.Body.CelsiusToFahrenheitResult |> float //|> LanguagePrimitives.FloatWithMeasure<fahrenheit>
        }

    member this.AsyncFahrenheitToCelsius(value : float (*<fahrenheit>*) ) = 
        async {
            let! response = value |> string |> this.FahrenheitToCelsiusAsync |> Async.AwaitTask 
            return response.Body.FahrenheitToCelsiusResult |> float //|> LanguagePrimitives.FloatWithMeasure<celsius>
        }

