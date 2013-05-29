namespace FSharp.Windows.Sample.Models

//[<Measure>] type celsius
//[<Measure>] type fahrenheit

type TempConveterModel = {

    mutable Celsius : float //<celsius> 
    mutable Fahrenheit : float //<fahrenheit>
    mutable ResponseStatus : string
    mutable Delay : int 
}


