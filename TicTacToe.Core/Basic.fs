namespace TicTacToe.Core

[<AutoOpen>]
module Matrix =
    type Matcher private () =
        static member CompareArraysExact(first:byte[,]) (second:byte[,]):bool =
            let firstSeq = first |> Seq.cast<byte>
            let secondSeq = second |> Seq.cast<byte>
            Seq.fold2 (fun acc el1 el2 -> acc && el1=el2) true firstSeq secondSeq

        static member CompareArrays (ignoredValues:byte array) (first:byte[,]) (second:byte[,]):bool =
            let firstSeq = first |> Seq.cast<byte>
            let secondSeq = second |> Seq.cast<byte>
            Seq.fold2 (fun acc el1 el2 -> acc && (Array.contains el1 ignoredValues || el1=el2)) true firstSeq secondSeq

        static member CompareArraysChangeValue (changePatternElement:byte -> byte) (first:byte[,]) (second:byte[,]):bool =
            let firstSeq = first |> Seq.cast<byte>
            let secondSeq = second |> Seq.cast<byte>
            Seq.fold2 (fun acc el1 el2 -> acc && changePatternElement el1=el2) true firstSeq secondSeq


        static member Match() =
            true

module Basic =
    [<Struct>]
    type Position = {X:int; Y:int} with
        member this.Coordinates = 
            struct (this.X, this.Y)

    type FieldType =
        | Empty = 0uy
        | PlayerOne = 1uy
        | PlayerTwo = 2uy

    type Player =
        | PlayerOne
        | PlayerTwo

        member x.ToFieldType() =
            match x with
            | PlayerOne -> FieldType.PlayerOne
            | PlayerTwo -> FieldType.PlayerTwo
            

    type PlayGrid(grid:FieldType[,]) =
        member this.Grid with get() = grid
        member this.Width with get() = grid.GetLength 1
        member this.Height with get() = grid.GetLength 0
       
        new(width: int, height: int) =
            new PlayGrid(Array2D.init height width (fun _ _ -> FieldType.Empty))

        member this.IsInRange (position:Position) =
            let struct (x, y) = position.Coordinates;
            x >= 0 && x < this.Width && y >= 0 && y <= this.Height

        member this.Field (position:Position) =
            this.Grid.[position.Y, position.X]

        member this.NeigboursOfField (position:Position) =
            let struct (x, y) = position.Coordinates
            [
                {position with X = x-1}
                {position with X = x+1}
                {position with Y = y+1}
                {position with Y = y-1}
                {X = x-1; Y= y-1}
                {X = x-1; Y= y+1}
                {X = x+1; Y= y-1}
                {X = x+1; Y= y+1}
            ]
            |> List.filter(this.IsInRange)

        member this.IsEmpty (position:Position) =
            match this.Field(position) with
            | FieldType.Empty -> true
            | FieldType.PlayerOne | FieldType.PlayerTwo -> false
            | _ -> raise(System.ArgumentException("Unknown value at position.", "position"))

        member this.Play (player:Player, position:Position) =
            match this.IsInRange position with
            | true ->  
                match this.IsEmpty(position) with
                | true -> this.Grid.[position.Y, position.X] <- player.ToFieldType(); this
                | _ -> raise(System.ArgumentException("Field at this position is already set.", "position"))
            | false -> raise(System.ArgumentOutOfRangeException("position", "Out of grid."))

        member this.AsByteArray():byte[,] = 
            Array2D.init this.Height this.Width (fun y x -> byte this.Grid.[y, x])


    type TurnResult =
        | Winner of Player
        | Draw
        | NextTurn

    
    type Game(width:int, heigth:int) =
        let _playGrid:PlayGrid = new PlayGrid(width, heigth)
        let mutable _turns = 0
        let mutable _currentPlayer = PlayerOne
        let mutable _possibleMoves = Set.ofSeq [ for yIndex in 0..heigth do for xIndex in 0..width -> {X = xIndex; Y = yIndex}]

        let NextPlayer() =
            match _currentPlayer with
            | PlayerOne -> PlayerTwo
            | PlayerTwo -> PlayerOne

        ////TODO: move elsewhere
        //let CompareArrays(first:byte[,]) (second:byte[,]):bool =
        //    let firstSeq = first |> Seq.cast<byte>
        //    let secondSeq = second |> Seq.cast<byte>
        //    Seq.fold2 (fun acc el1 el2 -> acc && (not (el1 <> el2))) true firstSeq secondSeq

        let GameResult() =
            NextTurn

        member this.Grid with get() = _playGrid
        member this.Turns with get() = _turns
        member this.CurrentPlayer with get() = _currentPlayer
        member this.PossibleMoves with get() = _possibleMoves

        member this.CanPlayMove (position:Position) =
            _possibleMoves.Contains position

        member this.PlayMove (position:Position) =
            _playGrid.Play(_currentPlayer, position) |> ignore
            let newMoves = Set.ofList(_playGrid.NeigboursOfField(position) |> List.filter _playGrid.IsEmpty)
            _possibleMoves <- 
                match _turns with
                | 0 -> newMoves
                | _ -> Set.union (_possibleMoves.Remove(position)) newMoves
            _turns <- _turns + 1
            _currentPlayer <- NextPlayer()
            //TODO: return Winner, Draw, NextTurn



        //TODO: move elsewhere
        member private this.FlipPlayer (value:byte) =
            match value with
            | 1uy -> 2uy
            | 2uy -> 1uy
            | other -> other

        member private this.MatchOnPosition(pattern:byte[,]) (fromPosition:Position) (compare:(byte[,] -> byte[,]->bool)) = 
            let patternWidth = pattern.GetLength 1
            let patternHeight = pattern.GetLength 0
            let struct(X, Y) = fromPosition.Coordinates
            let slice = this.Grid.AsByteArray().[Y..Y + patternHeight-1, X..X + patternWidth-1]
            compare pattern slice

        member this.MatchExact(pattern:byte[,]) (fromPosition:Position) =
            this.MatchOnPosition pattern fromPosition Matcher.CompareArraysExact

        member this.Match(pattern:byte[,]) (fromPosition:Position) =
            this.MatchOnPosition pattern fromPosition (Matcher.CompareArrays [|0uy|])

        member this.MatchFlipPlayer(pattern:byte[,]) (fromPosition:Position) =
            this.MatchOnPosition pattern fromPosition (Matcher.CompareArraysChangeValue this.FlipPlayer)

        member private this.MatchFirstPosition(pattern:byte[,]) (matchOnPosition:(byte[,] -> Position -> bool)) =
            let patternWidth = pattern.GetLength 1
            let patternHeight = pattern.GetLength 0
            let positions = Array.ofSeq [for yIndex in 0..this.Grid.Height - patternHeight - 1 do for xIndex in 0..this.Grid.Width-patternWidth-1 -> {X = xIndex; Y = yIndex}]
            Array.tryFind(fun pos -> matchOnPosition pattern pos) positions

        member this.MatchFirstExact(pattern:byte[,]) =
            this.MatchFirstPosition pattern this.MatchExact

        member this.MatchFirst(pattern:byte[,]) =
            this.MatchFirstPosition pattern this.Match

        member this.MatchFirstFlipPlayer(pattern:byte[,]) =
            this.MatchFirstPosition pattern this.MatchFlipPlayer

        member private this.MatchFirstPattern (patterns:byte[,] list) (matchFirstPosition:byte[,]->Position option) =
            List.tryFind(fun pattern -> (matchFirstPosition pattern).IsSome) patterns


        