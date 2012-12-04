﻿namespace FSharpx.TypeProviders

open System
open FSharpx.JSON
open System.Collections.Generic
open FSharpx.TypeProviders.Inference

// ------------------------------------------------------------------------------------------------
// Infers the structure of JSON file from data
// ------------------------------------------------------------------------------------------------

module internal JSONInference = 
    let rec provideElement name multi (childs:seq<JsonValue>) = 
        CompoundProperty(name,multi,collectElements childs,collectProperties childs)

    and collectProperties (elements:seq<JsonValue>) =
        let props =
          [for el in elements do
            match el with
            | JsonValue.Obj map -> 
                for prop in map do
                    match prop.Value with
                    | JsonValue.String _ -> yield prop.Key, typeof<string>
                    | JsonValue.NumDecimal n -> 
                            let t =
                                if n = decimal (int n) then typeof<int> else
                                if n = decimal (int64 n) then typeof<int64> else
                                typeof<float>
                            yield prop.Key, t
                    | JsonValue.NumDouble n -> yield prop.Key, typeof<float>
                    | JsonValue.Bool _ -> yield prop.Key, typeof<bool>
                    //| JsonValue.Date _ -> yield prop.Key, typeof<DateTime>
                    | _ -> ()              
            | _ -> ()]
        props
        |> Seq.groupBy fst
        |> Seq.map (fun (name, attrs) -> 
            SimpleProperty(
              name,
              attrs 
                |> Seq.map snd
                |> Seq.head,
              Seq.length attrs < Seq.length elements))

    and collectElements (elements:seq<JsonValue>)  =
        [ for el in elements do
            match el with
            | JsonValue.Obj map -> 
                for prop in map do            
                    match prop.Value with
                    | JsonValue.Obj _ -> yield prop.Key, false, prop.Value
                    | JsonValue.Array a -> 
                        for child in a do
                            yield prop.Key, true, child
                    | _ -> ()              
            | _ -> ()]
        |> Seq.groupBy (fun (fst,_,_) -> fst)
        |> Seq.map (fun (name, values) -> 
                provideElement
                    name 
                    (values |> Seq.head |> (fun (_,snd,_) -> snd)) 
                    (Seq.map (fun (_,_,third) -> third) values))