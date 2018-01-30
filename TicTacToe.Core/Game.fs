namespace TicTacToe.Core

[<AutoOpen>]
module Matrix =
    type Matcher private () =
        static member CompareArrays(first:byte[,], second:byte[,], ?ignoredValues: byte array) =
            let ignored = defaultArg ignoredValues [||]
            let firstSequence = first |> Seq.cast<byte>
            let secondSequence = second |> Seq.cast<byte>
            Seq.fold2 (fun acc el1 el2 -> acc && (Array.contains el1 ignored || el1 = el2)) true firstSequence secondSequence

        static member MatchArrays(first:byte[,], second:byte[,], ?ignoredValues: byte array) =
            let ignored = defaultArg ignoredValues [||]
            let result = Matcher.CompareArrays(first, second, ignored)

            let width = first.GetLength 1;
            match result with
            | true ->
                first
                |> Seq.cast<byte>
                |> Seq.mapi (fun i el -> (i, el))
                |> Seq.where (fun (_, el) -> not (Array.contains el ignored))
                |> Seq.map (fun (i, _) -> (i % width, i / width))
                |> Array.ofSeq
                |> Some
            | _ -> None

module Basic =
    open System.Runtime.InteropServices

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

        member this.GetSlice(x:int, y:int, width: int, height:int) =
            this.AsByteArray().[y..y + height-1, x..x + width-1]

        member this.IsMatch(pattern: byte[,], position: Position, ignoreEmpty: bool) =
            let ignoredValues = if ignoreEmpty then [|0uy|] else [||]
            let struct (X, Y) = position.Coordinates
            let slice = this.GetSlice(X, Y, pattern.GetLength 1, pattern.GetLength 0)
            Matcher.CompareArrays(pattern, slice, ignoredValues)

        member this.Match(pattern: byte[,], position: Position, ignoreEmpty: bool) =
            let ignoredValues = if ignoreEmpty then [|0uy|] else [||]
            let struct (X, Y) = position.Coordinates
            let slice = this.GetSlice(X, Y, pattern.GetLength 1, pattern.GetLength 0)
            match Matcher.MatchArrays(pattern, slice, ignoredValues) with
            | Some(array) -> array |> Array.map (fun (x, y) -> {X = x + X; Y = y + Y}) |> Some
            | _ -> None

        member this.TryFind(pattern: byte[,], ignoreEmpty: bool) =
            let patternWidth = pattern.GetLength 1
            let patternHeight = pattern.GetLength 0
            let positions = Array.ofSeq [for yIndex in 0..this.Height - patternHeight do for xIndex in 0..this.Width - patternWidth -> {X = xIndex; Y = yIndex}]
            Array.tryFind(fun position -> this.IsMatch(pattern, position, ignoreEmpty)) positions

        member this.TryFindAny(patterns: byte[,] list, ignoreEmpty: bool) =
            List.tryFind(fun pattern -> this.TryFind(pattern, ignoreEmpty).IsSome) patterns

        member this.FindAny(patterns: byte[,] list, ignoreEmpty: bool) =
            match this.TryFindAny(patterns, ignoreEmpty) with
            | Some(pattern) -> 
                match this.TryFind(pattern, ignoreEmpty) with
                | Some(position) -> 
                    match this.Match(pattern, position, ignoreEmpty) with
                    | Some(matches) -> Some(pattern, position, matches)
                    | _ -> None
                | _ -> None
            | _ -> None

    type TurnResult =
        | Winner of player:Player*positions:Position array
        | Draw
        | NextTurn
        member this.TryWinner([<Out>]player:Player byref, [<Out>]positions: Position array byref) =
            match this with
            | Winner(pl, pos) -> player <- pl; positions <- pos; true
            | _ -> false

    
    type Game(width:int, heigth:int) =
        let _playGrid:PlayGrid = new PlayGrid(width, heigth)
        let mutable _turns = 0
        let mutable _currentPlayer = PlayerOne
        let mutable _result = None
        let mutable _possibleMoves = Set.ofSeq [ for yIndex in 0..heigth - 1 do for xIndex in 0..width - 1 -> {X = xIndex; Y = yIndex}]

        let NextPlayer() =
            match _currentPlayer with
            | PlayerOne -> PlayerTwo
            | PlayerTwo -> PlayerOne

        member this.Grid with get() = _playGrid
        member this.Turns with get() = _turns
        member this.CurrentPlayer with get() = _currentPlayer
        member this.PossibleMoves with get() = _possibleMoves

        member private this.GameResultPlayerOne() =
            match this.Grid.FindAny(ArrayPatterns.Winning, true) with
            | Some(_, _, positions) -> Winner(PlayerOne, positions)
            | None -> if _possibleMoves.IsEmpty then Draw else NextTurn 

        member private this.GameResultPlayerTwo() =
            let patternsForSecondPlayer = ArrayPatterns.Winning |> List.map ArrayPatterns.SwitchToSecondPlayer
            match this.Grid.FindAny(patternsForSecondPlayer, true) with
            | Some(_, _, positions) -> Winner(PlayerTwo, positions)
            | None -> if _possibleMoves.IsEmpty then Draw else NextTurn

        member private this.GameResult(lastPlayer:Player) =
            match lastPlayer with
            | PlayerOne -> this.GameResultPlayerOne()
            | PlayerTwo -> this.GameResultPlayerTwo()

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
            let result = this.GameResult(_currentPlayer)
            _currentPlayer <- NextPlayer()
            match result with
            | Winner(_) | Draw -> 
                _possibleMoves <- Set.empty
                _result <- Some(result)
            | _ -> ()
            result

        