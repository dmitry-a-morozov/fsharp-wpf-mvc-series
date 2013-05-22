namespace CustomRuntimeClass.INPCTypeProvider

open System
open System.Reflection
open System.IO

open Microsoft.FSharp.Core.CompilerServices
open Microsoft.FSharp.Reflection
open Microsoft.FSharp.Quotations

open Samples.FSharp.ProvidedTypes

[<TypeProvider>]
type public NotifyPropertyChangedTypeProvider(config : TypeProviderConfig) as this = 
    inherit TypeProviderForNamespaces()

    let nameSpace = this.GetType().Namespace
    let assembly = Assembly.GetExecutingAssembly()
    let providerType = ProvidedTypeDefinition(assembly, nameSpace, "NotifyPropertyChanged", Some typeof<obj>)

    do 
        let parameters = [
            ProvidedStaticParameter("prototypesAssembly", typeof<string>)
        ]

        providerType.DefineStaticParameters(parameters, this.GenerateTypes)
        this.AddNamespace(nameSpace, [ providerType ])

    member internal this.GenerateTypes typeName parameters = 
        let outerType = ProvidedTypeDefinition(assembly, nameSpace, typeName, Some typeof<obj>)

        let fileName = string parameters.[0]
        let assemblyFileName = config.ReferencedAssemblies |> Array.find (fun fullPath -> Path.GetFileNameWithoutExtension fullPath = fileName)
        
        outerType.AddMembersDelayed <| fun() ->
            
            let prototypesAssembly = Assembly.LoadFrom assemblyFileName

            query {
                for prototype in prototypesAssembly.GetTypes() do
                where(FSharpType.IsRecord prototype)
                select(this.MapRecordToModelClass prototype)
            } 
            |> Seq.toList

        outerType

    member internal this.MapRecordToModelClass prototype = 

        let result = ProvidedTypeDefinition(prototype.Name, Some typeof<Model>)

        let prototypeName = prototype.AssemblyQualifiedName
        ProvidedConstructor([], InvokeCode = fun _ -> <@@ Model(prototype = Type.GetType prototypeName)  @@>) |> result.AddMember 

        for field in FSharpType.GetRecordFields prototype do
            let name = field.Name
            let property = ProvidedProperty(field.Name, field.PropertyType)
            property.GetterCode <- fun args -> <@@ (%%args.[0] : Model).[name] @@>
            if field.CanWrite 
            then 
                property.SetterCode <- fun args -> <@@ (%%args.[0] : Model).[name] <- %%(Expr.Coerce(args.[1], typeof<obj>)) @@>
            result.AddMember property

        result

[<assembly:TypeProviderAssembly>] 
do()