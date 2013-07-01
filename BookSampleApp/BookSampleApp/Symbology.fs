module Symbology

type Instrument = {
    Name : string
    LastPrice : decimal
}

open System
open System.Net

let yahoo symbol =
    use wc = new WebClient()
    let uri = sprintf "http://download.finance.yahoo.com/d/quotes.csv?s=%s&f=nl1" symbol 
    (wc.DownloadString uri)
        .Split([| Environment.NewLine |], StringSplitOptions.RemoveEmptyEntries) 
        |> Array.map (fun line -> 
            let xs = line.Split(',')
            if xs.[0] = sprintf "\"%s\"" symbol && xs.[1] = "0.00" 
            then None
            else Some { Name = xs.[0]; LastPrice = decimal xs.[1] }
        )
        |> Seq.exactlyOne
