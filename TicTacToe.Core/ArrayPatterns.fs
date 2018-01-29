namespace TicTacToe.Core

module ArrayPatterns =
    let Winning: byte[,] list = [
        Array2D.init 1 5 (fun y x -> 1uy)
        //array2D [|
        //    [|1uy; 1uy; 1uy; 1uy; 1uy|]
        //|];
        Array2D.init 5 1 (fun y x -> 1uy)
        //array2D [|
        //    [|1uy|];
        //    [|1uy|];
        //    [|1uy|];
        //    [|1uy|];
        //    [|1uy|]
        //|];
        Array2D.init 5 5 (fun y x -> if y=x then 1uy else 0uy)
        Array2D.init 5 5 (fun y x -> if y+x = 5-1 then 1uy else 0uy)
        //array2D [|
        //    [|1uy|];
        //    [|0uy;1uy|];
        //    [|0uy;0uy; 1uy|];
        //    [|0uy; 0uy; 0uy; 1uy|];
        //    [|0uy; 0uy; 0uy; 0uy; 1uy|];
        //|];
    ]

