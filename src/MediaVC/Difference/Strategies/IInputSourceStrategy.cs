﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaVC.Difference.Strategies
{
    internal interface IInputSourceStrategy : IEquatable<IInputSourceStrategy>
    {
        long Length { get; }

        long Position { get; set; }
    }
}