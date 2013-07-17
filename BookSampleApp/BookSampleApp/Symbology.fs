module Symbology

type Instrument = {
    Name : string
    LastPrice : decimal
}

open System
open System.Net

let yahoo symbol =
    use wc = new WebClient()
    //http://ichart.finance.yahoo.com/table.csv?s=MSFT&d=6&e=13&f=2013&g=d&a=2&b=13&c=1986&ignore=.csv
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
