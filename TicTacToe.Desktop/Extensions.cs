using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.FSharp.Core;
using TicTacToe.Core;

namespace TicTacToe.Desktop
{
    public static class Extensions
    {
        public static bool IsSome(this FSharpOption<Basic.Position> option)
        {
            return FSharpOption<Basic.Position>.get_IsSome(option);
        }

        public static bool IsNone(this FSharpOption<Basic.Position> option)
        {
            return FSharpOption<Basic.Position>.get_IsNone(option);
        }
    }
}
